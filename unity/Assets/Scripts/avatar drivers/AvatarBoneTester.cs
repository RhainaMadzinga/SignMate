using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class AvatarBoneTester : MonoBehaviour
{
    private void Start()
    {
        Animator animator = GetComponent<Animator>();

        Debug.Log("Animator found: " + (animator != null));

        if (animator == null)
        {
            Debug.LogError("Animator component not found.");
            return;
        }

        Debug.Log("Avatar assigned: " + (animator.avatar != null));

        if (animator.avatar == null)
        {
            Debug.LogError("Animator avatar is NULL.");
            return;
        }

        Debug.Log("Avatar valid: " + animator.avatar.isValid);
        Debug.Log("Avatar humanoid: " + animator.avatar.isHuman);

        if (!animator.avatar.isValid)
        {
            Debug.LogError("Animator avatar is not valid.");
            return;
        }

        if (!animator.avatar.isHuman)
        {
            Debug.LogError("Animator avatar is not humanoid.");
            return;
        }

        Debug.Log("Head: " + animator.GetBoneTransform(HumanBodyBones.Head));
        Debug.Log("Left Hand: " + animator.GetBoneTransform(HumanBodyBones.LeftHand));
        Debug.Log("Right Hand: " + animator.GetBoneTransform(HumanBodyBones.RightHand));

        Debug.Log("Left Thumb Proximal: " + animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal));
        Debug.Log("Left Thumb Intermediate: " + animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate));
        Debug.Log("Left Index Proximal: " + animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal));
        Debug.Log("Left Index Intermediate: " + animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate));
        Debug.Log("Left Index Distal: " + animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal));

        Debug.Log("Right Thumb Proximal: " + animator.GetBoneTransform(HumanBodyBones.RightThumbProximal));
        Debug.Log("Right Index Proximal: " + animator.GetBoneTransform(HumanBodyBones.RightIndexProximal));
        Debug.Log("Right Middle Proximal: " + animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal));
        Debug.Log("Right Ring Proximal: " + animator.GetBoneTransform(HumanBodyBones.RightRingProximal));
        Debug.Log("Right Little Proximal: " + animator.GetBoneTransform(HumanBodyBones.RightLittleProximal));
    }
}
    

