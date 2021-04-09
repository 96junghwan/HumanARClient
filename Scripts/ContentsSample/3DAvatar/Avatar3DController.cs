// Boss 가능한 버전

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellBig.Module.HumanDetection;

// 한 명의 3D Human 타입 아바타 제어 클래스
public class Avatar3DController : MonoBehaviour
{
    private bool UseLowPassFilter;
    private bool UseKalmanFilter;

    private const float LowPassParam = 0.1f;
    private const float KalmanParamQ = 0.001f;
    private const float KalmanParamR = 0.0015f;

    public GameObject avatar;
    private Avatar3DBone bone;

    private JointPoint[] actualJoints;
    private List<JointPoint> positionJointsRefs;
    private List<JointPoint> angleJointsRefs;

    // VNect Porting
    private Vector3 initPosition;
    private Quaternion InitGazeRotation;
    private Quaternion gazeInverse;


    public void Activate()
    {
        if (!avatar.activeSelf) { avatar.SetActive(true); }
    }

    public void Deactivate()
    {
        if (avatar.activeSelf) { avatar.SetActive(false); }
    }

    public void SetOptions(bool useLowPassFilter, bool useKalmanFilter)
    {
        UseLowPassFilter = useLowPassFilter;
        UseKalmanFilter = useKalmanFilter;
    }

    public void KalmanUpdate(JointPoint measurement)
    {
        measurementUpdate(measurement);
        measurement.Pos3D.x = measurement.X.x + (measurement.Now3D.x - measurement.X.x) * measurement.K.x;
        measurement.Pos3D.y = measurement.X.y + (measurement.Now3D.y - measurement.X.y) * measurement.K.y;
        measurement.Pos3D.z = measurement.X.z + (measurement.Now3D.z - measurement.X.z) * measurement.K.z;
        measurement.X = measurement.Pos3D;
    }

    public void measurementUpdate(JointPoint measurement)
    {
        measurement.K.x = (measurement.P.x + KalmanParamQ) / (measurement.P.x + KalmanParamQ + KalmanParamR);
        measurement.K.y = (measurement.P.y + KalmanParamQ) / (measurement.P.y + KalmanParamQ + KalmanParamR);
        measurement.K.z = (measurement.P.z + KalmanParamQ) / (measurement.P.z + KalmanParamQ + KalmanParamR);
        measurement.P.x = KalmanParamR * (measurement.P.x + KalmanParamQ) / (KalmanParamR + measurement.P.x + KalmanParamQ);
        measurement.P.y = KalmanParamR * (measurement.P.y + KalmanParamQ) / (KalmanParamR + measurement.P.y + KalmanParamQ);
        measurement.P.z = KalmanParamR * (measurement.P.z + KalmanParamQ) / (KalmanParamR + measurement.P.z + KalmanParamQ);
    }

    public void LowPassUpdate(JointPoint jp)
    {
        jp.PrevPos3D[0] = jp.Pos3D;

        for (var i = 1; i < jp.PrevPos3D.Length; i++)
        {
            jp.PrevPos3D[i] = jp.PrevPos3D[i] * LowPassParam + jp.PrevPos3D[i - 1] * (1f - LowPassParam);
        }

        jp.Pos3D = jp.PrevPos3D[jp.PrevPos3D.Length - 1];
    }

