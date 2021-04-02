using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellBig.Module.HumanDetection;



public class UnityChan3DController : MonoBehaviour
{
    public GameObject avatar;
    private UnityChanAvatarBone avatarBone;
    private List<GameObject> tempCubeList;

    private float minX = 300;
    private float minY = 300;
    private float minZ = 0;
    private float maxX = 300;
    private float maxY = 300;
    private float maxZ = 0;


    public void Move(Human3DJoint input)
    {
        //Debug.Log("Move");
        for(int i = 0; i < Joint3DData.POSITION_JOINT_MAX; i++)
        {
            /*
            // Position 이동
            if(avatarBone.positionObjectList[i] != null)
            {
                avatarBone.positionObjectList[i].transform.position = input.jointPositions[i];
            }
            */
            
            
            // 포지션만 이동
            //avatarBone.positionObjectList[(int)Joint3DData.PositionJointType.Pelvis].transform.position = input.jointPositions[(int)Joint3DData.PositionJointType.Pelvis];

            // 큐브 이동
            tempCubeList[i].transform.position = input.jointPositions[i];

            /*
            if(minX > input.jointPositions[i].x){
                minX = input.jointPositions[i].x;
            }
            if(minY > input.jointPositions[i].y){
                minY = input.jointPositions[i].y;
            }
            if(minZ > input.jointPositions[i].z){
                minZ = input.jointPositions[i].z;
            }
            if(maxX < input.jointPositions[i].x){
                maxX = input.jointPositions[i].x;
            }
            if(maxY < input.jointPositions[i].y){
                maxY = input.jointPositions[i].y;
            }
            if(maxZ < input.jointPositions[i].z){
                maxZ = input.jointPositions[i].z;
            }
            */
        }

        for(int i = 0; i < Joint3DData.ANGLE_JOINT_MAX; i++)
        {
            if(avatarBone.angleObjectList[i] != null)
            {
                //avatarBone.angleObjectList[i].transform.rotation = Quaternion.Euler(input.jointAngles[i].x, input.jointAngles[i].y, input.jointAngles[i].z);
                avatarBone.angleObjectList[i].transform.rotation = Quaternion.Euler(input.jointAngles[i]);

                //avatarBone.angleObjectList[i].transform.rotation = Quaternion.EulerAngles(input.jointAngles[i].x, input.jointAngles[i].y, input.jointAngles[i].z); // 사용되지 않음
                //avatarBone.angleObjectList[i].transform.rotation = Quaternion.EulerAngles(input.jointAngles[i]);    // 사용되지 않음
                //avatarBone.angleObjectList[i].transform.rotation = Quaternion.EulerRotation(input.jointAngles[i].x, input.jointAngles[i].y, input.jointAngles[i].z);    // 사용되지 않음
                //avatarBone.angleObjectList[i].transform.rotation = Quaternion.EulerRotation(input.jointAngles[i]);  // 사용되지 않음

                //float temp = 0;
                //avatarBone.angleObjectList[i].transform.rotation = Quaternion.AngleAxis(temp, input.jointAngles[i]);
            }
        }
    }
    
    public void Activate()
    {
        if(!avatar.activeSelf) { avatar.SetActive(true); }
    }

    public void Deactivate()
    {
        if(avatar.activeSelf) { avatar.SetActive(false); }
    }

    // 하드 코드, 그 전설의 시작
    // Child 세팅은 안해도 되지...?
    private void Awake()
    {   
        avatarBone = this.gameObject.GetComponent<UnityChanAvatarBone>();

        // 아래는 진짜 아바타 움직일 용도. 이거는 포인트만 집기
        
        tempCubeList = new List<GameObject>();
        for(int i = 0; i < Joint3DData.POSITION_JOINT_MAX; i++)
        {
            var newJoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newJoint.name = Joint3DData.PositionJointName[i];
            newJoint.transform.localScale = Vector3.one * 5.0f;   // 그냥 작은 큐브
            var cubeRenderer = newJoint.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", Color.green);
            newJoint.transform.parent = this.gameObject.transform;
            tempCubeList.Add(newJoint);
        }
    }

    private void OnDestroy()
    {
        if(tempCubeList != null)
        {
            tempCubeList.Clear();
            tempCubeList = null;
        }

        //Debug.Log("Min X : " + minX + ", Max X : " + maxX);
        //Debug.Log("Min Y : " + minY + ", Max Y : " + maxY);
        //Debug.Log("Min Z : " + minZ + ", Max Z : " + maxZ);
    }
}
