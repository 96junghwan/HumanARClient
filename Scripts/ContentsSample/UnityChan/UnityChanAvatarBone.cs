using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellBig.Module.HumanDetection;

public class UnityChanAvatarBone : MonoBehaviour
{
    public List<GameObject> positionObjectList;
    public List<GameObject> angleObjectList;
    
    public GameObject Global;
    public GameObject Head;
    public GameObject Neck;
    public GameObject L_Clavicle;
    public GameObject L_Shoulder;
    public GameObject L_Elbow;
    public GameObject L_Wrist;
    public GameObject R_Clavicle;
    public GameObject R_Shoulder;
    public GameObject R_Elbow;
    public GameObject R_Wrist;
    public GameObject L_UppderLeg;
    public GameObject L_LowerLeg;
    public GameObject L_Foot;
    public GameObject L_Toe;
    public GameObject R_UppderLeg;
    public GameObject R_LowerLeg;
    public GameObject R_Foot;
    public GameObject R_Toe;
    public GameObject L_Eye;
    public GameObject R_Eye;

    private Animator animator;

    private void Awake()
    {
        positionObjectList = new List<GameObject>();
        angleObjectList = new List<GameObject>();

        // Position
        positionObjectList.Add(null);
        positionObjectList.Add(Neck);
        positionObjectList.Add(R_Shoulder);
        positionObjectList.Add(R_Elbow);
        positionObjectList.Add(R_Wrist);
        positionObjectList.Add(L_Shoulder);
        positionObjectList.Add(L_Elbow);
        positionObjectList.Add(L_Wrist);
        positionObjectList.Add(null);
        positionObjectList.Add(R_UppderLeg);
        positionObjectList.Add(R_LowerLeg);
        positionObjectList.Add(R_Foot);
        positionObjectList.Add(L_UppderLeg);
        positionObjectList.Add(L_LowerLeg);
        positionObjectList.Add(L_Foot);
        positionObjectList.Add(R_Eye);
        positionObjectList.Add(L_Eye);
        positionObjectList.Add(null);
        positionObjectList.Add(null);
        positionObjectList.Add(L_Toe);
        positionObjectList.Add(null);
        positionObjectList.Add(null);
        positionObjectList.Add(R_Toe);
        positionObjectList.Add(null);
        positionObjectList.Add(null);

        positionObjectList.Add(R_Foot);
        positionObjectList.Add(R_LowerLeg);
        positionObjectList.Add(R_UppderLeg);
        positionObjectList.Add(L_UppderLeg);
        positionObjectList.Add(L_LowerLeg);
        positionObjectList.Add(L_Foot);
        positionObjectList.Add(R_Wrist);
        positionObjectList.Add(R_Elbow);
        positionObjectList.Add(R_Shoulder);
        positionObjectList.Add(L_Shoulder);
        positionObjectList.Add(L_Elbow);
        positionObjectList.Add(L_Wrist);

        positionObjectList.Add(Neck);
        positionObjectList.Add(null);
        positionObjectList.Add(Global); // Global
        positionObjectList.Add(null);
        positionObjectList.Add(null);
        positionObjectList.Add(null);
        positionObjectList.Add(Head);
        positionObjectList.Add(null);
        positionObjectList.Add(L_Eye);
        positionObjectList.Add(R_Eye);
        positionObjectList.Add(null);
        positionObjectList.Add(null);

        // Angle
        angleObjectList.Add(Global);
        angleObjectList.Add(L_UppderLeg);
        angleObjectList.Add(R_UppderLeg);
        angleObjectList.Add(null);
        angleObjectList.Add(L_LowerLeg);
        angleObjectList.Add(R_LowerLeg);
        angleObjectList.Add(null);
        angleObjectList.Add(L_Foot);
        angleObjectList.Add(R_Foot);
        angleObjectList.Add(null);
        angleObjectList.Add(L_Toe);
        angleObjectList.Add(R_Toe);
        angleObjectList.Add(null);
        angleObjectList.Add(L_Clavicle);
        angleObjectList.Add(R_Clavicle);
        angleObjectList.Add(null);
        angleObjectList.Add(L_Shoulder);
        angleObjectList.Add(R_Shoulder);
        angleObjectList.Add(L_Elbow);
        angleObjectList.Add(R_Elbow);
        angleObjectList.Add(L_Wrist);
        angleObjectList.Add(R_Wrist);
        angleObjectList.Add(null);
        angleObjectList.Add(null);

        Debug.Log("Position Bone List Count : " + positionObjectList.Count);
        Debug.Log("Angle Bone List Count : " + angleObjectList.Count);


        // 밑에는 일단 작동 안하는 것 같음
        /*
        animator = avatar.GetComponent<Animator>();

        // ========== Joint Position Transform List 초기화
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.Head));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.Neck));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightShoulder));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightHand));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftShoulder));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftHand));

        pTransList.Add(null);   // 8 : OP_Middle_Hip
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightFoot));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftFoot));

        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightEye));
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftEye));

        pTransList.Add(null);   // 17 : OP_R_Ear
        pTransList.Add(null);   // 18 : OP_L_Ear
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftToes));     // OP_L_Big_Toe
        pTransList.Add(null);     // OP_L_Small_Toe
        pTransList.Add(null);     // OP_L_Heel
        pTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightToes));     // OP_R_Big_Toe
        pTransList.Add(null);     // OP_R_Small_Toe
        pTransList.Add(null);     // OP_R_Heel
        
        pTransList.Add(null);     // R_Ankle
        pTransList.Add(null);     // R_Knee
        pTransList.Add(null);     // R_Hip
        pTransList.Add(null);     // L_Hip
        pTransList.Add(null);     // L_Knee
        pTransList.Add(null);     // L_Ankle

        pTransList.Add(null);     // R_Wrist
        pTransList.Add(null);     // R_Elbow
        pTransList.Add(null);     // R_Shoulder
        pTransList.Add(null);     // L_Shoulder
        pTransList.Add(null);     // L_Elbow
        pTransList.Add(null);     // L_Wrist

        pTransList.Add(null);     // Neck (LSP)
        pTransList.Add(null);     // TopOfHead (LSP)
        pTransList.Add(null);     // Pelvis (MPII)
        pTransList.Add(null);     // Thorax (MPII)
        pTransList.Add(null);     // Spine (H36M)
        pTransList.Add(null);     // Jaw (H36M)
        pTransList.Add(null);     // Head (H36M)
        pTransList.Add(null);     // Nose
        pTransList.Add(null);     // L_Eye
        pTransList.Add(null);     // R_Eye
        pTransList.Add(null);     // L_Ear
        pTransList.Add(null);     // R_Ear

        // ========== Joint Angle Transfrom List 초기화
        aTransList.Add(modelObject.transform);     // Global
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg));     // L_Hip
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));     // R_Hip
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.Spine));     // Spine_01
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg));     // L_Knee
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg));     // R_Knee
        aTransList.Add(null);     // Spine_02
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftFoot));     // L_Ankle
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightFoot));     // R_Ankle
        aTransList.Add(null);     // Spine_03
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftToes));     // L_Toe
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightToes));     // R_Toe
        aTransList.Add(null);     // Middle_Shoulder
        aTransList.Add(null);     // L_Clavicle
        aTransList.Add(null);     // R_Clavicle
        aTransList.Add(null);     // Nose
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));     // L_Shoulder
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));     // R_Shoulder
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));     // L_Elbow
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));     // R_Elbow
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.LeftHand));     // L_Wrist
        aTransList.Add(animator.GetBoneTransform(HumanBodyBones.RightHand));     // R_Wrist
        aTransList.Add(null);     // L_Palm : 손바닥인데 유효 X
        aTransList.Add(null);     // R_Palm : 손바닥인데 유효 X
        */
    }

    private void OnDestroy()
    {
        if(positionObjectList != null)
        {
            positionObjectList.Clear(); 
            positionObjectList = null;
        }
        
        if(angleObjectList != null)
        {
            angleObjectList.Clear(); 
            angleObjectList = null;
        }
    }
}
