using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellBig.Module.HumanDetection;

public class Character3DControlTower : MonoBehaviour
{
    const int WaitJointFrameMax = 200;
    private int textureFrameID;
    private int joint3DFrameID;
    private List<UnityChan3DController> controllerList;
    private CoreModuleStatusModel coreModuleStatusModel;
    
    // 새 유니티쨩 만들기
    private void CreateUnityChan(int count)
    {
        for(int i = 0; i < count; i++)
        {
            //var modelObject = Instantiate(Resources.Load<GameObject>("unitychan"), new Vector3(0, 0, 0), Quaternion.identity);
            var modelObject = Instantiate(Resources.Load<GameObject>("unitychan"));
            controllerList.Add(modelObject.GetComponent<UnityChan3DController>());
            Debug.Log("Created New Unity Chan!");
        }
    }

    // 모듈 상태 보고 메세지 수신 함수
    private void OnCoreModuleStatusReportMsg(CoreModuleStatusReportMsg msg)
    {
        if (msg.reportType == CoreModuleReportType.Error)
        {
            Debug.LogError(msg.statusMsg);
        }

        else if (msg.reportType == CoreModuleReportType.Warning)
        {
            Debug.LogWarning(msg.statusMsg);
        }

        else if(msg.reportType == CoreModuleReportType.Normal)
        {
            Debug.Log(msg.statusMsg);
        }
    }

    // 텍스처 재생 메세지 수신 함수 : 프레임 번호만 참고
    private void OnPlayFrameTextureMsg(PlayFrameTextureMsg msg)
    {
        textureFrameID = msg.frameID;

        // WaitJointFrameMax 만큼 영상 프레임 번호를 받을 동안 관절 프레임 번호는 업데이트 되지 않은 경우 : 아바타 비활성화
        if((textureFrameID - joint3DFrameID) > WaitJointFrameMax)
        {
            for(int i = 0; i<controllerList.Count; i++)
            {
                controllerList[i].Deactivate();
            }
        }
    }

    // 3D 관절 재생 메세지 수신 함수
    private void OnPlayHuman3DJointListMsg(PlayHuman3DJointListMsg msg)
    {
        // 3D 관절의 프레임 번호 저장
        joint3DFrameID = msg.frameID;

        // 부족한 만큼 새 아바타 생성
        if(controllerList.Count < msg.jointList.Count)
        {
            CreateUnityChan(msg.jointList.Count - controllerList.Count);
        }

        // 움직이거나, 없어지거나
        for(int i = 0; i < controllerList.Count; i++)
        {
            if(i < msg.jointList.Count)
            //if(i < 1)
            {
                controllerList[i].Activate();
                controllerList[i].Move(msg.jointList[i]);
            }

            else
            {
                controllerList[i].Deactivate();
            }
        }
    }

    private void Init()
    {
        coreModuleStatusModel = Model.First<CoreModuleStatusModel>();
        if(coreModuleStatusModel == null) { return; }

        controllerList = new List<UnityChan3DController>();
        textureFrameID = 1;
        joint3DFrameID = 1;

        Message.AddListener<CoreModuleStatusReportMsg>(OnCoreModuleStatusReportMsg);
        Message.AddListener<PlayFrameTextureMsg>(OnPlayFrameTextureMsg);
        Message.AddListener<PlayHuman3DJointListMsg>(OnPlayHuman3DJointListMsg);
        
        // HumanAR 모듈 시작
        Message.Send<CoreModuleControlMsg>(new CoreModuleControlMsg((int)CoreModuleIndex.Camera | (int)CoreModuleIndex.NetworkSocket | (int)CoreModuleIndex.Buffer,
            CoreModuleOperationIndex.Init));
        Message.Send<CoreModuleControlMsg>(new CoreModuleControlMsg((int)CoreModuleIndex.Camera | (int)CoreModuleIndex.NetworkSocket | (int)CoreModuleIndex.Buffer,
            CoreModuleOperationIndex.Play));

        Debug.Log("3D Character Control Tower Ready");
    }

    private void OnDestroy()
    {
        Message.RemoveListener<CoreModuleStatusReportMsg>(OnCoreModuleStatusReportMsg);
        Message.RemoveListener<PlayFrameTextureMsg>(OnPlayFrameTextureMsg);
        Message.RemoveListener<PlayHuman3DJointListMsg>(OnPlayHuman3DJointListMsg);

        if(controllerList != null)
        {
            controllerList.Clear();
            controllerList = null;
        }
    }

    private void Update()
    {
        if(coreModuleStatusModel == null) { Init(); }
    }
}
