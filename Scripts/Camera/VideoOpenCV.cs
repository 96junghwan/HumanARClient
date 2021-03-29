using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System.Threading;

namespace CellBig.Module.HumanDetection
{
    public class VideoOpenCV : MonoBehaviour
    {
        // 비디오 경로 : 절대 경로
        private string videoPath = "D:/Videos/SMTM/VVS.mp4";

        // 영상 데이터 저장용 변수
        private int frameID = 1;
        private VideoCapture capture;
        private Mat mat;
        private MatOfByte mob;

        // 쓰레드 관련 변수
        private Thread captureThread;
        private Queue<CapturedFrameMsg> msgQ;

        // 옵션 모델
        private CameraOptionModel cameraOptionModel;
        private CoreModuleStatusModel coreModuleStatusModel;

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
                CameraInit();
            }
        }

        // 기능 시작 함수
        private void ModulePlay()
        {
            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Ready)
            {
                captureThread.Start();
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
            }

            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Pause)
            {
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
            }
        }

        // 기능 일시 정지 함수
        private void ModulePause()
        {
            if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Playing)
            {
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
            mat = new Mat();
            mob = new MatOfByte();
            capture = new VideoCapture();
            msgQ = new Queue<CapturedFrameMsg>();
            captureThread = new Thread(new ThreadStart(CaptureThread));
        }

        // 변수 해제 함수
        private void ReleaseVariables()
        {
            // 쓰레드 종료 밑작업, 캡처 변수 해제
            if (mat != null) { mat.Dispose(); }
            if (mob != null) { mob.Dispose(); }

            // Thread 종료
            if (captureThread != null)
            {
                if (captureThread.IsAlive)
                {
                    captureThread.Abort();
                }
            }

            // VideoCapture 해제
            if (capture != null) { capture.Dispose(); capture = null; }
        }

        // 옵션 로드하고 초기 설정하는 함수
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
                        "at LoadOptionModel() of VideoOpenCV.cs"));
            }

            // 로드 성공
            else
            {
                AllocateVariables();
                CameraInit();
            }
        }

        // 카메라 초기화하는 함수
        private void CameraInit()
        {
            // 비디오 열기 성공했을 경우
            if (capture.open(videoPath))
            {
                // 오픈한 영상 사이즈 저장 후 출력
                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Playing;
                capture.read(mat);
                cameraOptionModel.camWidth = mat.width();
                cameraOptionModel.camHeight = mat.height();
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                        CoreModuleReportType.Normal,
                        (int)CoreModuleReportNormalCode.Camera_Etc,
                        "[Video Opened - width : " + cameraOptionModel.camWidth + ", height : " + cameraOptionModel.camHeight + "]",
                        "at CameraInit() of VideoOpenCV.cs"));

                coreModuleStatusModel.cameraStatus = CoreModuleStatus.Ready;
            }

            // 비디오 열기 실패했을 경우
            else
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                        CoreModuleReportType.Error,
                        (int)CoreModuleReportErrorCode.Camera_CannotOpenCamera,
                        "[입력한 경로에서 비디오를 찾을 수 없습니다]",
                        "at CameraInit() of VideoOpenCV.cs"));
            }
        }

        // 비디오 캡처 쓰레드
        private void CaptureThread()
        {
            while (coreModuleStatusModel.cameraStatus >= CoreModuleStatus.Playing)
            {
                Thread.Sleep(1);

                // 캡처 중지 상태일 경우
                if (coreModuleStatusModel.cameraStatus == CoreModuleStatus.Pause)
                {
                    continue;
                }

                // 동영상 끝난 경우
                if (!capture.read(mat))
                {
                    coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                        CoreModuleReportType.Normal,
                        (int)CoreModuleReportNormalCode.Camera_VideoEnd,
                        "[파일 재생 종료]", 
                        "at CameraInit() of VideoOpenCV.cs"));

                    capture.Dispose();
                    break;
                }

                // 영상 데이터 변환 후 메세지 큐에 입력
                Imgcodecs.imencode(".jpg", mat, mob);
                byte[] imgbyte = mob.toArray();
                msgQ.Enqueue(new CapturedFrameMsg(frameID, 0f, cameraOptionModel.camWidth, cameraOptionModel.camHeight, imgbyte));
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

            // 모델 로드 검사
            if (coreModuleStatusModel == null) { return; }

            // 재생 상태가 아니면 리턴
            if (coreModuleStatusModel.cameraStatus < CoreModuleStatus.Playing) { return; }

            // 카메라 준비 완료 : 메세지 큐에 있는 프레임 메세지로 전송, 한 번에 4개로 제한
            for (int i = 0; i < msgQ.Count; i++)
            {
                var msg = msgQ.Dequeue();
                msg.capturedTime = Time.unscaledTime;
                Message.Send<CapturedFrameMsg>(msg);
            }
        }
    }
}