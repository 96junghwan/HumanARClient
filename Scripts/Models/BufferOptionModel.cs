using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 버퍼 저장 및 재생 옵션 모델,
    /// 실시간 변경을 지원하는 옵션은 _RT로 표시됨
    /// </summary>
    public class BufferOptionModel : Model
    {
        /// <summary>
        /// BufferManager에서 설정할 Buffer의 사이즈 크기,
        /// 크게 지정할 수록 메모리를 많이 요구하나, 너무 적으면 딜레이 적용이 어려움
        /// </summary>
        public int bufferMax;

        /// <summary>
        /// BufferManager에서 원본 영상과 관절/마스크 데이터의 싱크를 맞추기 위한 딜레이,
        /// 초 단위
        /// </summary>
        public float delaySeconds_RT;

        /// <summary>
        /// BufferManager에서 Delay를 사용하지 않고 수신하는 데이터를 바로 재생하는 옵션
        /// </summary>
        public bool noDelay;
    }
}