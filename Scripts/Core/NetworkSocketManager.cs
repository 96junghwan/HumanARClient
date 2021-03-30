using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Linq;
using OpenCVForUnity;


namespace CellBig.Module.HumanDetection
{
    // Send Thread에서 바로 전송할 수 있도록 되어있는 구조체
    public struct SendData
    {
        public byte[] sendByte;
        public int sendByteCount;

        public SendData(byte[] sendByte, int sendByteCount)
        {
            this.sendByte = sendByte;
            this.sendByteCount = sendByteCount;
            
            //this.sendByte = new byte[NetworkInfo.NetworkBufferSize];
            //Buffer.BlockCopy(sendByte, 0, this.sendByte, 0, this.sendByteCount);
        }
    }

    // Packet 해독 결과 저장해서 넘겨주는 용도의 구조체 : 연산 결과 이외의 기타 패킷
    public struct PacketDecodeResult
    {
        public MSGType msgType;
        public ushort msg;
        public ushort msg2;

        public PacketDecodeResult(MSGType msgType, ushort msg, ushort msg2)
        {
            this.msgType = msgType;
            this.msg = msg;
            this.msg2 = msg2;
        }
    }




    // 휴먼포즈 검출해주는 딥러닝 서버와 연결하고, 캡처한 이미지를 송신하고 처리결과인 관절 좌표를 수신하는 동작을 주로 하는 클래스
    public class NetworkSocketManager : MonoBehaviour
    {
        // 옵션 모델 변수
        private CoreModuleStatusModel coreModuleStatusModel;
        private CameraOptionModel cameraOptionModel;
        private NetworkOptionModel networkOptionModel;
        private BufferOptionModel bufferOptionModel;
        private PreProcessOptionModel preProcessOptionModel;
        private PostProcessOptionModel postProcessOptionModel;
        private NetworkControlOptionModel networkControlOptionModel;

        // 네트워크 변수
        private Socket server;
        private PacketManager packetManager;
        public Queue<PacketDecodeResult> packetDecodeResultQ;

        // SendRate 적용하기 위한 변수
        private int frameCount = 1;

        // resize용 변수 : 이거 지울 예정
        private Texture2D resizedTexture;   // 리사이즈된 Texture 저장할 변수
        private Mat resizingMat;            // 리사이즈할 Mat 저장할 변수
        private Mat resizedMat;             // 리사이즈된 Mat 저장할 변수

        // Send용 변수
        private bool sendPause = false;
        private Thread sendThread;
        private Queue<SendData> sendQ;
        private Texture2D tempTexture;

        // Recv용 변수
        private byte[] asyncRecvBuffer;
        private byte[] recvHeaderBuffer;

        // 결과 데이터 전달 메세지 큐
        private Queue<DetectHumanJointResultMsg> humanPoseMsgQ;
        private Queue<DetectHumanMaskResultMsg> humanSegMsgQ;

        // 네트워크 제어 Coroutine용 변수
        private IEnumerator networkCheckCoroutine;
        private bool onNetworkCheckCoroutine = false;

        // 네트워크 제어 판단 근거 변수
        private int invalidDataFeedbackCount;
        private int serverSlowFeedbackCount;
        private int positiveNetworkFeedbackCount;
        private int checkCount;

        // NetworkSocketManager의 모듈 상태를 전달할 메세지 큐
        private Queue<CoreModuleStatusReportMsg> coreModuleStatusReportMsgQ;




