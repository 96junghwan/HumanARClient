using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 인간 하나의 ID, 2D 관절 좌표, 점수, 관절 개수를 가지고 있는 데이터 클래스
    /// </summary>
    public class HumanJoint
    {
        /// <summary>
        /// 2D 관절 좌표 리스트
        /// </summary>
        public List<Vector2> viewportJointPositions;

        /// <summary>
        /// 관절 별 정확도 점수 리스트 : 0 ~ 1 사이의 값
        /// </summary>
        public List<float> jointScores;

        /// <summary>
        /// 한 사람이 가지는 관절 개수
        /// </summary>
        public int jointMax;

        /// <summary>
        /// 사람 번호 : 현재 미지원
        /// </summary>
        public int humanID;

        // 빈 관절인지 검사하는 함수
        public bool isZero(JointData.JointType a, JointData.JointType b)
        {
            // 둘 중 하나가 0 벡터일 경우
            if (GetViewportJointPosition(a).Equals(JointData.EmptyVector) || GetViewportJointPosition(b).Equals(JointData.EmptyVector)) { return true; }

            // 둘 모두 0 벡터가 아닐 경우
            else { return false; }
        }

        public HumanJoint(List<Vector2> joints, List<float> scores, int jointMax)
        {
            this.viewportJointPositions = new List<Vector2>();
            this.jointScores = new List<float>();
            this.jointMax = jointMax;

            for (int j = 0; j < this.jointMax; j++)
            {
                this.viewportJointPositions.Add(joints[j]);
                this.jointScores.Add(scores[j]);
            }
        }

        public Vector2 GetViewportJointPosition(JointData.JointType type)
        {
            return this.viewportJointPositions[(int)type];
        }

        public float GetJointScore(JointData.JointType type)
        {
            return this.jointScores[(int)type];
        }
    }

    /// <summary>
    /// 인간 하나의 ID, 3D 관절 좌표, 점수, 관절 개수를 가지고 있는 데이터 클래스,
    /// 현재 미지원
    /// </summary>
    public class Human3DJoint
    {
        /// <summary>
        /// Bounding Box의 이미지 상 좌표 : (x, y, width, height)
        /// </summary>
        public List<int> bbox;

        /// <summary>
        /// 3D 관절 위치를 담고 있는 Vector3 리스트 : 이미지 좌표 기준임
        /// Z좌표는 이미지 크기에 따라 조정된 수치
        /// </summary>
        public List<Vector3> jointPositions;

        /// <summary>
        /// 3D 관절 회전 정보를 담고 있는 Vecter3 리스트 : Axis-Angle Format
        /// </summary>
        public List<Vector3> jointAngles;

        /// <summary>
        /// 생성자
        /// </summary>
        public Human3DJoint(List<int> bbox, List<Vector3> jointPositions, List<Vector3> jointAngles)
        {
            this.bbox = bbox;
            this.jointPositions = jointPositions;
            this.jointAngles = jointAngles;
        }
        
        /// <summary>
        /// (x, y, width, height) 구조의 Bounding Box를 넘겨주는 함수
        /// </summary>
        public List<int> GetBBox()
        {
            return this.bbox;
        }
        
        /// <summary>
        /// 지정한 관절의 이미지 기준 3D 좌표를 Vector3로 리턴하는 함수
        /// </summary>
        public Vector3 GetJointPosition(Joint3DData.PositionJointType type)
        {
            return this.jointPositions[(int)type];
        }

        /// <summary>
        /// 지정한 관절의 Axis-Angle Format 기준 3D 회전 정보를 Vector3로 리턴하는 함수
        /// </summary>
        public Vector3 GetJointAngle(Joint3DData.AngleJointType type)
        {
            return this.jointAngles[(int)type];
        }
    }

    /// <summary>
    /// 버퍼에서 재생하기 위한 한 프레임의 모든 정보를 담는 데이터 클래스
    /// </summary>
    public class HumanData
    {
        public int frameID;
        public int jointMax;
        public Texture2D texture;
        public List<HumanJoint> jointList;
        public List<Human3DJoint> joint3DList;
        public Mat mask;
        public float capturedTime;
        public bool isFrameUpdated;
        public bool isJointUpdated;
        public bool is3DJointUpdated;
        public bool isMaskUpdated;


        // 생성자 : 처음 버퍼 초기화에만 사용됨
        public HumanData(int width, int height)
        {
            frameID = 0;
            jointMax = 0;
            isFrameUpdated = false;
            isMaskUpdated = false;
            isJointUpdated = false;
            is3DJointUpdated = false;
            capturedTime = Time.unscaledTime;
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            jointList = new List<HumanJoint>();
            joint3DList = new List<Human3DJoint>();
            mask = new Mat(height, width, CvType.CV_8UC1);
        }
    }
}
