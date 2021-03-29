using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 *          (Joints)            (Object)
 * (0)      Nose                Head (머리)
 * (1)      Neck                Upperarm_L (이두박근)
 * (2)      BodyCenter          Upperarm_R (이두박근)
 * (3)      LShoulder           Forearm_L (전완근)
 * (4)      RShoulder           Forearm_R (전완근)
 * (5)      LElbow              Hand_L (손)
 * (6)      RElbow              Hand_R (손)
 * (7)      LWrist              Thigh_L (허벅지)
 * (8)      RWrist              Thigh_R (허벅지)
 * (9)      LHip                Calf_L (종아리)
 * (10)     RHip                Calf_R (종아리)
 * (11)     LKnee               Foot_L (발)
 * (12)     RKnee               Foot_R (발)
 * (13)     LAnkle              Torso (몸통)
 * (14)     RAnkle              
 * 
 */

namespace CellBig.Module.HumanDetection
{
    public class HumanCharacterSample : MonoBehaviour
    {
        // 지정된 오브젝트, 관절 개수 및 AR 2D 텍스처 타입 옵션
        int arType;

        // 한 인간이 가지는 2D 텍스처 게임 오브젝트를 저장할 리스트, 리스트 길이는 JOINT_MAX와 일치
        public List<GameObject> jointObjectList = new List<GameObject>();

        // 각 프리팹의 관절 별로 object 사이즈를 저장한 리스트, 사이즈 조정 필요
        // 오브젝트의 이름, 사이즈 리스트 순서 = { 머리, L이두박근, R이두박근, L전완근, R전완근, L손, R손, L허벅지, R허벅지, L종아리, R종아리, L발, R발, 몸통 }
        List<float> cb_object_size_list = new List<float> { 4f, 2f, 2f, 2f, 2f, 2f, 2f, 1.5f, 1.5f, 1.5f, 1.5f, 2f, 2f, 0.4f };   // creepboy size list
        List<float> iron_object_size_list = new List<float> { 6f, 1.5f, 1.5f, 2f, 2f, 2f, 2f, 1f, 1f, 1f, 1f, 2f, 2f, 0.4f }; // ironman size list

        // 각 프리팹의 저장 위치
        string iron_directory = "ironman/prefabs/";
        string cb_directory = "creepyboy/prefabs/";

        // CreepyBoy 관절 프리팹 14개의 이름 리스트 : torso-몸통, thigh-허벅지, calf-종아리
        List<string> cb_obj_name_list = new List<string> {
        "sp_head01", "sp_upperarm_L", "sp_upperarm_R", "sp_forearm_L", "sp_forearm_R",
        "sp_hand_L", "sp_hand_R",  "sp_thigh_L", "sp_thigh_R", "sp_calf_L", "sp_calf_R",
        "sp_foot_L", "sp_foot_R", "sp_torso01"
        };

        // IronMan 관절 프리팹 14개 이름 리스트 : torso-몸통, thigh-허벅지, calf-종아리
        List<string> iron_obj_name_list = new List<string> {
        "irm_head01", "irm_upperarm_L", "irm_upperarm_R", "irm_forearm_L", "irm_forearm_R",
        "irm_hand_L", "irm_hand_R", "irm_thigh_L", "irm_thigh_R", "irm_calf_L", "irm_calf_R",
        "irm_foot_L", "irm_foot_R", "irm_torso01"
        };

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

        public enum ObjectType : int
        {
            Head = 0,
            LUpperarm = 1,
            RUpperarm = 2,
            LForearm = 3,
            RForearm = 4,
            LHand = 5,
            RHand = 6,
            LThigh = 7,
            RThigh = 8,
            LCalf = 9,
            RCalf = 10,
            LFoot = 11,
            RFoot = 12,
            Torso = 13
        }


        // 타입 받아서 JOINT_MAX 만큼 게임오브젝트 생성
        public void Init(int arType)
        {
            this.arType = arType;
            
            // 지정한 옵션에 맞는 프리팹 불러와서 생성 후 이름 지정, 부모 설정 후 리스트에 추가
            for (int i = 0; i < JointContentsOption.Sprite_Max; i++)
            {
                if (arType == JointContentsOption.SpriteType_CreepyBoy)
                {
                    var part = Resources.Load<GameObject>(cb_directory + cb_obj_name_list[i]);
                    jointObjectList.Add(Instantiate(part, new Vector3(0f, 1f, 7f), Quaternion.identity));
                    jointObjectList[i].name = cb_obj_name_list[i];
                    jointObjectList[i].transform.parent = this.gameObject.transform;
                }

                else if (arType == JointContentsOption.SpriteType_IronMan)
                {
                    var part = Resources.Load<GameObject>(iron_directory + iron_obj_name_list[i]);
                    jointObjectList.Add(Instantiate(part, new Vector3(0f, 1f, 7f), Quaternion.identity));
                    jointObjectList[i].name = iron_obj_name_list[i];
                    jointObjectList[i].transform.parent = this.gameObject.transform;
                }
            }
        }