        // 코어 모듈 동작 제어 메세지 수신하는 함수
        private void OnInternalModuleContorl(InternalModuleContorl msg)
        {
            // 메세지에 해당 안되면 무시
            if ((msg.coreModuleIndex & (int)CoreModuleIndex.NetworkSocket) != (int)CoreModuleIndex.NetworkSocket) { return; }

            // 지정된 동작 수행
            switch (msg.coreModuleOperationIndex)
            {
                // 시작
                case CoreModuleOperationIndex.Init:
                    ModuleInit();
                    break;

                // 중지
                case CoreModuleOperationIndex.Pause:
                    ModulePause();
                    break;

                // 재개
                case CoreModuleOperationIndex.Play:
                    ModulePlay();
                    break;

                // 종료
                case CoreModuleOperationIndex.Stop:
                    ModuleStop();
                    break;

                // 재시작
                case CoreModuleOperationIndex.ReBoot:
                    ModuleStop();
                    ModuleInit();
                    break;

                // 에러
                default:
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                        CoreModuleReportType.Error,
                        (int)CoreModuleReportErrorCode.Network_Etc,
                        "[사전에 정의된 모듈 제어 동작이 아닙니다]",
                        "at OnCoreModuleControlMsg() of NetworkSocketManager.cs"));
                    break;
            }
        }

        // 자원 할당 함수
        private void ModuleInit()
        {
            LoadOptionModel();

            if (coreModuleStatusModel.networkStatus == CoreModuleStatus.NotReady)
            {
                AllocateVariables();
                coreModuleStatusModel.networkStatus = CoreModuleStatus.Ready;
            }
        }

        // 기능 시작 함수
        private void ModulePlay()
        {
            if (coreModuleStatusModel.networkStatus == CoreModuleStatus.Ready)
            {
                TryConnect();
            }

            if (coreModuleStatusModel.networkStatus == CoreModuleStatus.Pause)
            {
                coreModuleStatusModel.networkStatus = CoreModuleStatus.Playing;
            }
        }

        // 기능 일시 정지 함수
        private void ModulePause()
        {
            if (coreModuleStatusModel.networkStatus == CoreModuleStatus.Playing)
            {
                coreModuleStatusModel.networkStatus = CoreModuleStatus.Pause;
            }
        }

        // 기능 종료 및 자원 해제 함수
        private void ModuleStop()
        {
            if (coreModuleStatusModel.networkStatus > CoreModuleStatus.NotReady)
            {
                Disconnect();
                ReleaseVariables();
            }

            coreModuleStatusModel.networkStatus = CoreModuleStatus.NotReady;
        }




        // 캡쳐된 프레임 받아서 설정한 SendRate(전송률)에 맞춰서 송신 쓰레드 작업 큐에 입력하는 함수
        private void OnCapturedFrameMsg(CapturedFrameMsg msg)
        {
            // Send 중지 상태일 경우
            if (coreModuleStatusModel.networkStatus == CoreModuleStatus.Pause || sendPause) { return; }

            // 신경망 선택 안했을 경우 : 서버에 전송 안함
            if (networkOptionModel.nnType_RT == (int)NNType.None) { return; }

            // 전송률 미달 : 이번 프레임은 무시
            if (frameCount < networkOptionModel.sendRate_RT) { frameCount++; return; }

            // SendingQ에 데이터 쌓임 방지
            if (sendQ.Count > networkOptionModel.sendingQSizeMax) { return; }

            // 전송률 충족 : 전처리 후 Socket SendQ에 입력
            if (frameCount >= networkOptionModel.sendRate_RT)
            {
                // OCam인 경우
                if (cameraOptionModel.cameraType == CameraType.OCam)
                {
                    tempTexture.LoadRawTextureData(msg.imageByte);
                    tempTexture.Apply();

                    // resize 사용할 경우
                    if (preProcessOptionModel.useResize_RT)
                    {
                        packetManager.PutImage(msg.frameID, networkOptionModel.nnType_RT, 
                            ImagePreProcessor.ResizeImage480P(tempTexture, resizedTexture, resizingMat, resizedMat));
                    }

                    else
                    {
                        packetManager.PutImage(msg.frameID, networkOptionModel.nnType_RT, tempTexture.EncodeToJPG());
                    }
                }

                // OCam 아닌 경우
                else
                {
                    // 이미지 로드
                    tempTexture.LoadImage(msg.imageByte);
                    tempTexture.Apply();

                    // resize 사용할 경우
                    if (preProcessOptionModel.useResize_RT)
                    {
                        packetManager.PutImage(msg.frameID, networkOptionModel.nnType_RT,
                            ImagePreProcessor.ResizeImage480P(tempTexture, resizedTexture, resizingMat, resizedMat));
                    }

                    else
                    {
                        packetManager.PutImage(msg.frameID, networkOptionModel.nnType_RT, tempTexture.EncodeToJPG());
                    }
                }

                // 전송 카운트 초기화
                frameCount = 1;
            }
        }

        // 프레임 서버 연산 개별 요청 받는 함수
        private void OnPrivateFrameRequestMsg(PrivateFrameRequestMsg msg)
        {
            if (msg.frameID >= 0)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Network_Etc,
                            "[컨텐츠 용 별도 프레임 연산 요청은 frameID를 음수로 설정해주세욘]",
                            "at OnCoreModuleControlMsg() of NetworkSocketManager.cs"));
            }

            else
            {
                packetManager.PutImage(msg.frameID, msg.nnType, msg.jpgByte);
            }
        }

        // 네트워크 컴플레인 메세지 수신 함수
        private void OnNetworkFeedbackMsg(NetworkFeedbackMsg msg)
        {
            switch (msg.networkFeedbackType)
            {
                case NetworkFeedbackType.InvalidData:
                    invalidDataFeedbackCount++;
                    break;

                case NetworkFeedbackType.ServerResponseSlow:
                    serverSlowFeedbackCount++;
                    break;

                default:
                    break;
            }
        }




        // 서버와 연결 시도하는 함수
        private void TryConnect()
        {
            try
            {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(networkOptionModel.serverIp), networkOptionModel.serverPort);
                server.BeginConnect(ipep, new AsyncCallback(TryConnect_Callback), null);
            }
            catch (Exception e)
            {
                server = null;
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Network_NotOpenedNN,
                            "[딥러닝 서버가 열려있지 않습니다]",
                            "at TryConnect() of NetworkSocketManager.cs"));
            }
        }

        // 비동기 Connect 함수
        private void TryConnect_Callback(IAsyncResult ar)
        {
            try
            {
                server.EndConnect(ar);

                // 접속 성공
                if (server.Connected)
                {
                    // Send Thread, 비동기 Receive 시작
                    sendThread = new Thread(SendThread);
                    sendThread.Start();
                    server.BeginReceive(asyncRecvBuffer, 0, NetworkInfo.NetworkbufferMax, SocketFlags.None, new AsyncCallback(OnRecvCallback), null);  // 테스트
                    SendAccessCode(networkOptionModel.serverAccessKeyCode);

                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Normal,
                            (int)CoreModuleReportNormalCode.Network_Etc,
                            "[소켓 연결 성공. 딥러닝 연산 서비스 접속 시도 중...]",
                            "at TryConnect_Callback() of NetworkSocketManager.cs"));
                }

                // 접속 실패 : 접속 재시도
                else
                {
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Network_NotOpenedServerSocket,
                            "[소켓 연결 에러. 네트워크에 연걸되어있지 않거나, 선택하신 서버가 켜져 있지 않습니다. 연결 재시도 중...]",
                            "at TryConnect_Callback() of NetworkSocketManager.cs"));
                    //TryConnect();
                }
            }

            // 소켓 에러 : 접속 재시도 X
            catch (Exception e)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Network_NotOpenedServerSocket,
                            "[소켓 연결 에러. 네트워크에 연걸되어있지 않거나, 선택하신 서버가 켜져 있지 않습니다]",
                            "at TryConnect_Callback() of NetworkSocketManager.cs"));
                //TryConnect();
            }
        }

        // 서버에 프레임 송신하는 쓰레드
        private void SendThread()
        {
            int total;
            int count;
            int dataleft;

            while (coreModuleStatusModel.networkStatus >= CoreModuleStatus.Ready)
            {
                Thread.Sleep(1);

                if (sendQ.Count == 0) { continue; }

                // sendQ에서 데이터 꺼내고 변수 초기화
                var sendData = sendQ.Dequeue();
                dataleft = sendData.sendByteCount;
                //Debug.Log("Send Data : " + sendData.sendByteCount);
                total = 0;

                // Send 중지 혹은 socket 중지 상태일 경우
                if (sendPause || coreModuleStatusModel.networkStatus == CoreModuleStatus.Pause) { continue; }

                // 송신 버퍼가 null인 경우
                if (sendData.sendByte == null)
                {
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Network_Etc,
                            "[Socket send buffer is null]",
                            "at SendThread() of NetworkSocketManager.cs"));
                    continue;
                }

                // 메세지 송신
                try
                {
                    count = server.Send(sendData.sendByte, total, dataleft, SocketFlags.None);
                }

                // 메세지 송신 불가능한 경우 : 서버의 천재지변 / 서버가 말도 없이 클라를 토사구팽한 경우
                catch (SocketException e)
                {
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Warning,
                            (int)CoreModuleReportWarningCode.Network_Etc,
                            "[서버와 연결이 종료되었습니다. Send Thread 종료]",
                            "at SendThread() of NetworkSocketManager.cs"));
                    if (onNetworkCheckCoroutine) { onNetworkCheckCoroutine = false; }
                    break;
                }
            }
        }

        // 서버로부터 연산 결과인 관절 float 좌표 리스트를 수신하는 함수 : 비동기(쓰레드)여서, 직접 Message 보내지 말고 MessageQ에 넣어 Update(메인 쓰레드)에서 보냄
        private void OnRecvCallback(IAsyncResult ar)
        {
            int asyncRecvSize = 0;
            int bufferOffset = 0;

            // 서버에서 Packet 수신
            try { asyncRecvSize = server.EndReceive(ar); }
            catch (SocketException e)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Warning,
                            (int)CoreModuleReportWarningCode.Network_Etc,
                            "[서버와 연결이 끊겼습니다. Async Recv 종료]",
                            "at OnRecvCallback() of NetworkSocketManager.cs"));
                if (onNetworkCheckCoroutine) { onNetworkCheckCoroutine = false; }
                return;
            }

            // socket 중지 상태일 경우 : PacketManager의 TaskThread에도 넘기지 않고 패스함
            if (coreModuleStatusModel.networkStatus == CoreModuleStatus.Pause)
            {
                asyncRecvSize = 0;
            }

            // 받은 바이트 전부 까볼 때 까지 반복
            while (bufferOffset < asyncRecvSize)
            {
                // Packet Header 까기
                Buffer.BlockCopy(asyncRecvBuffer, bufferOffset, recvHeaderBuffer, 0, NetworkInfo.HeaderSize);            // 테스트
                PacketHeader header = PacketConverter.Bytes2PacketStruct<PacketHeader>(recvHeaderBuffer);
                bufferOffset += NetworkInfo.HeaderSize;

                /*
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Normal,
                            (int)CoreModuleReportNormalCode.Network_Etc,
                            "[msgType : " + header.msgType + "]",
                            "at OnRecvCallback() of NetworkSocketManager.cs"));
                */


                // Packet Struct, Packet Data 복사
                byte[] packetByte = new byte[header.packetStructSize + header.packetDataSize];
                Buffer.BlockCopy(asyncRecvBuffer, bufferOffset, packetByte, 0, (header.packetStructSize + header.packetDataSize));       // 테스트

                // PacketManager에 전달 : 쓰레드가 처리
                packetManager.PutPacket(header, packetByte);
                bufferOffset += (header.packetStructSize + header.packetDataSize);
            }

            // 다시 헤더 받기
            server.BeginReceive(asyncRecvBuffer, 0, NetworkInfo.NetworkbufferMax, SocketFlags.None, new AsyncCallback(OnRecvCallback), null);
        }




        // 옵션 모델 로드 함수
        private void LoadOptionModel()
        {
            // 필요한 옵션 모델 로드
            cameraOptionModel = Model.First<CameraOptionModel>();
            networkOptionModel = Model.First<NetworkOptionModel>();
            bufferOptionModel = Model.First<BufferOptionModel>();
            preProcessOptionModel = Model.First<PreProcessOptionModel>();
            postProcessOptionModel = Model.First<PostProcessOptionModel>();
            networkControlOptionModel = Model.First<NetworkControlOptionModel>();
            coreModuleStatusModel = Model.First<CoreModuleStatusModel>();

            // 옵션 모델 로드 실패
            if (networkOptionModel == null || bufferOptionModel == null || cameraOptionModel == null || preProcessOptionModel == null || postProcessOptionModel == null || networkControlOptionModel == null)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Network_Etc,
                            "[옵션 모델 로드에 실패했습니다]",
                            "at LoadOptionModel() of NetworkSocketManager.cs"));
            }
        }

        // 변수 할당 함수
        private void AllocateVariables()
        {
            onNetworkCheckCoroutine = false;
            invalidDataFeedbackCount = 0;
            serverSlowFeedbackCount = 0;
            positiveNetworkFeedbackCount = 0;

            // 합병된 결과 데이터를 메세지로 전송하기 위한 메세지 큐 초기 할당
            humanPoseMsgQ = new Queue<DetectHumanJointResultMsg>();
            humanSegMsgQ = new Queue<DetectHumanMaskResultMsg>();

            // 패킷 매니저, SendQ 할당
            sendQ = new Queue<SendData>();
            packetDecodeResultQ = new Queue<PacketDecodeResult>();
            packetManager = new PacketManager(sendQ, humanPoseMsgQ, humanSegMsgQ, packetDecodeResultQ);

            // recv용 임시 버퍼 변수
            asyncRecvBuffer = new byte[NetworkInfo.NetworkbufferMax];
            recvHeaderBuffer = new byte[NetworkInfo.HeaderSize];

            // 이미지 전처리 Resize용 변수 초기 할당 : 없앨 예정
            resizingMat = new Mat(cameraOptionModel.camHeight, cameraOptionModel.camWidth, CvType.CV_8UC3);

            resizedTexture = new Texture2D(640, 480, TextureFormat.RGB24, false);
            resizedMat = new Mat(480, 640, CvType.CV_8UC3);

            // 결과 데이터 수신용 리스트 초기 할당
            tempTexture = new Texture2D(cameraOptionModel.camWidth, cameraOptionModel.camHeight, TextureFormat.RGB24, false);
        }

        // 변수 해제 함수
        private void ReleaseVariables()
        {
            // PacketManager 인스턴스 해제
            packetManager.Destroy();

            // Mat
            if (!resizingMat.IsDisposed) { resizingMat.Dispose(); resizingMat = null; }
            if (!resizedMat.IsDisposed) { resizedMat.Dispose(); resizedMat = null; }

            // Texture2D
            if (tempTexture != null) { tempTexture = null; }
            if (resizedTexture != null) { resizedTexture = null; }

            // Queue
            if (humanPoseMsgQ != null) { humanPoseMsgQ.Clear(); humanPoseMsgQ = null; }
            if (humanSegMsgQ != null) { humanSegMsgQ.Clear(); humanSegMsgQ = null; }
            if (packetDecodeResultQ != null) { packetDecodeResultQ.Clear(); packetDecodeResultQ = null; }
        }

        // 딥러닝 서비스 시작할 수 있도록 메세지 리스너들 부착
        private void StartNetwork()
        {
            // Main Update 활성화
            coreModuleStatusModel.networkStatus = CoreModuleStatus.Playing;

            // 옵션에 따라 네트워크 제어 코루틴 시작
            InitNetworkResourceControlCoroutine();

            // 메세지 리스너 추가
            Message.AddListener<CapturedFrameMsg>(OnCapturedFrameMsg);
            Message.AddListener<PrivateFrameRequestMsg>(OnPrivateFrameRequestMsg);
            Message.AddListener<NetworkFeedbackMsg>(OnNetworkFeedbackMsg);

            coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Normal,
                            (int)CoreModuleReportNormalCode.Network_Etc,
                            "[딥러닝 연산 서비스 접속 성공]",
                            "at StartNetwork() of NetworkSocketManager.cs"));
        }

        // 서버 딥러닝 서비스 접속 요청 패킷 생성 후 전송
        private void SendAccessCode(string accessCode)
        {
            RequestAccessPacketStruct packetStruct = new RequestAccessPacketStruct(accessCode);
            byte[] packetStructByte = PacketConverter.PacketStruct2Bytes<RequestAccessPacketStruct>(packetStruct);
            packetManager.PutCommonPacket(MSGType.Request_Access, packetStructByte);
        }

        // 네트워크 기능 종료하는 함수, 종료해도 TryConnect()로 다시 연결 가능, 자원은 해제 안함
        private void Disconnect()
        {
            // 메세지 리스너 삭제
            Message.RemoveListener<CapturedFrameMsg>(OnCapturedFrameMsg);
            Message.RemoveListener<PrivateFrameRequestMsg>(OnPrivateFrameRequestMsg);
            Message.RemoveListener<NetworkFeedbackMsg>(OnNetworkFeedbackMsg);

            // 서버 살아있으면 Notify 메세지 직접 송신
            if (server.Connected)
            {
                NotifyPacketStruct packetStruct = new NotifyPacketStruct();
                packetStruct.notifyType = (ushort)NotifyType.Client_Close;
                byte[] packetStructByte = PacketConverter.PacketStruct2Bytes<NotifyPacketStruct>(packetStruct);

                int packetStructSize = packetStructByte.Length;
                byte[] sendHeaderByte = PacketConverter.PacketStruct2Bytes<PacketHeader>(new PacketHeader(MSGType.Notify, packetStructSize, 0));
                byte[] packetByte = new byte[NetworkInfo.HeaderSize + packetStructSize];

                Buffer.BlockCopy(sendHeaderByte, 0, packetByte, 0, NetworkInfo.HeaderSize);
                Buffer.BlockCopy(packetStructByte, 0, packetByte, NetworkInfo.HeaderSize, packetStructSize);

                server.Send(packetByte, 0, NetworkInfo.HeaderSize + packetStructSize, SocketFlags.None);
            }

            // 메인 Update 루프 중단
            coreModuleStatusModel.networkStatus = CoreModuleStatus.Ready;

            // 네트워크 제어 코루틴 해제
            onNetworkCheckCoroutine = false;
            if (networkControlOptionModel.useNetworkResourceContoller) { StopCoroutine(networkCheckCoroutine); }

            // 서버 소켓 해제
            if (server != null)
            {
                if (server.Connected)
                {
                    server.Close();
                }
                server = null;
            }

            // 송신 쓰레드 종료
            if (sendThread != null)
            {
                if (sendThread.IsAlive)
                {
                    sendThread.Abort();
                    sendThread = null;
                }
            }

            coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Normal,
                            (int)CoreModuleReportNormalCode.Network_Etc,
                            "[딥러닝 서버 연결 종료]",
                            "at Disconnect() of NetworkSocketManager.cs"));
        }

        // Packet 해독 결과 반응하는 함수
        private void PacketReact(PacketDecodeResult result)
        {
            switch (result.msgType)
            {
                case MSGType.Warning:
                    break;

                case MSGType.Error:
                    if (result.msg == (ushort)ErrorType.UnOpen_NN)
                    {
                        coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Error,
                                (int)CoreModuleReportErrorCode.Network_NotOpenedNN,
                            "[요청하신 딥러닝 연산을 수행할 신경망이 현재 접속한 서버에 열려있지 않습니다]",
                            "at PacketReact() of NetworkSocketManager.cs"));
                    }
                    break;

                case MSGType.Notify:
                    // 서버 측에서 연결 해제 통지
                    if (result.msg == (ushort)NotifyType.Server_Close)
                    {
                        if (coreModuleStatusModel.networkStatus > CoreModuleStatus.Ready) 
                        { 
                            //TryConnect(); 
                        }
                    }
                    break;

                case MSGType.Response_Access:
                    if (result.msg == (ushort)Access_Result.Accept) { StartNetwork(); }
                    else
                    {
                        coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Error,
                                (int)CoreModuleReportErrorCode.Network_InvalidAccessCode,
                            "[딥러닝 서비스 접속 실패. 접속 코드를 확인해주세요]",
                            "at PacketReact() of NetworkSocketManager.cs"));
                    }
                    break;

                // 서버 상태 요청 결과
                case MSGType.Response_ServerStatus:
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Normal,
                                (int)CoreModuleReportNormalCode.Network_Etc,
                            "[서버 상태 수신. 현재 접속 클라이언트 수 : " + result.msg + "]",
                            "at PacketReact() of NetworkSocketManager.cs"));
                    break;

                default:
                    break;
            }
        }




        // 네트워크 상태 주기적으로 검사해서 네트워크 설정을 제어하는 코루틴 
        private IEnumerator NetworkResourceController()
        {
            // 검사 항목 초기화하는 내부 함수
            void ResetCount()
            {
                checkCount = 0;
                invalidDataFeedbackCount = 0;
                serverSlowFeedbackCount = 0;
                positiveNetworkFeedbackCount = 0;
            }

            // 전송률 최대 값 지정
            const int sendRateMin = 1;
            const int sendRateMax = 5;

            // 주기적 리셋을 위한 변수
            const int checkMax = 50;

            // 외부에서 멈추지 않는 한 계속 수행함 : 현재 Disconnect()에서 멈추고있음
            while (onNetworkCheckCoroutine)
            {
                // 옵션 모델에 설정된 시간 마다 검사하도록 수행
                yield return new WaitForSecondsRealtime(networkControlOptionModel.networkCheckCycleTime);

                // socketPause 상태에서는 검사 중지
                if (coreModuleStatusModel.networkStatus == CoreModuleStatus.Pause) { continue; }

                // 신경망 선택 안했을 때 검사 중지
                if (networkOptionModel.nnType_RT == (int)NNType.None) { continue; }

                // 네트워크 검사 후 네트워크 제어 변수 조정
                // 검사 항목 : socketPauseCount, emptyDataPlayedCount
                // 제어 항목 : networkOptionModel.SendRate, preProcessOptionModel.UseResize

                // 주기적으로 리셋 : 안하면 SendRate 바닥치고 상태 나아졌을 때, SendRate 개선이 안됨
                if (++checkCount >= checkMax)
                {
                    ResetCount();
                }

                // 네트워크 상태 검사
                bool invalidDataCheck = invalidDataFeedbackCount > networkControlOptionModel.invalidDataFeedbackMax;
                bool serverSlowCheck = serverSlowFeedbackCount > networkControlOptionModel.serverSlowFeedbackMax;
                bool positiveNetworkCheck = (invalidDataFeedbackCount == 0) && (serverSlowFeedbackCount == 0);

                // 네트워크 상태 : Positive
                // 지정 횟수 만큼의 체크를 수행하는 동안 Send 중지나 빈 데이터 재생이 없었던 경우 : SendRate 낮춰서 더 많은 데이터 보내도록 하기
                if (positiveNetworkCheck)
                {
                    if (++positiveNetworkFeedbackCount >= networkControlOptionModel.positiveNetworkFeedbackMax)
                    {
                        if (networkOptionModel.sendRate_RT > sendRateMin)
                        {
                            coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Normal,
                                (int)CoreModuleReportNormalCode.Network_SendRateDown,
                                "[Send Rate 조정 : " + networkOptionModel.sendRate_RT + " -> " + (networkOptionModel.sendRate_RT - 1) + "]",
                                "at NetworkResourceController() of NetworkSocketManager.cs"));

                            networkOptionModel.sendRate_RT--;
                            ResetCount();
                        }
                    }
                }

                // 네트워크 상태 : Nagative
                // 1. 버퍼에서 빈 데이터 받은 횟수가 임계값 이상이면 변수 조정
                // 2. Send 중지 횟수가 임계값 이상이면 변수 조정
                if (invalidDataCheck || serverSlowCheck)
                {
                    // 전처리 옵션 제어 : UseResize 옵션 켜기, 문제 있으면 기능 뺄 예정
                    if (!preProcessOptionModel.useResize_RT && !preProcessOptionModel.resizeLock)
                    {
                        // 긴급 조치
                        sendPause = true;
                        sendQ.Clear();
                        yield return new WaitForSecondsRealtime(networkControlOptionModel.sendPauseTime);
                        sendPause = false;

                        coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Normal,
                                (int)CoreModuleReportNormalCode.Network_Etc,
                                "[Automatic Resize Option On]",
                                "at NetworkResourceController() of NetworkSocketManager.cs"));

                        preProcessOptionModel.useResize_RT = true;
                        ResetCount();
                    }

                    // 네트워크 제어 : SendRate 증가 가능한 경우 증가
                    else if (networkOptionModel.sendRate_RT < sendRateMax)
                    {
                        // 긴급 조치
                        sendPause = true;
                        sendQ.Clear();
                        yield return new WaitForSecondsRealtime(networkControlOptionModel.sendPauseTime);
                        sendPause = false;

                        coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Normal,
                                (int)CoreModuleReportNormalCode.Network_SendRateUp,
                                "[Send Rate 조정 : " + networkOptionModel.sendRate_RT + " -> " + (networkOptionModel.sendRate_RT + 1) + "]",
                                "at NetworkResourceController() of NetworkSocketManager.cs"));

                        networkOptionModel.sendRate_RT++;
                        ResetCount();
                    }

                    // 네트워크 제어 불가 : 더 이상 조정할 것이 없는 경우
                    else if (networkOptionModel.sendRate_RT >= sendRateMax)
                    {
                        // 긴급 조치
                        sendPause = true;
                        sendQ.Clear();
                        yield return new WaitForSecondsRealtime(networkControlOptionModel.sendPauseTime);
                        sendPause = false;

                        coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Warning,
                                (int)CoreModuleReportWarningCode.Network_SendRateMax,
                                "[서버 연산 속도가 느립니다. 연산 서버를 변경하거나, DelaySeconds 등의 옵션 조정을 권장합니다.]",
                                "at NetworkResourceController() of NetworkSocketManager.cs"));

                        ResetCount();
                    }
                }

            }
        }

        // 네트워크 제어 코루틴 시작하는 함수
        private void InitNetworkResourceControlCoroutine()
        {
            // 네트워크 제어 코루틴 세팅
            networkCheckCoroutine = NetworkResourceController();

            // 네트워크 제어 코루틴 사용할 시 : 코루틴 시작
            if (networkControlOptionModel.useNetworkResourceContoller && !bufferOptionModel.noDelay)
            {
                onNetworkCheckCoroutine = true;
                StartCoroutine(networkCheckCoroutine);
            }
        }




        private void Awake()
        {
            coreModuleStatusReportMsgQ = new Queue<CoreModuleStatusReportMsg>();
            Message.AddListener<InternalModuleContorl>(OnInternalModuleContorl);
        }

        private void OnDestroy()
        {
            if (coreModuleStatusModel != null)
            {
                ModuleStop();
                coreModuleStatusModel.networkStatus = CoreModuleStatus.None;
            }

            if (coreModuleStatusReportMsgQ != null) { coreModuleStatusReportMsgQ.Clear(); coreModuleStatusReportMsgQ = null; }
            Message.RemoveListener<InternalModuleContorl>(OnInternalModuleContorl);
        }

        private void Update()
        {
            // 모듈 상태 메세지 전송
            if (coreModuleStatusReportMsgQ.Count != 0)
            {
                Message.Send<CoreModuleStatusReportMsg>(coreModuleStatusReportMsgQ.Dequeue());
            }

            // 모델 로드 검사
            if (coreModuleStatusModel == null) { return; }

            // 모델 로드 안된 상태면 리턴
            if (coreModuleStatusModel.networkStatus < CoreModuleStatus.Ready) { return; }

            // Packet 해독 결과 처리
            if (packetDecodeResultQ != null)
            {
                for (int i = 0; i < packetDecodeResultQ.Count; i++)
                {
                    PacketReact(packetDecodeResultQ.Dequeue());
                }
            }

            // MaskQ에 있는 결과 마스크 메세지들 전부 전송
            if (humanSegMsgQ != null)
            {
                for (int i = 0; i < humanSegMsgQ.Count; i++)
                {
                    Message.Send<DetectHumanMaskResultMsg>(humanSegMsgQ.Dequeue());
                }
            }

            // PoseQ에 있는 결과 관절 메세지들 전부 전송
            if (humanPoseMsgQ != null)
            {
                for (int i = 0; i < humanPoseMsgQ.Count; i++)
                {
                    Message.Send<DetectHumanJointResultMsg>(humanPoseMsgQ.Dequeue());
                }
            }
        }
    }
}