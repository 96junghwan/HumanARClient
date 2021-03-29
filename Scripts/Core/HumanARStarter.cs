using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CellBig.Module.HumanDetection
{
    public class HumanARStarter : IModule
    {
        // 옵션 모델 변수
        private CoreModuleStatusModel coreModuleStatusModel;
        private CameraOptionModel cameraOptionModel;
        private NetworkOptionModel networkOptionModel;
        private BufferOptionModel bufferOptionModel;
        private PreProcessOptionModel preProcessOptionModel;
        private PostProcessOptionModel postProcessOptionModel;
        private OCamOptionModel oCamOptionModel;
        private NetworkControlOptionModel networkControlOptionModel;

        // 옵션 모델 생성 함수
        private void CreateOptionModels()
        {
            coreModuleStatusModel = new CoreModuleStatusModel();
            cameraOptionModel = new CameraOptionModel();
            networkOptionModel = new NetworkOptionModel();
            bufferOptionModel = new BufferOptionModel();
            preProcessOptionModel = new PreProcessOptionModel();
            postProcessOptionModel = new PostProcessOptionModel();
            oCamOptionModel = new OCamOptionModel();
            networkControlOptionModel = new NetworkControlOptionModel();
        }

        // 기본 옵션 모델 설정 함수
        private void SetOptionModels_Standard()
        {
            cameraOptionModel.cameraType = CameraType.OCam;
            cameraOptionModel.camWidth = 1280;
            cameraOptionModel.camHeight = 720;

            networkOptionModel.serverType = ServerType.Server_Titan;
            networkOptionModel.nnType_RT = (int)NNType.None;
            networkOptionModel.serverAccessKeyCode = AccessCode.Developer_AccessCode;

            preProcessOptionModel.flipOption = FlipOption.VerticalFlip;

            bufferOptionModel.delaySeconds_RT = 1f;
        }

        // 고급 옵션 모델 설정 함수
        private void SetOptionModels_Advanced()
        {
            coreModuleStatusModel.coreModuleUseOption = (int)CoreModuleIndex.Camera | (int)CoreModuleIndex.NetworkSocket | (int)CoreModuleIndex.Buffer;
            coreModuleStatusModel.cameraStatus = CoreModuleStatus.None;
            coreModuleStatusModel.networkStatus = CoreModuleStatus.None;
            coreModuleStatusModel.bufferStatus = CoreModuleStatus.None;

            networkOptionModel.sendRate_RT = 3;
            networkOptionModel.sendingQSizeMax = 10;

            bufferOptionModel.bufferMax = 128;
            bufferOptionModel.noDelay = false;

            preProcessOptionModel.useResize_RT = true;
            preProcessOptionModel.rotationOption = RotationOption.NoRotation;
#if UNITY_ANDROID
            // 안드로이드 환경에서는 전처리 부분이 불안정하여 Resize는 금지. 어차피 서버에서 함
            preProcessOptionModel.useResize_RT = false;
            preProcessOptionModel.resizeLock = true;
#else
            preProcessOptionModel.resizeLock = false;
#endif

            postProcessOptionModel.useEmptyJointLerpGenerator = true;
            postProcessOptionModel.useJointKalmanFilter = false;
            postProcessOptionModel.useJointLowPassFilter = false;
            postProcessOptionModel.useJointPerfectFilter = false;
            postProcessOptionModel.useJointAlignment = false;
            postProcessOptionModel.useJointParsing = true;
            postProcessOptionModel.useTempCompressJointShake_RT = false;

            oCamOptionModel.exposure = -6;
            oCamOptionModel.gain = 74;

            networkControlOptionModel.useNetworkResourceContoller = true;
            networkControlOptionModel.sendPauseTime = 0.3f;
            networkControlOptionModel.networkCheckCycleTime = 1f;
            networkControlOptionModel.invalidDataFeedbackMax = 20;
            networkControlOptionModel.serverSlowFeedbackMax = 20;
            networkControlOptionModel.positiveNetworkFeedbackMax = 40;
            
            // IP, Port 번호 자동 선택
            switch (networkOptionModel.serverType)
            {
                case ServerType.Server_Local:
                    networkOptionModel.serverIp = ServerAddress.Server_Local_IP;
                    networkOptionModel.serverPort = ServerAddress.Server_Local_Port;
                    break;

                case ServerType.Server_2080:
                    networkOptionModel.serverIp = ServerAddress.Server_2080_IP;
                    networkOptionModel.serverPort = ServerAddress.Server_2080_Port;
                    break;

                case ServerType.Server_Titan:
                    networkOptionModel.serverIp = ServerAddress.Server_Titan_IP;
                    networkOptionModel.serverPort = ServerAddress.Server_Titan_Port;
                    break;

                case ServerType.Server_Fashion:
                    networkOptionModel.serverIp = ServerAddress.Server_Fashion_IP;
                    networkOptionModel.serverPort = ServerAddress.Server_Fashion_Port;
                    break;

                default:
                    Debug.LogError("You have chosen wrong ServerType at NetworkOptionModel");
                    break;
            }
        }




        // 컨텐츠에서 보낸 코어 모듈 제어 메세지 수신 함수
        private void OnCoreModuleControlMsg(CoreModuleControlMsg msg)
        {
            if ((msg.coreModuleIndex & (int)CoreModuleIndex.Camera) == (int)CoreModuleIndex.Camera)
            {
                Message.Send<InternalModuleContorl>(new InternalModuleContorl((int)CoreModuleIndex.Camera, msg.coreModuleOperationIndex));
            }

            if ((msg.coreModuleIndex & (int)CoreModuleIndex.Buffer) == (int)CoreModuleIndex.Buffer)
            {
                Message.Send<InternalModuleContorl>(new InternalModuleContorl((int)CoreModuleIndex.Buffer, msg.coreModuleOperationIndex));
            }

            if ((msg.coreModuleIndex & (int)CoreModuleIndex.NetworkSocket) == (int)CoreModuleIndex.NetworkSocket)
            {
                Message.Send<InternalModuleContorl>(new InternalModuleContorl((int)CoreModuleIndex.NetworkSocket, msg.coreModuleOperationIndex));
            }
        }

        // 옵션 모델 생성 후 설정하고, 코어 모듈 Add하는 Set 함수
        private void SetHumanAR()
        {
            CreateOptionModels();
            SetOptionModels_Standard();
            SetOptionModels_Advanced();
            AddCoreComponent();
            Message.AddListener<CoreModuleControlMsg>(OnCoreModuleControlMsg);
        }

        // 코어 스크립트 컴포넌트{Camera, Network, Buffer} 추가하는 함수
        private void AddCoreComponent()
        {
            if ((coreModuleStatusModel.coreModuleUseOption & (int)CoreModuleIndex.Camera) == (int)CoreModuleIndex.Camera)
            {
                AddCamera();
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.NotReady;
            }

            if ((coreModuleStatusModel.coreModuleUseOption & (int)CoreModuleIndex.NetworkSocket) == (int)CoreModuleIndex.NetworkSocket)
            {
                this.gameObject.AddComponent<NetworkSocketManager>();
                coreModuleStatusModel.networkStatus = CoreModuleStatus.NotReady;
            }

            if ((coreModuleStatusModel.coreModuleUseOption & (int)CoreModuleIndex.Buffer) == (int)CoreModuleIndex.Buffer)
            {
                this.gameObject.AddComponent<BufferManager>();
                coreModuleStatusModel.bufferStatus = CoreModuleStatus.NotReady;
            }
        }

        // 옵션 별로 카메라 컴포넌트 추가하는 함수
        private void AddCamera()
        {
            switch (cameraOptionModel.cameraType)
            {
                case CameraType.None:
                    break;

                case CameraType.WebcamTexture:
                    this.gameObject.AddComponent<WebcamTextureCam>();
                    break;

                case CameraType.OCam:
                    this.gameObject.AddComponent<OCam>();
                    break;

                case CameraType.VideoOpenCV:
                    this.gameObject.AddComponent<VideoOpenCV>();
                    break;

                case CameraType.VideoLoader:
                    this.gameObject.AddComponent<VideoLoader>();
                    break;

                default:
                    break;
            }
        }

        // 세팅 완료, 씬에 모듈 로드 요청 함수, IModule 오버라이드
        protected override void OnLoadStart()
        {
            SetHumanAR();
            SetResourceLoadComplete();
        }

        private void OnDestroy()
        {
            Message.RemoveListener<CoreModuleControlMsg>(OnCoreModuleControlMsg);
        }
    }
}