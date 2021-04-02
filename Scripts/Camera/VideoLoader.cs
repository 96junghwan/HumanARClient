using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Video;



// VideoPlayer에서 캡처하는 기능이 대부분 MainThread 기반이라, 콘텐츠 쪽에 영향을 줄 수 있음
// 비디오 재생 뒤지게 빠르긴 함 : 1080p 영상도 원본 재생 속도에 가까울 정도임



namespace CellBig.Module.HumanDetection
{ 
    public class VideoLoader : MonoBehaviour
    {
        // 옵션 모델
        private CameraOptionModel cameraOptionModel;
        private CoreModuleStatusModel coreModuleStatusModel;

        // 비디오 경로
        //private string videoPath = "D:/Videos/SMTM/VVS.mp4";
        //private string videoPath = "D:/Videos/SMTM/Freak.mp4";
        //private string videoPath = "D:/Videos/SMTM/YearEnd.mp4";
        //private string videoPath = "D:/Videos/SMTM/Yay.mp4";
        //private string videoPath = "D:/Videos/SMTM/NewNew.mp4";
        //private string videoPath = "D:/Videos/SMTM/Hero.mp4";
        private string videoPath = "D:/Videos/temp/dance.mp4";

        // 비디오 재생 및 저장 변수
        private VideoPlayer videoPlayer;
        private Texture2D captureTexture;
        private RenderTexture renderTexture;
        private int frameID = 1;

        // 코루틴 캡처 변수
        private WaitForSeconds waitTime = new WaitForSeconds(0.1f);
        private WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();

        // Thread 변수
        private Thread captureThread;
        private Queue<CapturedFrameMsg> msgQ;

        // 모듈 상태를 전달할 메세지 큐
        private Queue<CoreModuleStatusReportMsg> coreModuleStatusReportMsgQ;
        
        


        // 코어 모듈 동작 제어 메세지 수신하는 함수
        private void OnInternalModuleContorl(InternalModuleContorl msg)
        {
            // 메세지에 해당 안되면 무시
            if ((msg.coreModuleIndex & (int)CoreModuleIndex.Camera) != (int)CoreModuleIndex.Camera) { return; }

            // 지정된 동작 수행
            switch (msg.coreModuleOperationIndex)
            {
                // Init
                case CoreModuleOperationIndex.Init:
                    ModuleInit();
                    break;

                // 시작
                case CoreModuleOperationIndex.Play:
                    ModulePlay();
                    break;

                // 일시중지
                case CoreModuleOperationIndex.Pause:
                    ModulePause();
                    break;

                // 종료
                case CoreModuleOperationIndex.Stop:
                    ModuleStop();
                    break;

                // 재부팅
                case CoreModuleOperationIndex.ReBoot:
                    ModuleStop();
                    ModuleInit();
                    break;

                // 에러
                default:
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                        CoreModuleReportType.Error,
                        (int)CoreModuleReportErrorCode.Camera_Etc,
                        "[사전에 정의된 모듈 제어 동작이 아닙니다]",
                        "at OnCoreModuleControlMsg() of VideoLoader.cs"));
                    break;
            }
        }

        // 자원 할당 함수
        private void ModuleInit()
        {
            LoadOptionModel();

            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.NotReady)
            {
                AllocateVariables();
                VideoInit();
            }
        }

        // 기능 시작 함수
        private void ModulePlay()
        {
            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Ready)
            {
                videoPlayer.Play();
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
                StartCoroutine(CaptureCoroutine());
            }

            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Pause)
            {
                videoPlayer.Play();
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
            }
        }

        // 기능 일시 정지 함수
        private void ModulePause()
        {
            videoPlayer.Pause();
            coreModuleStatusModel.cameraStatus = CoreModuleStatus.Pause;
        }

        // 기능 종료 및 자원 해제 함수
        private void ModuleStop()
        {
            if (coreModuleStatusModel != null)
            {
                if (coreModuleStatusModel.cameraStatus > CoreModuleStatus.NotReady)
                {
                    ReleaseVariables();
                }
            }

            coreModuleStatusModel.cameraStatus = CoreModuleStatus.NotReady;
        }




        // 옵션 모델 로드 함수
        private void LoadOptionModel()
        {
            cameraOptionModel = Model.First<CameraOptionModel>();
            coreModuleStatusModel = Model.First<CoreModuleStatusModel>();

            // 로드 실패
            if (cameraOptionModel == null)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_Etc,
                            "[옵션 모델 로드에 실패했습니다]",
                            "at LoadOptionModel() of VideoLoader.cs"));
            }
        }

        // 변수 할당 함수
        private void AllocateVariables()
        {
            renderTexture = new RenderTexture(cameraOptionModel.camWidth, cameraOptionModel.camHeight, 24);
            captureTexture = new Texture2D(cameraOptionModel.camWidth, cameraOptionModel.camHeight, TextureFormat.RGB24, false);
        }

        // 변수 해제 함수
        private void ReleaseVariables()
        {
            if (videoPlayer.isPlaying || videoPlayer.isPaused) { videoPlayer.Stop(); }
            videoPlayer = null;
        }

        // VideoPlayer 초기화 함수
        private void VideoInit()
        {
            videoPlayer = this.gameObject.AddComponent<VideoPlayer>();
            videoPlayer.url = videoPath;
            videoPlayer.waitForFirstFrame = true;
  
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;

            // isPrepard 필드도 존재함
            videoPlayer.Play();
            videoPlayer.Pause();

            // 비디오 오픈 여부 검사
            if (!videoPlayer.enabled)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Camera_CannotOpenCamera,
                            "[입력한 경로의 비디오를 열 수 없습니다]",
                            "at VideoInit() of VideoLoader.cs"));
            }

            else
            {
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Ready;
            }
        }

        // 캡처 코루틴 : 동작하긴 하는데 MainThread 기반
        private IEnumerator CaptureCoroutine()
        {
            while (coreModuleStatusModel.cameraStatus > CoreModuleStatus.Ready)
            {
                //yield return waitTime;
                yield return null;
                //yield return frameEnd;

                if (videoPlayer == null) { break; }
                if (videoPlayer.isPaused) { continue; }

                RenderTexture.active = renderTexture;
                captureTexture.ReadPixels(new Rect(0, 0, cameraOptionModel.camWidth, cameraOptionModel.camHeight), 0, 0);
                captureTexture.Apply();
                RenderTexture.active = null;
                Message.Send<CapturedFrameMsg>(new CapturedFrameMsg(frameID, Time.unscaledTime, cameraOptionModel.camWidth, cameraOptionModel.camHeight, captureTexture.EncodeToJPG()));
                frameID++;
            }
        }




        private void Awake()
        {
            coreModuleStatusReportMsgQ = new Queue<CoreModuleStatusReportMsg>();
            Message.AddListener<InternalModuleContorl>(OnInternalModuleContorl);
        }

        private void OnDestroy()
        {
            if (coreModuleStatusModel != null)
            {
                ModuleStop();
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.None;
            }

            if (coreModuleStatusReportMsgQ != null)
            {
                coreModuleStatusReportMsgQ.Clear();
                coreModuleStatusReportMsgQ = null;
            }

            Message.RemoveListener<InternalModuleContorl>(OnInternalModuleContorl);
        }

        private void Update()
        {
            // 모듈 상태 메세지 전송
            if (coreModuleStatusReportMsgQ.Count != 0)
            {
                Message.Send<CoreModuleStatusReportMsg>(coreModuleStatusReportMsgQ.Dequeue());
            }
        }
    }
}
