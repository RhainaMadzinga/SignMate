using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HumanoidHandReferences : MonoBehaviour
{
    public Transform leftHand;
    public Transform leftThumbProximal;
    public Transform leftThumbIntermediate;
    public Transform leftThumbDistal;
    public Transform leftIndexProximal;
    public Transform leftIndexIntermediate;
    public Transform leftIndexDistal;
    public Transform leftMiddleProximal;
    public Transform leftMiddleIntermediate;
    public Transform leftMiddleDistal;
    public Transform leftRingProximal;
    public Transform leftRingIntermediate;
    public Transform leftRingDistal;
    public Transform leftLittleProximal;
    public Transform leftLittleIntermediate;
    public Transform leftLittleDistal;

    public Transform rightHand;
    public Transform rightThumbProximal;
    public Transform rightThumbIntermediate;
    public Transform rightThumbDistal;
    public Transform rightIndexProximal;
    public Transform rightIndexIntermediate;
    public Transform rightIndexDistal;
    public Transform rightMiddleProximal;
    public Transform rightMiddleIntermediate;
    public Transform rightMiddleDistal;
    public Transform rightRingProximal;
    public Transform rightRingIntermediate;
    public Transform rightRingDistal;
    public Transform rightLittleProximal;
    public Transform rightLittleIntermediate;
    public Transform rightLittleDistal;

    private void Awake()
    {
        Animator animator = GetComponent<Animator>();

        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        leftThumbProximal = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
        leftThumbIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
        leftThumbDistal = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal);
        leftIndexProximal = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
        leftIndexIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
        leftIndexDistal = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
        leftMiddleProximal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
        leftMiddleIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
        leftMiddleDistal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);
        leftRingProximal = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal);
        leftRingIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate);
        leftRingDistal = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal);
        leftLittleProximal = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);
        leftLittleIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate);
        leftLittleDistal = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal);

        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        rightThumbProximal = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
        rightThumbIntermediate = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
        rightThumbDistal = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
        rightIndexProximal = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        rightIndexIntermediate = animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
        rightIndexDistal = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal);
        rightMiddleProximal = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        rightMiddleIntermediate = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
        rightMiddleDistal = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
        rightRingProximal = animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
        rightRingIntermediate = animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate);
        rightRingDistal = animator.GetBoneTransform(HumanBodyBones.RightRingDistal);
        rightLittleProximal = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
        rightLittleIntermediate = animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate);
        rightLittleDistal = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal);
    }
}
