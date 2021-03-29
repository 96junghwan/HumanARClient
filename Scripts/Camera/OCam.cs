using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading;
using OpenCVForUnity;

namespace CellBig.Module.HumanDetection
{
    public class OCam : MonoBehaviour
    {
        // OCam DLL 함수 Import
        private static class Dll
        {
            [DllImport("oCam")]
            public static extern bool Connect(int width, int height, double fps);
            [DllImport("oCam")]
            public static extern bool Disconnect();
            [DllImport("oCam")]
            public static extern bool Play();
            [DllImport("oCam")]
            public static extern bool Stop();
            [DllImport("oCam")]
            public static extern bool CopyBuffer(byte[] dst);
            [DllImport("oCam")]
            public static extern bool IsConnected();
            [DllImport("oCam")]
            public static extern bool IsPlaying();
            [DllImport("oCam")]
            public static extern bool GetExposure(out long value);
            [DllImport("oCam")]
            public static extern bool SetExposure(long value);
            [DllImport("oCam")]
            public static extern bool GetGain(out long value);
            [DllImport("oCam")]
            public static extern bool SetGain(long value);
            
        }

        // 옵션 모델 변수
        private CameraOptionModel cameraOptionModel;
        private PreProcessOptionModel preProcessOptionModel;
        private OCamOptionModel oCamOptionModel;
        private CoreModuleStatusModel coreModuleStatusModel;
        private bool onLoading = false;

        // 현재 OCam의 적용되고 있는 값들
        private long currExposure;
        private long currGain;

        // OCam에 적용 요청하는 값들 : 현재는 Unity Inspector 창에서 조절
        public long requestExposure;
        public long requestGain;

        // 프레임 저장용 변수
        private int frameID = 1;
        private int captureFrameByteSize;
        //private byte[] captureByte;
        private byte[] taskByte;    // 전처리용 바이트 배열
        private MatOfByte mob;
        private Mat captureMat;

        // 쓰레드용 변수
        private int mainQCount; // 메인 쪽 메세지 큐 개수 저장용 변수
        static readonly object camLock = new object();
        private Thread captureThread;
        private Queue<CapturedFrameMsg> captureFrameMsgQ;

        // 모듈 상태를 전달할 메세지 큐
        private Queue<CoreModuleStatusReportMsg> coreModuleStatusReportMsgQ;




