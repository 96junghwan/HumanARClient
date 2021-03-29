using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 코어 모듈의 현재 상태를 저장하고 있는 모델
    /// </summary>
    public class CoreModuleStatusModel : Model
    {
        /// <summary>
        /// {Camera, NetworkSocket, Buffer} 의 사용 옵션을 비트마스크로 설정,
        /// ManagementConstants.cs의 CoreModuleIndex enum 참조,
        /// 예시) coreModuleUseOption = (int)CoreModuleIndex.Camera | (int)CoreModuleIndex.Buffer;
        /// </summary>
        public int coreModuleUseOption;

        /// <summary>
        /// 카메라 모듈의 현재 상태,
        /// CoreModuleStatus 참조
        /// </summary>
        public CoreModuleStatus cameraStatus;

        /// <summary>
        /// 네트워크 모듈의 현재 상태,
        /// CoreModuleStatus 참조
        /// </summary>
        public CoreModuleStatus networkStatus;

        /// <summary>
        /// 버퍼 모듈의 현재 상태,
        /// CoreModuleStatus 참조
        /// </summary>
        public CoreModuleStatus bufferStatus;
    }
}