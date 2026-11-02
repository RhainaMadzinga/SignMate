using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SignHomeBSLAnimator : MonoBehaviour
{
    [Header("Playback")]
    public bool playOnStart = false;
    public KeyCode replayKey = KeyCode.H;
    public float signDuration = 1.25f;
    public bool holdLastPose = false;

    [Header("Smooth Start / End")]
    [Tooltip("How long the IK fades in from the avatar's normal pose.")]
    public float startFromNormalDuration = 0.35f;
    [Tooltip("How long the IK fades out back to the avatar's normal/ready pose.")]
    public float returnToNormalDuration = 0.45f;

    [Header("IK Weights")]
    [Range(0f, 1f)] public float handPositionWeight = 1f;
    [Range(0f, 1f)] public float handRotationWeight = 0.85f;
    [Range(0f, 1f)] public float elbowHintWeight = 1f;

    [Header("Body Collision Safety")]
    public float minForwardFromChest = 0.22f;
    public float minSideFromCenter = 0.10f;

    [Header("Body-relative Hand Targets")]
    [Tooltip("Start/ready position for each hand, relative to the chest. X is mirrored for left/right.")]
    public Vector3 startOffset = new Vector3(0.28f, -0.18f, 0.32f);

    [Tooltip("Near-centre HOME contact/shape position, relative to the chest. X is mirrored for left/right.")]
    public Vector3 homeContactOffset = new Vector3(0.12f, 0.02f, 0.42f);

    [Tooltip("Movement after the contact/hold. X is mirrored for left/right.")]
    public Vector3 homeOutOffset = new Vector3(0.30f, -0.10f, 0.54f);

    [Tooltip("Final relaxed signing-space position before fading out. X is mirrored for left/right.")]
    public Vector3 finishOffset = new Vector3(0.24f, -0.26f, 0.36f);

    [Header("Elbow Bend Control")]
    [Tooltip("X pushes elbow outward. Z brings elbow forward. Y moves elbow up/down. X is mirrored for left/right.")]
    public Vector3 elbowHintOffset = new Vector3(0.58f, -0.25f, 0.42f);

    [Header("Hand Rotation")]
    [Tooltip("Rotation when hands rise into the HOME shape.")]
    public Vector3 contactHandRotationOffsetEuler = new Vector3(12f, 75f, -10f);

    [Tooltip("Rotation while hands move away/down from the HOME shape.")]
    public Vector3 outHandRotationOffsetEuler = new Vector3(10f, 60f, -5f);

    [Tooltip("Extra X wrist twist while holding the HOME shape.")]
    public float contactExtraXTwist = 0f;

    [Tooltip("Extra Z wrist twist while holding the HOME shape.")]
    public float contactExtraZTwist = 0f;

    [Header("Finger Shape")]
    [Tooltip("Most rigs curl fingers around local X. If fingers bend backwards, use (-1,0,0) or try (0,-1,0).")]
    public Vector3 fingerCurlAxis = new Vector3(-1f, 0f, 0f);

    [Tooltip("For this HOME version, index fingers stay mostly extended.")]
    public float indexCurl = 0f;

    [Tooltip("Middle/ring/little curl. Increase if you want a stronger pointing hand shape.")]
    public float foldedFingerCurl = 35f;

    [Tooltip("Thumb curl amount.")]
    public float thumbCurl = 10f;

    [Header("Smoothing")]
    public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool drawDebugGizmos = true;

    private Animator animator;
    private Transform chest;
    private bool isPlaying;
    private float timer;
    private FingerBone[] fingerBones;
    private Vector3 lastRightTarget;
    private Vector3 lastLeftTarget;
    private Vector3 lastRightElbowHint;
    private Vector3 lastLeftElbowHint;

    private struct FingerBone
    {
        public Transform bone;
        public Quaternion startLocalRotation;
        public float curlAmount;

        public FingerBone(Transform bone, float curlAmount)
        {
            this.bone = bone;
            this.startLocalRotation = bone != null ? bone.localRotation : Quaternion.identity;
            this.curlAmount = curlAmount;
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("[SignHomeBSLAnimator] No Animator found. Put this script on the avatar GameObject with the Animator.");
            enabled = false;
            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogError("[SignHomeBSLAnimator] Animator is not Humanoid. Change the model Rig to Humanoid.");
            enabled = false;
            return;
        }

        chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);

        if (chest == null)
        {
            Debug.LogError("[SignHomeBSLAnimator] Missing Chest/Spine bone in the Humanoid Avatar mapping.");
            enabled = false;
            return;
        }

        CacheFingerBones();

        if (showDebugLogs)
        {
            Debug.Log("[SignHomeBSLAnimator] Ready. Press H to replay HOME.");
            Debug.Log("[SignHomeBSLAnimator] Animator Controller assigned: " + (animator.runtimeAnimatorController != null));
        }
    }

    private void Start()
    {
        if (playOnStart)
            PlayHome();
    }

    private void Update()
    {
        if (Input.GetKeyDown(replayKey))
            PlayHome();

        if (!isPlaying)
            return;

        timer += Time.deltaTime;

        float totalDuration = signDuration + Mathf.Max(0.01f, returnToNormalDuration);
        if (timer >= totalDuration)
        {
            timer = totalDuration;
            isPlaying = false;
        }
    }

    public void PlayHome()
    {
        timer = 0f;
        isPlaying = true;

        if (showDebugLogs)
            Debug.Log("[SignHomeBSLAnimator] Playing BSL HOME.");
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || chest == null)
            return;

        bool active = isPlaying || (holdLastPose && timer > 0f);
        float ikWeight = active ? GetIKBlendWeight() : 0f;

        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, signDuration));

        Vector3 rightTarget = GetHomeHandPosition(true, t);
        Vector3 leftTarget = GetHomeHandPosition(false, t);
        Quaternion rightRotation = GetHomeHandRotation(true, t);
        Quaternion leftRotation = GetHomeHandRotation(false, t);
        Vector3 rightElbow = GetElbowHintPosition(true);
        Vector3 leftElbow = GetElbowHintPosition(false);

        lastRightTarget = rightTarget;
        lastLeftTarget = leftTarget;
        lastRightElbowHint = rightElbow;
        lastLeftElbowHint = leftElbow;

        ApplyHandIK(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, rightTarget, rightRotation, rightElbow, ikWeight);
        ApplyHandIK(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, leftTarget, leftRotation, leftElbow, ikWeight);
    }

    private void ApplyHandIK(AvatarIKGoal handGoal, AvatarIKHint elbowHint, Vector3 target, Quaternion rotation, Vector3 elbowPosition, float ikWeight)
    {
        animator.SetIKPositionWeight(handGoal, handPositionWeight * ikWeight);
        animator.SetIKRotationWeight(handGoal, handRotationWeight * ikWeight);
        animator.SetIKPosition(handGoal, target);
        animator.SetIKRotation(handGoal, rotation);

        animator.SetIKHintPositionWeight(elbowHint, elbowHintWeight * ikWeight);
        animator.SetIKHintPosition(elbowHint, elbowPosition);
    }

    private void LateUpdate()
    {
        if (fingerBones == null)
            return;

        bool active = isPlaying || (holdLastPose && timer > 0f);
        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, signDuration));
        float shapeWeight = active ? FingerShapeWeight(t) * GetIKBlendWeight() : 0f;

        Vector3 axis = fingerCurlAxis.sqrMagnitude < 0.0001f ? Vector3.right : fingerCurlAxis.normalized;

        for (int i = 0; i < fingerBones.Length; i++)
        {
            FingerBone fb = fingerBones[i];
            if (fb.bone == null) continue;

            Quaternion curl = Quaternion.AngleAxis(fb.curlAmount * shapeWeight, axis);
            fb.bone.localRotation = fb.startLocalRotation * curl;
        }
    }

    private Vector3 GetHomeHandPosition(bool rightHand, float t)
    {
        float side = rightHand ? 1f : -1f;

        Vector3 startPos = BodyPoint(chest, new Vector3(startOffset.x * side, startOffset.y, startOffset.z));
        Vector3 contactPos = BodyPoint(chest, new Vector3(homeContactOffset.x * side, homeContactOffset.y, homeContactOffset.z));
        Vector3 outPos = BodyPoint(chest, new Vector3(homeOutOffset.x * side, homeOutOffset.y, homeOutOffset.z));
        Vector3 finishPos = BodyPoint(chest, new Vector3(finishOffset.x * side, finishOffset.y, finishOffset.z));

        Vector3 target;

        // HOME motion:
        // 0.00-0.35: both hands rise from signing-space ready position
        // 0.35-0.55: hold the HOME handshape/position
        // 0.55-0.82: hands move slightly out/down/forward
        // 0.82-1.00: settle before fading back to normal
        if (t < 0.35f)
        {
            target = Vector3.Lerp(startPos, contactPos, Ease(t / 0.35f));
        }
        else if (t < 0.55f)
        {
            target = contactPos;
        }
        else if (t < 0.82f)
        {
            target = Vector3.Lerp(contactPos, outPos, Ease((t - 0.55f) / 0.27f));
        }
        else
        {
            target = Vector3.Lerp(outPos, finishPos, Ease((t - 0.82f) / 0.18f));
        }

        return KeepHandOutsideBody(target, side);
    }

    private Quaternion GetHomeHandRotation(bool rightHand, float t)
    {
        float side = rightHand ? 1f : -1f;

        Quaternion baseFacing = Quaternion.LookRotation(transform.forward, transform.up);

        Vector3 contactEuler = contactHandRotationOffsetEuler;
        Vector3 outEuler = outHandRotationOffsetEuler;

        float twistWeight = 0f;
        if (t < 0.35f)
            twistWeight = Ease(t / 0.35f);
        else if (t < 0.55f)
            twistWeight = 1f;
        else if (t < 0.82f)
            twistWeight = 1f - Ease((t - 0.55f) / 0.27f);
        else
            twistWeight = 0f;

        contactEuler.x += contactExtraXTwist * twistWeight;
        contactEuler.z += contactExtraZTwist * twistWeight;
        outEuler.x += contactExtraXTwist * twistWeight;
        outEuler.z += contactExtraZTwist * twistWeight;

        Quaternion contactRot = baseFacing * Quaternion.Euler(
            contactEuler.x,
            contactEuler.y * side,
            contactEuler.z * side
        );

        Quaternion outRot = baseFacing * Quaternion.Euler(
            outEuler.x,
            outEuler.y * side,
            outEuler.z * side
        );

        float blend = Mathf.Clamp01((t - 0.55f) / 0.27f);
        return Quaternion.Slerp(contactRot, outRot, Ease(blend));
    }

    private Vector3 GetElbowHintPosition(bool rightHand)
    {
        float side = rightHand ? 1f : -1f;
        return BodyPoint(chest, new Vector3(elbowHintOffset.x * side, elbowHintOffset.y, elbowHintOffset.z));
    }

    private Vector3 KeepHandOutsideBody(Vector3 target, float side)
    {
        if (chest == null)
            return target;

        Vector3 chestLocal = chest.InverseTransformPoint(target);
        chestLocal.z = Mathf.Max(chestLocal.z, minForwardFromChest);
        chestLocal.x = Mathf.Sign(side) * Mathf.Max(Mathf.Abs(chestLocal.x), minSideFromCenter);
        return chest.TransformPoint(chestLocal);
    }

    private Vector3 BodyPoint(Transform reference, Vector3 localOffset)
    {
        return reference.position
            + transform.right * localOffset.x
            + transform.up * localOffset.y
            + transform.forward * localOffset.z;
    }

    private float GetIKBlendWeight()
    {
        if (!isPlaying && !holdLastPose)
            return 0f;

        if (timer < startFromNormalDuration)
            return Ease(timer / Mathf.Max(0.01f, startFromNormalDuration));

        if (timer <= signDuration)
            return 1f;

        if (holdLastPose)
            return 1f;

        float returnT = (timer - signDuration) / Mathf.Max(0.01f, returnToNormalDuration);
        return 1f - Ease(returnT);
    }

    private float Ease(float value)
    {
        value = Mathf.Clamp01(value);
        if (easing != null)
            return easing.Evaluate(value);
        return Mathf.SmoothStep(0f, 1f, value);
    }

    private float FingerShapeWeight(float t)
    {
        if (t < 0.18f)
            return Ease(t / 0.18f);

        if (t > 0.88f)
            return 1f - Ease((t - 0.88f) / 0.12f);

        return 1f;
    }

    private void CacheFingerBones()
    {
        fingerBones = new FingerBone[]
        {
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightThumbProximal), thumbCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate), thumbCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightThumbDistal), thumbCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightIndexProximal), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightIndexDistal), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightRingProximal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightRingDistal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightLittleProximal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.RightLittleDistal), foldedFingerCurl),

            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal), thumbCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate), thumbCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal), thumbCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal), indexCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftRingProximal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftRingDistal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal), foldedFingerCurl),
        };
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;

        Gizmos.DrawSphere(lastRightTarget, 0.025f);
        Gizmos.DrawSphere(lastLeftTarget, 0.025f);
        Gizmos.DrawSphere(lastRightElbowHint, 0.025f);
        Gizmos.DrawSphere(lastLeftElbowHint, 0.025f);
        Gizmos.DrawLine(lastRightElbowHint, lastRightTarget);
        Gizmos.DrawLine(lastLeftElbowHint, lastLeftTarget);
    }
}
