using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 카메라 옵션 모델
    /// </summary>
    public class CameraOptionModel : Model
    {
        /// <summary>
        /// 사용할 카메라 타입 옵션,
        /// ARConstants.cs의 CameraType 클래스 참조
        /// </summary>
        public CameraType cameraType;

        /// <summary>
        /// 카메라 영상의 가로 해상도 옵션
        /// </summary>
        public int camWidth;

        /// <summary>
        /// 카메라 영상의 세로 해상도 옵션
        /// </summary>
        public int camHeight;
    }
}