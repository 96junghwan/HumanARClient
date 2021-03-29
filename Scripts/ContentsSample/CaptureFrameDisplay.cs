using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace CellBig.Module.HumanDetection
{
    // 캡처된 원본 영상만 디스플레이 해주는 클래스 (샘플 콘텐츠)
    public class CaptureFrameDisplay : MonoBehaviour
    {
        public RawImage rawImage;
        Canvas canvas;

        // 메세지로 들어온 원본 영상 재생
        void OnPlayFrameTextureMsg(PlayFrameTextureMsg msg)
        {
            rawImage.texture = msg.texture;
        }

        public void Init()
        {
            canvas = GetComponent<Canvas>();
            Message.AddListener<PlayFrameTextureMsg>(OnPlayFrameTextureMsg);
        }

        private void OnDestroy()
        {
            Message.RemoveListener<PlayFrameTextureMsg>(OnPlayFrameTextureMsg);
        }

        private void Update()
        {
            if (canvas == null) { Init(); }

            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = Camera.main.farClipPlane - 0.01f;
            }
        }
    }
}