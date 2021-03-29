using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 이미지 데이터의 전처리 옵션 모델,
    /// 실시간 변경을 지원하는 옵션은 _RT로 표시됨
    /// </summary>
    public class PreProcessOptionModel : Model
    {
        /// <summary>
        /// 영상의 상하좌우 반전 옵션,
        /// ARConstants.cs의 FlipOption 클래스 참조
        /// </summary>
        public FlipOption flipOption;

        /// <summary>
        /// 영상의 회전 옵션
        /// ARConstants.cs의 RotationOption 클래스 참조
        /// </summary>
        public RotationOption rotationOption;

        /// <summary>
        /// 서버에 전송하기 전 이미지를 640*480으로 줄이는 옵션,
        /// 최적화를 위해 봉인 중
        /// </summary>
        public bool useResize_RT;

        /// <summary>
        /// 문제가 있어서 서버에 보내기 전 리사이즈 On/Off를 금지하는 옵션, 
        /// 안드로이드에서 적용 필요하며 현재 자동으로 설정 중
        /// </summary>
        public bool resizeLock;
    }
}