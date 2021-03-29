using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// [임시] 자동 네트워크 상태 제어 기능의 옵션 모델,
    /// 현재는 NetworkSocketManager의 코루틴에서 체크하며 제어하고 있음,
    /// 효과는 좋음 : 승현 대리님이 금지하셨으나 쓰지 않기에는 너무 달달한 금단의 기술
    /// </summary>
    public class NetworkControlOptionModel : Model
    {
        /// <summary>
        /// "용서해라, 사스케..."
        /// </summary>
        public bool useNetworkResourceContoller;

        /// <summary>
        /// 네트워크 상태 체크 주기
        /// </summary>
        public float networkCheckCycleTime;

        /// <summary>
        /// 서버가 너무 느릴 때 잠깐 SendThread를 멈추는 시간
        /// </summary>
        public float sendPauseTime;

        /// <summary>
        /// 이미 재생한 프레임의 데이터를 수신했다는 피드백의 한도 횟수
        /// </summary>
        public int invalidDataFeedbackMax;

        /// <summary>
        /// 서버가 느리다는 피드백의 한도 횟수
        /// </summary>
        public int serverSlowFeedbackMax;

        /// <summary>
        /// 서버 상태 긍정적인 피드백의 한도 횟수
        /// </summary>
        public int positiveNetworkFeedbackMax;
    }
}