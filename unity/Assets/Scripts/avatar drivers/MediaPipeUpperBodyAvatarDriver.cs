using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MediaPipeUpperBodyAvatarDriver : MonoBehaviour
{
    [Header("Smoothing")]
    public float smoothSpeed = 12f;

    [Header("Offsets")]
    public Vector3 chestOffsetEuler;
    public Vector3 headOffsetEuler;
    public Vector3 leftUpperArmOffsetEuler;
    public Vector3 leftLowerArmOffsetEuler;
    public Vector3 rightUpperArmOffsetEuler;
    public Vector3 rightLowerArmOffsetEuler;

    private Animator animator;

    private Transform chest;
    private Transform upperChest;
    private Transform head;
    private Transform leftUpperArm;
    private Transform leftLowerArm;
    private Transform rightUpperArm;
    private Transform rightLowerArm;

    private Quaternion chestOffset;
    private Quaternion headOffset;
    private Quaternion leftUpperArmOffset;
    private Quaternion leftLowerArmOffset;
    private Quaternion rightUpperArmOffset;
    private Quaternion rightLowerArmOffset;

    private Vector3[] poseLandmarks;
    private bool hasPose;

    private void Awake()
{
    Animator animator = GetComponent<Animator>();
    Debug.Log("Animator on object: " + gameObject.name + " = " + animator);

    if (animator == null)
    {
        Debug.LogError("No Animator found on this object.");
        return;
    }

    Debug.Log("Animator avatar = " + animator.avatar);
    Debug.Log("Animator isHuman = " + (animator.avatar != null && animator.avatar.isHuman));
    Debug.Log("Animator isValid = " + (animator.avatar != null && animator.avatar.isValid));

    chest = animator.GetBoneTransform(HumanBodyBones.Chest);
    upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
    head = animator.GetBoneTransform(HumanBodyBones.Head);
    leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
    leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
    rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
    rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

    Debug.Log("Chest = " + chest);
    Debug.Log("UpperChest = " + upperChest);
    Debug.Log("Head = " + head);
    Debug.Log("LeftUpperArm = " + leftUpperArm);
    Debug.Log("LeftLowerArm = " + leftLowerArm);
    Debug.Log("RightUpperArm = " + rightUpperArm);
    Debug.Log("RightLowerArm = " + rightLowerArm);
    }

    public void SetPoseLandmarks(Vector3[] newPoseLandmarks)
    {
         Debug.Log("SetPoseLandmarks called on " + gameObject.name);
         
        if (newPoseLandmarks == null || newPoseLandmarks.Length < 25)
        {
            hasPose = false;
            return;
        }

        poseLandmarks = newPoseLandmarks;
        hasPose = true;

        Debug.Log("SetPoseLandmarks called: " + newPoseLandmarks.Length);

    }

    private void LateUpdate()
    {
        if (!hasPose || poseLandmarks == null || poseLandmarks.Length < 25)
            return;

        // MediaPipe Holistic pose landmark indices
        Vector3 nose = poseLandmarks[0];

        Vector3 leftShoulder = poseLandmarks[11];
        Vector3 rightShoulder = poseLandmarks[12];

        Vector3 leftElbow = poseLandmarks[13];
        Vector3 rightElbow = poseLandmarks[14];

        Vector3 leftWrist = poseLandmarks[15];
        Vector3 rightWrist = poseLandmarks[16];

        Vector3 leftHip = poseLandmarks[23];
        Vector3 rightHip = poseLandmarks[24];

        Vector3 shoulderCenter = (leftShoulder + rightShoulder) * 0.5f;
        Vector3 hipCenter = (leftHip + rightHip) * 0.5f;

        Vector3 shoulderRight = SafeDirection(leftShoulder, rightShoulder, transform.right);
        Vector3 torsoUp = SafeDirection(hipCenter, shoulderCenter, transform.up);

        Vector3 torsoForward = Vector3.Cross(shoulderRight, torsoUp).normalized;
        if (torsoForward.sqrMagnitude < 0.0001f)
            torsoForward = transform.forward;

        DriveChest(torsoForward, torsoUp);
        DriveHead(nose, shoulderCenter, shoulderRight, torsoUp, torsoForward);

        DriveArm(
            leftShoulder, leftElbow, leftWrist,
            leftUpperArm, leftLowerArm,
            leftUpperArmOffset, leftLowerArmOffset,
            torsoUp
        );

        DriveArm(
            rightShoulder, rightElbow, rightWrist,
            rightUpperArm, rightLowerArm,
            rightUpperArmOffset, rightLowerArmOffset,
            torsoUp
        );
    }

    private void DriveChest(Vector3 forward, Vector3 up)
    {
        Transform chestTarget = upperChest != null ? upperChest : chest;
        if (chestTarget == null) return;

        Quaternion target = Quaternion.LookRotation(forward, up) * chestOffset;
        chestTarget.rotation = Smooth(chestTarget.rotation, target);
    }

    private void DriveHead(Vector3 nose, Vector3 shoulderCenter, Vector3 shoulderRight, Vector3 torsoUp, Vector3 torsoForward)
    {
        if (head == null) return;

        Vector3 headForward = SafeDirection(shoulderCenter, nose, torsoForward);
        Vector3 headUp = Vector3.Cross(headForward, shoulderRight).normalized;

        if (headUp.sqrMagnitude < 0.0001f)
            headUp = torsoUp;

        Quaternion target = Quaternion.LookRotation(headForward, headUp) * headOffset;
        head.rotation = Smooth(head.rotation, target);
    }

    private void DriveArm(
        Vector3 shoulder,
        Vector3 elbow,
        Vector3 wrist,
        Transform upperArmBone,
        Transform lowerArmBone,
        Quaternion upperOffset,
        Quaternion lowerOffset,
        Vector3 upHint)
    {
        if (upperArmBone != null)
        {
            Vector3 upperDir = SafeDirection(shoulder, elbow, upperArmBone.forward);
            Quaternion upperTarget = Quaternion.LookRotation(upperDir, upHint) * upperOffset;
            upperArmBone.rotation = Smooth(upperArmBone.rotation, upperTarget);
        }

        if (lowerArmBone != null)
        {
            Vector3 lowerDir = SafeDirection(elbow, wrist, lowerArmBone.forward);
            Quaternion lowerTarget = Quaternion.LookRotation(lowerDir, upHint) * lowerOffset;
            lowerArmBone.rotation = Smooth(lowerArmBone.rotation, lowerTarget);
        }
    }

    private Quaternion Smooth(Quaternion current, Quaternion target)
    {
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        return Quaternion.Slerp(current, target, t);
    }

    private Vector3 SafeDirection(Vector3 from, Vector3 to, Vector3 fallback)
    {
        Vector3 dir = to - from;
        if (dir.sqrMagnitude < 0.000001f)
            return fallback.normalized;

        return dir.normalized;
    }
}