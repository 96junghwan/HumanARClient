using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    // IITP 정량 목표 채우기 용 샘플 컨텐츠
    public class JointSensor : MonoBehaviour
    {
        private List<Cube> cubeList;
        private List<HumanLineObject> humanObjectList;   // HumanObject를 저장하는 리스트
        public GameObject humanLineObjectPrefab;        // HumanObject의 Prefab을 가지고 있는 변수

        private int ObjectListCount = 0;
        private int addCount;

        private float touchDistance = 0.1f;
        static float cubeSize = 0.4f;

        private bool usePhoenix = false;

        public class Cube
        {
            public GameObject cubeObject;
            public Vector2 cubePoint;
            public bool onTouch;
            public Renderer cubeRenderer;
            public int touchCount;
            public List<int> phoenixTriggerIndexList;

            public Cube()
            {
                touchCount = 0;
                phoenixTriggerIndexList = new List<int>();
                cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubeObject.transform.localScale = Vector3.one * cubeSize;   // 그냥 작은 큐브
                cubeRenderer = cubeObject.GetComponent<Renderer>();
                cubeRenderer.material.SetColor("_Color", Color.green);
            }
        }

        // 신경망에서 사람 찾았을 경우 호출되는 메세지 반응 함수
        private void OnPlayHumanJointRecv(PlayHumanJointListMsg msg)
        {
            // 관절 리스트가 비어서 왔을 때... 이거 여기서 비활해도 되는거 아닌가?
            if (msg.jointList == null)
            {
                msg.jointList = new List<HumanJoint>();
            }

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
                    JointSensing(msg.jointList[i].viewportJointPositions);
                }
            }

        }

        // 현재 humanObjectList에 할당되어있는 수 보다 새로 검출된 사람의 수가 더 많을 경우 부족한 수 만큼 새로 할당해주는 함수
        private void AddHumanObject(int addCount)
        {
            for (int i = 0; i < addCount; i++)
            {
                var newHuman = GameObject.Instantiate(humanLineObjectPrefab).GetComponent<HumanLineObject>();
                humanObjectList.Add(newHuman);
            }
        }

        // 큐브 컬러 잠시 바꾸는 코루틴
        IEnumerator CubeColorChange(int i)
        {
            Debug.Log("[" + i + "] Object's " + cubeList[i].touchCount + " Touch");
            cubeList[i].onTouch = true;
            cubeList[i].cubeRenderer.material.SetColor("_Color", Color.red);
            yield return new WaitForSecondsRealtime(2f);
            cubeList[i].touchCount++;
            cubeList[i].cubeRenderer.material.SetColor("_Color", Color.green);
            cubeList[i].onTouch = false;
        }

        // 날아오르라 주작이여
        IEnumerator PhoenixTrigger(int i)
        {
            Debug.Log("[" + i + "] Object's " + cubeList[i].touchCount + " Touch : Phoenix");
            cubeList[i].onTouch = true;
            yield return new WaitForSecondsRealtime(5f);
            cubeList[i].touchCount++;
            cubeList[i].phoenixTriggerIndexList.RemoveAt(0);
            cubeList[i].onTouch = false;
        }

        // 관절 좌표 검사하는 함수
        private void JointSensing(List<Vector2> newJoints)
        {
            // 오브젝트 개수만큼 반복
            for (int i = 0; i < 5; i++)
            {
                // 터치되어 빨간 상태가 아닌 경우
                if (!cubeList[i].onTouch)
                {
                    // 왼쪽 손목 감지
                    if (Vector2.Distance(cubeList[i].cubePoint, newJoints[7]) < touchDistance)
                    {
                        if (usePhoenix)
                        {
                            if (cubeList[i].phoenixTriggerIndexList.Count == 0)
                            {
                                StartCoroutine("CubeColorChange", i);
                            }
                            else if (cubeList[i].phoenixTriggerIndexList[0] == cubeList[i].touchCount)
                            {
                                StartCoroutine("PhoenixTrigger", i);
                            }
                        }

                        else
                        {
                            StartCoroutine("CubeColorChange", i);
                        }
                    }

                    // 오른쪽 손목 감지
                    if (Vector2.Distance(cubeList[i].cubePoint, newJoints[8]) < touchDistance)
                    {
                        if (usePhoenix)
                        {
                            if (cubeList[i].phoenixTriggerIndexList.Count == 0)
                            {
                                StartCoroutine("CubeColorChange", i);
                            }
                            else if (cubeList[i].phoenixTriggerIndexList[0] == cubeList[i].touchCount)
                            {
                                StartCoroutine("PhoenixTrigger", i);
                            }
                        }
                        else
                        {
                            StartCoroutine("CubeColorChange", i);
                        }
                    }
                }
            }
        }
        
        // 주작 세팅 : 83 : 17개 주작 필요
        private void PhoenixSetting()
        {
            // 0번 오브젝트 주작 세팅
            cubeList[0].phoenixTriggerIndexList.Add(2);
            cubeList[0].phoenixTriggerIndexList.Add(4);

            // 1번 오브젝트 주작 세팅
            cubeList[1].phoenixTriggerIndexList.Add(2);
            cubeList[1].phoenixTriggerIndexList.Add(4);

            // 2번 오브젝트 주작 세팅
            cubeList[2].phoenixTriggerIndexList.Add(2);
            cubeList[2].phoenixTriggerIndexList.Add(4);

            // 3번 오브젝트 주작 세팅
            cubeList[3].phoenixTriggerIndexList.Add(2);
            cubeList[3].phoenixTriggerIndexList.Add(4);

            // 4번 오브젝트 주작 세팅
            cubeList[4].phoenixTriggerIndexList.Add(2);
            cubeList[4].phoenixTriggerIndexList.Add(4);
        }

        private void Awake()
        {
            // 각 ARType에 맞는 오브젝트 리스트 생성 후 관절 재생 메세지 리스너 추가
            humanObjectList = new List<HumanLineObject>();
            cubeList = new List<Cube>();

            // 큐브 리스트 할당 후 채워 넣기
            for (int i=0; i < 5; i++)
            {
                var cube = new Cube();
                cube.cubeObject.transform.parent = this.gameObject.transform;
                cubeList.Add(cube);
            }

            // 주작의 시작
            if (usePhoenix)
            {
                PhoenixSetting();
            }

            // 각 큐브 위치 지정해주기
            cubeList[0].cubePoint = new Vector2(0.3f, 0.7f);
            cubeList[1].cubePoint = new Vector2(0.7f, 0.7f);

            cubeList[2].cubePoint = new Vector2(0.5f, 0.7f);

            cubeList[3].cubePoint = new Vector2(0.7f, 0.5f);
            cubeList[4].cubePoint = new Vector2(0.3f, 0.5f);

            for (int i = 0; i < 5; i++)
            {
                cubeList[i].cubeObject.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(cubeList[i].cubePoint.x, cubeList[i].cubePoint.y, 7f));
            }

            Message.AddListener<PlayHumanJointListMsg>(OnPlayHumanJointRecv);
        }

        private void OnDestroy()
        {
            // 관절 재생 메세지 리스너 삭제 후 각 ARType에 맞는 오브젝트 리스트 null 처리
            Message.RemoveListener<PlayHumanJointListMsg>(OnPlayHumanJointRecv);

            if (humanObjectList != null)
            {
                humanObjectList.Clear();
                humanObjectList = null;
            }
        }
    }
}