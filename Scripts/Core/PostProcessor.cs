using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity;

namespace CellBig.Module.HumanDetection
{
    // 임시 관절 후처리 클래스, BufferManager에서 이 클래스의 인스턴스 들고 있음
    public class TempJointPostProcesser
    {
        // 멤버
        private PostProcessOptionModel postProcessOptionModel;
        private List<List<HumanJoint>> lagacyHumanList;
        private List<int> lagacyFrameIDList;
        private List<float> lowPassParamList;

        // 상수
        private const int LagacyListMax = 9;
        private const float KamlanQ = 0.001f;
        private const float KamlanR = 0.0015f;

        // 생성자
        public TempJointPostProcesser(PostProcessOptionModel postProcessOptionModel)
        {
            this.postProcessOptionModel = postProcessOptionModel;

            lagacyFrameIDList = new List<int>();
            lagacyHumanList = new List<List<HumanJoint>>();

            // 하드 코딩
            lowPassParamList = new List<float>();
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
            lowPassParamList.Add(0.1f);
        }

        // 파괴자
        public void Destroy()
        {
            lagacyHumanList.Clear();
            lagacyHumanList = null;
        }

        // 임시 떨림 보정 함수
        public void CompressJointShake(int frameID, List<HumanJoint> newHuman)
        {
            // 빈 데이터 들어왔을 경우 : 탈락
            if (newHuman.Count == 0)
            {
                return;
            }

            // 보정하기에 갯수가 부족한 경우
            if (lagacyFrameIDList.Count < LagacyListMax)
            {
                lagacyFrameIDList.Add(frameID);
                lagacyHumanList.Add(newHuman);
                return;
            }

            // 프레임 번호 연속성 검사
            int frameSequenceCheck = frameID - lagacyFrameIDList[LagacyListMax-1];
            for (int i = LagacyListMax - 1; i>0; i--)
            {
                frameSequenceCheck += (lagacyFrameIDList[i] - lagacyFrameIDList[i-1]);
            }

            // 프레임 번호 연속성 검사 탈락
            if (frameSequenceCheck != LagacyListMax) { return; }

            // 사람 수 검사
            bool humanNumberCheck = true;

            humanNumberCheck = (newHuman.Count != lagacyHumanList[LagacyListMax - 1].Count);

            for (int i = LagacyListMax - 1; i > 0; i--)
            {
                if (lagacyHumanList[i].Count != lagacyHumanList[i - 1].Count)
                {
                    humanNumberCheck = false;
                    break;
                }
            }

            // 사람 수 검사 탈락
            if (!humanNumberCheck) { return; }

            // 임시 Kalman Filter 적용
            //TempKalmanFilter(newHuman);

            // 임시 Low Pass Filter 적용
            TempLowPassFilter(newHuman);

            // 다음 검사를 위해 lagacy에 새로 입력
            lagacyFrameIDList.RemoveAt(0);
            lagacyFrameIDList.Add(frameID);
            lagacyHumanList.RemoveAt(0);
            lagacyHumanList.Add(newHuman);
        }

        // 임시 Kalman Filter 적용 함수 : 미구현
        private void TempKalmanFilter(List<HumanJoint> newHuman)
        {
            // 구현 예정
        }

        // 임시 Low Pass Filter 적용 함수 : 이거 적용 전에 관절 검사도 해야하는데...
        private void TempLowPassFilter(List<HumanJoint> newHuman)
        {
            for (int h = 0; h<newHuman.Count; h++)
            {
                for (int j = 0; j<newHuman[0].jointMax; j++)
                {
                    // 하드 코딩
                    newHuman[h].viewportJointPositions[j] =
                        (lagacyHumanList[0][h].viewportJointPositions[j] * lowPassParamList[0]) +
                        (lagacyHumanList[1][h].viewportJointPositions[j] * lowPassParamList[1]) +
                        (lagacyHumanList[2][h].viewportJointPositions[j] * lowPassParamList[2]) +
                        (lagacyHumanList[3][h].viewportJointPositions[j] * lowPassParamList[3]) +
                        (lagacyHumanList[4][h].viewportJointPositions[j] * lowPassParamList[4]) +
                        (lagacyHumanList[5][h].viewportJointPositions[j] * lowPassParamList[5]) +
                        (lagacyHumanList[6][h].viewportJointPositions[j] * lowPassParamList[6]) +
                        (lagacyHumanList[7][h].viewportJointPositions[j] * lowPassParamList[7]) +
                        (lagacyHumanList[8][h].viewportJointPositions[j] * lowPassParamList[8]) +
                        (lagacyHumanList[9][h].viewportJointPositions[j] * lowPassParamList[9]) +
                        (newHuman[h].viewportJointPositions[j] * lowPassParamList[10]);
                }
            }
        }
    }


