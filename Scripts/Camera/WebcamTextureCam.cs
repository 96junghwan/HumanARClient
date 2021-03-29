using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using OpenCVForUnity;
using UnityEngine.Android;

namespace CellBig.Module.HumanDetection
{
    public class WebcamTextureCam : MonoBehaviour
    {
        // 옵션 모델
        private CameraOptionModel cameraOptionModel;
        private PreProcessOptionModel preProcessOptionModel;
        private CoreModuleStatusModel coreModuleStatusModel;

        // 캡처 관련 변수
        private int frameID = 1;
        private WebCamTexture webcam;
        private Mat webcamMat;
        private Mat webcamMatTask;
        private MatOfByte mob;
        private Color32[] colorBuffer;

        // 쓰레드 관리 변수
        private bool didUpdated = false;
        private Thread captureThread;
        private Mat threadMat;
        private Mat threadRotatedMat;
        private Queue<CapturedFrameMsg> captureFrameCompleteMsgQ;
        static readonly object camLock = new object();

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
                CameraInit();
                AllocateVariables();
            }
        }

        // 기능 시작 함수
        private void ModulePlay()
        {
            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Ready)
            {
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
                webcam.Play();
                captureThread.Start();
            }

            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Pause)
            {
                webcam.Play();
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
            }
        }

        // 기능 일시 정지 함수
        private void ModulePause()
        {
            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Playing)
            {
                webcam.Pause();
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Pause;
            }
        }

        // 기능 종료 및 자원 해제 함수
        private void ModuleStop()
        {
            if (coreModuleStatusModel.cameraStatus > CoreModuleStatus.NotReady)
            {
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.NotReady;
                ReleaseVariables();
            }

            coreModuleStatusModel.cameraStatus = CoreModuleStatus.NotReady;
        }




        // 변수 할당 함수
        private void AllocateVariables()
        {
            captureFrameCompleteMsgQ = new Queue<CapturedFrameMsg>();
            mob = new MatOfByte();
            webcamMat = new Mat(cameraOptionModel.camHeight, cameraOptionModel.camWidth, CvType.CV_8UC3);
            threadMat = new Mat(cameraOptionModel.camHeight, cameraOptionModel.camWidth, CvType.CV_8UC3);
            threadRotatedMat = new Mat(cameraOptionModel.camHeight, cameraOptionModel.camWidth, CvType.CV_8UC3);
            webcamMatTask = new Mat(cameraOptionModel.camHeight, cameraOptionModel.camWidth, CvType.CV_8UC3);
            captureThread = new Thread(new ThreadStart(CaptureThread));
            colorBuffer = new Color32[cameraOptionModel.camWidth * cameraOptionModel.camHeight];
        }

        // 변수, 기능 해제 함수
        private void ReleaseVariables()
        {
            if (captureFrameCompleteMsgQ != null)
            {
                captureFrameCompleteMsgQ.Clear();
                captureFrameCompleteMsgQ = null;
            }

            if (webcamMat != null)
            {
                webcamMat.Dispose();
                webcamMat = null;
            }

            if (webcamMatTask != null && webcamMatTask.IsDisposed == false)
            {
                webcamMatTask.Dispose();
                webcamMatTask = null;
            }

            if (mob != null)
            {
                mob.Dispose();
                mob = null;
            }

            if (threadMat != null && threadMat.IsDisposed == false)
            {
                threadMat.Dispose();
                threadMat = null;
            }

            if (threadRotatedMat != null && threadRotatedMat.IsDisposed == false)
            {
                threadRotatedMat.Dispose();
                threadRotatedMat = null;
            }

            // 기능 종료
            if (captureThread != null && captureThread.IsAlive)
            {
                captureThread.Abort();
            }

            if (webcam != null && webcam.isPlaying)
            {
                webcam.Stop();
            }
        }

        // 옵션 모델 로드하는 함수
        private void LoadOptionModel()
        {
            cameraOptionModel = Model.First<CameraOptionModel>();
            preProcessOptionModel = Model.First<PreProcessOptionModel>();
            coreModuleStatusModel = Model.First<CoreModuleStatusModel>();

            // 로드 실패
            if (cameraOptionModel == null || preProcessOptionModel == null)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                        CoreModuleReportType.Error,
                        (int)CoreModuleReportErrorCode.Camera_Etc,
                        "[옵션 모델 로드에 실패했습니다]",
                        "at LoadOptionModel() of WebcamTextureCam.cs"));
            }
        }

        // 카메라 초기 설정하는 함수
        private void CameraInit()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
