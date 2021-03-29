using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CellBig.Module.HumanDetection
{
    // 사람 하나에 대한 게임오브젝트들(관절 15개)을 관리하는 클래스 (샘플 콘텐츠)
    public class HumanJointObject : MonoBehaviour
    {
        List<GameObject> object_list;   // 각 관절의 GameObject를 담을 리스트 (15개가 들어감)


        // 스스로 인간 하나 비활성화 하는 함수
        public void DeactivateSelf()
        {
            if (!(this.gameObject == null) && this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(false);
            }
        }

        // 스스로 인간 하나 활성화 하는 함수
        public void ActivateSelf()
        {
            if (!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
            }
        }

        // 관절 오브젝트 하나 활성화하는 함수
        void ActivateJoint(int index)
        {
            if (!object_list[index].activeSelf) { object_list[index].SetActive(true); }
        }

        // 관절 오브젝트 하나 비활성화 하는 함수
        void DeactivateJoint(int index)
        {
            if (object_list[index].activeSelf) { object_list[index].SetActive(false); }
        }

        // 새로운 포지션 입력 받고 위치 새로 지정해준 후 로테이션 새로 적용하는 함수
        public void MovePosition(List<Vector2> newJoints)
        {
            for (int i = 0; i < JointData.TARGET_JOINT_MAX; i++)
            {
                // 빈 좌표를 가진 관절일 경우
                if (newJoints[i].Equals(JointData.EmptyVector))
                {
                    object_list[i].transform.position = JointData.InvisibleVector;
                    DeactivateJoint(i);
                }

                // 정상적인 좌표를 가진 관절일 경우
                else
                {
                    ActivateJoint(i);
                    object_list[i].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[i] + new Vector3(0f, 0f, 7f));
                }
            }
        }

        private void OnDestroy()
        {
            if (object_list != null) { object_list.Clear(); object_list = null; }
        }

        private void Awake()
        {
            // 관절 15개 각각 : 생성, 이름, 크기, 컬러, 부모 설정 후 리스트에 추가
            object_list = new List<GameObject>();
            Color color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);

            for (int i = 0; i < JointData.TARGET_JOINT_MAX; i++)
            {
                var newJoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                newJoint.name = JointData.JointNameList[i];
                newJoint.transform.localScale = Vector3.one * 0.2f;   // 그냥 작은 큐브
                var cubeRenderer = newJoint.GetComponent<Renderer>();
                //cubeRenderer.material.SetColor("_Color", Color.green);
                cubeRenderer.material.SetColor("_Color",color);
                newJoint.transform.parent = this.gameObject.transform;
                object_list.Add(newJoint);
            }

        }
    }
}