using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 사용할 카메라 종류
    /// </summary>
    public enum CameraType : int
    {
        /// <summary>
        /// 카메라 미사용 옵션
        /// </summary>
        None = 0,

        /// <summary>
        /// 웹캠 사용 옵션 : Android에서는 본 옵션 사용 권장
        /// </summary>
        WebcamTexture,

        /// <summary>
        /// OCam 사용 옵션
        /// </summary>
        OCam,

        /// <summary>
        /// OpenCV를 이용한 비디오 옵션
        /// </summary>
        VideoOpenCV,

        /// <summary>
        /// Unity의 VideoPlayer 기능 사용 옵션, 비디오는 빠르나 메인 Update 사용해서 별로임
        /// </summary>
        VideoLoader,
    }

    /// <summary>
    /// 접속할 서버 PC 종류
    /// </summary>
    public enum ServerType : int
    {
        /// <summary>
        /// 로컬 PC 서버
        /// </summary>
        Server_Local = 0,

        /// <summary>
        /// 2080 PC 서버
        /// </summary>
        Server_2080,

        /// <summary>
        /// Titan PC 서버
        /// </summary>
        Server_Titan,

        /// <summary>
        /// 패션 PC 서버
        /// </summary>
        Server_Fashion,
    }

    /// <summary>
    /// 서버 연결에 필요한 주소 정보
    /// </summary>
    public static class ServerAddress
    {
        public const string Server_Local_IP = "127.0.0.1";
        public const string Server_2080_IP = "106.242.112.46";
        public const string Server_Titan_IP = "106.242.112.46";
        public const string Server_Fashion_IP = "112.217.220.82";

        public const int Server_Local_Port = 9999;
        public const int Server_2080_Port = 9999;
        public const int Server_Titan_Port = 8888;
        public const int Server_Fashion_Port = 43522;
    }

    /// <summary>
    /// 딥러닝 서버의 서비스 접속에 필요한 접속 코드,
    /// 상용화 시 HumanAR 서버 담당자와 협의 필요
    /// </summary>
    public static class AccessCode
    {
        /// <summary>
        /// 개발자 접속 코드
        /// </summary>
        public const string Developer_AccessCode = "99.99.99";

        /// <summary>
        /// 임시 접속 코드
        /// </summary>
        public const string Temp_AccessCode = "01.04.01";
    }

    /// <summary>
    /// 원본 영상 반전 옵션
    /// </summary>
    public enum FlipOption : int
    {
        /// <summary>
        /// 상하좌우 반전
        /// </summary>
        DoubleFlip = -1,

        /// <summary>
        /// 상하 반전
        /// </summary>
        VerticalFlip = 0,

        /// <summary>
        /// 좌우 반전
        /// </summary>
        HorizontalFlip = 1,

        /// <summary>
        /// 반전 미사용
        /// </summary>
        NoFlip = 2,
    }

    /// <summary>
    /// 회전 옵션, 현재 미지원, Android는 카메라 코드에서 전처리로 왼쪽 90도 회전으로 직접 수행 중
    /// </summary>
    public enum RotationOption : int
    {
        /// <summary>
        /// 회전 미사용
        /// </summary>
        NoRotation = 0,

        /// <summary>
        /// 시계방향 90도 회전
        /// </summary>
        ClockWise90,

        /// <summary>
        /// 시계방향 180도 회전
        /// </summary>
        ClockWise180,
    }

    /// <summary>
    /// 사용할 신경망 종류 : 비트 마스크 연산 가능,
    /// 예시) (int)NNType.AlphaPose | (int)NNType.Segmentation
    /// </summary>
    public enum NNType : int
    {
        /// <summary>
        /// 연산 요청 안함
        /// </summary>
        None = 0,

        /// <summary>
        /// YOLACT를 이용한 Human Segmentation 사용 옵션
        /// </summary>
        Segmentation = 1,

        /// <summary>
        /// FastPose 사용 옵션, 현재 사용 안하는 옵션,
        /// 사용을 원한다면 서버 담당자에게 연락
        /// </summary>
        FastPose = 2,

        /// <summary>
        /// AlphaPose를 이용한 2D Human Pose Estimation 사용 옵션
        /// </summary>
        AlphaPose = 4,

        /// <summary>
        /// BMC를 이용한 3D Human Pose Estimation 사용 옵션
        /// </summary>
        BMC = 8,
    }

    /// <summary>
    /// Joint를 다룰 때 필요한 static 데이터
    /// </summary>
    public static class JointData
    {
        /// <summary>
        /// 비어있는 관절 좌표, 원래는 서버에서 0,0으로 채워서 보내나, y축 반전 때문에 0, 1로 설정됨
        /// </summary>
        public static Vector2 EmptyVector = new Vector2(0.0f, 1.0f);

        /// <summary>
        /// 보이지 않게 하기위한 벡터 위치, 사용할 필요는 없음
        /// </summary>
        public static Vector3 InvisibleVector = new Vector3(-1f, -1f, -20f);

        /// <summary>
        /// 관절 이름 리스트
        /// </summary>
        public static List<string> JointNameList = new List<string> { "Nose", "Neck", "Heart", "LShoulder", "RShouler", "LElbow", "RElbow", "LWrist", "RWrist", "LHip", "RHip", "LKnee", "RKnee", "LAnkle", "RAnkle" };

        /// <summary>
        /// 관절 Enum
        /// </summary>
        public enum JointType : int
        {
            Nose = 0,
            Neck = 1,
            BodyCenter = 2,
            LShoulder = 3,
            RShoulder = 4,
            LElbow = 5,
            RElbow = 6,
            LWrist = 7,
            RWrist = 8,
            LHip = 9,
            RHip = 10,
            LKnee = 11,
            RKnee = 12,
            LAnkle = 13,
            RAnkle = 14
        }

        /// <summary>
        /// FastPose의 관절 Enum
        /// </summary>
        public enum FastJointType : int
        {
            Head = 0,
            LShoulder = 1,
            Rshoulder = 2,
            LElbow = 3,
            RElbow = 4,
            LWrist = 5,
            RWrist = 6,
            LHip = 7,
            RHip = 8,
            LKnee = 9,
            RKnee = 10,
            LAnkle = 11,
            RAnkle = 12
        }

        /// <summary>
        /// AlphaPose의 관절 Enum
        /// </summary>
        public enum AlphaJointType : int
        {
            Nose = 0,
            LEye = 1,
            REye = 2,
            LEar = 3,
            REar = 4,
            LShoulder = 5,
            RShoulder = 6,
            LElbow = 7,
            RElbow = 8,
            LWrist = 9,
            RWrist = 10,
            LHip = 11,
            RHip = 12,
            LKnee = 13,
            RKnee = 14,
            LAnkle = 15,
            RAnkle = 16,
            Neck = 17
        }

        /// <summary>
        /// 타겟 관절 개수
        /// </summary>
        public const int TARGET_JOINT_MAX = 15; 

        /// <summary>
        /// FastPose의 원본 관절 개수
        /// </summary>
        public const int FAST_JOINT_MAX = 13;

        /// <summary>
        /// AlphaPose의 원본 관절 개수
        /// </summary>
        public const int ALPHA_JOINT_MAX = 18; 
    }

    /// <summary>
    /// 3D Joint를 다룰 때 필요한 static 데이터
    /// </summary>
    public static class Joint3DData
    {
        /// <summary>
        /// 포지션 정보가(X, Y, Z) 들어오는 관절 개수
        /// </summary>
        public const int POSITION_JOINT_MAX = 49;

        /// <summary>
        /// 각도 정보가(X, Y, Z) 들어오는 관절 개수
        /// </summary>
        public const int ANGLE_JOINT_MAX = 24;

        /// <summary>
        /// 포지션 정보가 들어오는 관절의 타입
        /// </summary>
        public enum PositionJointType : int
        {
            OP_Nose = 0,
            
            OP_Neck,
            
            OP_R_Shoulder,

            OP_R_Elblow, 
            
            OP_R_Wrist, 
            
            OP_L_Shoulder, 
            
            OP_L_Elbow, 
            
            OP_L_Wrist, 
            
            OP_Middle_Hip, 
            
            OP_R_Hip, 
            
            OP_R_Knee, 
            
            OP_R_Ankle, 
            
            OP_L_Hip, 
            
            OP_L_Knee, 
            
            OP_L_Ankle, 
            
            OP_R_Eye, 
            
            OP_L_Eye, 
            
            OP_R_Ear, 
            
            OP_L_Ear, 
            
            OP_L_Big_Toe, 
            
            OP_L_Small_Toe, 
            
            OP_L_Heel, 
            
            OP_R_Big_Toe, 
            
            OP_R_Small_Toe, 
            
            OP_R_Heel, 
            
            R_Ankle, 
            
            R_Knee, 
            
            R_Hip, 
            
            L_Hip, 
            
            L_Knee, 
            
            L_Ankle, 
            
            R_Wrist, 
            
            R_Elbow, 
            
            R_Shoulder, 
            
            L_Shoulder, 
            
            L_Elbow, 
            
            L_Wrist, 
            
            Neck, // (LSP)
            
            TopOfHead, // (LSP)
            
            Pelvis, //(MPII), 
            
            Thorax, //(MPII)
            
            Spine, //(H36M)
            
            Jaw, //(H36M)
            
            Head, //(H36M) 
            
            Nose, 
            
            L_Eye, 
            
            R_Eye, 
            
            L_Ear,
            
            R_Ear,
        }

        /// <summary>
        /// 각도 정보가 들어오는 관절의 타입
        /// </summary>
        public enum AngleJointType : int
        {
            Global = 0,
            L_Hip,
            R_Hip,
            Spine_01,
            L_Knee,
            R_Knee,
            Spine_02,
            L_Ankle,
            R_Ankle,
            Spine_03,
            L_Toe,
            R_Toe,
            Middle_Shoulder,
            L_Clavice,
            R_Clavice,
            Nose,
            L_Shoulder,
            R_Shoulder,
            L_Elbow,
            R_Elbow,
            L_Wrist,
            R_Wrist,
            L_Palm, // (Invalid for SMPL-X)
            R_Palm, // (Invalid for SMPL-X)
        }
    }
    
    /// <summary>
    /// 네트워크 내부 Feedback 타입, 컨텐츠에서는 다룰 일 없음
    /// </summary>
    public static class NetworkFeedbackType
    {
        public const int InvalidData = 1;           // 이미 재생한 데이터 수신함
        public const int ServerResponseSlow = 2;    // 서버 응답이 느림
    }

    /// <summary>
    /// Joint를 다룰 샘플 콘텐츠 씬에서 필요한 옵션 : RnD 팀에서만 사용
    /// </summary>
    public static class JointContentsOption
    {
        // AR 타입 상수
        public const int ARType_Cube = 0;
        public const int ARType_Sprite = 1;
        public const int ARType_Picture = 2;
        public const int ARType_Picture2 = 3;

        // Sprite AR Type 옵션 지정용 상수
        public const int SpriteType_CreepyBoy = 0;
        public const int SpriteType_IronMan = 1;
        public const int Sprite_Max = 14;    // 한 인간의 최대 오브젝트 개수

        // Picture AR Type 옵션 지정용 상수
        public const int PictureType_Knight = 0;
        public const int PictureType_Hat = 1;
        public const int PictureType_Glove = 2;

        // Picture AR Type 2 옵션 지정용 상수
        public const int PictureType_Reactor = 0;
        public const int PictureType_Captain = 1;
        public const int PictureType_Magic = 2;
    }
}