using UnityEngine;
using CellBig.Module.HumanDetection;


// 샘플 콘텐츠 : HumanAR 제어 튜토리얼 용
public class SampleC : MonoBehaviour
{
    // Unity Inspector 창에서 체크하여 모듈 시작 신호를 받기위한 변수
    public bool START = false;
    private bool onModuleLoading = false;
    private bool isModelLoaded = false;
    private bool isModuleStarted = false;

    // Unity Inspector 창에서 체크하여 실시간 옵션 변경하기 위한 변수
    public bool useAlphaPose = false;
    public bool useSegmentation = false;
    public float delaySeconds = 1f;

    // 사용할 옵션 모델
    private BufferOptionModel bufferOptionModel;
    private NetworkOptionModel networkOptionModel;
    private CoreModuleStatusModel coreModuleStatusModel;

   

    
    // 모듈 상태 메세지 수신 함수 : 현재는 로그 출력만
    private void OnCoreModuleStatusReportMsg(CoreModuleStatusReportMsg msg)
    {
        if (msg.reportType == CoreModuleReportType.Error)
        {
            Debug.LogError(msg.statusMsg);
        }

        else if (msg.reportType == CoreModuleReportType.Warning)
        {
            Debug.LogWarning(msg.statusMsg);
        }

        else if(msg.reportType == CoreModuleReportType.Normal)
        {
            Debug.Log(msg.statusMsg);
        }
    }

    // HumanARStarter에서 세팅한 옵션 모델 중 사용할 것만 불러오기
    private void LoadOptionModel()
    {
        // 모듈 싱글톤 인스턴스 얻기 : 첫 생성은 HumanARStarter에서 수행함
        bufferOptionModel = Model.First<BufferOptionModel>();
        networkOptionModel = Model.First<NetworkOptionModel>();
        coreModuleStatusModel = Model.First<CoreModuleStatusModel>();

        // 옵션 모델 로드 실패
        if (bufferOptionModel == null || networkOptionModel == null || coreModuleStatusModel == null)
        {
            Debug.LogWarning("아직 HumanAR 모듈의 옵션 모델이 생성되지 않았습니다.");
            START = false;
        }

        // 옵션 모델 로드 성공
        else
        {
            isModelLoaded = true;
            Go();
        }
    }

    // 사전 커스텀 옵션 모델 세팅
    private void CustomOptionModelSet()
    {
        // 아래 방식과 같이 코드 상으로나 .json이나 .txt, 혹은 UI 등 여러 방법으로 세팅을 불러와서 ※어떻게든 Model을 수정하면 됨※
        bufferOptionModel.delaySeconds_RT = 1f;
    }

    // 실시간 옵션 검사 후 모델 수정
    private void OptionModelChange_RT()
    {
        if (delaySeconds != bufferOptionModel.delaySeconds_RT)
        {
            bufferOptionModel.delaySeconds_RT = delaySeconds;
        }

        if (useAlphaPose && (networkOptionModel.nnType_RT & (int)NNType.AlphaPose) != (int)NNType.AlphaPose)
        {
            networkOptionModel.nnType_RT = networkOptionModel.nnType_RT | (int)NNType.AlphaPose;
        }

        if (!useAlphaPose && (networkOptionModel.nnType_RT & (int)NNType.AlphaPose) == (int)NNType.AlphaPose)
        {
            networkOptionModel.nnType_RT = networkOptionModel.nnType_RT - (int)NNType.AlphaPose;
        }

        if (useSegmentation && (networkOptionModel.nnType_RT & (int)NNType.Segmentation) != (int)NNType.Segmentation)
        {
            networkOptionModel.nnType_RT = networkOptionModel.nnType_RT | (int)NNType.Segmentation;
        }

        if (!useSegmentation && (networkOptionModel.nnType_RT & (int)NNType.Segmentation) == (int)NNType.Segmentation)
        {
            networkOptionModel.nnType_RT = networkOptionModel.nnType_RT - (int)NNType.Segmentation;
        }
    }

    // HumanAR 모듈 시작하는 함수
    private void Go()
    {
        // 커스텀 세팅
        CustomOptionModelSet();

        // HumanAR의 코어 모듈들에게 시작 메세지 전송 : 사실상 이 한 줄이 SampleC.cs 코드의 본체
        // 중간에 CoreModuleOperationIndex.Pause와 CoreModuleOperationIndex.Resume으로 바꿔서 지시하면 일시정지 가능
        Message.Send<CoreModuleControlMsg>(new CoreModuleControlMsg((int)CoreModuleIndex.Camera | (int)CoreModuleIndex.NetworkSocket | (int)CoreModuleIndex.Buffer, CoreModuleOperationIndex.Init));
        Message.Send<CoreModuleControlMsg>(new CoreModuleControlMsg((int)CoreModuleIndex.Camera | (int)CoreModuleIndex.NetworkSocket | (int)CoreModuleIndex.Buffer, CoreModuleOperationIndex.Play));

        // 시작 처리
        isModuleStarted = true;

        // 모듈 로딩 종료
        onModuleLoading = false;
    }

    // HumanAR 모듈 종료하는 함수
    private void End()
    {
        Message.Send<CoreModuleControlMsg>(new CoreModuleControlMsg((int)CoreModuleIndex.Camera | (int)CoreModuleIndex.NetworkSocket | (int)CoreModuleIndex.Buffer, CoreModuleOperationIndex.Stop));
        isModuleStarted = false;
    }




    private void Awake()
    {
        Message.AddListener<CoreModuleStatusReportMsg>(OnCoreModuleStatusReportMsg);
    }

    private void OnDestroy()
    {
        Message.RemoveListener<CoreModuleStatusReportMsg>(OnCoreModuleStatusReportMsg);
    }

    private void Update()
    {
        // Inspector 창에서 START 변수 체크되면 일단 모델 로드 후 모듈 시작
        if (START && !isModuleStarted && !onModuleLoading)
        {
            onModuleLoading = true;
            LoadOptionModel();
        }

        // Inspector 창에서 START 변수 체크 해제되면 모듈 종료
        if (!START && isModelLoaded && isModuleStarted)
        {
            End();
        }

        // 실시간 옵션 변경 검사 : 이 방법은 Inspector창으로 제어하기 위한 무식한 방법
        if (isModelLoaded && isModuleStarted)
        {
            OptionModelChange_RT();
        }
    }
}