    // 연속되는 프레임의 관절에 칼만 필터를 적용하는 static 클래스 : 부드러운 움직임 효과
    public static class JointKalmanFilter
    {
        const float KalmanParamQ = 0.001f;
        const float KalmanParamR = 0.0015f;

        public static void KalmanFiltering()
        {

        }

        public static void MeasurementUpdate()
        {

        }
    }


    // 연속되는 프레임의 관절에 저주파 통과 필터를 적용하는 static 클래스 : 블러링 효과
    public static class JointLowPassFilter
    {
        const float LowPassParam = 0.1f;

        public static void LowPassFiltering()
        {

        }
    }


    // 한 프레임에서 유효한 관절 개수가 일정 이상인 사람의 관절만 남기는 static 클래스
    public static class JointPerfectFilter
    {
        
        // 관절 체계 : 한 사람당 관절 개수, 만족해야 하는 유효 관절 개수 - 입력 추가
        public static void PerfectFiltering()
        {

        }
    }


    // 관절 사이에 빈 관절 계산해서 생성하는 static 클래스
    public static class EmptyJointLerpGenerator
    {
        // HumanJoint 2개 받아서 보간한 결과로 새로운 HumanJoint 인스턴스를 생성하고 반환하는 함수
        // 사람 번호 바뀌는 것 방지 추가할 예정
        public static List<HumanJoint> JointLerpGenerator(List<HumanJoint> a, List<HumanJoint> b, float t, int jointMax)
        {
            List<HumanJoint> result = new List<HumanJoint>();

            for (int i = 0; i < a.Count; i++) // 사람 수 만큼 반복
            {
                List<Vector2> tempJoints = new List<Vector2>();
                List<float> tempScores = new List<float>();
                for (int j = 0; j < jointMax; j++)   // 관절 좌표 개수만큼 반복
                {
                    // 둘 중에 하나 빈 좌표면 보간 안함
                    if (a[i].viewportJointPositions[j].Equals(JointData.EmptyVector) || b[i].viewportJointPositions[j].Equals(JointData.EmptyVector))
                    {
                        tempJoints.Add(a[i].viewportJointPositions[j]);
                    }

                    // 관절 거리가 너무 멀 경우에는 보간 안함
                    else if (Vector2.Distance(a[i].viewportJointPositions[j], b[i].viewportJointPositions[j]) > 0.05f)
                    {
                        tempJoints.Add(a[i].viewportJointPositions[j]);
                    }

                    // 둘 사이 보간함
                    else
                    {
                        tempJoints.Add(Vector2.Lerp(a[i].viewportJointPositions[j], b[i].viewportJointPositions[j], t));
                    }

                    // 보간된 좌표는 정확도 점수 0으로 세팅
                    tempScores.Add(0f);
                }
                result.Add(new HumanJoint(tempJoints, tempScores, jointMax));
            }

            return result;
        }
    }
   

    // FastPose나 AlphaPose 등에서 처리한 결과가 들어왔을 때 들어온 관절 리스트를 관절 15개 기준으로 재배열해주는 static 클래스
    public static class JointParser
    {
        // jointParsing 용
        static List<float> newScores = new List<float>();
        static List<float> temp_xs = new List<float>();
        static List<float> temp_ys = new List<float>();
        static float temp_x;
        static float temp_y;

        // Bytes2Human3DJointList용
        const int HUMAN_MAX = 10;   // 최대 10명
        static int tempByteOffset;
        static int tempByteCount;
        static int tempIndex;
        static int tempJointIndex;

