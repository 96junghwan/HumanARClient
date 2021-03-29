using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    // 패킷 입출력 관리하는 클래스
    // NetworkSocketManager에게 종속적
    public class PacketManager
    {
        // 옵션 모델
        private PostProcessOptionModel postProcessOptionModel;

        // 처리 결과 수출할 외부 변수
        private Queue<SendData> sendQ;
        private Queue<DetectHumanJointResultMsg> humanPoseMsgQ;
        private Queue<DetectHumanMaskResultMsg> humanSegMsgQ;
        private Queue<PacketDecodeResult> packetDecodeResultQ;

        // 외부에서 처리할 일감 받아서 저장해놓는 큐
        private Queue<ReceivedPacket> recvPacketQ;
        private Queue<SendingImage> sendingImageQ;

        // PacketTaskThread 관리 변수
        private Thread packetTaskThread;
        private bool isOperating;

        // 임시 bytes
        private byte[] sendHeaderByte;
        private byte[] sendPacketByte;
        private byte[] sendCommonPacketByte;

        // 패킷 병합 수행 클래스
        private FramePacketSpliter framePacketSpliter;
        private SegPacketMerger segPacketMerger;
        private Pose2DPacketMerger pose2DPacketMerger;
        private int mergedPacketListMax = 5;




        // 생성자
        public PacketManager(Queue<SendData> sendQ, Queue<DetectHumanJointResultMsg> humanPoseMsgQ, Queue<DetectHumanMaskResultMsg> humanSegMsgQ, Queue<PacketDecodeResult> packetDecodeResultQ)
        {
            this.sendQ = sendQ;
            this.humanPoseMsgQ = humanPoseMsgQ;
            this.humanSegMsgQ = humanSegMsgQ;
            this.packetDecodeResultQ = packetDecodeResultQ;

            postProcessOptionModel = Model.First<PostProcessOptionModel>();

            AllocateVariables();
        }

        // 파괴자
        public void Destroy()
        {
            ReleaseVariables();
        }

        // 변수 할당 함수
        private void AllocateVariables()
        {
            sendHeaderByte = new byte[NetworkInfo.HeaderSize];
            sendPacketByte = new byte[NetworkInfo.NetworkBufferSize];
            sendCommonPacketByte = new byte[128];

            framePacketSpliter = new FramePacketSpliter(this.sendQ);
            segPacketMerger = new SegPacketMerger(mergedPacketListMax, this.humanSegMsgQ);
            pose2DPacketMerger = new Pose2DPacketMerger(mergedPacketListMax, postProcessOptionModel, this.humanPoseMsgQ);

            recvPacketQ = new Queue<ReceivedPacket>();
            sendingImageQ = new Queue<SendingImage>();

            isOperating = true;
            packetTaskThread = new Thread(PacketTaskThread);
            packetTaskThread.Start();
        }

        // 변수 해제 함수
        private void ReleaseVariables()
        {
            // Thread 해제
            isOperating = false;

            if (packetTaskThread != null)
            {
                packetTaskThread.Abort();
                packetTaskThread = null;
            }

            // 큐 해제
            recvPacketQ = new Queue<ReceivedPacket>();
            sendingImageQ = new Queue<SendingImage>();
        }

        // 받은 Packet 해독 작업해달라고 넣는 함수
        public void PutPacket(PacketHeader header, byte[] packetByte)
        {
            this.recvPacketQ.Enqueue(new ReceivedPacket(header, packetByte));
        }

        // 이미지 작업해달라고 넣는 함수
        public void PutImage(int frameID, int nnType, byte[] imageByte)
        {
            this.sendingImageQ.Enqueue(new SendingImage(frameID, nnType, imageByte));
        }

        // 패킷 Encode/Decode 수행하는 쓰레드
        private void PacketTaskThread()
        {
            while (isOperating)
            {
                Thread.Sleep(1);

                // 보낼 이미지 작업 한 번
                if (this.sendingImageQ.Count != 0)
                {
                    var data = sendingImageQ.Dequeue();
                    framePacketSpliter.PutFrame(data.frameID, data.nnType, data.imageByte);
                }

                // 받은 데이터 작업 한 번
                if (this.recvPacketQ.Count != 0)
                {
                    var data = recvPacketQ.Dequeue();
                    this.PacketReact(data.header, data.packetByte);
                }
            }
        }

        // 결과 데이터 패킷 이외의 패킷들 리액션하는 함수
        private void PacketReact(PacketHeader header, byte[] packetByte)
        {
            // [Segmentation 결과] 패킷 수신 시 : 패킷 매니저에 전달
            if (header.msgType == (ushort)MSGType.Response_NNCal_Segmentation)
            {
                this.PutNewSegmentationPacket(header, packetByte);
            }

            // [2DPose 결과] 패킷 수신 시 : 패킷 매니저에 전달
            else if (header.msgType == (ushort)MSGType.Response_NNCal_2DPose)
            {
                this.PutNew2DPosePacket(header, packetByte);
            }

            // [경고 메세지] 패킷 수신 시
            else if (header.msgType == (ushort)MSGType.Warning)
            {
                WarningPacketStruct packet = new WarningPacketStruct();
                packet = PacketConverter.Bytes2PacketStruct<WarningPacketStruct>(packetByte);
                packetDecodeResultQ.Enqueue(new PacketDecodeResult(MSGType.Warning, packet.warningType, 0));
            }

            // [에러 메세지] 패킷 수신 시
            else if (header.msgType == (ushort)MSGType.Error)
            {
                ErrorPacketStruct packet = new ErrorPacketStruct();
                packet = PacketConverter.Bytes2PacketStruct<ErrorPacketStruct>(packetByte);
                packetDecodeResultQ.Enqueue(new PacketDecodeResult(MSGType.Error, packet.errorType, 0));
            }

            // [통지 메세지] 패킷 수신 시 : 보통 서버 연결 끊겠다는 뜻
            else if (header.msgType == (ushort)MSGType.Notify)
            {
                NotifyPacketStruct packet = new NotifyPacketStruct();
                packet = PacketConverter.Bytes2PacketStruct<NotifyPacketStruct>(packetByte);
                packetDecodeResultQ.Enqueue(new PacketDecodeResult(MSGType.Notify, packet.notifyType, 0));
            }

            // [서비스 접속 요청 응답] 패킷 수신 시
            else if (header.msgType == (ushort)MSGType.Response_Access)
            {
                ResponseAccessPacketStruct packet = new ResponseAccessPacketStruct();
                packet = PacketConverter.Bytes2PacketStruct<ResponseAccessPacketStruct>(packetByte);
                packetDecodeResultQ.Enqueue(new PacketDecodeResult(MSGType.Response_Access, packet.accessResult, 0));
            }

            // [서버 상태 요청 응답] 패킷 수신 시
            else if (header.msgType == (ushort)MSGType.Response_ServerStatus)
            {
                ResponseServerStatusPacketStruct packet = new ResponseServerStatusPacketStruct();
                packet = PacketConverter.Bytes2PacketStruct<ResponseServerStatusPacketStruct>(packetByte);
                packetDecodeResultQ.Enqueue(new PacketDecodeResult(MSGType.Response_ServerStatus, (ushort)packet.CCU, packet.serverBufferStatus));
            }

            // [에러] : 맞는 타입이 없거나 잘못 옴
            else
            {
                // 이런 경우에 Error 패킷 서버로 보냄
                Debug.LogWarning("Wrong MSGType : " + header.msgType);
            }
        }

        // (추가 데이터 필요없는) 전송할 패킷의 msgType과 structByte만 딱 받아서 sendQ에 넣어주는 함수
        public void PutCommonPacket(MSGType msgType, byte[] packetStructByte)
        {
            // packetStructByte가 null인 경우 : 헤더만 가지고 있는 패킷 종류일 경우
            if (packetStructByte == null)
            {
                sendHeaderByte = PacketConverter.PacketStruct2Bytes<PacketHeader>(new PacketHeader(msgType, 0, 0));
                this.sendQ.Enqueue(new SendData(sendHeaderByte, NetworkInfo.HeaderSize));
            }

            else
            {
                int packetStructSize = packetStructByte.Length;
                sendHeaderByte = PacketConverter.PacketStruct2Bytes<PacketHeader>(new PacketHeader(msgType, packetStructSize, 0));
                Buffer.BlockCopy(sendHeaderByte, 0, sendCommonPacketByte, 0, NetworkInfo.HeaderSize);
                Buffer.BlockCopy(packetStructByte, 0, sendCommonPacketByte, NetworkInfo.HeaderSize, packetStructSize);
                this.sendQ.Enqueue(new SendData(sendCommonPacketByte, NetworkInfo.HeaderSize + packetStructSize));
            }
        }

        // 새로운 세그멘테이션 결과 패킷 받아서 SegPacketMerger에 저장하는 함수
        public void PutNewSegmentationPacket(PacketHeader header, byte[] packetByte)
        {
            ResponseSegmentationPacketStruct packetStruct = new ResponseSegmentationPacketStruct();
            byte[] packetStructByte = new byte[header.packetStructSize];
            byte[] packetData = new byte[header.packetDataSize];

            Buffer.BlockCopy(packetByte, 0, packetStructByte, 0, header.packetStructSize);
            Buffer.BlockCopy(packetByte, header.packetStructSize, packetData, 0, header.packetDataSize);
            packetStruct = PacketConverter.Bytes2PacketStruct<ResponseSegmentationPacketStruct>(packetStructByte);

            segPacketMerger.PutPacket(packetStruct, packetData);
        }

        // 새로운 2DPose 결과 패킷 받아서 Pose2DPacketMerger에 저장하는 함수
        public void PutNew2DPosePacket(PacketHeader header, byte[] packetByte)
        {
            Response2DPosePacketStruct packetStruct = new Response2DPosePacketStruct();
            byte[] packetStructByte = new byte[header.packetStructSize];
            byte[] packetData = new byte[header.packetDataSize];

            Buffer.BlockCopy(packetByte, 0, packetStructByte, 0, header.packetStructSize);
            Buffer.BlockCopy(packetByte, header.packetStructSize, packetData, 0, header.packetDataSize);
            packetStruct = PacketConverter.Bytes2PacketStruct<Response2DPosePacketStruct>(packetStructByte);

            pose2DPacketMerger.PutPacket(packetStruct, packetData);
        }
    }
}