        // 스스로 비활성화 하는 함수
        public void DeactivateSelf()
        {
            if (this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(false);
            }
        }

        // 스스로 활성화 하는 함수
        public void ActivateSelf()
        {
            if (!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
            }
        }
        
        // 새로운 포지션 입력 받고 위치 새로 지정해주는 함수
        public void MovePosition(List<Vector2> newJoints, float globalSizeOption)
        {
            List<bool> validJointsList = new List<bool>();      // 각 관절별 유효 여부 리스트, 개수 : JOINT_MAX
            List<bool> validObjectList = new List<bool>();      // 각 오브젝트별 유효 여부 리스트, 개수 : OBJECT_MAX
            List<float> distance = new List<float>();           // 각 오브젝트에 사용되는 vector distance 리스트, 개수 : OBJECT_MAX


            // 관절 좌표 한바퀴 돌면서 유효한지 검사
            for (int i = 0; i<JointData.TARGET_JOINT_MAX; i++)
            {
                // 빈 관절인 경우
                if (newJoints[i].Equals(JointData.EmptyVector))
                {
                    validJointsList.Add(false);
                }

                // 유효한 관절인 경우
                else
                {
                    validJointsList.Add(true);
                }
            }


            // 관절 오브젝트 유효 여부 리스트 초기화
            for (int i = 0; i < JointContentsOption.Sprite_Max; i++)
            {
                validObjectList.Add(true);
            }


            // 머리 : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.Nose] || !validJointsList[(int)JointType.Neck] || !validJointsList[(int)JointType.BodyCenter]) { validObjectList[(int)ObjectType.Head] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.Neck], newJoints[(int)JointType.BodyCenter]) * 0.3f);

            // 이두박근 L : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.LShoulder] || !validJointsList[(int)JointType.LElbow]) { validObjectList[(int)ObjectType.LUpperarm] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.LShoulder], newJoints[(int)JointType.LElbow]));

            // 이두박근 R : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.RShoulder] || !validJointsList[(int)JointType.RElbow]) { validObjectList[(int)ObjectType.RUpperarm] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.RShoulder], newJoints[(int)JointType.RElbow]));

            // 전완근 L : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.LElbow] || !validJointsList[(int)JointType.LWrist]) { validObjectList[(int)ObjectType.LForearm] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.LElbow], newJoints[(int)JointType.LWrist]));

            // 전완근 R : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.RElbow] || !validJointsList[(int)JointType.RWrist]) { validObjectList[(int)ObjectType.RForearm] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.RElbow], newJoints[(int)JointType.RWrist]));

            // 손 L : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.LElbow] || !validJointsList[(int)JointType.LWrist]) { validObjectList[(int)ObjectType.LHand] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.Neck], newJoints[(int)JointType.BodyCenter]) * 0.6f);

            // 손 R : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.RElbow] || !validJointsList[(int)JointType.RWrist]) { validObjectList[(int)ObjectType.RHand] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.Neck], newJoints[(int)JointType.BodyCenter]) * 0.6f);

            // 허벅지 L : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.LHip] || !validJointsList[(int)JointType.LKnee]) { validObjectList[(int)ObjectType.LThigh] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.LHip], newJoints[(int)JointType.LKnee]));

            // 허벅지 R : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.RHip] || !validJointsList[(int)JointType.RKnee]) { validObjectList[(int)ObjectType.RThigh] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.RHip], newJoints[(int)JointType.RKnee]));

            // 종아리 L : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.LKnee] || !validJointsList[(int)JointType.LAnkle]) { validObjectList[(int)ObjectType.LCalf] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.LKnee], newJoints[(int)JointType.LAnkle]));

            // 종아리 R : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.RKnee] || !validJointsList[(int)JointType.RAnkle]) { validObjectList[(int)ObjectType.RCalf] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.RKnee], newJoints[(int)JointType.RAnkle]));

            // 발 L : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.LAnkle]) { validObjectList[(int)ObjectType.LFoot] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.Neck], newJoints[(int)JointType.BodyCenter]) * 0.6f);

            // 발 R : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.RAnkle]) { validObjectList[(int)ObjectType.RFoot] = false; }
            distance.Add(Vector3.Distance(newJoints[(int)JointType.Neck], newJoints[(int)JointType.BodyCenter]) * 0.6f);

            // 몸통 : 빈 좌표 검사, 거리 측정
            if (!validJointsList[(int)JointType.Nose] || !validJointsList[(int)JointType.Neck] || !validJointsList[(int)JointType.LShoulder] || !validJointsList[(int)JointType.RShoulder] 
                || !validJointsList[(int)JointType.LHip] || !validJointsList[(int)JointType.RHip] || !validJointsList[(int)JointType.LKnee] || !validJointsList[(int)JointType.RKnee])
                { validObjectList[(int)ObjectType.Torso] = false; }
            distance.Add(Mathf.Sqrt(Vector3.Distance(newJoints[(int)JointType.LShoulder], newJoints[(int)JointType.RHip])) * 0.7f);
            if (!validObjectList[(int)ObjectType.Torso]) { validObjectList[(int)ObjectType.Head] = false; }



            // 좌표, 거리를 계산하는 데 필요한 좌표들이 모두 유효하면 활성화 후 연산, 유효하지 않으면 해당 관절 오브젝트 비활성화
            for (int i = 0; i < JointContentsOption.Sprite_Max; i++)
            {
                if (validObjectList[i])
                {
                    // 활성화
                    jointObjectList[i].SetActive(true);
                    
                    // 관절 별로 좌표 지정
                    if (i == (int)ObjectType.Head) { jointObjectList[i].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[1] + new Vector3(0, 0, 5f)); }            // 머리인 경우
                    else if (i == (int)ObjectType.Torso) { jointObjectList[i].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[2] + new Vector3(0, 0, 5f)); }      // 몸통인 경우
                    else { jointObjectList[i].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[i+2] + new Vector3(0, 0, 5f)); }                 // 그 외의 경우