    public Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }

    public Quaternion GetInverse(JointPoint p1, JointPoint p2, Vector3 forward)
    {
        return Quaternion.Inverse(Quaternion.LookRotation(p1.Transform.position - p2.Transform.position, forward));
    }

    private void Init()
    {
        bone = avatar.GetComponent<Avatar3DBone>();

        // ========== JointPoint-GameObject 연결 작업
        actualJoints = new JointPoint[ActualJointType.Count.Int()];
        for (var i = 0; i < ActualJointType.Count.Int(); i++)
        {
            actualJoints[i] = new JointPoint();
        }

        actualJoints[ActualJointType.Global.Int()].Transform = bone.Global.transform;

        actualJoints[ActualJointType.Head.Int()].Transform = bone.Head.transform;
        actualJoints[ActualJointType.Neck.Int()].Transform = bone.Neck.transform;
        actualJoints[ActualJointType.Hips.Int()].Transform = bone.Hips.transform;
        actualJoints[ActualJointType.Spine.Int()].Transform = bone.Spine.transform;
        actualJoints[ActualJointType.Chest.Int()].Transform = bone.Chest.transform;
        actualJoints[ActualJointType.UpperChest.Int()].Transform = bone.UpperChest.transform;

        actualJoints[ActualJointType.L_Shoulder.Int()].Transform = bone.L_Shoulder.transform;
        actualJoints[ActualJointType.L_Elbow.Int()].Transform = bone.L_Elbow.transform;
        actualJoints[ActualJointType.L_Wrist.Int()].Transform = bone.L_Wrist.transform;

        actualJoints[ActualJointType.R_Shoulder.Int()].Transform = bone.R_Shoulder.transform;
        actualJoints[ActualJointType.R_Elbow.Int()].Transform = bone.R_Elbow.transform;
        actualJoints[ActualJointType.R_Wrist.Int()].Transform = bone.R_Wrist.transform;

        actualJoints[ActualJointType.L_Hip.Int()].Transform = bone.L_Hip.transform;
        actualJoints[ActualJointType.L_Knee.Int()].Transform = bone.L_Knee.transform;
        actualJoints[ActualJointType.L_Ankle.Int()].Transform = bone.L_Ankle.transform;
        actualJoints[ActualJointType.L_Toe.Int()].Transform = bone.L_Toe.transform;

        actualJoints[ActualJointType.R_Hip.Int()].Transform = bone.R_Hip.transform;
        actualJoints[ActualJointType.R_Knee.Int()].Transform = bone.R_Knee.transform;
        actualJoints[ActualJointType.R_Ankle.Int()].Transform = bone.R_Ankle.transform;
        actualJoints[ActualJointType.R_Toe.Int()].Transform = bone.R_Toe.transform;

        // ========== Child Settings
        actualJoints[ActualJointType.Hips.Int()].Child = actualJoints[ActualJointType.Neck.Int()];
        actualJoints[ActualJointType.Neck.Int()].Child = actualJoints[ActualJointType.Head.Int()];

        actualJoints[ActualJointType.L_Shoulder.Int()].Child = actualJoints[ActualJointType.L_Elbow.Int()];
        actualJoints[ActualJointType.L_Elbow.Int()].Child = actualJoints[ActualJointType.L_Wrist.Int()];

        actualJoints[ActualJointType.R_Shoulder.Int()].Child = actualJoints[ActualJointType.R_Elbow.Int()];
        actualJoints[ActualJointType.R_Elbow.Int()].Child = actualJoints[ActualJointType.R_Wrist.Int()];

        actualJoints[ActualJointType.L_Hip.Int()].Child = actualJoints[ActualJointType.L_Knee.Int()];
        actualJoints[ActualJointType.L_Knee.Int()].Child = actualJoints[ActualJointType.L_Ankle.Int()];
        actualJoints[ActualJointType.L_Ankle.Int()].Child = actualJoints[ActualJointType.L_Toe.Int()];

        actualJoints[ActualJointType.R_Hip.Int()].Child = actualJoints[ActualJointType.R_Knee.Int()];
        actualJoints[ActualJointType.R_Knee.Int()].Child = actualJoints[ActualJointType.R_Ankle.Int()];
        actualJoints[ActualJointType.R_Ankle.Int()].Child = actualJoints[ActualJointType.R_Toe.Int()];


        // ========== Parent Settings
        actualJoints[ActualJointType.L_Elbow.Int()].Parent = actualJoints[ActualJointType.L_Shoulder.Int()];      // (실험) Shoulder도 빼볼까?
        actualJoints[ActualJointType.R_Elbow.Int()].Parent = actualJoints[ActualJointType.R_Shoulder.Int()];          // (실험) Shoulder도 빼볼까?


        // ========== Position Ref Table 작성
        positionJointsRefs = new List<JointPoint>();
        for (int i = 0; i < Joint3DData.PositionJointType.Count.Int(); i++)
        {
            positionJointsRefs.Add(null);
        }

        positionJointsRefs[Joint3DData.PositionJointType.Pelvis.Int()] = actualJoints[ActualJointType.Spine.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.Spine.Int()] = actualJoints[ActualJointType.Chest.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.Thorax.Int()] = actualJoints[ActualJointType.UpperChest.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.OP_Middle_Hip.Int()] = actualJoints[ActualJointType.Hips.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.Neck.Int()] = actualJoints[ActualJointType.Neck.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.Head.Int()] = actualJoints[ActualJointType.Head.Int()];

        positionJointsRefs[Joint3DData.PositionJointType.L_Hip.Int()] = actualJoints[ActualJointType.L_Hip.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.L_Knee.Int()] = actualJoints[ActualJointType.L_Knee.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.L_Ankle.Int()] = actualJoints[ActualJointType.L_Ankle.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.OP_L_Big_Toe.Int()] = actualJoints[ActualJointType.L_Toe.Int()];

        positionJointsRefs[Joint3DData.PositionJointType.R_Hip.Int()] = actualJoints[ActualJointType.R_Hip.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.R_Knee.Int()] = actualJoints[ActualJointType.R_Knee.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.R_Ankle.Int()] = actualJoints[ActualJointType.R_Ankle.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.OP_R_Big_Toe.Int()] = actualJoints[ActualJointType.R_Toe.Int()];

        positionJointsRefs[Joint3DData.PositionJointType.L_Shoulder.Int()] = actualJoints[ActualJointType.L_Shoulder.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.L_Elbow.Int()] = actualJoints[ActualJointType.L_Elbow.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.L_Wrist.Int()] = actualJoints[ActualJointType.L_Wrist.Int()];

        positionJointsRefs[Joint3DData.PositionJointType.R_Shoulder.Int()] = actualJoints[ActualJointType.R_Shoulder.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.R_Elbow.Int()] = actualJoints[ActualJointType.R_Elbow.Int()];
        positionJointsRefs[Joint3DData.PositionJointType.R_Wrist.Int()] = actualJoints[ActualJointType.R_Wrist.Int()];

        // 골반 기준 정면 각도 계산
        var forward = TriangleNormal(
            actualJoints[ActualJointType.Hips.Int()].Transform.position,
            actualJoints[ActualJointType.L_Hip.Int()].Transform.position,
            actualJoints[ActualJointType.R_Hip.Int()].Transform.position
        );

        // 초기 회전 정보(원본 아바타의 회전 정보) 저장
        foreach (var jointPoint in actualJoints)
        {
            if (jointPoint.Transform != null)
            {
                jointPoint.InitRotation = jointPoint.Transform.rotation;
            }

            if (jointPoint.Child != null)
            {
                jointPoint.Inverse = GetInverse(jointPoint, jointPoint.Child, forward);
                jointPoint.InverseRotation = jointPoint.Inverse * jointPoint.InitRotation;
            }
        }

        // For Hip Rotation
        var hip = actualJoints[ActualJointType.Hips.Int()];
        initPosition = actualJoints[ActualJointType.Hips.Int()].Transform.position;
        hip.Inverse = Quaternion.Inverse(Quaternion.LookRotation(forward));
        hip.InverseRotation = hip.Inverse * hip.InitRotation;

        // For Head Rotation
        var head = actualJoints[ActualJointType.Head.Int()];
        head.InitRotation = actualJoints[ActualJointType.Head.Int()].Transform.rotation;
        var gaze = bone.Nose.transform.position - actualJoints[ActualJointType.Head.Int()].Transform.position;
        head.Inverse = Quaternion.Inverse(Quaternion.LookRotation(gaze));
        head.InverseRotation = head.Inverse * head.InitRotation;
    }

    public void PoseUpdate(Human3DJoint input)
    {
        if (actualJoints == null)
        {
            Init();
        }

        // Position Update
        for (int i = 0; i < Joint3DData.PositionJointType.Count.Int(); i++)
        {
            if (positionJointsRefs[i] != null)
            {
                positionJointsRefs[i].Now3D = input.jointPositions[i];
                KalmanUpdate(positionJointsRefs[i]);
                LowPassUpdate(positionJointsRefs[i]);
                positionJointsRefs[i].Transform.position = Camera.main.ViewportToWorldPoint(positionJointsRefs[i].Pos3D + new Vector3(0f, 0f, 3f));
            }
        }

        // 골반 기준 정면 Vector3 계산
        var forward = TriangleNormal(
            actualJoints[ActualJointType.Hips.Int()].Pos3D,
            actualJoints[ActualJointType.L_Hip.Int()].Pos3D,
            actualJoints[ActualJointType.R_Hip.Int()].Pos3D);

        // Rotate Update
        for (int i = 0; i < ActualJointType.Count.Int(); i++)
        {
            if (i == ActualJointType.Head.Int()) { continue; }

            if (actualJoints[i].Parent != null)
            {
                var fv = actualJoints[i].Parent.Pos3D - actualJoints[i].Pos3D;
                actualJoints[i].Transform.rotation = Quaternion.LookRotation(actualJoints[i].Pos3D - actualJoints[i].Child.Pos3D, fv) * actualJoints[i].InverseRotation;
            }
            else if (actualJoints[i].Child != null)
            {
                actualJoints[i].Transform.rotation = Quaternion.LookRotation(actualJoints[i].Pos3D - actualJoints[i].Child.Pos3D, forward) * actualJoints[i].InverseRotation;
            }
        }

    }
}