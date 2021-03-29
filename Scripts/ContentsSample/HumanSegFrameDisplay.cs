using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity;

namespace CellBig.Module.HumanDetection
{
    public class HumanSegFrameDisplay : MonoBehaviour
    {
        // 원본 영상 및 세그멘테이션 마스킹 적용을 위한 변수
        private CameraOptionModel cameraOptionModel;
        private Canvas canvas;

        public RawImage background;
        public RawImage human;
        public Texture2D background_texture;

        private Texture2D texture;
        private Texture2D maskTex;
        private Mat mask255Mat;
        private Mat maskMat;

        private int maskWaitMax = 10;
        private int textureID;
        private int maskID;

        // Texture 재생 메세지 수신 함수
        private void OnPlayFrameTextureMsg(PlayFrameTextureMsg msg)
        {
            human.texture = msg.texture;
            textureID = msg.frameID;

            // 마스크가 10프레임 째 안오고 있는 경우 : 255 마스크로 적용해서 배경 가리기
            if (10 < (textureID - maskID))
            {
                mask255Mat.copyTo(maskMat);
                HumanSegMaskProcessor.MaskProcessing(maskMat, maskTex);
            }
        }

        // Texture & Mask 재생 메세지 수신 함수
        private void OnPlayFrameTextureAndHumanMaskMsg(PlayFrameTextureAndHumanMaskMsg msg)
        {
            // 콘텐츠 쪽에서 요청한 데이터일 경우 : 패스
            if (msg.frameID < 0) { return; }

            human.texture = msg.texture;

            // 배경 마스킹
            msg.mask.copyTo(maskMat);
            HumanSegMaskProcessor.MaskProcessing(maskMat, maskTex);
            maskID = msg.frameID;
        }

        // 변수 할당 함수
        private void AllocateVariables()
        {
            canvas = GetComponent<Canvas>();
            texture = new Texture2D(cameraOptionModel.camWidth, cameraOptionModel.camHeight, TextureFormat.RGB24, false);
            maskMat = new Mat(cameraOptionModel.camHeight, cameraOptionModel.camWidth, CvType.CV_8UC1);
            mask255Mat = HumanSegMaskProcessor.CreateAlphaMat_255(cameraOptionModel.camHeight, cameraOptionModel.camWidth);
            maskTex = HumanSegMaskProcessor.CreateAlphaTexture_255(cameraOptionModel.camWidth, cameraOptionModel.camHeight);
        }

        // 변수 해제 함수
        private void ReleaseVariables()
        {
            if (maskMat != null) { maskMat.Dispose(); maskMat = null; }
            if (mask255Mat != null) { mask255Mat.Dispose(); mask255Mat = null; }
        }

        // 옵션 모델 로드 함수
        private void LoadOptionModel()
        {
            cameraOptionModel = Model.First<CameraOptionModel>();
            if (cameraOptionModel == null) { Debug.Log("Option Model is not setted yet"); }
            else { AllocateVariables(); Init(); }
        }

        // 초기 설정 함수
        private void Init()
        {
            human.material.SetTexture("_MainTex", texture);
            human.material.SetTexture("_AlphaTex", maskTex);
            SetBackground();

            Message.AddListener<PlayFrameTextureAndHumanMaskMsg>(OnPlayFrameTextureAndHumanMaskMsg);
            Message.AddListener<PlayFrameTextureMsg>(OnPlayFrameTextureMsg);
        }

        // 세그멘테이션 배경으로 쓰일 텍스처 설정
        private void SetBackground()
        {
            background.texture = background_texture;
        }

        private void OnDestroy()
        {
            ReleaseVariables();
            Message.RemoveListener<PlayFrameTextureMsg>(OnPlayFrameTextureMsg);
            Message.RemoveListener<PlayFrameTextureAndHumanMaskMsg>(OnPlayFrameTextureAndHumanMaskMsg);
        }
        
        private void Update()
        {
            // 옵션 모델 로드 안됐으면 로드
            if (cameraOptionModel == null) { LoadOptionModel(); }

            // 캔버스에 카메라 설정 안됐을 경우 : 설정
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = Camera.main.farClipPlane - 0.01f;
            }
        }
    }
}