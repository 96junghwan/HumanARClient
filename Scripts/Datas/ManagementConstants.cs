using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 코어 스크립트의 종류 : 비트 마스크 연산 가능,
    /// 예시) (int)CoreModuleIndex.Camera | (int)CoreModuleIndex.Buffer
    /// </summary>
    public enum CoreModuleIndex : int
    {
        /// <summary>
        /// 카메라 모듈
        /// </summary>
        Camera = 1,

        /// <summary>
        /// 딥러닝 서버와 통신하는 네트워크 소켓 모듈
        /// </summary>
        NetworkSocket = 2,

        /// <summary>
        /// 원본 영상과 딥러닝 연산 결과 데이터의 싱크를 맞추어 재생하는 버퍼 모듈
        /// </summary>
        Buffer = 4,
    }

    /// <summary>
    /// 코어 모듈이 수행할 동작의 인덱스
    /// </summary>
    public enum CoreModuleOperationIndex : int
    {
        /// <summary>
        /// 초기 설정 및 필요 자원 로드 명령
        /// </summary>
        Init = 1, 

        /// <summary>
        /// 시작 명령,
        /// 사전에 필요한 자원이 할당되어야 함
        /// </summary>
        Play,

        /// <summary>
        /// 일시중지 명령
        /// </summary>
        Pause,

        /// <summary>
        /// 종료 명령,
        /// 기능 종료 및 자원 해제
        /// </summary>
        Stop,

        /// <summary>
        /// 재부팅 명령,
        /// Stop -> Init의 순서로 명령을 한 것과 동일한 합성 명령,
        /// 실시간 변경이 어려운 해상도 옵션이나 카메라 변경 옵션, 서버 종류 교체 등의 작업에 용이함
        /// </summary>
        ReBoot,
    }

    /// <summary>
    /// 코어 모듈의 상태
    /// </summary>
    public enum CoreModuleStatus
    {
        /// <summary>
        /// Component로 부착이 안되어있는 상태
        /// </summary>
        None = 1,

        /// <summary>
        /// Component로 부착만 되어있는 상태
        /// </summary>
        NotReady,

        /// <summary>
        /// 기능 수행에 필요한 자원이 할당되어 시작 대기 중인 상태
        /// </summary>
        Ready,

        /// <summary>
        /// 기능이 동작 중인 상태
        /// </summary>
        Playing,

        /// <summary>
        /// 기능 일시중지 상태
        /// </summary>
        Pause,
    }

    /// <summary>
    /// 코어 모듈의 보고 종류
    /// </summary>
    public enum CoreModuleReportType : int
    {
        /// <summary>
        /// 에러, 즉시 조치 취하는 것이 바람직함
        /// </summary>
        Error = 1,

        /// <summary>
        /// 경고
        /// </summary>
        Warning,

        /// <summary>
        /// 정상
        /// </summary>
        Normal,
    }

    /// <summary>
    /// 보고할 코어 모듈의 에러 상태 세부 코드
    /// </summary>
    public enum CoreModuleReportErrorCode : int
    {
        /// <summary>
        /// 카메라 에러,
        /// 카메라 혹은 비디오가 열리지 않는 에러
        /// </summary>
        Camera_CannotOpenCamera = 1,

        /// <summary>
        /// 카메라 에러,
        /// 기타 에러
        /// </summary>
        Camera_Etc,




        /// <summary>
        /// 네트워크 소켓 에러,
        /// 해당 서버의 소켓이 열려있지 않거나 본 장치가 네트워크에 연결되어 있지 않음
        /// </summary>
        Network_NotOpenedServerSocket = 1000,

        /// <summary>
        /// 네트워크 소켓 에러,
        /// 요청한 신경망이 접속된 서버에 열려있지 않음
        /// </summary>
        Network_NotOpenedNN,

        /// <summary>
        /// 해당 서버의 인원이 가득 차 서비스에 접속할 수 없음
        /// </summary>
        Network_ServerFull,

        /// <summary>
        /// 해당 서버에 알맞은 접속 코드가 아님
        /// </summary>
        Network_InvalidAccessCode,

        /// <summary>
        /// 네트워크 소켓 에러,
        /// 기타 에러
        /// </summary>
        Network_Etc,

        


        /// <summary>
        /// 버퍼 에러,
        /// 기타 에러
        /// </summary>
        Buffer_Etc = 2000,
    }

    /// <summary>
    /// 보고할 코어 모듈의 경고 상태 세부 코드
    /// </summary>
    public enum CoreModuleReportWarningCode : int
    {
        /// <summary>
        /// 카메라 경고,
        /// 기타 경고
        /// </summary>
        Camera_Etc = 1,




        /// <summary>
        /// 네트워크 소켓 경고,
        /// 전송률이 한계까지 느려져있음
        /// </summary>
        Network_SendRateMax = 1000,

        /// <summary>
        /// 네트워크 소켓 경고,
        /// 기타 경고
        /// </summary>
        Network_Etc,

        


        /// <summary>
        /// 버퍼 경고,
        /// 버퍼에서 해당 프레임 번호에 해당하는 인덱스를 찾을 수 없음
        /// </summary>
        Buffer_CannotFoundIndex = 2000,

        /// <summary>
        /// 버퍼 경고,
        /// 이미 재생한 프레임의 데이터를 수신함
        /// </summary>
        Buffer_InvalidData,

        /// <summary>
        /// 버퍼 경고,
        /// 기타 경고
        /// </summary>
        Buffer_Etc,
    }

    /// <summary>
    /// 보고할 코어 모듈의 정상 상태 세부 코드
    /// </summary>
    public enum CoreModuleReportNormalCode
    {
        /// <summary>
        /// 카메라 정상,
        /// 비디오 재생 끝남
        /// </summary>
        Camera_VideoEnd = 1,

        /// <summary>
        /// 카메라 정상,
        /// 기타 코드
        /// </summary>
        Camera_Etc,



        
        /// <summary>
        /// 네트워크 소켓 정상,
        /// Negative 피드백 받아서 전송률 낮춤
        /// </summary>
        Network_SendRateUp = 1000,

        /// <summary>
        /// 네트워크 소켓 정상,
        /// Positive 피드백 받아서 전송률 높임
        /// </summary>
        Network_SendRateDown,

        /// <summary>
        /// 네트워크 소켓 정상,
        /// 기타 코드
        /// </summary>
        Network_Etc,




        /// <summary>
        /// 버퍼 정상,
        /// 기타 코드
        /// </summary>
        Buffer_Etc = 2000,
    }
}
