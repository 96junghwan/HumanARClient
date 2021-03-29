using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    public class FramePacketSpliter
    {
        // 내부 클래스
        private class MergedPacket
        {
            public int frameID;
            public int nnType;
            public uint imageWholeSize;
            public byte[] imageByte;

            public uint remainBytes;
            public uint nextOffset;
            public bool isProcessing;

            // 생성자
            public MergedPacket()
            {
                this.remainBytes = 0;
                this.nextOffset = 0;
                this.isProcessing = false;
            }

            // 한 프레임의 데이터 입력하는 함수
            public void PutFrame(int frameID, int nnType, byte[] imageByte)
            {
                this.frameID = frameID;
                this.nnType = nnType;
                this.imageByte = imageByte;

                this.imageWholeSize = (uint)imageByte.Length;
                this.remainBytes = this.imageWholeSize;
                this.nextOffset = 0;
                this.isProcessing = true;
                //Debug.Log(this.imageWholeSize);
            }

            // 작업 끝났다고 표시하는 함수
            public void Clear()
            {
                this.remainBytes = 0;
                this.nextOffset = 0;
                this.isProcessing = false;
            }
        }

        private MergedPacket mergedPacket;
        private Queue<SendData> sendQ;

        // 생성자
        public FramePacketSpliter(Queue<SendData> sendQ)
        {
            this.sendQ = sendQ;
            this.mergedPacket = new MergedPacket();
        }

        // 새 이미지 넣어서, 하나하나 쪼개서 sendQ에 넣는 함수
        public void PutFrame(int frameID, int nnType, byte[] imageByte)
        {
            // 필요한 변수 초기화
            ushort dataSize = 0;
            uint offset = 0;
            int order = 0;
            byte[] sendHeaderByte = new byte[NetworkInfo.HeaderSize];
            byte[] packetStuctByte = new byte[1];
            int structByteSize;

            // 내부 클래스 인스턴스에 일단 정보들 저장
            this.mergedPacket.PutFrame(frameID, nnType, imageByte);

            // 한 프레임을 전부 쪼개서 보낼 때 까지 반복
            while (mergedPacket.isProcessing)
            {
                // 이번 패킷에 들어갈 이미지 사이즈 계산
                dataSize = (ushort)mergedPacket.remainBytes;
                if (dataSize > 4000) { dataSize = 4000; }

                // offset 계산
                offset = mergedPacket.nextOffset;
                mergedPacket.nextOffset += dataSize;
                mergedPacket.remainBytes -= dataSize;

                // order 계산
                if (offset == 0) { order += (int)Order.First; }
                if (mergedPacket.remainBytes == 0) { order += (int)Order.End; }

                // 패킷 struct, Header 생성 및 byte[]로 변환
                RequestNNCalPacketStruct packetStruct = new RequestNNCalPacketStruct(mergedPacket.frameID, mergedPacket.imageWholeSize, dataSize, offset, order, mergedPacket.nnType);
                packetStuctByte = PacketConverter.PacketStruct2Bytes<RequestNNCalPacketStruct>(packetStruct);
                structByteSize = packetStuctByte.Length;
                sendHeaderByte = PacketConverter.PacketStruct2Bytes<PacketHeader>(new PacketHeader(MSGType.Request_NNCal, structByteSize, dataSize));

                // packet {header, struct, data} 메모리 카피
                byte[] sendPacketByte = new byte[NetworkInfo.HeaderSize + structByteSize + dataSize];
                Buffer.BlockCopy(sendHeaderByte, 0, sendPacketByte, 0, NetworkInfo.HeaderSize);
                Buffer.BlockCopy(packetStuctByte, 0, sendPacketByte, NetworkInfo.HeaderSize, structByteSize);
                if (dataSize > 0) { Buffer.BlockCopy(mergedPacket.imageByte, (int)offset, sendPacketByte, NetworkInfo.HeaderSize + structByteSize, dataSize); }

                // sendQ에 입력
                this.sendQ.Enqueue(new SendData(sendPacketByte, sendPacketByte.Length));        // 문제 시 Lock

                // 다음 사이클을 위한 후처리
                order = 0;
                if (mergedPacket.remainBytes <= 0) { mergedPacket.Clear(); break; }
            }
        }
    }
}