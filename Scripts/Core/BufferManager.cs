using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity;

namespace CellBig.Module.HumanDetection
{
    // 지정된 수 만큼 버퍼를 생성해 재생을 관리하는 클래스
    public class BufferManager : MonoBehaviour
    {
        // 옵션 모델
        private CoreModuleStatusModel coreModuleStatusModel;
        private CameraOptionModel cameraOptionModel;
        private NetworkOptionModel networkOptionModel;
        private BufferOptionModel bufferOptionModel;
        private PostProcessOptionModel postProcessOptionModel;

        // 버퍼 관리 변수
        private List<HumanData> buffer;
        private int SAVE_INDEX;
        private int PLAY_INDEX;
        private int lastFoundIndex;     // 마지막으로 관절/마스크 저장했던 버퍼의 인덱스, 추후 탐색을 빨리 하기 위해 남기는 Lagacy

        // 휴먼 포즈 보간용
        private List<HumanJoint> lagacyJointList;   // 관절 업데이트가 늦어질 때를 대비해 이전에 수신한 관절 정보 담아두는 리스트. [프레임 보간 용]
        private int emptyFrameCount;

        // 네트워크 상태 피드백 메세지 큐
        private Queue<NetworkFeedbackMsg> networkFeedbackMsgQ;

        // 버퍼 옵션 : noDelay용 변수
        private Queue<PlayHumanJointListMsg> noDelayJointMsgQ;
        private Queue<PlayFrameTextureAndHumanMaskMsg> noDelayMaskMsgQ;
        private MatOfByte mob;
        private Mat noDelayMask;
        private int noDelayFrameCount;

        // 모듈 상태를 전달할 메세지 큐
        private Queue<CoreModuleStatusReportMsg> coreModuleStatusReportMsgQ;

        // 임시 관절 후처리 클래스 인스턴스
        private TempJointPostProcesser tempJointPostProcesser;




