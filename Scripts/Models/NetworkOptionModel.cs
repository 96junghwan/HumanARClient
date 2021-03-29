using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// 딥러닝 서버와의 Socket 통신 옵션 모델,
    /// 실시간 변경을 지원하는 옵션은 _RT로 표시됨
    /// </summary>
    public class NetworkOptionModel : Model
    {
        /// <summary>
        /// 서버로 보낼 이미지의 전송률 : (1/SendRate)의 비율로 서버에 전송함,
        /// 만약 3으로 설정했다면, 이미지 3장을 캡처할 때 1장을 서버에 전송함
        /// </summary>
        public int sendRate_RT;

        /// <summary>
        /// 접속할 서버 컴퓨터,
        /// ARConstants.cs의 ServerType 클래스 참조
        /// </summary>
        public ServerType serverType;

        /// <summary>
        /// 접속할 서버의 Port 번호,
        /// 자동으로 설정됨,
        /// ARConstants.cs의 ServerAddress 클래스 참조
        /// </summary>
        public int serverPort;

        /// <summary>
        /// 서버의 딥러닝 신경망 타입 옵션,
        /// 비트마스크로 설정,
        /// ARConstants.cs의 NNType 클래스 참조
        /// </summary>
        public int nnType_RT;

        /// <summary>
        /// 송신 큐의 최대 크기 지정,
        /// 네트워크 버퍼가 터지지 않도록 위한 설정
        /// </summary>
        public int sendingQSizeMax;

        /// <summary>
        /// 딥러닝 서버에 접속한 뒤 서비스를 이용하기 위한 접속 코드,
        /// 상용화 버전에서 클라이언트의 버전 제한을 걸기 위함,
        /// ※상용화 빌드 시 담당자와 협의 필요함※
        /// ARConstants.cs의 AccessCode 클래스 참조
        /// </summary>
        public string serverAccessKeyCode;

        /// <summary>
        /// 접속할 서버 컴퓨터의 IP 주소,
        /// 자동으로 설정됨,
        /// ARConstants.cs의 ServerAddress 클래스 참조
        /// </summary>
        public string serverIp;
    }
}