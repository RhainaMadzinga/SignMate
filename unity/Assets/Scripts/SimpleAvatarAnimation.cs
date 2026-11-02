using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SimpleAvatarAnimation : MonoBehaviour
{
    public float speed = 2f;
    public float armRaiseAmount = 60f;
    public float forearmBendAmount = 30f;
    public float headTurnAmount = 15f;

    private Animator animator;
    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform head;

    private Quaternion rightUpperArmStart;
    private Quaternion rightLowerArmStart;
    private Quaternion headStart;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        head = animator.GetBoneTransform(HumanBodyBones.Head);

        if (rightUpperArm != null) rightUpperArmStart = rightUpperArm.localRotation;
        if (rightLowerArm != null) rightLowerArmStart = rightLowerArm.localRotation;
        if (head != null) headStart = head.localRotation;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;

        if (rightUpperArm != null)
        {
            Quaternion target = rightUpperArmStart * Quaternion.Euler(0f, 0f, -armRaiseAmount);
            rightUpperArm.localRotation = Quaternion.Slerp(rightUpperArmStart, target, t);
        }

        if (rightLowerArm != null)
        {
            Quaternion target = rightLowerArmStart * Quaternion.Euler(0f, 0f, -forearmBendAmount);
            rightLowerArm.localRotation = Quaternion.Slerp(rightLowerArmStart, target, t);
        }

        if (head != null)
        {
            Quaternion target = headStart * Quaternion.Euler(0f, headTurnAmount, 0f);
            head.localRotation = Quaternion.Slerp(headStart, target, t);
        }
    }
}