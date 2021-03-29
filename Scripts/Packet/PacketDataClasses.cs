using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    // 받은 패킷 데이터 넘겨주기 위한 데이터 클래스
    public class ReceivedPacket
    {
        public PacketHeader header;
        public byte[] packetByte;

        public ReceivedPacket(PacketHeader header, byte[] packetByte)
        {
            this.header = header;
            this.packetByte = packetByte;
        }
    }

    // 서버로 보낼 이미지를 넘겨주기 위한 데이터 클래스
    public class SendingImage
    {
        public int frameID;
        public int nnType;
        public byte[] imageByte;

        public SendingImage(int frameID, int nnType, byte[] imageByte)
        {
            this.frameID = frameID;
            this.nnType = nnType;
            this.imageByte = imageByte;
        }
    }
}