        static int[] bboxArray = new int[HUMAN_MAX * 4];
        static float[] jointPositionArray = new float[HUMAN_MAX * Joint3DData.POSITION_JOINT_MAX * 3];
        static float[] jointAngleArray = new float[HUMAN_MAX * Joint3DData.ANGLE_JOINT_MAX * 3];




        // 서버에서 온 관절 데이터 byte[]를 HumanJointList로 파싱까지 해서 반환하는 함수
        public static List<HumanJoint> Bytes2HumanJointList(byte[] jointByte, int jointNumbers, bool usePerfectOption, bool useJointParsing)
        {
            List<Vector2> coordList = new List<Vector2>();
            List<float> scoreList = new List<float>();

            float[] floatArray = new float[jointByte.Length / 4];
            Buffer.BlockCopy(jointByte, 0, floatArray, 0, jointByte.Length);

            for (int i = 0; i < floatArray.Length; i+=3)
            {
                coordList.Add(new Vector2(floatArray[i], 1f - floatArray[i + 1]));
                scoreList.Add(floatArray[i + 2]);
            }

            List<HumanJoint> result = JointParsing(coordList, scoreList, jointNumbers, usePerfectOption, useJointParsing);

            //coordList.Clear();
            //scoreList.Clear();

            return result;
        }

        // 작업 필요 : BBOX, Position, Angle 추출
        // 서버에서 온 관절 데이터 byte[]를 HumanJointList로 파싱까지 해서 반환하는 함수
        public static List<Human3DJoint> Bytes2Human3DJointList(byte[] inputByte, int people)
        {
            List<Human3DJoint> result = new List<Human3DJoint>();
            tempByteOffset = 0;
            tempByteCount = 0;

            // bboxArray need bytes = people count * 4 data(w, y, width, height) * int32(4 bytes)
            tempByteCount = (people * 4 * 4);
            Buffer.BlockCopy(inputByte, tempByteOffset, bboxArray, 0, tempByteCount);
            tempByteOffset += tempByteCount;

            // jointPositionArray need bytes = people count * 49 joints * 3 vector(x, y, z) * float32(4 bytes)
            tempByteCount = (people * Joint3DData.POSITION_JOINT_MAX * 3 * 4);
            Buffer.BlockCopy(inputByte, tempByteOffset, jointPositionArray, 0, tempByteCount);
            tempByteOffset += tempByteCount;

            // jointAngleArray need bytes = people count * 24 joints * 3 vector(x, y, z) * float32(4 bytes)
            tempByteCount = people * Joint3DData.ANGLE_JOINT_MAX * 3 * 4;
            Buffer.BlockCopy(inputByte, tempByteOffset, jointAngleArray, 0, tempByteCount);

            // 한 명씩 끊어서 Human3DJoint를 만든 후 List<Human3DJoint>에 추가하기
            for (int i = 0; i < people; i++)
            {
                var bbox = new List<int>();
                var position = new List<Vector3>();
                var angle = new List<Vector3>();

                // Human BBox 데이터 옮기기
                tempIndex = i * 4;
                for(int b = 0; b < 4; b++)
                {
                    bbox.Add(bboxArray[tempIndex + b]);
                }

                // Joint Position 데이터 옮기기
                tempIndex = i * Joint3DData.POSITION_JOINT_MAX * 3;
                for(int p = 0; p < Joint3DData.POSITION_JOINT_MAX; p++)
                {
                    tempJointIndex = p * 3;
                    position.Add(new Vector3(
                        jointPositionArray[tempIndex + tempJointIndex],
                        480f - jointPositionArray[tempIndex + tempJointIndex + 1],
                        jointPositionArray[tempIndex + tempJointIndex + 2]
                    ));
                }

                // Joint Angle 데이터 옮기기
                tempIndex = i * Joint3DData.ANGLE_JOINT_MAX * 3;
                for(int a = 0; a < Joint3DData.ANGLE_JOINT_MAX; a++)
                {
                    tempJointIndex = a * 3;
                    angle.Add(new Vector3(
                        jointAngleArray[tempIndex + tempJointIndex],
                        jointAngleArray[tempIndex + tempJointIndex + 1],
                        jointAngleArray[tempIndex + tempJointIndex + 2]
                    ));
                }

                // 결과 리스트에 사람 하나 추가
                result.Add(new Human3DJoint(bbox, position, angle));
            }

            return result;
        }

