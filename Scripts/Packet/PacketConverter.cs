using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    // 구조체와 바이트를 서로 변환하는 기능을 가진 정적 연산 클래스
    public static class PacketConverter
    {
        // 구조체를 byte[]로 마샬링하는 함수
        public static byte[] PacketStruct2Bytes<T>(T src)
        {
            int objectSize = Marshal.SizeOf<T>();
            byte[] dst = new byte[objectSize];
            IntPtr ptr = Marshal.AllocHGlobal(objectSize);
            Marshal.StructureToPtr(src, ptr, false);
            Marshal.Copy(ptr, dst, 0, objectSize);
            Marshal.FreeHGlobal(ptr);
            return dst;
        }

        // byte[]를 구조체로 마샬링하는 함수
        public static T Bytes2PacketStruct<T>(byte[] src)
        {
            int structSize = Marshal.SizeOf<T>();
            if (structSize > src.Length) { Debug.LogError("Does not match with input byte[] and input struct"); }
            IntPtr ptr = Marshal.AllocHGlobal(structSize);
            Marshal.Copy(src, 0, ptr, structSize);
            T result = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
            return result;
        }
    }
}