        // 코어 모듈 동작 제어 메세지 수신하는 함수
        private void OnInternalModuleContorl(InternalModuleContorl msg)
        {
            // 메세지에 해당 안되면 무시
            if ((msg.coreModuleIndex & (int)CoreModuleIndex.Buffer) != (int)CoreModuleIndex.Buffer) { return; }

            // 지정된 동작 수행
            switch (msg.coreModuleOperationIndex)
            {
                // 자원 할당
                case CoreModuleOperationIndex.Init:
                    ModuleInit();
                    break;

                // 기능 시작
                case CoreModuleOperationIndex.Play:
                    ModulePlay();
                    break;

                // 기능 일시정지
                case CoreModuleOperationIndex.Pause:
                    ModulePause();
                    break;

                // 기능 종료
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
                        (int)CoreModuleReportErrorCode.Buffer_Etc,
                        "[사전에 정의된 모듈 제어 동작이 아닙니다]",
                        "at OnCoreModuleControlMsg() of BufferManager.cs"));
                    break;
            }
        }

        // 자원 할당 함수
        private void ModuleInit()
        {
            LoadOptionModel();

            if (coreModuleStatusModel.bufferStatus == CoreModuleStatus.NotReady)
            {
                AllocateVariables();
                coreModuleStatusModel.bufferStatus = CoreModuleStatus.Ready;
            }
            
        }

        // 기능 시작 함수
        private void ModulePlay()
        {
            if (coreModuleStatusModel.bufferStatus == CoreModuleStatus.Ready)
            {
                coreModuleStatusModel.bufferStatus = CoreModuleStatus.Playing;
            }
        }

        // 기능 일시 정지 함수
        private void ModulePause()
        {
            if (coreModuleStatusModel.bufferStatus == CoreModuleStatus.Playing)
            {
                coreModuleStatusModel.bufferStatus = CoreModuleStatus.Pause;
            }
        }

        // 기능 종료 및 자원 해제 함수
        private void ModuleStop()
        {
            if (coreModuleStatusModel.bufferStatus > CoreModuleStatus.NotReady)
            {
                ReleaseVariables();
            }

            coreModuleStatusModel.bufferStatus = CoreModuleStatus.NotReady;
        }




        // 캡처된 프레임을 버퍼에 저장하는 함수
        private void OnCapturedFrameMsg(CapturedFrameMsg msg)
        {
            // 카메라 타입에 따라 텍스처 저장 방법 다르게 적용
            if (cameraOptionModel.cameraType == CameraType.OCam)
            {
                buffer[SAVE_INDEX].texture.LoadRawTextureData(msg.imageByte);
                //buffer[SAVE_INDEX].texture.LoadImage(msg.imageByte);
            }
            else
            {
                buffer[SAVE_INDEX].texture.LoadImage(msg.imageByte);
            }

            // 버퍼에 저장
            buffer[SAVE_INDEX].frameID = msg.frameID;
            buffer[SAVE_INDEX].capturedTime = msg.capturedTime;
            buffer[SAVE_INDEX].texture.Apply();
            buffer[SAVE_INDEX].isFrameUpdated = true;

            // SAVE_INDEX 증가
            if (++SAVE_INDEX >= bufferOptionModel.bufferMax) { SAVE_INDEX = 0; }
        }     

        // 수신된 관절 리스트를 버퍼에 저장하는 함수
        private void OnDetectHumanJointResultMsg(DetectHumanJointResultMsg msg)
        {
            // 콘텐츠에서 요청한 데이터일 경우
            if (msg.frameID < 0)
            {
                return;
            }

            // 버퍼에서 딜레이 적용 안하고 바로 재생하는 경우
            if (bufferOptionModel.noDelay)
            {
                noDelayJointMsgQ.Enqueue(new PlayHumanJointListMsg(msg.frameID, msg.jointList));
                return;
            }

            // 버퍼에서 프레임 번호 탐색
            int index = FindIndex(msg.frameID);

            // 버퍼에 해당 프레임 번호가 없을 경우
            if (index == -1)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Warning,
                            (int)CoreModuleReportWarningCode.Buffer_CannotFoundIndex,
                        "[버퍼에서 일치하는 frameID를 찾을 수 없습니다 : " + msg.frameID + "]",
                        "at OnDetectHumanJointResultMsg() of BufferManager.cs"));
                networkFeedbackMsgQ.Enqueue(new NetworkFeedbackMsg(NetworkFeedbackType.InvalidData));
                return;
            }

            // 이미 재생된 프레임의 관절 리스트를 수신한 경우 : 관절 밀림 현상
            if (!buffer[index].isFrameUpdated)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Warning,
                            (int)CoreModuleReportWarningCode.Buffer_InvalidData,
                        "[이미 재생된 프레임의 관절을 수신했습니다]",
                        "at OnDetectHumanJointResultMsg() of BufferManager.cs"));
                networkFeedbackMsgQ.Enqueue(new NetworkFeedbackMsg(NetworkFeedbackType.InvalidData));
                return;
            }

            // 사람이 검출되지 않은 경우
            if (msg.jointList.Count == 0)
            {
                buffer[index].jointList = msg.jointList;
                buffer[index].isJointUpdated = true;
            }

            // 사람이 한 명 이상 검출된 경우
            else
            {
                // 매칭되는 프레임 번호가 버퍼 안에 있을 경우
                buffer[index].jointMax = msg.jointList[0].jointMax;
                buffer[index].jointList = msg.jointList;
                buffer[index].isJointUpdated = true;

                // 이전 빈 프레임 싹 보간 작업
                if (postProcessOptionModel.useEmptyJointLerpGenerator)
                {
                    int tempIndex = index;
                    emptyFrameCount = networkOptionModel.sendRate_RT - 1;

                    // 빈 프레임 수 만큼 보간
                    for (int i = 1; i <= emptyFrameCount; i++)
                    {
                        if (lagacyJointList == null) { break; } // 이전 관절 데이터가 없음. 보간 못함
                        if (buffer[index].jointList.Count == lagacyJointList.Count) // 사람 수 같을 때만 보간함.
                        {
                            if (--tempIndex < 0) { tempIndex = bufferOptionModel.bufferMax - 1; }
                            buffer[tempIndex].jointList = EmptyJointLerpGenerator.JointLerpGenerator(lagacyJointList, buffer[index].jointList, (float)(emptyFrameCount - i) / (emptyFrameCount + 2), buffer[index].jointMax);
                            buffer[tempIndex].isJointUpdated = true;
                        }
                    }

                    // 레거시 새로 갱신
                    lagacyJointList = msg.jointList;
                }
            }
        }

        // 수신된 마스크를 버퍼에 저장하는 함수
        private void OnDetectHumanMaskResultMsg(DetectHumanMaskResultMsg msg)
        {
            // 콘텐츠 쪽에서 요청한 데이터일 경우
            if (msg.frameID < 0) { return; }

            // jpg byte[] -> Mat
            mob.fromArray(msg.maskByte);
            Mat tempMask = Imgcodecs.imdecode(mob, Imgcodecs.IMWRITE_JPEG_QUALITY);
            Imgproc.cvtColor(tempMask, tempMask, Imgproc.COLOR_BGR2GRAY);

            // 버퍼에서 딜레이 적용 안하고 바로 재생하는 옵션일 경우 
            if (bufferOptionModel.noDelay)
            {
                // 결과 마스크가 원본 프레임과 크기가 일치하지 않을 경우 : 원본 프레임 크기만큼 마스크 크기 키우기
                if (cameraOptionModel.camHeight != msg.height)
                {
                    Imgproc.resize(tempMask, noDelayMask, new Size(cameraOptionModel.camWidth, cameraOptionModel.camHeight));
                }

                // 마스크와 프레임의 크기가 일치할 경우 : 바로 적용
                else
                {
                    tempMask.copyTo(noDelayMask);
                }

                // Update로 옮기기
                noDelayMaskMsgQ.Enqueue(new PlayFrameTextureAndHumanMaskMsg(msg.frameID, null, noDelayMask));
                return;
            }

            // 버퍼에서 프레임 번호 탐색
            int index = FindIndex(msg.frameID);

            // 버퍼에 해당 프레임 번호가 없을 경우
            if (index == -1)
            {
                /*
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Warning,
                            (int)CoreModuleReportWarningCode.Buffer_CannotFoundIndex,
                        "[버퍼에서 일치하는 frameID를 찾을 수 없습니다 : " + msg.frameID + "]",
                        "at OnDetectHumanMaskResultMsg() of BufferManager.cs"));
                */
                return;
            }

            // 이미 재생된 프레임의 마스크가 들어온 경우 : 마스크 밀림 현상
            if (!buffer[index].isFrameUpdated)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Warning,
                            (int)CoreModuleReportWarningCode.Buffer_InvalidData,
                        "[이미 재생된 프레임의 마스크를 수신했습니다]",
                        "at OnDetectHumanMaskResultMsg() of BufferManager.cs"));
                networkFeedbackMsgQ.Enqueue(new NetworkFeedbackMsg(NetworkFeedbackType.InvalidData));
                return;
            }

            // 버퍼에 해당 프레임 번호가 있을 경우 : 정상 데이터 수신
            if (index != -1)
            {
                // 결과 마스크가 원본 프레임과 크기가 일치하지 않을 경우 : 원본 프레임 크기만큼 마스크 크기 키우기
                if (cameraOptionModel.camHeight != msg.height)
                {
                    Imgproc.resize(tempMask, buffer[index].mask, new Size(cameraOptionModel.camWidth, cameraOptionModel.camHeight));
                    buffer[index].isMaskUpdated = true;
                }

                // 마스크와 프레임의 크기가 일치할 경우 : 바로 적용
                else
                {
                    tempMask.copyTo(buffer[index].mask);
                    buffer[index].isMaskUpdated = true;
                }
            }

            tempMask.Dispose();
        }




        // 옵션 모델들 로드하는 함수
        private void LoadOptionModel()
        {
            cameraOptionModel = Model.First<CameraOptionModel>();
            networkOptionModel = Model.First<NetworkOptionModel>();
            bufferOptionModel = Model.First<BufferOptionModel>();
            postProcessOptionModel = Model.First<PostProcessOptionModel>();
            coreModuleStatusModel = Model.First<CoreModuleStatusModel>();

            // 옵션 모델 로드 실패
            if (cameraOptionModel == null || networkOptionModel == null || bufferOptionModel == null || postProcessOptionModel == null)
            {
                coreModuleStatusReportMsgQ.Enqueue(new CoreModuleStatusReportMsg(
                            CoreModuleReportType.Error,
                            (int)CoreModuleReportErrorCode.Buffer_Etc,
                        "[옵션 모델 로드에 실패했습니다]",
                        "at LoadOptionModel() of BufferManager.cs"));
            }
        }
        
        // 변수 할당 함수
        private void AllocateVariables()
        {
            SAVE_INDEX = 0;
            PLAY_INDEX = 0;
            lastFoundIndex = 0;
            noDelayFrameCount = 1;

            tempJointPostProcesser = new TempJointPostProcesser(postProcessOptionModel);
            noDelayMask = new Mat(cameraOptionModel.camHeight, cameraOptionModel.camWidth, CvType.CV_8UC1);
            mob = new MatOfByte();

            networkFeedbackMsgQ = new Queue<NetworkFeedbackMsg>();
            noDelayJointMsgQ = new Queue<PlayHumanJointListMsg>();
            noDelayMaskMsgQ = new Queue<PlayFrameTextureAndHumanMaskMsg>();
            
            lagacyJointList = new List<HumanJoint>();
            buffer = new List<HumanData>();

            // 버퍼 지정된 한도만큼 미리 생성
            for (int i = 0; i < bufferOptionModel.bufferMax; i++)
            {
                buffer.Add(new HumanData(cameraOptionModel.camWidth, cameraOptionModel.camHeight));
            }

            // 메세지 리스너 추가
            Message.AddListener<CapturedFrameMsg>(OnCapturedFrameMsg);
            Message.AddListener<DetectHumanJointResultMsg>(OnDetectHumanJointResultMsg);
            Message.AddListener<DetectHumanMaskResultMsg>(OnDetectHumanMaskResultMsg);
        }

        // 변수 해제 함수
        private void ReleaseVariables()
        {
            // buffer의 Mask Mat 자원 해제
            for (int i = 0; i < bufferOptionModel.bufferMax; i++)
            {
                buffer[i].mask.Dispose();
            }

            tempJointPostProcesser.Destroy();

            // 자원 해제
            if (buffer != null) { buffer.Clear(); buffer = null; }
            if (lagacyJointList != null) { lagacyJointList = null; }
            if (noDelayMask != null) { noDelayMask.Dispose(); noDelayMask = null; }

            Message.RemoveListener<CapturedFrameMsg>(OnCapturedFrameMsg);
            Message.RemoveListener<DetectHumanJointResultMsg>(OnDetectHumanJointResultMsg);
            Message.RemoveListener<DetectHumanMaskResultMsg>(OnDetectHumanMaskResultMsg);
        }

        // 버퍼에서 입력된 frameID 찾아서 인덱스 반환하는 함수
        int FindIndex(int frameID)
        {
            int index = -1;
            int tempIndex = lastFoundIndex;

            for (int i = 0; i < bufferOptionModel.bufferMax; i++)
            {
                if (buffer[tempIndex].frameID == frameID)
                {
                    index = tempIndex;
                    lastFoundIndex = index;
                    break;
                }

                if (++tempIndex >= bufferOptionModel.bufferMax) { tempIndex = 0; }
            }

            return index;
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
                coreModuleStatusModel.bufferStatus = CoreModuleStatus.None;
            }

            // 리스너 해제
            if (coreModuleStatusReportMsgQ != null) { coreModuleStatusReportMsgQ.Clear(); coreModuleStatusReportMsgQ = null; }
            Message.RemoveListener<InternalModuleContorl>(OnInternalModuleContorl);
        }

        private void Update()
        {
            // HumanAR의 유일한 자동 자원 해제 구간, 안 쓸 방안을 찾고 있음 : Pool 방식 써야 해결될 듯
            Resources.UnloadUnusedAssets();

            // 모듈 상태 메세지 전송
            if (coreModuleStatusReportMsgQ.Count != 0)
            {
                Message.Send<CoreModuleStatusReportMsg>(coreModuleStatusReportMsgQ.Dequeue());
            }

            // 모델 검사
            if (coreModuleStatusModel == null) { return; }

            // 버퍼 준비 안된 경우
            if (coreModuleStatusModel.bufferStatus < CoreModuleStatus.Playing) { return; }

            // NetworkFeedbackMsg 보낼 것이 있을 경우
            for (int i = 0; i < networkFeedbackMsgQ.Count; i++)
            {
                Message.Send<NetworkFeedbackMsg>(networkFeedbackMsgQ.Dequeue());
            }

            // noDelay Joint 전송
            if (noDelayJointMsgQ.Count != 0)
            {
                Message.Send<PlayHumanJointListMsg>(noDelayJointMsgQ.Dequeue());
            }

            // noDelay Mask 전송
            if (noDelayMaskMsgQ.Count != 0)
            {
                Message.Send<PlayFrameTextureAndHumanMaskMsg>(noDelayMaskMsgQ.Dequeue());
            }

            // noDelay 충돌 검사
            if (bufferOptionModel.noDelay && bufferOptionModel.delaySeconds_RT > 0f) { bufferOptionModel.delaySeconds_RT = 0f; }

            // DelaySeconds 복사
            float delaySeconds = bufferOptionModel.delaySeconds_RT;

            // (현재 시간) > (캡처된 시간 + 딜레이 시간) 을 만족하고 프레임이 아직 재생이 안된 신선한 프레임인 경우에 재생 데이터 메세지로 전송
            while (Time.unscaledTime > (buffer[PLAY_INDEX].capturedTime + delaySeconds))
            {
                // 재생 버퍼 중지 상태가 아닐 경우 : 현재 버퍼 상태에 따라 재생 메세지 송신
                if (coreModuleStatusModel.bufferStatus == CoreModuleStatus.Playing)
                {
                    if (buffer[PLAY_INDEX].isFrameUpdated)
                    {
                        Message.Send<PlayFrameTextureMsg>(new PlayFrameTextureMsg(buffer[PLAY_INDEX].frameID, buffer[PLAY_INDEX].texture));
                    }

                    else { break; }

                    if (buffer[PLAY_INDEX].isJointUpdated)
                    {
                        // 임시 보정 적용
                        if (postProcessOptionModel.useTempCompressJointShake_RT) { tempJointPostProcesser.CompressJointShake(buffer[PLAY_INDEX].frameID, buffer[PLAY_INDEX].jointList); }
                        Message.Send<PlayHumanJointListMsg>(new PlayHumanJointListMsg(buffer[PLAY_INDEX].frameID, buffer[PLAY_INDEX].jointList));
                    }

                    if (buffer[PLAY_INDEX].isMaskUpdated)
                    {
                        Message.Send<PlayFrameTextureAndHumanMaskMsg>(new PlayFrameTextureAndHumanMaskMsg(buffer[PLAY_INDEX].frameID, buffer[PLAY_INDEX].texture, buffer[PLAY_INDEX].mask));
                    }
                }

                // 데이터 재생 처리, 버퍼 인덱스 증가
                buffer[PLAY_INDEX].isFrameUpdated = false;
                buffer[PLAY_INDEX].isJointUpdated = false;
                buffer[PLAY_INDEX].isMaskUpdated = false;
                if (++PLAY_INDEX >= bufferOptionModel.bufferMax) { PLAY_INDEX = 0; }
            }
        }
    }
}