                    // 관절 별로 크기 지정
                    if(arType == 0) { jointObjectList[i].transform.localScale = new Vector3(jointObjectList[i].transform.localScale.x < 0 ? -1 : 1, 1f, 1f) * cb_object_size_list[i] * distance[i] * globalSizeOption; }
                    else if (arType == 1) { jointObjectList[i].transform.localScale = new Vector3(jointObjectList[i].transform.localScale.x < 0 ? -1 : 1, 1f, 1f) * iron_object_size_list[i] * distance[i] * globalSizeOption; }
                }

                else
                {
                    // 비활성화
                    jointObjectList[i].SetActive(false);
                }
            }

            
            // 물체 바라보는 방향 수정 : 머리[0], 어깨L[1], 어깨R[2], 팔뚝L[3], 팔뚝R[4], 손L[5], 손R[6], 허벅지L[7], 허벅지R[8], 종아리L[9], 종아리R[10], 몸통[13]
            jointObjectList[(int)ObjectType.Head].transform.LookAt(transform.position + Vector3.forward, (Vector3)newJoints[(int)JointType.Nose] - (Vector3)newJoints[(int)JointType.Neck]);
            jointObjectList[(int)ObjectType.LUpperarm].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.LForearm].transform.position - jointObjectList[(int)ObjectType.LUpperarm].transform.position);
            jointObjectList[(int)ObjectType.RUpperarm].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.RForearm].transform.position - jointObjectList[(int)ObjectType.RUpperarm].transform.position);
            jointObjectList[(int)ObjectType.LForearm].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.LHand].transform.position - jointObjectList[(int)ObjectType.LForearm].transform.position);
            jointObjectList[(int)ObjectType.RForearm].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.RHand].transform.position - jointObjectList[(int)ObjectType.RForearm].transform.position);
            jointObjectList[(int)ObjectType.LHand].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.LHand].transform.position - jointObjectList[(int)ObjectType.LForearm].transform.position);
            jointObjectList[(int)ObjectType.RHand].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.RHand].transform.position - jointObjectList[(int)ObjectType.RForearm].transform.position);
            jointObjectList[(int)ObjectType.LThigh].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.LCalf].transform.position - jointObjectList[(int)ObjectType.LThigh].transform.position);
            jointObjectList[(int)ObjectType.RThigh].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.RCalf].transform.position - jointObjectList[(int)ObjectType.RThigh].transform.position);
            jointObjectList[(int)ObjectType.LCalf].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.LFoot].transform.position - jointObjectList[(int)ObjectType.LCalf].transform.position);
            jointObjectList[(int)ObjectType.RCalf].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.RFoot].transform.position - jointObjectList[(int)ObjectType.RCalf].transform.position);
            jointObjectList[(int)ObjectType.Torso].transform.LookAt(transform.position + Vector3.forward, jointObjectList[(int)ObjectType.Head].transform.position - jointObjectList[(int)ObjectType.Torso].transform.position);
            
        }

    }
}