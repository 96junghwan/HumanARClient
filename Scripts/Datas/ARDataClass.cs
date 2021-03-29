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
        /// 3D 관절 위치를 담고 있는 Vector3 리스트
        /// </summary>
        public List<Vector3> jointPositions;

        /// <summary>
        /// 각 관절의 점수 리스트, 0 ~ 1로 정규화
        /// </summary>
        public List<float> jointScores;

        /// <summary>
        /// 한 사람의 관절 개수
        /// </summary>
        public int jointMax;

        /// <summary>
        /// 사람 번호
        /// </summary>
        public int humanID;

        /// <summary>
        /// 생성자
        /// </summary>
        public Human3DJoint()
        {

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
        public Mat mask;
        public float capturedTime;
        public bool isFrameUpdated;
        public bool isJointUpdated;
        public bool isMaskUpdated;


        // 생성자 : 처음 버퍼 초기화에만 사용됨
        public HumanData(int width, int height)
        {
            frameID = 0;
            jointMax = 0;
            isFrameUpdated = false;
            isMaskUpdated = false;
            isJointUpdated = false;
            capturedTime = Time.unscaledTime;
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            jointList = new List<HumanJoint>();
            mask = new Mat(height, width, CvType.CV_8UC1);
        }
    }
}
