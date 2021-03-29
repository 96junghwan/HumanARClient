using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// OCam 전용 옵션 모델
    /// 실시간 변경을 지원하는 옵션은 _RT로 표시됨
    /// </summary>
    public class OCamOptionModel : Model
    {
        /// <summary>
        /// OCam의 Exposure 설정 옵션
        /// </summary>
        public long exposure;

        /// <summary>
        /// OCam의 gain 설정 옵션
        /// </summary>
        public long gain;
    }
}