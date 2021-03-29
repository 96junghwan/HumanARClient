using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 서버에서 수신한 결과 데이터의 후처리 옵션 모델
    /// 실시간 변경을 지원하는 옵션은 _RT로 표시됨
    /// </summary>
    public class PostProcessOptionModel : Model
    {
        /// <summary>
        /// 전송률(SendRate) 설정으로 인해 중간에 비어있는 관절 데이터를 보간해주는 옵션
        /// </summary>
        public bool useEmptyJointLerpGenerator;

        /// <summary>
        /// 모든 관절이 일정 점수를 만족하는 경우만 HumanJoint 리스트에 추가하는 옵션
        /// </summary>
        public bool useJointPerfectFilter;

        /// <summary>
        /// 관절의 Kalman Filter 사용 옵션,
        /// 현재 미지원
        /// </summary>
        public bool useJointKalmanFilter;

        /// <summary>
        /// 관절의 Low Pass Filter 사용 옵션,
        /// 현재 미지원
        /// </summary>
        public bool useJointLowPassFilter;

        /// <summary>
        /// 서버에서 수신한 관절 체계를 15개 구조로 변경하는 옵션
        /// </summary>
        public bool useJointParsing;

        /// <summary>
        /// 관절 데이터를 기준에 따라 정렬하는 옵션,
        /// 현재 미지원
        /// </summary>
        public bool useJointAlignment;

        /// <summary>
        /// 임시로 관절 떨림 보정해주는 옵션,
        /// 실험 단계
        /// </summary>
        public bool useTempCompressJointShake_RT;
    }
}