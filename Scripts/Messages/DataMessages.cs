using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// from Camera,
    /// 캡쳐한 프레임 정보를 전달하는 메세지 클래스
    /// </summary>
    public class CapturedFrameMsg : Message
    {
        public int frameID;
        public int width;
        public int height;
        public float capturedTime;
        public byte[] imageByte;

        public CapturedFrameMsg(int frameID, float capturedTime, int width, int height, byte[] imageByte)
        {
            this.frameID = frameID;
            this.capturedTime = capturedTime;
            this.width = width;
            this.height = height;
            this.imageByte = imageByte;
        }
    }

    /// <summary>
    /// from Contents,
    /// 특정 프레임의 서버 신경망 연산을 요청하는 메세지 클래스 : 콘텐츠 부분에서 사용,
    /// frameID 반드시 음수 설정 필요
    /// </summary>
    public class PrivateFrameRequestMsg : Message
    {
        /// <summary>
        /// 프레임 번호, Buffer에서 재생하지 않도록 반드시 음수로 설정해야 함
        /// </summary>
        public int frameID;

        /// <summary>
        /// 이미지의 가로 크기
        /// </summary>
        public int width;

        /// <summary>
        /// 이미지의 세로 크기
        /// </summary>
        public int height;

        /// <summary>
        /// .jpg 형태로 압축한 byte[], Texture2D.EncodeToJPG() 함수 사용
        /// </summary>
        public byte[] jpgByte;

        /// <summary>
        /// 요청할 딥러닝 연산 종류를 비트마스크 사용해서 입력,
        /// ARConstants.cs의 NNType 클래스 참조
        /// </summary>
        public int nnType;

        /// <summary>
        /// 별도 연산을 요청하는 메세지 생성자
        /// </summary>
        /// <param name="frameID"> 프레임 번호, 재생하지 않기 위해 반드시 음수 설정</param>
        /// <param name="width"> 프레임의 가로 크기</param>
        /// <param name="height"> 프레임의 세로 크기</param>
        /// <param name="jpgByte"> 프레임을 .jpg로 압축한 byte[]</param>
        /// <param name="nnType"> 서버에 요청할 딥러닝 연산 종류, NNType 클래스 참조</param>
        public PrivateFrameRequestMsg(int frameID, int width, int height, byte[] jpgByte, int nnType)
        {
            this.frameID = frameID;
            this.width = width;
            this.height = height;
            this.nnType = nnType;
            this.jpgByte = jpgByte;
        }
    }




    /// <summary>
    /// from NetworkSocket,
    /// 서버에서 수신한 관절 메세지 클래스,
    /// PrivateFrameRequestMsg로 요청한 데이터는 직접 이 메세지의 리스너를 Add해서 받아야 함
    /// </summary>
    public class DetectHumanJointResultMsg : Message
    {
        /// <summary>
        /// 관절의 프레임 번호
        /// </summary>
        public int frameID;

        /// <summary>
        /// 관절 리스트, 한 프레임 안의 모든 사람 관절 정보가 포함됨
        /// </summary>
        public List<HumanJoint> jointList;

        public DetectHumanJointResultMsg(int frameID, List<HumanJoint> jointList)
        {
            this.frameID = frameID;
            this.jointList = jointList;
        }
    }

    /// <summary>
    /// from NetworkSocket,
    /// 서버에서 방금 수신한 마스크 메세지 클래스,
    /// PrivateFrameRequestMsg로 요청한 데이터는 직접 이 메세지의 리스너를 Add해서 받아야 함,
    /// 별도로 요청한 마스크 데이터는 HumanSegMaskProcessor.Bytes2MatMask를 이용해 Mat으로 변환해야 함
    /// </summary>
    public class DetectHumanMaskResultMsg : Message
    {
        /// <summary>
        /// 마스크의 프레임 번호
        /// </summary>
        public int frameID;

        /// <summary>
        /// 마스크의 가로 크기
        /// </summary>
        public int width;

        /// <summary>
        /// 마스크의 세로 크기
        /// </summary>
        public int height;

        /// <summary>
        /// 마스크의 jpg 형태로 압축된 바이트 배열, 
        /// 직접 사용하려면 HumanSegMaskProcessor.Bytes2MatMask()를 이용해 Mat으로 변환해야 함
        /// </summary>
        public byte[] maskByte;

        /// <summary>
        /// 마스크 바이트 배열의 사이즈
        /// </summary>
        public int maskByteSize;

        public DetectHumanMaskResultMsg(int frameID, int width, int height, byte[] maskByte, int maskByteSize)
        {
            this.frameID = frameID;
            this.width = width;
            this.height = height;
            this.maskByteSize = maskByteSize;
            this.maskByte = maskByte;
        }
    }

    /// <summary>
    /// 서버에서 방금 수신한 3D 관절 좌표 메시지 클래스
    /// NetworkSocketManager가 BufferManager에게 보낼 것임
    /// </summary>
    public class DetectHuman3DJointResultMsg : Message
    {
        /// <summary>
        /// 관절의 프레임 번호
        /// </summary>
        public int frameID;

        /// <summary>
        /// 3D 관절 데이터 리스트, 한 프레임 안의 모든 사람 관절 정보가 포함됨
        /// </summary>
        public List<Human3DJoint> jointList;

        public DetectHuman3DJointResultMsg(int frameID, List<Human3DJoint> jointList)
        {
            this.frameID = frameID;
            this.jointList = jointList;
        }
    }




    /// <summary>
    /// From Buffer,
    /// 영상 단독 재생 메세지 클래스
    /// </summary>
    public class PlayFrameTextureMsg : Message
    {
        /// <summary>
        /// 원본 영상의 프레임 번호
        /// </summary>
        public int frameID;

        /// <summary>
        /// 원본 영상 Texture2D 데이터
        /// </summary>
        public Texture2D texture;

        public PlayFrameTextureMsg(int frameID, Texture2D texture)
        {
            this.frameID = frameID;
            this.texture = texture;
        }
    }

    /// <summary>
    /// from Buffer,
    /// 한 프레임의 관절 재생 메세지 클래스
    /// </summary>
    public class PlayHumanJointListMsg : Message
    {
        /// <summary>
        /// 관절의 프레임 번호
        /// </summary>
        public int frameID;

        /// <summary>
        /// 한 프레임의 모든 사람의 관절 데이터
        /// </summary>
        public List<HumanJoint> jointList;

        public PlayHumanJointListMsg(int frameID, List<HumanJoint> jointList)
        {
            this.frameID = frameID;
            this.jointList = jointList;
        }
    }

    /// <summary>
    /// from Buffer,
    /// 영상 + 마스크 재생 메세지 클래스
    /// </summary>
    public class PlayFrameTextureAndHumanMaskMsg : Message
    {
        /// <summary>
        /// 원본 영상과 마스크의 프레임 번호
        /// </summary>
        public int frameID;

        /// <summary>
        /// 원본 영상 Texture2D 데이터
        /// </summary>
        public Texture2D texture;

        /// <summary>
        /// 마스크 데이터
        /// </summary>
        public Mat mask;

        public PlayFrameTextureAndHumanMaskMsg(int frameID, Texture2D texture, Mat mask)
        {
            this.frameID = frameID;
            this.texture = texture;
            this.mask = mask;
        }
    }


    /// <summary>
    /// from Buffer,
    /// 한 프레임의 3D 관절 재생 메시지 클래스
    /// </summary>
    public class PlayHuman3DJointListMsg : Message
    {
        /// <summary>
        /// 관절의 프레임 번호
        /// </summary>
        public int frameID;

        /// <summary>
        /// 한 프레임의 모든 사람의 관절 데이터
        /// </summary>
        public List<Human3DJoint> jointList;

        public PlayHuman3DJointListMsg(int frameID, List<Human3DJoint> jointList)
        {
            this.frameID = frameID;
            this.jointList = jointList;
        }
    }
}