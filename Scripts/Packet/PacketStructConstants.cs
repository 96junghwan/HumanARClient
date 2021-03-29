using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    public static class NetworkInfo
    {
        public const int HeaderSize = 16;
        public const int NetworkBufferSize = 4096;
        public const int NetworkbufferMax = 65535;
    }

    public enum MSGType : ushort
    {
        Warning = 0,
        Error,
        Notify,

        Request_Access = 1000,
        Request_ServerStatus,
        Request_NNCal,

        Response_Access = 2000,
        Response_ServerStatus,
        Response_NNCal_Segmentation,
        Response_NNCal_2DPose,
    }



    public enum WarningType : ushort
    {
        Server_Getting_Busy = 0,
        Server_Clients_Full,

        Client_Response_Slow = 1000,
    }

    public enum ErrorType : ushort
    {
        Unsuitable_PacketHeaderStruct = 0,
        Unsuitable_PacketStruct,
        Unsuitable_PacketStructField,
        Unsuitable_PacketType,

        UnOpen_NN = 1000,
    }

    public enum NotifyType : ushort
    {
        Client_Close = 0,

        Server_Close = 1000,
    }



    public enum Access_Result : ushort
    {
        Accept = 0,

        Reject_Unsuitable_AccessCode = 1000,
        Reject_Old_AccessCode,
        Reject_Full_CCU,
    }

    public enum NNCal_Result : ushort
    {
        Success = 0,

        Fail_Server_Busy = 1000,
        Fail_Image_Crack,
        Fail_InputData_Error,
    }

    public enum Server_Status : ushort
    {
        Idle = 0,
        Normal,
        Busy,
        Jammed,
    }

    public enum Order : int
    {
        First = 1,
        Middle = 2,
        End = 4,
    }
}