using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity;

namespace CellBig.Module.HumanDetection
{
    public class HumanADManager : MonoBehaviour
    {
        // 옵션 모델 변수
        CameraOptionModel cameraOptionModel;

        // 임시
        Mat maskFromSocket;

        // 화면 관련 변수
        Canvas canvas;
        public RawImage background;
        public RawImage human;
        Texture2D background_texture;
        Texture2D texture;
        Texture2D maskTex;
        Texture2D emptyMaskTex;

        // 콘텐츠 제어 변수
        bool doBackgroundCapture = false;
        bool inverseMaskRequest = false;
        bool onClick = false;

        // 캡처 휴먼 오브젝트 관련 변수
        public GameObject capturedHuman;
        Texture2D originalTexture;
        Texture2D quadTexture;
        Texture2D quadMaskTexture;

        // 마우스 관련 변수
        Vector3 startMouseClickVector;
        Vector3 endMouseClickVector;



        // 옵션 모델 로드하는 함수
        private void LoadOptionModels()
        {
            cameraOptionModel = Model.First<CameraOptionModel>();

            // 로드 실패
            if (cameraOptionModel == null)
            {
                Debug.Log("Option Model is not Setted yet");
            }

            // 로드 성공
            else
            {
                // 실시간 프레임 텍스처 설정
                texture = new Texture2D(cameraOptionModel.camWidth, cameraOptionModel.camHeight, TextureFormat.RGB24, false);
                human.material.SetTexture("_MainTex", texture);

                // 백그라운드 텍스처 설정 (임시)
                background_texture = new Texture2D(cameraOptionModel.camWidth, cameraOptionModel.camHeight, TextureFormat.RGB24, false);
                background.texture = background_texture;

                // Mat 생성
                maskFromSocket = new Mat(cameraOptionModel.camWidth, cameraOptionModel.camWidth, CvType.CV_8UC1);

                // 메세지 리스너 설정
                Message.AddListener<PlayFrameTextureAndHumanMaskMsg>(OnPlayFrameTextureAndHumanMaskMsg);
                Message.AddListener<DetectHumanMaskResultMsg>(OnDetectHumanMaskResultMsg);
            }
        }

        // 현재 쓸데없이 계속 세그멘테이션 연산 요청 중, 계속 이런 구조라면 필요한 경우만 요청하도록 추후에 변경
        // Texture와 해당되는 마스크 동시에 메세지로 받는 함수
        void OnPlayFrameTextureAndHumanMaskMsg(PlayFrameTextureAndHumanMaskMsg msg)
        {
            // 실시간 프레임 재생
            human.texture = msg.texture;

            // 백그라운드 복사 후 적용
            if (doBackgroundCapture)
            {
                // 배경 이미지 새로 지정
                if (background_texture == null) { background_texture = new Texture2D(cameraOptionModel.camWidth, cameraOptionModel.camHeight, TextureFormat.RGB24, false); }
                Graphics.CopyTexture(msg.texture, background_texture);
                background.texture = background_texture;

                // 업데이트
                doBackgroundCapture = false;
                Debug.Log("Background Captured");
            }

            // 마스킹 요청
            if (inverseMaskRequest)
            {
                // 원본 프레임 복사
                originalTexture = new Texture2D(cameraOptionModel.camWidth, cameraOptionModel.camHeight, TextureFormat.RGB24, false);
                Graphics.CopyTexture(msg.texture, originalTexture);

                // 개별 프레임 연산 요청 메세지 송신
                byte[] frameData = originalTexture.EncodeToJPG();
                Message.Send<PrivateFrameRequestMsg>(new PrivateFrameRequestMsg(0, cameraOptionModel.camWidth, cameraOptionModel.camHeight, frameData, (int)NNType.Segmentation));
                inverseMaskRequest = false;
            }
        }

        // 개별 요청한 마스크 데이터를 받는 함수
        void OnDetectHumanMaskResultMsg(DetectHumanMaskResultMsg msg)
        {
            capturedHuman.SetActive(true);
            //maskFromSocket.put(0, 0, msg.maskByte, 0, msg.maskByte.Length);
            HumanSegMaskProcessor.InverseAreaMaskProcessing(maskFromSocket, maskTex, human, cameraOptionModel.camWidth, cameraOptionModel.camHeight, startMouseClickVector, endMouseClickVector, originalTexture, quadTexture, quadMaskTexture, capturedHuman);
        }

        // 키보드 R키 눌렸을 경우 호출되고 모든 오브젝트 Reset 하는 함수
        void ResetObject()
        {
            // 실시간 프레임 마스크 새로 할당해서 기존 마스크 해제.. 잘 되나?
            maskTex = new Texture2D(cameraOptionModel.camWidth, cameraOptionModel.camHeight, TextureFormat.R8, false);
            Color[] colors = maskTex.GetPixels();
            for (int i = 0; i < (cameraOptionModel.camWidth * cameraOptionModel.camHeight); i++ )
            {
                colors[i].r = 255;
            }
            maskTex.SetPixels(colors);
            maskTex.Apply();
            human.material.SetTexture("_AlphaTex", maskTex);

            capturedHuman.transform.position = new Vector3(-1f, -1f, -20f);
            capturedHuman.SetActive(false);
            Debug.Log("Reset");
        }

        // 키보드 C키 눌렸을 경우 호출되고 Background용 NoHuman 프레임 Capture 하는 함수
        void CaptureNoHumanBackground()
        {
            doBackgroundCapture = true;
        }

        private void OnDestroy()
        {
            if (maskFromSocket != null) { maskFromSocket.Dispose(); maskFromSocket = null; }
            Message.RemoveListener<PlayFrameTextureAndHumanMaskMsg>(OnPlayFrameTextureAndHumanMaskMsg);
            Message.RemoveListener<DetectHumanMaskResultMsg>(OnDetectHumanMaskResultMsg);
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            capturedHuman.SetActive(false);
        }

        private void Update()
        {
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = Camera.main.farClipPlane - 0.01f;
            }

            // 마우스 클릭 시작한 경우
            if (Input.GetMouseButtonDown(0))
            {
                onClick = true;
                startMouseClickVector = Input.mousePosition;
            }

            // 마우스 클릭 중인 경우
            if (Input.GetMouseButton(0))
            {
                if (capturedHuman.activeSelf)
                {
                    Vector3 screenPos = Input.mousePosition;
                    screenPos.z = 50f;
                 //   capturedHuman.transform.position = Camera.main.ScreenToWorldPoint(screenPos);     // 좌표 변환이 실시간으로 안먹음. 걍 안먹음
                    //capturedHuman.transform.position = Input.mousePosition;
                    //capturedHuman.transform.position = Camera.main.ScreenToViewportPoint(Input.mousePosition); // 되는데, 변화량이 미미함
                }
            }

            // 마우스 클릭 끝난 경우
            if (Input.GetMouseButtonUp(0))
            {
                endMouseClickVector = Input.mousePosition;
                onClick = false;

                // 게임 오브젝트 아직 없는 경우
                if (!capturedHuman.activeSelf)
                {
                    inverseMaskRequest = true;
                }
            }

            //  C키 눌렸을 때
            if (Input.GetKeyDown(KeyCode.C))
            {
                CaptureNoHumanBackground();
            }

            // R키 눌렸을 때
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetObject();
            }
        }
    }
}