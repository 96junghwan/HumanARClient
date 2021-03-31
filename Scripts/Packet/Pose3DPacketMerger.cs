using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CellBig.Module.HumanDetection
{
    public class Pose3DPacketMerger
    {
        // 내부 클래스
        class MergedPacket
        {
            public int frameID;
            public uint jointWholeSize;
            public ushort result;
            public ushort people;
            public byte[] resultByte;
            public bool isProcessing;

            // 생성자
            public MergedPacket() { isProcessing = false; }

            // 작업 끝났다고 표시하는 함수
            public void Clear() { isProcessing = false; }
        }

        // 멤버 변수
        private PostProcessOptionModel postProcessOptionModel;
        private Queue<DetectHuman3DJointResultMsg> human3DPoseMsgQ;
        private List<MergedPacket> packetList;
        private int listMax;

        // 생성자
        public Pose3DPacketMerger(int listMax, PostProcessOptionModel postProcessOptionModel, Queue<DetectHuman3DJointResultMsg> human3DPoseMsgQ)
        {
            this.listMax = listMax;
            this.postProcessOptionModel = postProcessOptionModel;
            this.human3DPoseMsgQ = human3DPoseMsgQ;

            packetList = new List<MergedPacket>();
            for (int i = 0; i < listMax; i++) { packetList.Add(new MergedPacket()); }
        }

        // 수신한 관절 패킷 하나 넣는 함수
        public void PutPacket(Response3DPosePacketStruct packet, byte[] jointByte)
        {
            int index = FindIndex(packet.order, packet.frameID);

            // 인덱스 찾은 경우
            if (index != -1)
            {
                // 프레임의 첫 패킷인 경우
                if ((packet.order & (int)Order.First) == (int)Order.First)
                {
                    packetList[index].frameID = packet.frameID;
                    packetList[index].jointWholeSize = packet.jointWholeSize;
                    packetList[index].result = packet.result;
                    packetList[index].people = packet.people;
                    packetList[index].resultByte = new byte[packet.jointWholeSize];
                }

                // 마스크 데이터 복사하여 저장
                Buffer.BlockCopy(jointByte, 0, packetList[index].resultByte, (int)packet.offset, packet.dataSize);

                // 프레임의 마지막 패킷인 경우 : NetworkManager의 MSGQ에 전송
                if ((packet.order & (int)Order.End) == (int)Order.End)
                {
                    SendMsg(index);
                    BrokenPacketCheck(packetList[index].frameID);
                }
            }

            // 인덱스 못찾은 경우
            else
            {
                Debug.LogWarning("적절한 인덱스를 찾을 수 없습니다.");
            }
        }

        // 필드 값으로 마스크 검출 결과 메세지 생성해서 전송까지 하는 함수
        public void SendMsg(int index)
        {
            List<HumanJoint> jointList;

            // 사람이 검출된 경우
            if (packetList[index].jointWholeSize > 0)
            {
                jointList = JointParser.Bytes2Human3DJointList(
                    packetList[index].resultByte, 
                    packetList[index].people, 
                    postProcessOptionModel.useJointPerfectFilter, 
                    postProcessOptionModel.useJointParsing);
            }

            // 사람이 검출되지 않은 경우
            else
            {
                jointList = new List<Human3DJoint>();
            }
            
            // NetworkManger에 전달
            this.human3DPoseMsgQ.Enqueue(new DetectHuman3DJointResultMsg(packetList[index].frameID, jointList));

            // 리스트 방 비워주기
            packetList[index].Clear();
        }

        // 클래스 내부에서 알맞은 리스트의 인덱스 찾아주는 함수
        private int FindIndex(int order, int frameID)
        {
            int index = -1;

            // 신규 패킷인 경우 : 리스트의 빈 공간 탐색
            if ((order & (int)Order.First) == (int)Order.First)
            {
                for (int i = 0; i < listMax; i++)
                {
                    if (!packetList[i].isProcessing) { index = i; break; }
                }
            }

            // 신규 패킷이 아닌 경우
            else
            {
                for (int i = 0; i < listMax; i++)
                {
                    if (packetList[i].frameID == frameID) { index = i; break; }
                }
            }

            return index;
        }

        // 입력된 frameID를 기준으로 일정 이상 떨어진 frameID를 갖고 있고, 아직 작업 중이면 비워주기
        private void BrokenPacketCheck(int lastFrameID)
        {
            for (int i = 0; i < packetList.Count; i++)
            {
                if ((lastFrameID - packetList[i].frameID) >= 10 && packetList[i].isProcessing)
                {
                    Debug.Log("Broken Packet Found");
                    packetList[i].isProcessing = false;
                }
            }
        }
    }
}