        // 걍 쓰자
        // 서버에서 온 관절 데이터를 지정된 관절 체계로 파싱하는 함수, 약간의 후처리도 여기서 하고 있음
        public static List<HumanJoint> JointParsing(List<Vector2> joints, List<float> scores, int jointNumbers, bool perfectOption, bool useTargetJoint)
        {
            // 입력된 NN_OPTION에 따라 각각 알맞은 관절 재배열 함수 호출
            if (jointNumbers == JointData.FAST_JOINT_MAX) { Fast2Target(joints, scores); }
            else if (jointNumbers == JointData.ALPHA_JOINT_MAX) { Alpha2Target(joints, scores); }
            else { Debug.LogError("JointParsing Error"); }

            // 서버에서 넘어온 처리 결과를 한 사람씩 끊어서(관절 15개) 반환하기 위한 리스트 생성
            List<HumanJoint> humanJointList = new List<HumanJoint>();

            // 한 사람당 할당할 관절 개수 설정
            int jointMax;

            // 타겟 관절 체계 사용 시
            if (useTargetJoint)
            {
                jointMax = JointData.TARGET_JOINT_MAX;
            }

            // 원본 관절 체계 사용 시
            else
            {
                jointMax = jointNumbers;
            }

            // 한 프레임의 모든 사람들의 좌표들을 한사람씩(지정한 관절 개수 만큼) 끊어서 새로 생성
            for (int i = 0; i < (joints.Count / jointMax); i++)
            {
                int emptyCount = 0;
                List<Vector2> tempJoints = new List<Vector2>();
                List<float> tempScores = new List<float>();

                // 한 명씩 관절이랑 점수 리스트 끊기
                for (int j = 0; j < jointMax; j++)
                {
                    tempJoints.Add(joints[(i * jointMax) + j]);
                    if (joints[(i * jointMax) + j].Equals(JointData.EmptyVector)) { emptyCount++; }
                    tempScores.Add(scores[(i * jointMax) + j]);
                }

                // 관절 꽉 차있는 인간만 전송
                if (perfectOption)
                {
                    if (emptyCount == 0)
                    {
                        humanJointList.Add(new HumanJoint(tempJoints, tempScores, jointMax));
                    }
                }

                // 관절 꽉 차지 않아도 전송
                else
                {
                    if (emptyCount < jointMax)
                    {
                        humanJointList.Add(new HumanJoint(tempJoints, tempScores, jointMax));
                    }
                }
            }

            return humanJointList;
        }

