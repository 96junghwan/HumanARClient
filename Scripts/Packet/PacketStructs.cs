using System;
using System.Runtime.InteropServices;   // Marshal

namespace CellBig.Module.HumanDetection
{
    // 모든 CBAR Packet의 공통 Header
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    [Serializable]
    public struct PacketHeader
    {
        public ushort msgType;
        public ushort packetStructSize;
        public ushort packetDataSize;

        // 생성자
        public PacketHeader(MSGType msgType, int packetStructSize, int packetDataSize)
        {
            this.msgType = (ushort)msgType;
            this.packetStructSize = (ushort)packetStructSize;
            this.packetDataSize = (ushort)packetDataSize;
        }
    }



    // 서버/클라 송신 : 경고 Packet Struct
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct WarningPacketStruct
    {
        public ushort warningType;
    }

    // 서버/클라 송신 : 에러 Packet Struct
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct ErrorPacketStruct
    {
        public ushort errorType;
    }

    // 서버/클라 송신 : 통지 Packet Struct
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct NotifyPacketStruct
    {
        public ushort notifyType;
    }



    // 클라 송신 : 서버 접속 요청 Packet Struct
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct RequestAccessPacketStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
        public string accessCode;   // UTF8?

        // 생성자
        public RequestAccessPacketStruct(string accessCode)
        {
            this.accessCode = accessCode;
        }
    }

    // 클라 송신 : 서버 상태 요청 Packet Struct
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct RequestServerStatusPacketStruct
    {

    }

    // 클라 송신 : 딥러닝 서버 이미지 연산 요청 Packet Struct / 나눠서 보낼 수 있음
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct RequestNNCalPacketStruct
    {
        public int frameID;
        public uint imageWholeSize;
        public ushort dataSize;
        public uint offset;
        public int order;
        public int nnType;

        public RequestNNCalPacketStruct(int frameID, uint imageWholeSize, ushort dataSize, uint offset, int order, int nnType)
        {
            this.frameID = frameID;
            this.imageWholeSize = imageWholeSize;
            this.dataSize = dataSize;
            this.offset = offset;
            this.order = order;
            this.nnType = nnType;
        }
    }



    // 서버 송신 : 서버 접속 응답 Packet Struct
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct ResponseAccessPacketStruct
    {
        public ushort accessResult;
    }

    // 서버 송신 : 서버 상태 응답 Packet Struct
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct ResponseServerStatusPacketStruct
    {
        public uint CCU;
        public ushort serverBufferStatus;
    }

    // 서버 송신 : 세그멘테이션 결과 Packet Struct / 나뉘어서 올 수 있음
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct ResponseSegmentationPacketStruct
    {
        public int frameID;
        public uint maskWholeSize;
        public ushort dataSize;
        public ushort result;
        public int nnType;
        public uint offset;
        public int order;
        public ushort width;
        public ushort height;
    }

    // 서버 송신 : 2D 휴먼포즈 결과 Packet Struct / 나뉘어서 올 수 있음
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct Response2DPosePacketStruct
    {
        public int frameID;
        public uint jointWholeSize;
        public ushort dataSize;
        public ushort result;
        public int nnType;
        public uint offset;
        public int order;
        public ushort jointNumbers;
    }
}