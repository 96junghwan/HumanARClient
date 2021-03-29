using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellBig.Module.HumanDetection
{
    /// <summary>
    /// HumanAR의 코어 모듈의 동작을 제어하는 메세지,
    /// 코어 모듈을 지정하여 {시작, 중단, 재개, 종료, 재시작} 등을 지시 가능
    /// </summary>
    public class CoreModuleControlMsg : Message
    {
        /// <summary>
        /// 명령을 내리고자 하는 코어모듈의 인덱스, 비트마스크 사용, ManagementConstants.cs의 CoreModuleIndex 클래스 참조
        /// </summary>
        public int coreModuleIndex;

        /// <summary>
        /// 지정한 모듈에게 내릴 명령,
        /// ManagementConstants.cs의 CoreModuleOperationIndex 클래스 참조
        /// </summary>
        public CoreModuleOperationIndex coreModuleOperationIndex;

        /// <summary>
        /// 코어 모듈 제어 메세지 생성자
        /// </summary>
        /// <param name="coreModuleIndex"> 동작을 지시할 코어 모듈 번호, ManagementConstants.cs의 CoreModuleIndex 클래스 참조, 비트마스크 사용 가능 </param>
        /// <param name="coreModuleOperationIndex"> 코어 모듈에 지시할 동작 번호, ManagementConstants.cs의 CoreModuleOperationIndex 클래스 참조 </param>
        public CoreModuleControlMsg(int coreModuleIndex, CoreModuleOperationIndex coreModuleOperationIndex)
        {
            this.coreModuleIndex = coreModuleIndex;
            this.coreModuleOperationIndex = coreModuleOperationIndex;
        }
    }
    
    /// <summary>
    /// HumanAR의 코어 모듈 동작 중의 특이사항을 전달하는 메세지
    /// </summary>
    public class CoreModuleStatusReportMsg : Message
    {
        /// <summary>
        /// 코어 모듈의 보고 타입 : 에러/경고/정상,
        /// ManagementConstants.cs의 CoreModuleReportType 참조
        /// </summary>
        public CoreModuleReportType reportType;

        /// <summary>
        /// 보고 사항의 상세 코드, 
        /// ManagementConstants.cs의 각종 CoreModuleReport(Error/Warning/Normal)Code 참조
        /// </summary>
        public int reportCode;

        /// <summary>
        /// 상태 메세지 String, 전달하고자 하는 말
        /// </summary>
        public string statusMsg;

        /// <summary>
        /// 상태 메세지를 송신한 함수명과 스크립트명
        /// </summary>
        public string statusReference;

        /// <summary>
        /// HumanAR의 코어 모듈 동작 중의 특이사항을 전달하는 메세지 생성자
        /// </summary>
        /// <param name="reportType">에러/경고/정상 중에 하나 선택, CoreModuleReportType 참조</param>
        /// <param name="reportCode">보고 상세 코드, CoreModuleReport(Error/Warning/Normal)Code 중 하나 참조</param>
        /// <param name="statusMsg">보고할 메세지 String, 전달하고자 하는 말</param>
        /// <param name="statusReference">상태 메세지를 송신한 함수명과 스크립트명</param>
        public CoreModuleStatusReportMsg(CoreModuleReportType reportType, int reportCode, string statusMsg, string statusReference)
        {
            this.reportType = reportType;
            this.reportCode = reportCode;
            this.statusMsg = statusMsg;
            this.statusReference = statusReference;
        }
    }
    



    /// <summary>
    /// 내부 모듈 제어 메세지, 컨텐츠에서 모듈 제어 신호를 받고 실제로 제어 신호를 받는 메시지,
    /// 컨텐츠에서 사용할 일은 없음
    /// </summary>
    public class InternalModuleContorl : Message
    {
        /// <summary>
        /// 코어 모듈 인덱스,
        /// CoreModuleIndex enum을 비트마스크 연산 이용해 입력
        /// </summary>
        public int coreModuleIndex;

        /// <summary>
        /// 코어 모듈에게 전달할 명령 인덱스
        /// </summary>
        public CoreModuleOperationIndex coreModuleOperationIndex;

        public InternalModuleContorl(int coreModuleIndex, CoreModuleOperationIndex coreModuleOperationIndex)
        {
            this.coreModuleIndex = coreModuleIndex;
            this.coreModuleOperationIndex = coreModuleOperationIndex;
        }
    }

    /// <summary>
    /// 소켓 네트워크 Controller에 피드백 전달하는 메세지
    /// 컨텐츠에서 사용할 일은 없음
    /// </summary>
    public class NetworkFeedbackMsg : Message
    {
        public int networkFeedbackType;

        public NetworkFeedbackMsg(int networkFeedbackType)
        {
            this.networkFeedbackType = networkFeedbackType;
        }
    }
}