        // FastPose의 처리 결과는 한 사람 당 13개 관절을 가지기 때문에, 관절 13개 타입의 리스트를 15개 타입의 리스트로 변환해주는 함수
        public static void Fast2Target(List<Vector2> joints, List<float> scores)
        {
            newScores.Clear();
            temp_xs.Clear();
            temp_ys.Clear();

            for (int i = 0; i < (joints.Count / JointData.FAST_JOINT_MAX); i++)
            {
                // Nose
                temp_xs.Add(joints[(i * JointData.FAST_JOINT_MAX) + 0].x);
                temp_ys.Add(joints[(i * JointData.FAST_JOINT_MAX) + 0].y);
                newScores.Add(scores[i * JointData.FAST_JOINT_MAX + 0]);

                // Neck
                temp_x = (joints[(i * JointData.FAST_JOINT_MAX) + 1].x + joints[(i * JointData.FAST_JOINT_MAX) + 2].x) * 0.5f;
                temp_y = (joints[(i * JointData.FAST_JOINT_MAX) + 1].y + joints[(i * JointData.FAST_JOINT_MAX) + 2].y) * 0.5f;
                temp_xs.Add(temp_x);
                temp_ys.Add(temp_y);
                newScores.Add((scores[i * JointData.FAST_JOINT_MAX + 1] + scores[i * JointData.FAST_JOINT_MAX + 2]) * 0.5f);

                // Heart
                if (joints[(i * JointData.FAST_JOINT_MAX) + 1] == JointData.EmptyVector ||
                    joints[(i * JointData.FAST_JOINT_MAX) + 2] == JointData.EmptyVector ||
                    joints[(i * JointData.FAST_JOINT_MAX) + 7] == JointData.EmptyVector ||
                    joints[(i * JointData.FAST_JOINT_MAX) + 8] == JointData.EmptyVector)
                {
                    temp_x = JointData.EmptyVector.x;
                    temp_y = JointData.EmptyVector.y;
                }
                else
                {
                    temp_x = (joints[(i * JointData.FAST_JOINT_MAX) + 1].x + joints[(i * JointData.FAST_JOINT_MAX) + 2].x + joints[(i * JointData.FAST_JOINT_MAX) + 7].x + joints[(i * JointData.FAST_JOINT_MAX) + 8].x) * 0.25f;
                    temp_y = (joints[(i * JointData.FAST_JOINT_MAX) + 1].y + joints[(i * JointData.FAST_JOINT_MAX) + 2].y + joints[(i * JointData.FAST_JOINT_MAX) + 7].y + joints[(i * JointData.FAST_JOINT_MAX) + 8].y) * 0.25f;
                }
                temp_xs.Add(temp_x);
                temp_ys.Add(temp_y);
                newScores.Add((scores[i * JointData.FAST_JOINT_MAX + 1] + scores[i * JointData.FAST_JOINT_MAX + 2] + scores[i * JointData.FAST_JOINT_MAX + 7] + scores[i * JointData.FAST_JOINT_MAX + 8]) * 0.25f);

                // Shoulder ~ Ankle
                for (int j = 1; j < 13; j++)
                {
                    temp_xs.Add(joints[(i * JointData.FAST_JOINT_MAX) + j].x);
                    temp_ys.Add(joints[(i * JointData.FAST_JOINT_MAX) + j].y);
                    newScores.Add(scores[i * JointData.FAST_JOINT_MAX + j]);
                }
            }

            // 재배열된 벡터로 버퍼의 벡터 리스트 채워넣기
            joints.Clear();
            scores.Clear();
            for (int i = 0; i < temp_xs.Count; i++)
            {
                joints.Add(new Vector2(temp_xs[i], temp_ys[i]));
                scores.Add(newScores[i]);
            }
        }

