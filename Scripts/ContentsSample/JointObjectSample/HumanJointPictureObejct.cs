using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    public class HumanJointPictureObejct : MonoBehaviour
    {
        List<GameObject> objectList;
        float distance;
        public int type;   // Picture Type : 0은 모자, 1은 칼-방패, 2는 글로브

        // {모자, 칼, 방패, 글러브} 사이즈 리스트
        static List<Vector3> objectSizeList = new List<Vector3> {
            new Vector3(2f, 2f, 0.5f),
            new Vector3(5f, 5f, 0.5f),
            new Vector3(1f, 10f, 0.5f),
            new Vector3(2f, 2f, 0.5f)
        };

        // 각각 텍스처
        public Texture2D hat;
        public Texture2D shield;
        public Texture2D sword;
        public Texture2D glove;


        public void Init(int type)
        {
            this.type = type;   

            // 관절 15개 각각 : 생성, 이름, 크기, 컬러, 부모 설정 후 리스트에 추가
            objectList = new List<GameObject>();


            // 칼방패 타입
            if (type == JointContentsOption.PictureType_Knight)
            {
                // 칼
                var newObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                newObject.transform.localScale = objectSizeList[1];
                var cubeRenderer = newObject.GetComponent<Renderer>();
                cubeRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
                cubeRenderer.material.mainTexture = sword;
                newObject.transform.parent = this.gameObject.transform;
                objectList.Add(newObject);

                // 방패
                var newObject2 = GameObject.CreatePrimitive(PrimitiveType.Quad);
                newObject2.transform.localScale = objectSizeList[2];
                var cubeRenderer2 = newObject2.GetComponent<Renderer>();
                cubeRenderer2.material = new Material(Shader.Find("Unlit/Transparent"));
                cubeRenderer2.material.mainTexture = shield;
                newObject2.transform.parent = this.gameObject.transform;
                objectList.Add(newObject2);
            }

            // 모자 타입
            else if (type == JointContentsOption.PictureType_Hat)
            {
                // 모자 
                var newObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                newObject.transform.localScale = objectSizeList[0];
                var cubeRenderer = newObject.GetComponent<Renderer>();
                cubeRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
                cubeRenderer.material.mainTexture = hat;
                newObject.transform.parent = this.gameObject.transform;
                objectList.Add(newObject);
            }

            // 글러브 타입
            else if (type == JointContentsOption.PictureType_Glove)
            {
                // 글러브
                var newObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                newObject.transform.localScale = objectSizeList[3];
                var cubeRenderer = newObject.GetComponent<Renderer>();
                cubeRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
                cubeRenderer.material.mainTexture = glove;
                newObject.transform.parent = this.gameObject.transform;
                objectList.Add(newObject);

                // 글러브
                var newObject2 = GameObject.CreatePrimitive(PrimitiveType.Quad);
                newObject2.transform.localScale = objectSizeList[3];
                var cubeRenderer2 = newObject2.GetComponent<Renderer>();
                cubeRenderer2.material = new Material(Shader.Find("Unlit/Transparent"));
                cubeRenderer2.material.mainTexture = glove;
                newObject2.transform.parent = this.gameObject.transform;
                objectList.Add(newObject2);
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
        public void MovePosition(List<Vector2> newJoints)
        {
            distance = Vector3.Distance(newJoints[1], newJoints[2]);

            // 칼-방패 타입
            if (type == JointContentsOption.PictureType_Knight)
            {
                // 칼
                if (newJoints[7].Equals(JointData.EmptyVector) || newJoints[5].Equals(JointData.EmptyVector))
                {
                    objectList[0].transform.position = JointData.InvisibleVector;
                }
                else
                {
                    objectList[0].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[7] + new Vector3(0f, 0f, 7f));
                    objectList[0].transform.localScale = new Vector3(1f, 1f, 1f) + objectSizeList[1] * distance;
                    objectList[0].transform.LookAt(objectList[0].transform.position + Vector3.forward, (Vector3)newJoints[7] - (Vector3)newJoints[5]);
                }

                // 방패
                if (newJoints[8].Equals(JointData.EmptyVector) || newJoints[6].Equals(JointData.EmptyVector))
                {
                    objectList[1].transform.position = JointData.InvisibleVector;
                }
                else
                {
                    objectList[1].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[8] + new Vector3(0f, 0f, 7f));
                    objectList[1].transform.localScale = new Vector3(1f, 1f, 1f) + objectSizeList[2] * distance;
                    objectList[1].transform.LookAt(objectList[1].transform.position + Vector3.forward, (Vector3)newJoints[8] - (Vector3)newJoints[6]);
                }
            }

            // 모자 타입
            else if (type == JointContentsOption.PictureType_Hat)
            {
                // 모자
                if (newJoints[0].Equals(JointData.EmptyVector))
                {
                    objectList[0].transform.position = JointData.InvisibleVector;
                }
                else
                {
                    objectList[0].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[0] + new Vector3(0f, (distance * 0.8f), 7f));
                    objectList[0].transform.localScale = new Vector3(1f, 1f, 1f) + objectSizeList[0] * distance * 2f;
                }
            }

            // 글러브 타입
            else if (type == JointContentsOption.PictureType_Glove)
            {
                // L글러브
                if (newJoints[7].Equals(JointData.EmptyVector) || newJoints[5].Equals(JointData.EmptyVector))
                {
                    objectList[0].transform.position = JointData.InvisibleVector;
                }
                else
                {
                    objectList[0].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[7] + new Vector3(0f, 0f, 7f));
                    objectList[0].transform.localScale = new Vector3(1f, 1f, 1f) + objectSizeList[3] * distance;
                    objectList[0].transform.LookAt(objectList[0].transform.position + Vector3.forward, (Vector3)newJoints[7] - (Vector3)newJoints[5]);
                }

                // R글러브
                if (newJoints[8].Equals(JointData.EmptyVector) || newJoints[6].Equals(JointData.EmptyVector))
                {
                    objectList[1].transform.position = JointData.InvisibleVector;
                }
                else
                {
                    objectList[1].transform.position = Camera.main.ViewportToWorldPoint((Vector3)newJoints[8] + new Vector3(0f, 0f, 7f));
                    objectList[1].transform.localScale = new Vector3(1f, 1f, 1f) + objectSizeList[3] * distance;
                    objectList[1].transform.LookAt(objectList[1].transform.position + Vector3.forward, (Vector3)newJoints[8] - (Vector3)newJoints[6]);
                }
            }
        }
    }
}