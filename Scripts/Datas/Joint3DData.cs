using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    // enum 관련 확장 메서드
    public static partial class EnumExtend
    {
        public static int Int(this Joint3DData.PositionJointType i)
        {
            return (int)i;
        }

        public static int Int(this Joint3DData.AngleJointType i)
        {
            return (int)i;
        }

        public static int Int(this ActualJointType i)
        {
            return (int)i;
        }
    }

    // 한 관절의 모든 3D 정보를 담은 클래스
    public class JointPoint
    {
        public Vector3 Now3D = new Vector3();       // 신참 Position
        public Vector3 NowAngle = new Vector3();    // 신참 Angle

        public Vector3 Pos3D = new Vector3();
        public Vector3[] PrevPos3D = new Vector3[6];

        // Bones
        public Transform Transform = null;
        public Quaternion InitRotation;
        public Quaternion Inverse;
        public Quaternion InverseRotation;

        public JointPoint Child = null;
        public JointPoint Parent = null;

        // For Kalman filter
        public Vector3 P = new Vector3();
        public Vector3 X = new Vector3();
        public Vector3 K = new Vector3();
    }

    /// <summary>
    /// 실제 아바타에 박혀있는 3D 관절의 타입
    /// </summary>
    public enum ActualJointType : int
    {
        Global = 0,

        Hips,
        Spine,
        Chest,
        UpperChest,

        Neck,
        Head,

        L_Hip,
        L_Knee,
        L_Ankle,
        L_Toe,

        R_Hip,
        R_Knee,
        R_Ankle,
        R_Toe,

        L_Shoulder,
        L_Elbow,
        L_Wrist,

        R_Shoulder,
        R_Elbow,
        R_Wrist,

        Count,
    }

    /// <summary>
    /// 3D Joint를 다룰 때 필요한 static 데이터
    /// </summary>
    public static class Joint3DData
    {
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

            Count,
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
            Count,
        }

        /// <summary>
        /// 포지션 정보 들어오는 49개 관절 이름 리스트
        /// </summary>
        public static List<string> PositionJointName = new List<string>(){
            "0 : OP_Nose",
            "1 : OP_Neck",
            "2 : OP_R_Shoulder",
            "3 : OP_R_Elbow",
            "4 : OP_R_Wrist",
            "5 : OP_L_Shoulder",
            "6 : OP_L_Elbow",
            "7 : OP_L_Wrist",
            "8 : OP_Middle_Hip",
            "9 : OP_R_Hip",
            "10 : OP_R_Knee",
            "11 : OP_R_Ankle",
            "12 : OP_L_Hip",
            "13 : OP_L_Knee",
            "14 : OP_L_Ankle",
            "15 : OP_R_Eye",
            "16 : OP_L_Eye",
            "17 : OP_R_Ear",
            "18 : OP_L_Ear",
            "19 : OP_L_Big_Toe",
            "20 : OP_L_Small_Toe",
            "21 : OP_L_Heel",
            "22 : OP_R_Big_Toe",
            "23 : OP_R_Small_Toe",
            "24 : OP_R_Heel",

            "25 : R_Ankle",
            "26 : R_Knee",
            "27 : R_Hip",
            "28 : L_Hip",
            "29 : L_Knee",
            "30 : L_Ankle",
            "31 : R_Wrist",
            "32 : R_Elbow",
            "33 : R_Shoulder",
            "34 : L_Shoulder",
            "35 : L_Elbow",
            "36 : L_Wrist",

            "37 : Neck (LSP)",
            "38 : Top of Head (LSP)",
            "39 : Pelvis (MPII)",
            "40 : Thorax (LSP)",
            "41 : Spine (H36M)",
            "42 : Jaw (H36M)",
            "43 : Head (H36M)",
            "44 : Nose",
            "45 : L_Eye",
            "46 : R_Eye",
            "47 : L_Ear",
            "48 : R_Ear"
        };

        /// <summary>
        /// 각도 정보 들어오는 24개 관절 이름 리스트
        /// </summary>
        public static List<string> AnkleJointName = new List<string>(){
            "0 : Global",
            "1 : L_Hip",
            "2 : R_Hip",
            "3 : Spine_01",
            "4 : L_Knee",
            "5 : R_Knee",
            "6 : Spine_02",
            "7 : L_Ankle",
            "8 : R_Ankle",
            "9 : Spine_03",
            "10 : L_Toe",
            "11 : R_Toe",
            "12 : Middle_Shoulder",
            "13 : L_Clavice",
            "14 : R_Clavice",
            "15 : Nose",
            "16 : L_Shoulder",
            "17 : R_Shoulder",
            "18 : L_Elbow",
            "19 : R_Elbow",
            "20 : L_Wrist",
            "21 : R_Wrist",
            "22 : L_Palm (Invalid)",
            "23 : R_Palm (Invalid)"
        };
    }

    /// <summary>
    /// 3D Joint를 적용할 샘플 3D 아바타 타입
    /// </summary>
    public enum Avatar3DType : int
    {
        Boss = 0,
        UnityChan,
    }
}