        // 코어 모듈 동작 제어 메세지 수신하는 함수
        private void OnInternalModuleContorl(InternalModuleContorl msg)
        {
            // 메세지에 해당 안되면 무시
            if ((msg.coreModuleIndex & (int)CoreModuleIndex.Camera) != (int)CoreModuleIndex.Camera) { return; }

            // 지정된 동작 수행
            switch (msg.coreModuleOperationIndex)
            {
                // Init
                case CoreModuleOperationIndex.Init:
                    ModuleInit();
                    break;

                // 시작
                case CoreModuleOperationIndex.Play:
                    ModulePlay();
                    break;

                // 일시중지
                case CoreModuleOperationIndex.Pause:
                    ModulePause();
                    break;

                // 종료
                case CoreModuleOperationIndex.Stop:
                    ModuleStop();
                    break;

                // 재부팅
                case CoreModuleOperationIndex.ReBoot:
                    ModuleStop();
                    ModuleInit();
                    break;

                // 에러
                default:
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                        CoreModuleReportType.Error,
                        (int)CoreModuleReportErrorCode.Camera_Etc,
                        "[사전에 정의된 모듈 제어 동작이 아닙니다]",
                        "at OnCoreModuleControlMsg() of VideoLoader.cs"));
                    break;
            }
        }

        // 자원 할당 함수
        private void ModuleInit()
        {
            LoadOptionModel();

            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.NotReady)
            {
                AllocateVariables();
                CameraInit();
            }
        }

        // 기능 시작 함수
        private void ModulePlay()
        {
            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Ready)
            {
                captureThread.Start();
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
            }

            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Pause)
            {
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
            }
        }

        // 기능 일시 정지 함수
        private void ModulePause()
        {
            if (coreModuleStatusModel.cameraStatus > CoreModuleStatus.Playing)
            {
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Pause;
            }
        }

        // 기능 종료 및 자원 해제 함수
        private void ModuleStop()
        {
            if (coreModuleStatusModel.cameraStatus > CoreModuleStatus.NotReady)
            {
                ReleaseVariables();
            }

            coreModuleStatusModel.cameraStatus = CoreModuleStatus.NotReady;
        }




        // 변수 할당 함수
        private void AllocateVariables()
        {
            captureFrameByteSize = cameraOptionModel.camWidth * cameraOptionModel.camHeight * 3;
            //captureByte = new byte[captureFrameByteSize];
            taskByte = new byte[captureFrameByteSize];
            mob = new MatOfByte();
            captureMat = new Mat(cameraOptionModel.camHeight, cameraOptionModel.camWidth, CvType.CV_8UC3);
            captureFrameMsgQ = new Queue<CapturedFrameMsg>();
            captureThread = new Thread(CaptureThread);
        }

        // 변수, 기능 해제 함수
        private void ReleaseVariables()
        {
            coreModuleStatusModel.cameraStatus = CoreModuleStatus.Ready;
            if (mob != null) { mob.Dispose(); mob = null; }
            if (captureMat != null) { captureMat.Dispose(); captureMat = null; }

            // 쓰레드 종료 대기
            Thread.Sleep(500);

            // 쓰레드 종료
            if (captureThread != null)
            {
                if (captureThread.IsAlive)
                {
                    captureThread.Abort();
                    captureThread = null;
                }
            }

            // 카메라 종료
            if (!Dll.Disconnect())
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_Etc,
                           "[카메라 연결 해제 실패]",
                           "at ReleaseVariables() of OCam.cs"));
            }

            if (captureFrameMsgQ != null) { captureFrameMsgQ.Clear(); captureFrameMsgQ = null; }
        }

        // 옵션 모델 로드하는 함수
        private void LoadOptionModel()
        {
            cameraOptionModel = Model.First<CameraOptionModel>();
            preProcessOptionModel = Model.First<PreProcessOptionModel>();
            oCamOptionModel = Model.First<OCamOptionModel>();
            coreModuleStatusModel = Model.First<CoreModuleStatusModel>();

            // 로드 실패
            if (cameraOptionModel == null || preProcessOptionModel == null || oCamOptionModel == null)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                    CoreModuleReportType.Error,
                    (int)CoreModuleReportErrorCode.Camera_Etc,
                    "[옵션 모델 로드에 실패했습니다]",
                    "at LoadOptionModel() of OCam.cs"));
            }

            // 로드 성공
            else
            {
                AllocateVariables();
                CameraInit();
            }
        }

        // 카메라 열고 재생 시작하는 함수
        private void CameraInit()
        {
            // 카메라 연결 체크
            if (!Dll.IsConnected())
            {
                // 카메라 해상도 지정 및 카메라 Open : 실패
                if (!Dll.Connect(cameraOptionModel.camWidth, cameraOptionModel.camHeight, 30))
                {
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Error,
                                (int)CoreModuleReportErrorCode.Camera_Etc,
                               "[OCam 연결 실패]",
                               "at CameraInit() of OCam.cs"));
                    ReleaseVariables();
                    return;
                }

                // 카메라 Open 성공
                else
                {
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Normal,
                                (int)CoreModuleReportNormalCode.Camera_Etc,
                               "[OCam 연결 성공]",
                               "at CameraInit() of OCam.cs"));

                    coreModuleStatusModel.cameraStatus = CoreModuleStatus.Ready;

                    // OCam 초기 세팅
                    SetOCamExposure();
                    SetOCamGain();
                    requestExposure = oCamOptionModel.exposure;
                    requestGain = oCamOptionModel.gain;
                }
            }

            // 재생 중인지 체크
            if (!Dll.IsPlaying())
            {
                // 카메라 재생 에러
                if (!Dll.Play())
                {
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                                CoreModuleReportType.Error,
                                (int)CoreModuleReportErrorCode.Camera_Etc,
                               "[OCam Play Error]",
                               "at CameraInit() of OCam.cs"));

                    ReleaseVariables();
                    return;
                }
            }
        }

        // OCam Exposure 값 가져오는 함수
        private void GetOCamExposure()
        {
            if (!Dll.GetExposure(out currExposure))
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_Etc,
                           "[OCam Exposure Get Error]",
                           "at GetOCamExposure() of OCam.cs"));
            }
        }

        // OCam 노출도 설정하는 함수
        private void SetOCamExposure()
        {
            if (!Dll.SetExposure(oCamOptionModel.exposure))
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_Etc,
                           "[OCam Exposure Setting Error]",
                           "at SetOCamExposure() of OCam.cs"));
            }
        }

        // OCam gain 값 얻어오는 함수
        private void GetOCamGain()
        {
            if (!Dll.GetGain(out currGain))
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_Etc,
                           "[OCam Get gain Error]",
                           "at GetOCamgain() of OCam.cs"));
            }
            
        }

        // OCam gain 값 설정하는 함수
        private void SetOCamGain()
        {
            if (!Dll.SetGain(oCamOptionModel.gain))
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_Etc,
                           "[OCam gain Setting Error]",
                           "at SetOCamgain() of OCam.cs"));
            }
        }

        // 매 Update()마다 호출되어 프레임 캡처하고 메세지 큐에 입력하는 쓰레드 함수
        private void CaptureThread()
        {
            int msgQCount;

            while (coreModuleStatusModel.cameraStatus > CoreModuleStatus.Ready)
            {
                // 카메라 중지 상태일 경우 : 캡처 안함
                if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Pause)
                {
                    continue;
                }

                lock (camLock)
                {
                    msgQCount = captureFrameMsgQ.Count;
                }

                // 메세지 큐가 일정 이상 쌓이지 않았을 경우에만 캡처 수행
                if (msgQCount < 10)
                {
                    // 카메라 체크
                    if (!Dll.IsPlaying())
                    {
                        coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_Etc,
                           "[카메라가 작동 중이 아닙니다]",
                           "at CaptureThread() of OCam.cs"));

                        ReleaseVariables();
                    }

                    byte[] captureByte = new byte[captureFrameByteSize];

                    // 프레임 캡처
                    if (!Dll.CopyBuffer(captureByte))
                    {
                        coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_Etc,
                           "[CopyBuffer에 캡처할 수 없습니다. OCam 연결 상태를 확인해주세요.]",
                           "at CaptureThread() of OCam.cs"));

                        ReleaseVariables();
                    }

                    // 전처리 : 옵션에 따라 이미지 상하좌우 반전 수행
                    ImagePreProcessor.FlipImageByte(captureByte, taskByte, cameraOptionModel.camWidth, cameraOptionModel.camHeight, preProcessOptionModel.flipOption);

                    // 인코딩
                    //Marshal.Copy(captureByte, 0, captureMat.nativeObj, captureFrameByteSize);
                    /*
                    captureMat.put(0, 0, captureByte, 0, captureFrameByteSize);
                    Imgproc.cvtColor(captureMat, captureMat, Imgproc.COLOR_RGB2BGR);
                    Imgcodecs.imencode(".jpg", captureMat, mob);
                    byte[] jpgByte = mob.toArray();
                    */
                    //mob.put(0, 0, captureByte, 0, captureFrameByteSize);

                    lock (camLock)
                    {
                        // 캡처 메세지 큐에 입력
                        captureFrameMsgQ.Enqueue(new CapturedFrameMsg(frameID, 0, cameraOptionModel.camWidth, cameraOptionModel.camHeight, captureByte));
                    }

                    // 프레임 ID 증가
                    frameID++;
                }

                Thread.Sleep(1);
            }

            coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(CoreModuleReportType.Normal,
                (int)CoreModuleReportNormalCode.Camera_VideoEnd,
                "[OCam Capture Thread 종료]",
                "at CaptureThread() of OCam.cs"));
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
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.None;
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

            // 카메라 작동 중인 경우
            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Playing)
            {
                int msgQCount;

                lock (camLock)
                {
                    msgQCount = captureFrameMsgQ.Count;
                }

                // 시간 추가해서 프레임 메세지 전송
                for (int i = 0; i < msgQCount; i++)
                {
                    var msg = captureFrameMsgQ.Dequeue();
                    msg.capturedTime = Time.unscaledTime;
                    Message.Send(msg);
                    if (i > 3) { break; }
                }
                /*
                

                // 실시간 노출도 설정 from Model
                if (currExposure != oCamOptionModel.exposure)
                {
                    //requestExposure = oCamOptionModel.exposure;
                    SetOCamExposure();
                    //GetOCamExposure();
                }

                // 실시간 gain 설정 부분 from Model
                if (currGain != oCamOptionModel.gain)
                {
                    //requestGain = oCamOptionModel.gain;
                    SetOCamGain();
                    //GetOCamGain();
                }

                // 실시간 노출도 설정 from Inspector
                if (currExposure != requestExposure)
                {
                    //oCamOptionModel.exposure = requestExposure;
                    SetOCamExposure();
                    //GetOCamExposure();
                }

                // 실시간 gain 설정 부분 from Inspector
                if (currGain != requestGain)
                {
                    //oCamOptionModel.exposure = requestGain;
                    SetOCamGain();
                    //GetOCamGain();
                }
                */
            }
        }
    }
}