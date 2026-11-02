using UnityEngine;

/// <summary>
/// Keeps a humanoid avatar in a BSL/signing ready pose.
/// Attach this to the same GameObject as the Animator.
///
/// Purpose:
/// - Gives the avatar bent arms in front of the chest.
/// - Prevents sign animations from starting with arms hanging down.
/// - Lets other sign scripts temporarily disable/override the ready pose.
///
/// Usage:
/// - Add to avatar root with Animator.
/// - Animator must be Humanoid.
/// - Press Play.
/// - Toggle Apply Ready Pose to test.
/// - Other sign scripts can call SetReadyPoseWeight(0) before a sign and SetReadyPoseWeight(1) after.
/// </summary>
[RequireComponent(typeof(Animator))]
public class BSLReadyPoseController : MonoBehaviour
{
    [Header("Main Control")]
    [Tooltip("Turn this on to keep the avatar in the BSL ready pose when no sign is playing.")]
    public bool applyReadyPose = true;

    [Range(0f, 1f)]
    [Tooltip("Overall strength of the ready pose. 1 = full pose, 0 = no effect.")]
    public float readyPoseWeight = 1f;

    [Tooltip("How quickly the avatar blends into the ready pose.")]
    public float blendSpeed = 6f;

    [Header("Body")]
    [Tooltip("Optional slight chest rotation for a more natural signing stance.")]
    public Vector3 chestRotationOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("Optional slight spine rotation for a more natural signing stance.")]
    public Vector3 spineRotationOffset = new Vector3(0f, 0f, 0f);

    [Header("Right Arm Ready Pose")]
    [Tooltip("Right upper arm local rotation offset. Adjust in Inspector if the elbow points wrong way.")]
    public Vector3 rightUpperArmOffset = new Vector3(20f, 0f, -42f);

    [Tooltip("Right lower arm local rotation offset. Increase/decrease X to bend elbow more/less.")]
    public Vector3 rightLowerArmOffset = new Vector3(78f, 0f, 0f);

    [Tooltip("Right hand local rotation offset. Adjust Y/Z to rotate palm/wrist.")]
    public Vector3 rightHandOffset = new Vector3(0f, 35f, -10f);

    [Header("Left Arm Ready Pose")]
    [Tooltip("Left upper arm local rotation offset. Adjust in Inspector if the elbow points wrong way.")]
    public Vector3 leftUpperArmOffset = new Vector3(20f, 0f, 42f);

    [Tooltip("Left lower arm local rotation offset. Increase/decrease X to bend elbow more/less.")]
    public Vector3 leftLowerArmOffset = new Vector3(78f, 0f, 0f);

    [Tooltip("Left hand local rotation offset. Adjust Y/Z to rotate palm/wrist.")]
    public Vector3 leftHandOffset = new Vector3(0f, -35f, 10f);

    [Header("Finger Pose")]
    [Tooltip("Enable a relaxed finger bend so hands don't look stiff.")]
    public bool poseFingers = true;

    [Tooltip("Axis used for curling fingers. If fingers bend backwards, change X from -1 to 1, or try Y/Z.")]
    public Vector3 fingerCurlAxis = new Vector3(-1f, 0f, 0f);

    [Range(-90f, 90f)] public float relaxedFingerCurl = 12f;
    [Range(-90f, 90f)] public float relaxedThumbCurl = 8f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Animator animator;

    private Transform chest;
    private Transform spine;

    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform rightHand;
    private Transform leftUpperArm;
    private Transform leftLowerArm;
    private Transform leftHand;

    private Quaternion chestBase;
    private Quaternion spineBase;
    private Quaternion rightUpperArmBase;
    private Quaternion rightLowerArmBase;
    private Quaternion rightHandBase;
    private Quaternion leftUpperArmBase;
    private Quaternion leftLowerArmBase;
    private Quaternion leftHandBase;

    private readonly HumanBodyBones[] rightFingerBones =
    {
        HumanBodyBones.RightThumbProximal,
        HumanBodyBones.RightThumbIntermediate,
        HumanBodyBones.RightThumbDistal,
        HumanBodyBones.RightIndexProximal,
        HumanBodyBones.RightIndexIntermediate,
        HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal,
        HumanBodyBones.RightMiddleIntermediate,
        HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal,
        HumanBodyBones.RightRingIntermediate,
        HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal,
        HumanBodyBones.RightLittleIntermediate,
        HumanBodyBones.RightLittleDistal
    };

    private readonly HumanBodyBones[] leftFingerBones =
    {
        HumanBodyBones.LeftThumbProximal,
        HumanBodyBones.LeftThumbIntermediate,
        HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal,
        HumanBodyBones.LeftIndexIntermediate,
        HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal,
        HumanBodyBones.LeftMiddleIntermediate,
        HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal,
        HumanBodyBones.LeftRingIntermediate,
        HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal,
        HumanBodyBones.LeftLittleIntermediate,
        HumanBodyBones.LeftLittleDistal
    };

    private Transform[] rightFingerTransforms;
    private Transform[] leftFingerTransforms;
    private Quaternion[] rightFingerBase;
    private Quaternion[] leftFingerBase;

    private float currentWeight = 0f;
    private float externalWeightMultiplier = 1f;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("[BSLReadyPoseController] No Animator found on this GameObject.");
            enabled = false;
            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogError("[BSLReadyPoseController] Animator is not Humanoid. Set the model Rig to Humanoid.");
            enabled = false;
            return;
        }

        CacheBones();
        CacheBasePose();
    }

    private void CacheBones()
    {
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);

        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

        rightFingerTransforms = new Transform[rightFingerBones.Length];
        leftFingerTransforms = new Transform[leftFingerBones.Length];

        for (int i = 0; i < rightFingerBones.Length; i++)
            rightFingerTransforms[i] = animator.GetBoneTransform(rightFingerBones[i]);

        for (int i = 0; i < leftFingerBones.Length; i++)
            leftFingerTransforms[i] = animator.GetBoneTransform(leftFingerBones[i]);

        if (showDebugLogs)
        {
            Debug.Log("[BSLReadyPoseController] Bones cached. " +
                      $"RightHand: {(rightHand ? rightHand.name : "NULL")}, " +
                      $"LeftHand: {(leftHand ? leftHand.name : "NULL")}");
        }
    }

    private void CacheBasePose()
    {
        if (chest) chestBase = chest.localRotation;
        if (spine) spineBase = spine.localRotation;

        if (rightUpperArm) rightUpperArmBase = rightUpperArm.localRotation;
        if (rightLowerArm) rightLowerArmBase = rightLowerArm.localRotation;
        if (rightHand) rightHandBase = rightHand.localRotation;

        if (leftUpperArm) leftUpperArmBase = leftUpperArm.localRotation;
        if (leftLowerArm) leftLowerArmBase = leftLowerArm.localRotation;
        if (leftHand) leftHandBase = leftHand.localRotation;

        rightFingerBase = new Quaternion[rightFingerTransforms.Length];
        leftFingerBase = new Quaternion[leftFingerTransforms.Length];

        for (int i = 0; i < rightFingerTransforms.Length; i++)
            if (rightFingerTransforms[i]) rightFingerBase[i] = rightFingerTransforms[i].localRotation;

        for (int i = 0; i < leftFingerTransforms.Length; i++)
            if (leftFingerTransforms[i]) leftFingerBase[i] = leftFingerTransforms[i].localRotation;
    }

    private void LateUpdate()
    {
        float targetWeight = applyReadyPose ? readyPoseWeight * externalWeightMultiplier : 0f;
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * blendSpeed);

        if (currentWeight <= 0.001f)
            return;

        ApplyReadyPose(currentWeight);
    }

    private void ApplyReadyPose(float weight)
    {
        ApplyBoneOffset(spine, spineBase, spineRotationOffset, weight);
        ApplyBoneOffset(chest, chestBase, chestRotationOffset, weight);

        ApplyBoneOffset(rightUpperArm, rightUpperArmBase, rightUpperArmOffset, weight);
        ApplyBoneOffset(rightLowerArm, rightLowerArmBase, rightLowerArmOffset, weight);
        ApplyBoneOffset(rightHand, rightHandBase, rightHandOffset, weight);

        ApplyBoneOffset(leftUpperArm, leftUpperArmBase, leftUpperArmOffset, weight);
        ApplyBoneOffset(leftLowerArm, leftLowerArmBase, leftLowerArmOffset, weight);
        ApplyBoneOffset(leftHand, leftHandBase, leftHandOffset, weight);

        if (poseFingers)
        {
            ApplyFingerPose(rightFingerTransforms, rightFingerBase, weight, true);
            ApplyFingerPose(leftFingerTransforms, leftFingerBase, weight, false);
        }
    }

    private void ApplyBoneOffset(Transform bone, Quaternion baseRotation, Vector3 eulerOffset, float weight)
    {
        if (!bone) return;

        Quaternion target = baseRotation * Quaternion.Euler(eulerOffset);
        bone.localRotation = Quaternion.Slerp(bone.localRotation, target, weight);
    }

    private void ApplyFingerPose(Transform[] bones, Quaternion[] baseRotations, float weight, bool rightHandSide)
    {
        if (bones == null || baseRotations == null) return;

        for (int i = 0; i < bones.Length; i++)
        {
            if (!bones[i]) continue;

            bool isThumb = i < 3;
            float curl = isThumb ? relaxedThumbCurl : relaxedFingerCurl;

            // Mirror left/right very slightly so both hands don't twist identically on asymmetric rigs.
            Vector3 axis = fingerCurlAxis;
            if (!rightHandSide) axis.x *= 1f;

            Quaternion target = baseRotations[i] * Quaternion.AngleAxis(curl, axis.normalized);
            bones[i].localRotation = Quaternion.Slerp(bones[i].localRotation, target, weight);
        }
    }

    /// <summary>
    /// Call this from sign scripts.
    /// 1 = ready pose active. 0 = sign script can fully take over.
    /// </summary>
    public void SetReadyPoseWeight(float weight)
    {
        externalWeightMultiplier = Mathf.Clamp01(weight);
    }

    /// <summary>
    /// Use this before playing a sign so the sign script can control the arms.
    /// </summary>
    public void DisableForSign()
    {
        SetReadyPoseWeight(0f);
    }

    /// <summary>
    /// Use this after a sign finishes to return to signing-ready pose.
    /// </summary>
    public void EnableAfterSign()
    {
        SetReadyPoseWeight(1f);
    }

    /// <summary>
    /// Re-save the current local bone rotations as the base pose.
    /// Use this if your avatar already has a good imported pose before this script runs.
    /// </summary>
    public void RecalibrateBasePose()
    {
        CacheBasePose();
        if (showDebugLogs) Debug.Log("[BSLReadyPoseController] Base pose recalibrated.");
    }
}