#endif
            webcam = new WebCamTexture(cameraOptionModel.camWidth, cameraOptionModel.camHeight);
            webcam.requestedFPS = 30;
            webcam.Play();
            webcam.Pause();
            
            // 지정한 사이즈로 열리지 않을 가능성이 있음
            cameraOptionModel.camWidth = webcam.width;
            cameraOptionModel.camHeight = webcam.height;

            coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                        CoreModuleReportType.Normal,
                        (int)CoreModuleReportNormalCode.Camera_Etc,
                        "[Opened Webcam - Width : " + cameraOptionModel.camWidth + ", Height : " + cameraOptionModel.camHeight + "]",
                        "at CameraInit() of WebcamTextureCam.cs"));

            coreModuleStatusModel.cameraStatus = CoreModuleStatus.Ready;
        }

        // Update에서 업데이트 처리 해주면 이미지 변환하고 메세지 큐에 계속 넣는 쓰레드
        private void CaptureThread()
        {
            bool thread_didUpdated;

            while (coreModuleStatusModel.cameraStatus > CoreModuleStatus.Ready)
            {
                lock (camLock) { thread_didUpdated = didUpdated; }

                if (thread_didUpdated)
                {
                    lock (camLock)
                    {
                        webcamMatTask.copyTo(threadMat);
                        didUpdated = false;
                    }

                    // 컬러 변환 -> Flip -> 인코딩 -> 바이트 변환
                    Imgproc.cvtColor(threadMat, threadMat, Imgproc.COLOR_RGB2BGR);

#if UNITY_ANDROID
                    ImagePreProcessor.RotateImageMat(threadMat, threadRotatedMat, Core.ROTATE_90_CLOCKWISE);
                    if (!(preProcessOptionModel.flipOption == FlipOption.NoFlip))
                    {
                        ImagePreProcessor.FlipImageMat(threadRotatedMat, threadRotatedMat, (int)preProcessOptionModel.flipOption);
                    }

                    Imgcodecs.imencode(".jpg", threadRotatedMat, mob);
#else
                    if (!(preProcessOptionModel.flipOption == FlipOption.NoFlip))
                    {
                        ImagePreProcessor.FlipImageMat(threadMat, threadMat, (int)preProcessOptionModel.flipOption);
                    }

                    Imgcodecs.imencode(".jpg", threadMat, mob);
#endif

                    byte[] imgbyte = mob.toArray();
                    captureFrameCompleteMsgQ.Enqueue(new CapturedFrameMsg(frameID, 0, cameraOptionModel.camWidth, cameraOptionModel.camHeight, imgbyte));

                    // 프레임 ID 증가
                    frameID++;
                }

                Thread.Sleep(1);
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

            coreModuleStatusModel.cameraStatus = CoreModuleStatus.None;
            Message.RemoveListener<InternalModuleContorl>(OnInternalModuleContorl);
        }

        private void Update()
        {
            // 모듈 상태 메세지 전송
            if (coreModuleStatusReportMsgQ.Count != 0)
            {
                Message.Send<CoreModuleStatusReportMsg>(coreModuleStatusReportMsgQ.Dequeue());
            }

            // 모델 로드 검사
            if (coreModuleStatusModel == null) { return; }

            // 카메라 상태 검사
            if (coreModuleStatusModel.cameraStatus < CoreModuleStatus.Playing) { return; }

            // 웹캠 프레임 업데이트 되면 읽어서 쓰레드가 작업할 수 있도록 처리
            if (webcam.didUpdateThisFrame)
            {
                Utils.webCamTextureToMat(webcam, webcamMat, colorBuffer);
                lock (camLock)
                {
                    webcamMat.copyTo(webcamMatTask);
                    didUpdated = true;
                }
            }

            // 메세지 큐 목록 전송 : 대부분 1만 차있음
            for (int i = 0; i < captureFrameCompleteMsgQ.Count; i++)
            {
                var msg = captureFrameCompleteMsgQ.Dequeue();
                msg.capturedTime = Time.unscaledTime;
                Message.Send(msg);
            }
        }
    }
}