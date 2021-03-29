using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    public class HumanLineObject : MonoBehaviour
    {
        // 뼈 하나 클래스
        public class Skeleton
        {
            public GameObject LineObject;
            public LineRenderer Line;

            public Vector3 start;
            public Vector3 end;
        }

        public List<Skeleton> Skeletons = new List<Skeleton>();    // 뼈 12개 리스트
        public Material SkeletonMaterial;   // 외부 머테리얼 입력

        // 스스로 인간 하나 비활성화 하는 함수
        public void DeactivateSelf()
        {
            if (!(this.gameObject == null) && this.gameObject.activeSelf)
            {
                for (int i = 0; i < Skeletons.Count; i++)
                {
                    if (Skeletons[i].LineObject.activeSelf)
                    {
                        Skeletons[i].LineObject.SetActive(false);
                    }
                }

                this.gameObject.SetActive(false);
            }
        }

        // 스스로 인간 하나 활성화 하는 함수
        public void ActivateSelf()
        {
            if (!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);

                for (int i = 0; i < Skeletons.Count; i++)
                {
                    if (!Skeletons[i].LineObject.activeSelf)
                    {
                        Skeletons[i].LineObject.SetActive(true);
                    }
                }
            }
        }

        // 뼈 하나 추가하는 함수
        private void AddSkeleton()
        {
            var sk = new Skeleton()
            {
                LineObject = new GameObject("Line"),
                start = new Vector3(),
                end = new Vector3(),
            };

            sk.Line = sk.LineObject.AddComponent<LineRenderer>();
            sk.Line.startWidth = 0.1f;
            sk.Line.endWidth = 0.1f;

            // define the number of vertex
            sk.Line.positionCount = 2;
            sk.Line.material = SkeletonMaterial;
            //sk.Line.transform.parent = this.gameObject.transform;

            Skeletons.Add(sk);
        }

        // 사람의 뼈 12개를 생성하는 함수
        public void Init()
        {
            // 뼈 개수인 12개만큼 뼈 생성
            for (int i = 0; i < 13; i++)
            {
                AddSkeleton();
            }
        }

        // 메세지 수신 시로 변경
        public void MovePosition(List<Vector2> newJoints)
        {
            // 코-목
            Skeletons[0].start = new Vector3(newJoints[0].x, newJoints[0].y, 7f);
            Skeletons[0].end = new Vector3(newJoints[1].x, newJoints[1].y, 7f);

            // 양 어깨
            Skeletons[1].start = new Vector3(newJoints[3].x, newJoints[3].y, 7f);
            Skeletons[1].end = new Vector3(newJoints[4].x, newJoints[4].y, 7f);

            // 왼어깨-왼골반
            Skeletons[2].start = new Vector3(newJoints[3].x, newJoints[3].y, 7f);
            Skeletons[2].end = new Vector3(newJoints[9].x, newJoints[9].y, 7f);

            // 오른어깨-오른골반
            Skeletons[3].start = new Vector3(newJoints[4].x, newJoints[4].y, 7f);
            Skeletons[3].end = new Vector3(newJoints[10].x, newJoints[10].y, 7f);

            // 왼어깨-왼팔꿈치
            Skeletons[4].start = new Vector3(newJoints[3].x, newJoints[3].y, 7f);
            Skeletons[4].end = new Vector3(newJoints[5].x, newJoints[5].y, 7f);

            // 왼팔꿈치-왼손목
            Skeletons[5].start = new Vector3(newJoints[5].x, newJoints[5].y, 7f);
            Skeletons[5].end = new Vector3(newJoints[7].x, newJoints[7].y, 7f);

            // 오른어깨-오른팔꿈치
            Skeletons[6].start = new Vector3(newJoints[4].x, newJoints[4].y, 7f);
            Skeletons[6].end = new Vector3(newJoints[6].x, newJoints[6].y, 7f);

            // 오른팔꿈치-오른손목
            Skeletons[7].start = new Vector3(newJoints[6].x, newJoints[6].y, 7f);
            Skeletons[7].end = new Vector3(newJoints[8].x, newJoints[8].y, 7f);

            // 왼골반-오른골반
            Skeletons[8].start = new Vector3(newJoints[9].x, newJoints[9].y, 7f);
            Skeletons[8].end = new Vector3(newJoints[10].x, newJoints[10].y, 7f);

            // 왼골반-왼무릎
            Skeletons[9].start = new Vector3(newJoints[9].x, newJoints[9].y, 7f);
            Skeletons[9].end = new Vector3(newJoints[11].x, newJoints[11].y, 7f);

            // 왼무릎-왼발목
            Skeletons[10].start = new Vector3(newJoints[11].x, newJoints[11].y, 7f);
            Skeletons[10].end = new Vector3(newJoints[13].x, newJoints[13].y, 7f);

            // 오른골반-오른무릎
            Skeletons[11].start = new Vector3(newJoints[10].x, newJoints[10].y, 7f);
            Skeletons[11].end = new Vector3(newJoints[12].x, newJoints[12].y, 7f);

            // 오른무릎-오른발목
            Skeletons[12].start = new Vector3(newJoints[12].x, newJoints[12].y, 7f);
            Skeletons[12].end = new Vector3(newJoints[14].x, newJoints[14].y, 7f);

            // 각 새로 입력받은 좌표로 LineObject 포지션 이동
            foreach (var sk in Skeletons)
            {
                sk.Line.SetPosition(0, Camera.main.ViewportToWorldPoint(sk.start));
                sk.Line.SetPosition(1, Camera.main.ViewportToWorldPoint(sk.end));
            }
        }

        private void Awake()
        {
            Init();
        }

    }
}