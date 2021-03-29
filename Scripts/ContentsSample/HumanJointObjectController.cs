using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CellBig.Module.HumanDetection
{
    // 한 프레임에서 검출된 사람 관절 전부를 관리하는 클래스, 샘플 컨텐츠
    public class HumanJointObjectController : MonoBehaviour
    {
        int ARType = JointContentsOption.ARType_Cube;   // AR 오브젝트 타입
        int typeCount = 0;  // 각 AR 콘텐츠 타입에 따라 종류를 다르게 설정하기 위한 변수

        // 일반, 광각 카메라 등 각의 차이에 따라 직접 입력하여 오브젝트의 크기에 영향을 주는 변수
        public float globalSizeOption = 1.0f;      // R&D 팀 카메라 기준 : 1

        List<HumanJointObject> humanObjectList;   // HumanObject를 저장하는 리스트
        public GameObject humanJointObjectPrefab;        // HumanObject의 Prefab을 가지고 있는 변수

        List<HumanCharacterSample> humanCharacterObjectList;     // HumanCharacter 오브젝트 리스트
        public GameObject humanCharacterObjectPrefab;   // CreepyBoy/Ironman 등의 버전 2 Prefab

        List<HumanJointPictureObejct> humanPictureObjectList;    // HumanPartPicture 오브젝트 리스트
        public GameObject humanPictureObjectPrefab;     // Part Picture AR 버전 Prefab

        List<HumanJointPictureObject2> humanPictureObject2List;    // HumanPartPicture 오브젝트 리스트
        public GameObject humanPictureObject2Prefab;     // Part Picture AR 버전 Prefab

        int ObjectListCount = 0;
        int addCount;


        // 신경망에서 사람 찾았을 경우 호출되는 메세지 반응 함수
        void OnPlayHumanJointListMsg(PlayHumanJointListMsg msg)
        {
            // Cube Type
            if (ARType == JointContentsOption.ARType_Cube)
            {
                // 사람 부족하면 인력 충원함
                if (msg.jointList.Count > ObjectListCount)
                {
                    addCount = msg.jointList.Count - ObjectListCount;
                    AddHumanObject(addCount);
                    ObjectListCount += addCount;
                }

                // 아래 사람에게 순서 맞춰서 이동 하청시킴
                for (int i = 0; i < ObjectListCount; i++)
                {
                    // 새 좌표 안넘어온 애들 : 비활성화함
                    if (msg.jointList.Count <= i)
                    {
                        humanObjectList[i].DeactivateSelf();
                    }

                    // 새 좌표 넘어온 애들 : 활성화 하고 좌표 옮긴 뒤 로테이션 변경함.
                    else
                    {
                        humanObjectList[i].ActivateSelf();
                        humanObjectList[i].MovePosition(msg.jointList[i].viewportJointPositions);
                    }
                }
            }

            // Sprite Type
            else if (ARType == JointContentsOption.ARType_Sprite)
            {
                // 사람 부족하면 인력 충원함
                if (msg.jointList.Count > ObjectListCount)
                {
                    addCount = msg.jointList.Count - ObjectListCount;
                    AddHumanObject(addCount);
                    ObjectListCount += addCount;
                }

                // 아래 사람에게 순서 맞춰서 이동 하청시킴
                for (int i = 0; i < ObjectListCount; i++)
                {
                    // 새 좌표 안넘어온 애들 : 비활성화함
                    if (msg.jointList.Count <= i)
                    {
                        humanCharacterObjectList[i].DeactivateSelf();
                    }

                    // 새 좌표 넘어온 애들 : 활성화 하고 좌표 옮긴 뒤 로테이션 변경함.
                    else
                    {
                        humanCharacterObjectList[i].ActivateSelf();
                        humanCharacterObjectList[i].MovePosition(msg.jointList[i].viewportJointPositions, globalSizeOption);
                    }
                }
            }
            
            // Picture Type
            else if (ARType == JointContentsOption.ARType_Picture)
            {
                // 사람 부족하면 인력 충원함
                if (msg.jointList.Count > ObjectListCount)
                {
                    addCount = msg.jointList.Count - ObjectListCount;
                    AddHumanObject(addCount);
                    ObjectListCount += addCount;
                }

                // 아래 사람에게 순서 맞춰서 이동 하청시킴
                for (int i = 0; i < ObjectListCount; i++)
                {
                    // 새 좌표 안넘어온 애들 : 비활성화함
                    if (msg.jointList.Count <= i)
                    {
                        humanPictureObjectList[i].DeactivateSelf();
                    }

                    // 새 좌표 넘어온 애들 : 활성화 하고 좌표 옮긴 뒤 로테이션 변경함.
                    else
                    {
                        humanPictureObjectList[i].ActivateSelf();
                        humanPictureObjectList[i].MovePosition(msg.jointList[i].viewportJointPositions);
                    }
                }
            }

            // Picture Type 2
            else if (ARType == JointContentsOption.ARType_Picture2)
            {
                // 사람 부족하면 인력 충원함
                if (msg.jointList.Count > ObjectListCount)
                {
                    addCount = msg.jointList.Count - ObjectListCount;
                    AddHumanObject(addCount);
                    ObjectListCount += addCount;
                }

                // 아래 사람에게 순서 맞춰서 이동 하청시킴
                for (int i = 0; i < ObjectListCount; i++)
                {
                    // 새 좌표 안넘어온 애들 : 비활성화함
                    if (msg.jointList.Count <= i)
                    {
                        humanPictureObject2List[i].DeactivateSelf();
                    }

                    // 새 좌표 넘어온 애들 : 활성화 하고 좌표 옮긴 뒤 로테이션 변경함.
                    else
                    {
                        humanPictureObject2List[i].ActivateSelf();
                        humanPictureObject2List[i].MovePosition(msg.jointList[i].viewportJointPositions);
                    }
                }
            }
        }

        // 현재 humanObjectList에 할당되어있는 수 보다 새로 검출된 사람의 수가 더 많을 경우 부족한 수 만큼 새로 할당해주는 함수
        void AddHumanObject(int addCount)
        {
            for (int i = 0; i < addCount; i++)
            {
                // Cube Type 생성
                if (ARType == JointContentsOption.ARType_Cube)
                {
                    var newHuman = GameObject.Instantiate(humanJointObjectPrefab).GetComponent<HumanJointObject>();
                    humanObjectList.Add(newHuman);
                }

                // Sprite Type 생성
                else if (ARType == JointContentsOption.ARType_Sprite)
                {
                    var newHuman = GameObject.Instantiate(humanCharacterObjectPrefab).GetComponent<HumanCharacterSample>();
                    newHuman.Init(typeCount);
                    humanCharacterObjectList.Add(newHuman);
                    if (++typeCount >= 2) { typeCount = 0; }
                }

                // Picture Type 생성
                if (ARType == JointContentsOption.ARType_Picture)
                {
                    var newHuman = GameObject.Instantiate(humanPictureObjectPrefab).GetComponent<HumanJointPictureObejct>();
                    newHuman.Init(typeCount);
                    humanPictureObjectList.Add(newHuman);
                    if (++typeCount >= 3) { typeCount = 0; }
                }

                // Picture Type 생성
                if (ARType == JointContentsOption.ARType_Picture2)
                {
                    var newHuman = GameObject.Instantiate(humanPictureObject2Prefab).GetComponent<HumanJointPictureObject2>();
                    newHuman.Init(typeCount);
                    humanPictureObject2List.Add(newHuman);
                    if (++typeCount >= 3) { typeCount = 0; }
                }
            }
        }

        private void Start()
        {
            // 각 ARType에 맞는 오브젝트 리스트 생성 후 관절 재생 메세지 리스너 추가
            if (ARType == JointContentsOption.ARType_Cube) { humanObjectList = new List<HumanJointObject>(); }
            else if (ARType == JointContentsOption.ARType_Sprite) { humanCharacterObjectList = new List<HumanCharacterSample>(); }
            else if (ARType == JointContentsOption.ARType_Picture) { humanPictureObjectList = new List<HumanJointPictureObejct>(); }
            else if (ARType == JointContentsOption.ARType_Picture2) { humanPictureObject2List = new List<HumanJointPictureObject2>(); }
            else { Debug.LogError("Choose proper ARType. your ARType input : " + ARType); }
            Message.AddListener<PlayHumanJointListMsg>(OnPlayHumanJointListMsg);
        }

        private void OnDestroy()
        {
            // 관절 재생 메세지 리스너 삭제 후 각 ARType에 맞는 오브젝트 리스트 null 처리
            Message.RemoveListener<PlayHumanJointListMsg>(OnPlayHumanJointListMsg);
            if (ARType == JointContentsOption.ARType_Cube) { if (humanObjectList != null) { humanObjectList.Clear(); humanObjectList = null; } }
            if (ARType == JointContentsOption.ARType_Sprite) { if(humanCharacterObjectList != null) { humanCharacterObjectList.Clear(); humanCharacterObjectList = null; } }
            if (ARType == JointContentsOption.ARType_Picture) { if (humanPictureObjectList != null) { humanPictureObjectList.Clear(); humanPictureObjectList = null; } }
            if (ARType == JointContentsOption.ARType_Picture2) { if (humanPictureObject2List != null) { humanPictureObject2List.Clear(); humanPictureObject2List = null; } }
        }

    }
}