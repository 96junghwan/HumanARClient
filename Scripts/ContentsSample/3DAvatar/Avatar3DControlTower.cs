using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellBig.Module.HumanDetection;

public class Avatar3DControlTower : MonoBehaviour
{
    const int WaitJointFrameMax = 200;
    private int textureFrameID;
    private int joint3DFrameID;
    private List<Avatar3DController> controllerList;
    private CoreModuleStatusModel coreModuleStatusModel;

    // 3D 관절 후처리 옵션
    [Header("Avatar Type")]
    [Tooltip("0 : Master, 1 : UnityChan")]
    [Range(0, 1)]
    public int avatarType = 0;

    [Header("Joint PostProcess Option")]
    public bool useLowPassFilter;
    public bool useKalmanFilter;

    // 새 아바타 만들기
    private void CreateUnityChan(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Inspector에 따라 다른 아바타 Prefab 선택 후 생성
            string AvatarPrefabName;
            switch (avatarType)
            {
                case (int)Avatar3DType.Boss:
                    AvatarPrefabName = "Boss 1";
                    break;

                case (int)Avatar3DType.UnityChan:
                    AvatarPrefabName = "UnityChan";
                    break;
                default:
                    AvatarPrefabName = "Boss 1";
                    break;
            }

            // 아바타 생성 후 컨트롤러 Component 받기
            var prefabDir = "3DAvatars/";
            var modelObject = Instantiate(Resources.Load<GameObject>(prefabDir + AvatarPrefabName));
            var controller = modelObject.GetComponent<Avatar3DController>();
            controller.SetOptions(useLowPassFilter, useKalmanFilter);
            controllerList.Add(controller);
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

        else if (msg.reportType == CoreModuleReportType.Normal)
        {
            Debug.Log(msg.statusMsg);
        }
    }

    // 텍스처 재생 메세지 수신 함수 : 프레임 번호만 참고
    private void OnPlayFrameTextureMsg(PlayFrameTextureMsg msg)
    {
        // 텍스쳐의 프레임 번호 저장
        textureFrameID = msg.frameID;

        // WaitJointFrameMax 만큼 영상 프레임 번호를 받을 동안 관절 프레임 번호는 업데이트 되지 않은 경우 : 아바타 비활성화
        if ((textureFrameID - joint3DFrameID) > WaitJointFrameMax)
        {
            for (int i = 0; i < controllerList.Count; i++)
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
        if (controllerList.Count < msg.jointList.Count)
        {
            CreateUnityChan(msg.jointList.Count - controllerList.Count);
        }

        // 움직이거나, 사라지거나.
        for (int i = 0; i < controllerList.Count; i++)
        {
            if (i < msg.jointList.Count)
            {
                controllerList[i].Activate();
                controllerList[i].PoseUpdate(msg.jointList[i]);
            }

            else
            {
                controllerList[i].Deactivate();
            }
        }
    }

    // 초기화 함수
    private void Init()
    {
        // 코어 모듈 로드
        coreModuleStatusModel = Model.First<CoreModuleStatusModel>();
        if (coreModuleStatusModel == null) { return; }

        // 일회성으로 옵션만 바꾸기 위해 옵션 모델 로드
        CameraOptionModel cameraOptionModel = Model.First<CameraOptionModel>();
        NetworkOptionModel networkOptionModel = Model.First<NetworkOptionModel>();

        // 컨텐츠 전용 HumanAR 옵션 지정
        cameraOptionModel.cameraType = CellBig.Module.HumanDetection.CameraType.VideoLoader;
        cameraOptionModel.camWidth = 640;
        cameraOptionModel.camHeight = 480;
        networkOptionModel.serverType = ServerType.Server_Local;
        networkOptionModel.nnType_RT = (int)NNType.BMC;

        // 아바타 컨트롤러 리스트 생성
        controllerList = new List<Avatar3DController>();
        textureFrameID = 1;
        joint3DFrameID = 1;

        // 메시지 리스너 부착
        Message.AddListener<CoreModuleStatusReportMsg>(OnCoreModuleStatusReportMsg);
        Message.AddListener<PlayFrameTextureMsg>(OnPlayFrameTextureMsg);
        Message.AddListener<PlayHuman3DJointListMsg>(OnPlayHuman3DJointListMsg);

        // HumanAR 모듈 초기화 -> 시작
        Message.Send<CoreModuleControlMsg>(new CoreModuleControlMsg((int)CoreModuleIndex.Camera | (int)CoreModuleIndex.NetworkSocket | (int)CoreModuleIndex.Buffer,
            CoreModuleOperationIndex.Init));
        Message.Send<CoreModuleControlMsg>(new CoreModuleControlMsg((int)CoreModuleIndex.Camera | (int)CoreModuleIndex.NetworkSocket | (int)CoreModuleIndex.Buffer,
            CoreModuleOperationIndex.Play));
    }

    private void OnDestroy()
    {
        Message.RemoveListener<CoreModuleStatusReportMsg>(OnCoreModuleStatusReportMsg);
        Message.RemoveListener<PlayFrameTextureMsg>(OnPlayFrameTextureMsg);
        Message.RemoveListener<PlayHuman3DJointListMsg>(OnPlayHuman3DJointListMsg);

        if (controllerList != null)
        {
            controllerList.Clear();
            controllerList = null;
        }
    }

    private void Update()
    {
        if (coreModuleStatusModel == null) { Init(); }
    }
}