        // AlphaPose의 처리 결과는 한 사람 당 18개 관절을 가지기 때문에, 관절 18개 타입의 리스트를 15개 타입의 리스트로 변환해주는 함수
        public static void Alpha2Target(List<Vector2> joints, List<float> scores)
        {
            newScores.Clear();
            temp_xs.Clear();
            temp_ys.Clear();

            for (int i = 0; i < (joints.Count / JointData.ALPHA_JOINT_MAX); i++)
            {
                // Nose
                temp_xs.Add(joints[(i * JointData.ALPHA_JOINT_MAX) + 0].x);
                temp_ys.Add(joints[(i * JointData.ALPHA_JOINT_MAX) + 0].y);
                newScores.Add(scores[(i * JointData.ALPHA_JOINT_MAX) + 0]);

                // Neck
                if (joints[(i * JointData.ALPHA_JOINT_MAX) + 5].Equals(JointData.EmptyVector) ||
                joints[(i * JointData.ALPHA_JOINT_MAX) + 6].Equals(JointData.EmptyVector))
                {
                    temp_x = JointData.EmptyVector.x;
                    temp_y = JointData.EmptyVector.y;
                }
                else
                {
                    temp_x = (joints[(i * JointData.ALPHA_JOINT_MAX) + 5].x + joints[(i * JointData.ALPHA_JOINT_MAX) + 6].x) * 0.5f;
                    temp_y = (joints[(i * JointData.ALPHA_JOINT_MAX) + 5].y + joints[(i * JointData.ALPHA_JOINT_MAX) + 6].y) * 0.5f;
                }
                temp_xs.Add(temp_x);
                temp_ys.Add(temp_y);
                newScores.Add((scores[(i * JointData.ALPHA_JOINT_MAX) + 5] + scores[(i * JointData.ALPHA_JOINT_MAX) + 6]) * 0.5f);

                // Heart
                if (joints[(i * JointData.ALPHA_JOINT_MAX) + 5].Equals(JointData.EmptyVector) ||
                    joints[(i * JointData.ALPHA_JOINT_MAX) + 6].Equals(JointData.EmptyVector) ||
                    joints[(i * JointData.ALPHA_JOINT_MAX) + 11].Equals(JointData.EmptyVector) ||
                    joints[(i * JointData.ALPHA_JOINT_MAX) + 12].Equals(JointData.EmptyVector))
                {
                    temp_x = JointData.EmptyVector.x;
                    temp_y = JointData.EmptyVector.y;
                }
                else
                {
                    temp_x = (joints[(i * JointData.ALPHA_JOINT_MAX) + 5].x + joints[(i * JointData.ALPHA_JOINT_MAX) + 6].x + joints[(i * JointData.ALPHA_JOINT_MAX) + 11].x + joints[(i * JointData.ALPHA_JOINT_MAX) + 12].x) * 0.25f;
                    temp_y = (joints[(i * JointData.ALPHA_JOINT_MAX) + 5].y + joints[(i * JointData.ALPHA_JOINT_MAX) + 6].y + joints[(i * JointData.ALPHA_JOINT_MAX) + 11].y + joints[(i * JointData.ALPHA_JOINT_MAX) + 12].y) * 0.25f;
                }
                temp_xs.Add(temp_x);
                temp_ys.Add(temp_y);
                newScores.Add((scores[(i * JointData.ALPHA_JOINT_MAX) + 5] + scores[(i * JointData.ALPHA_JOINT_MAX) + 6] + scores[(i * JointData.ALPHA_JOINT_MAX) + 11] + scores[(i * JointData.ALPHA_JOINT_MAX) + 12]) * 0.25f);

                // Shoulder ~ Ankle
                for (int j = 5; j < 17; j++)
                {
                    temp_xs.Add(joints[(i * JointData.ALPHA_JOINT_MAX) + j].x);
                    temp_ys.Add(joints[(i * JointData.ALPHA_JOINT_MAX) + j].y);
                    newScores.Add(scores[(i * JointData.ALPHA_JOINT_MAX) + j]);
                }
            }

            // 재배열된 벡터로 버퍼의 벡터 리스트 채워넣기
            joints.Clear();
            scores.Clear();

            for (int i = 0; i < temp_xs.Count; i++)
            {
                joints.Add(new Vector2(temp_xs[i], temp_ys[i]));
                scores.Add(newScores[i]);
            }
        }
    }


    /// <summary>
    /// Human Segmentation 관련 처리하는 static 클래스
    /// </summary>
    public static class HumanSegMaskProcessor
    {
        // 값이 0으로 채워진 Alpha용 Texture2D를 생성해서 반환하는 함수
        public static Texture2D CreateAlphaTexture_0(int width, int height)
        {
            Texture2D result = new Texture2D(width, height, TextureFormat.R8, false);
            Mat mask = Mat.zeros(height, width, CvType.CV_8UC1);
            Utils.fastMatToTexture2D(mask, result);
            result.Apply();
            return result;
        }

        // 값이 255로 채워진 Alpha용 Texture2D를 생성해서 반환하는 함수
        public static Texture2D CreateAlphaTexture_255(int width, int height)
        {
            Texture2D result = new Texture2D(width, height, TextureFormat.R8, false);
            Mat mask = new Mat(new Size(width, height), CvType.CV_8UC1, new Scalar(255));
            Utils.fastMatToTexture2D(mask, result);
            result.Apply();
            return result;
        }

        // 값이 255로 채워진 Alpha용 Mat을 생성해서 반환하는 함수
        public static Mat CreateAlphaMat_255(int height, int width)
        {
            Mat mask = new Mat(new Size(width, height), CvType.CV_8UC1, new Scalar(255));
            return mask;
        }

        /// <summary>
        /// jpg bytes의 마스크 데이터를 Mat 타입으로 변환하여 반환하는 함수
        /// </summary>
        /// <param name="maskByte">마스크 데이터의 바이트 배열</param>
        /// <param name="width">마스크 데이터의 가로 크기</param>
        /// <param name="heigth">마스크 데이터의 세로 크기</param>
        /// <returns></returns>
        public static Mat Bytes2MatMask(byte[] maskByte, int width, int height)
        {
            MatOfByte mob = new MatOfByte();
            mob.fromArray(maskByte);
            Mat resultMask = Imgcodecs.imdecode(mob, Imgcodecs.IMWRITE_JPEG_QUALITY);
            Imgproc.cvtColor(resultMask, resultMask, Imgproc.COLOR_BGR2GRAY);
            return resultMask;
        }

        /// <summary>
        /// 마스크를 Texture2D로 변환하는 함수
        /// 변환된 마스크 Texture를 바로 AlphaTexture로 적용 가능
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="maskTex"></param>
        public static void MaskProcessing(Mat mask, Texture2D maskTex)
        {
            Utils.fastMatToTexture2D(mask, maskTex);
            maskTex.Apply();
        }

        // 네모 일정 구역 안에 한정해서 사람을 투명하게 마스킹하는 함수 : 옥외광고용, 분리 필요
        public static void InverseAreaMaskProcessing(Mat mask, Texture2D maskTex, RawImage human, int width, int height, Vector3 mouseStartVector, Vector3 mouseEndVector, Texture2D originalFrameTexture, Texture2D quadTexture, Texture2D quadMaskTexture, GameObject quad)
        {
            // 스크린 좌표 체계를 픽셀 좌표 체계로 변환 : y축 반전
            Vector3 pixel_mouseStartVector = new Vector3(mouseStartVector.x, (Screen.height - mouseStartVector.y), 0f);
            Vector3 pixel_mouseEndVector = new Vector3(mouseEndVector.x, (Screen.height - mouseEndVector.y), 0f);

            int areaStartX;
            int areaStartY;
            int areaEndX;
            int areaEndY;

            // 네모 모양의 구역 좌표 계산 : X 좌표들
            if (pixel_mouseStartVector.x < pixel_mouseEndVector.x)
            {
                areaStartX = (int)pixel_mouseStartVector.x;
                areaEndX = (int)pixel_mouseEndVector.x;
            }
            else
            {
                areaStartX = (int)pixel_mouseEndVector.x;
                areaEndX = (int)pixel_mouseStartVector.x;
            }

            // 네모 모양의 구역 좌표 계산 : Y 좌표들
            if (pixel_mouseStartVector.y < pixel_mouseEndVector.y)
            {
                areaStartY = (int)pixel_mouseStartVector.y;
                areaEndY = (int)pixel_mouseEndVector.y;
            }
            else
            {
                areaStartY = (int)pixel_mouseEndVector.y;
                areaEndY = (int)pixel_mouseStartVector.y;
            }

            // 마스크 사이즈(원본 영상 크기)에 맞게 좌표 사이즈 조정 : 실제 마스크 좌표로 변환
            float tempStartX = ((float)areaStartX / (float)Screen.width) * width;
            float tempStartY = ((float)areaStartY / (float)Screen.height) * height;
            float tempEndX = ((float)areaEndX / (float)Screen.width) * width;
            float tempEndY = ((float)areaEndY / (float)Screen.height) * height;

            // 실시간 프레임 마스크 초기화
            maskTex = new Texture2D(width, height, TextureFormat.R8, false);
            human.material.SetTexture("_AlphaTex", maskTex);

            // int 형태로 드래그 구역 사이즈 저장
            areaStartX = (int)tempStartX;
            areaStartY = (int)tempStartY;
            areaEndX = (int)tempEndX;
            areaEndY = (int)tempEndY;

            // 영역 크기 계산
            int areaWidth = areaEndX - areaStartX;
            int areaHeight = areaEndY - areaStartY;
            Debug.Log("area width : " + areaWidth + ", areaHeight : " + areaHeight);

            // 오브젝트 텍스처, 오브젝트 마스크 텍스처 생성
            quadTexture = new Texture2D(areaWidth, areaHeight, TextureFormat.RGB24, false);
            quadMaskTexture = new Texture2D(areaWidth, areaHeight, TextureFormat.R8, false);

            // Quad Renderer 받기
            Vector3 lt = Camera.main.ViewportToWorldPoint(new Vector3(tempStartX / width, 1f - tempStartY / height, 10f));
            Vector3 lb = Camera.main.ViewportToWorldPoint(new Vector3(tempStartX / width, 1f - tempEndY / height, 10f));
            Vector3 rt = Camera.main.ViewportToWorldPoint(new Vector3(tempEndX / width, 1f - tempStartY / height, 10f));
            Vector3 rb = Camera.main.ViewportToWorldPoint(new Vector3(tempEndX / width, 1f - tempEndY / height, 10f));

            //Create Quad.
            Vector3[] vertices = new Vector3[] { lt, rt, lb, rb };
            Vector2[] uvs = new Vector2[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
            int[] tri = new int[] { 0, 1, 2, 1, 3, 2 };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = tri;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = quad.GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            var quadRenderer = quad.GetComponent<MeshRenderer>();

            quadRenderer.material.SetTexture("_MainTex", quadTexture);
            quadRenderer.material.SetTexture("_AlphaTex", quadMaskTexture);
            //quadRenderer.transform.localScale = new Vector3(areaWidth * 0.13f, areaHeight * 0.1f, 1f);

            // 원본 프레임, 원본 마스크, 오브젝트 프레임, 오브젝트 마스크 픽셀값 Get
            Color[] originalTextureColors = originalFrameTexture.GetPixels();
            Color[] realFrameMaskColors = maskTex.GetPixels();
            Color[] objectFrameColors = quadTexture.GetPixels();
            Color[] objectMaskColors = quadMaskTexture.GetPixels();

            // 실시간 프레임 마스킹
            int w = 0;
            int h = 0;
            try
            {
                for (w = 0; w < width; w++)
                {
                    for (h = 0; h < height; h++)
                    {
                        // 구역 내부일 경우 : 사람이면 투명하게 마스킹 함
                        if (w >= areaStartX && w < areaEndX && h > tempStartY && h < areaEndY)
                        {
                            // 실시간 프레임 마스킹
                            double[] data = mask.get(h, w);
                            realFrameMaskColors[((height - 1) - h) * width + w].r = data[0] == 0 ? 255 : 0;

                            // 게임 오브젝트 프레임 텍스처 복사
                            objectFrameColors[((areaHeight - 1) - h + areaStartY) * areaWidth + (w - areaStartX)] = originalTextureColors[((height - 1) - h) * width + w];

                            // 게임 오브젝트 프레임 마스킹
                            objectMaskColors[((areaHeight - 1) - h + areaStartY) * areaWidth + (w - areaStartX)].r = data[0] == 0 ? 0 : 255;
                        }

                        // 구역 외부일 경우 : 불투명하게 마스킹 함
                        else
                        {
                            realFrameMaskColors[((height - 1) - h) * width + w].r = 255;
                        }
                    }
                }
            }
            catch
            {
                Debug.LogError("h : " + h + ", w : " + w + ", Start X, Y : (" + areaStartX + ", " + areaStartY + ") / End X, Y : (" + areaEndX + ", " + areaEndY + ")");
            }

            // 각 텍스처 변경 사항 저장
            maskTex.SetPixels(realFrameMaskColors);
            maskTex.Apply();

            quadTexture.SetPixels(objectFrameColors);
            quadTexture.Apply();

            quadMaskTexture.SetPixels(objectMaskColors);
            quadMaskTexture.Apply();

            // quad 생성 위치 지정 : start-end vector 평균으로 position 지정
            //Vector3 center = new Vector3((mouseStartVector.x + mouseEndVector.x) * 0.5f, (mouseStartVector.y + mouseEndVector.y) * 0.5f, 50f);
            //quad.transform.position = Camera.main.ScreenToWorldPoint(center);
        }
    }
}