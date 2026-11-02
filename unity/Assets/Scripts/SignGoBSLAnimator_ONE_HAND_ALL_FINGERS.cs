using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SignGoBSLAnimator : MonoBehaviour
{
    public enum SignHand { Right, Left }

    [Header("Playback")]
    public bool playOnStart = false;
    public KeyCode replayKey = KeyCode.G;
    public float signDuration = 1.15f;
    public bool holdLastPose = false;
    public SignHand signHand = SignHand.Right;

    [Header("Smooth Start / End")]
    public float startFromNormalDuration = 0.30f;
    public float returnToNormalDuration = 0.45f;

    [Header("IK Weights")]
    [Range(0f, 1f)] public float handPositionWeight = 1f;
    [Range(0f, 1f)] public float handRotationWeight = 0.85f;
    [Range(0f, 1f)] public float elbowHintWeight = 1f;

    [Header("One-Hand GO Targets - Chest Relative")]
    [Tooltip("Start position before GO begins. X is side of body, Y is height, Z is forward.")]
    public Vector3 startOffset = new Vector3(0.18f, -0.10f, 0.32f);

    [Tooltip("Main forward movement for GO.")]
    public Vector3 forwardOffset = new Vector3(0.24f, -0.12f, 0.82f);

    [Tooltip("Final finishing position after the forward movement.")]
    public Vector3 finishOffset = new Vector3(0.27f, -0.15f, 0.98f);

    [Header("Elbow Bend Control")]
    [Tooltip("X pushes elbow outward, Y moves elbow up/down, Z moves elbow forward.")]
    public Vector3 elbowHintOffset = new Vector3(0.55f, -0.25f, 0.45f);

    [Header("Hand Rotation")]
    [Tooltip("Wrist rotation for the signing hand. Y turns palm, Z twists wrist, X tilts up/down.")]
    public Vector3 handRotationOffsetEuler = new Vector3(5f, 82f, -18f);

    [Tooltip("Extra wrist twist while the hand moves forward.")]
    public Vector3 forwardTwistEuler = new Vector3(8f, 0f, -12f);

    [Header("Finger Shape")]
    [Tooltip("Most rigs curl fingers around local X. If fingers bend backwards, use (1,0,0) or try (0,-1,0).")]
    public Vector3 fingerCurlAxis = new Vector3(-1f, 0f, 0f);

    [Tooltip("Curl applied to thumb, index, middle, ring, and little fingers. No separate index/middle control.")]
    public float allFingerCurl = 35f;

    [Header("Body Collision Safety")]
    public float minForwardFromChest = 0.25f;
    public float minSideFromCenter = 0.12f;

    [Header("Smoothing")]
    public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool drawDebugGizmos = true;

    private Animator animator;
    private Transform chest;
    private bool isPlaying;
    private bool isReturning;
    private float timer;
    private float returnTimer;
    private FingerBone[] fingerBones;
    private Vector3 lastTarget;
    private Vector3 lastElbowHint;

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
            Debug.LogError("[SignGoBSLAnimator] No Animator found. Put this script on the avatar GameObject with the Animator.");
            enabled = false;
            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogError("[SignGoBSLAnimator] Animator is not Humanoid. Change the model Rig to Humanoid.");
            enabled = false;
            return;
        }

        chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);

        if (chest == null)
        {
            Debug.LogError("[SignGoBSLAnimator] Missing Chest/Spine bone in Humanoid Avatar mapping.");
            enabled = false;
            return;
        }

        CacheFingerBones();

        if (showDebugLogs)
            Debug.Log("[SignGoBSLAnimator] Ready. Press G to replay one-handed GO.");
    }

    private void Start()
    {
        if (playOnStart)
            PlayGo();
    }

    private void Update()
    {
        if (Input.GetKeyDown(replayKey))
            PlayGo();

        if (isPlaying)
        {
            timer += Time.deltaTime;
            if (timer >= signDuration)
            {
                timer = signDuration;
                isPlaying = false;
                isReturning = true;
                returnTimer = 0f;
            }
        }
        else if (isReturning)
        {
            returnTimer += Time.deltaTime;
            if (returnTimer >= returnToNormalDuration)
                isReturning = false;
        }
    }

    public void PlayGo()
    {
        timer = 0f;
        returnTimer = 0f;
        isPlaying = true;
        isReturning = false;

        if (showDebugLogs)
            Debug.Log("[SignGoBSLAnimator] Playing one-handed BSL GO.");
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || chest == null)
            return;

        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, signDuration));
        float activeWeight = GetActiveWeight(t);
        float side = GetSide();

        Vector3 target = KeepHandOutsideBody(GetGoHandPosition(t), side);
        Quaternion rotation = GetGoHandRotation(t);
        Vector3 elbowHintPosition = BodyPoint(chest, new Vector3(elbowHintOffset.x * side, elbowHintOffset.y, elbowHintOffset.z));

        lastTarget = target;
        lastElbowHint = elbowHintPosition;

        AvatarIKGoal handGoal = signHand == SignHand.Right ? AvatarIKGoal.RightHand : AvatarIKGoal.LeftHand;
        AvatarIKHint elbowHint = signHand == SignHand.Right ? AvatarIKHint.RightElbow : AvatarIKHint.LeftElbow;

        animator.SetIKPositionWeight(handGoal, handPositionWeight * activeWeight);
        animator.SetIKRotationWeight(handGoal, handRotationWeight * activeWeight);
        animator.SetIKPosition(handGoal, target);
        animator.SetIKRotation(handGoal, rotation);

        animator.SetIKHintPositionWeight(elbowHint, elbowHintWeight * activeWeight);
        animator.SetIKHintPosition(elbowHint, elbowHintPosition);
    }

    private void LateUpdate()
    {
        if (fingerBones == null)
            return;

        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, signDuration));
        float shapeWeight = GetActiveWeight(t) * FingerShapeWeight(t);
        Vector3 axis = fingerCurlAxis.sqrMagnitude < 0.0001f ? Vector3.right : fingerCurlAxis.normalized;

        for (int i = 0; i < fingerBones.Length; i++)
        {
            FingerBone fb = fingerBones[i];
            if (fb.bone == null) continue;

            Quaternion curl = Quaternion.AngleAxis(fb.curlAmount * shapeWeight, axis);
            fb.bone.localRotation = fb.startLocalRotation * curl;
        }
    }

    private float GetActiveWeight(float t)
    {
        if (isPlaying)
        {
            if (startFromNormalDuration <= 0.001f)
                return 1f;

            float startPortion = Mathf.Clamp01(startFromNormalDuration / Mathf.Max(0.01f, signDuration));
            if (t < startPortion)
                return Ease(t / startPortion);

            return 1f;
        }

        if (isReturning)
        {
            if (returnToNormalDuration <= 0.001f)
                return 0f;

            return 1f - Ease(returnTimer / returnToNormalDuration);
        }

        return holdLastPose && timer > 0f ? 1f : 0f;
    }

    private Vector3 GetGoHandPosition(float t)
    {
        float side = GetSide();

        Vector3 start = BodyPoint(chest, new Vector3(startOffset.x * side, startOffset.y, startOffset.z));
        Vector3 forward = BodyPoint(chest, new Vector3(forwardOffset.x * side, forwardOffset.y, forwardOffset.z));
        Vector3 finish = BodyPoint(chest, new Vector3(finishOffset.x * side, finishOffset.y, finishOffset.z));

        if (t < 0.18f)
            return start;

        if (t < 0.75f)
            return Vector3.Lerp(start, forward, Ease((t - 0.18f) / 0.57f));

        return Vector3.Lerp(forward, finish, Ease((t - 0.75f) / 0.25f));
    }

    private Quaternion GetGoHandRotation(float t)
    {
        float side = GetSide();
        Quaternion baseFacing = Quaternion.LookRotation(transform.forward, transform.up);

        Vector3 euler = handRotationOffsetEuler;
        euler.y *= side;
        euler.z *= side;

        float twistWeight = 0f;
        if (t >= 0.18f && t < 0.75f)
            twistWeight = Ease((t - 0.18f) / 0.57f);
        else if (t >= 0.75f)
            twistWeight = 1f - Ease((t - 0.75f) / 0.25f);

        Vector3 twist = forwardTwistEuler;
        twist.y *= side;
        twist.z *= side;

        euler += twist * twistWeight;
        return baseFacing * Quaternion.Euler(euler);
    }

    private Vector3 KeepHandOutsideBody(Vector3 target, float side)
    {
        Vector3 chestLocal = chest.InverseTransformPoint(target);
        chestLocal.z = Mathf.Max(chestLocal.z, minForwardFromChest);
        chestLocal.x = Mathf.Sign(side) * Mathf.Max(Mathf.Abs(chestLocal.x), minSideFromCenter);
        return chest.TransformPoint(chestLocal);
    }

    private float GetSide()
    {
        return signHand == SignHand.Right ? 1f : -1f;
    }

    private Vector3 BodyPoint(Transform reference, Vector3 localOffset)
    {
        return reference.position
            + transform.right * localOffset.x
            + transform.up * localOffset.y
            + transform.forward * localOffset.z;
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
        if (t < 0.16f)
            return Ease(t / 0.16f);

        if (t > 0.86f)
            return 1f - Ease((t - 0.86f) / 0.14f);

        return 1f;
    }

    private void CacheFingerBones()
    {
        bool right = signHand == SignHand.Right;

        HumanBodyBones thumbProximal = right ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal;
        HumanBodyBones thumbIntermediate = right ? HumanBodyBones.RightThumbIntermediate : HumanBodyBones.LeftThumbIntermediate;
        HumanBodyBones thumbDistal = right ? HumanBodyBones.RightThumbDistal : HumanBodyBones.LeftThumbDistal;

        HumanBodyBones indexProximal = right ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal;
        HumanBodyBones indexIntermediate = right ? HumanBodyBones.RightIndexIntermediate : HumanBodyBones.LeftIndexIntermediate;
        HumanBodyBones indexDistal = right ? HumanBodyBones.RightIndexDistal : HumanBodyBones.LeftIndexDistal;

        HumanBodyBones middleProximal = right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal;
        HumanBodyBones middleIntermediate = right ? HumanBodyBones.RightMiddleIntermediate : HumanBodyBones.LeftMiddleIntermediate;
        HumanBodyBones middleDistal = right ? HumanBodyBones.RightMiddleDistal : HumanBodyBones.LeftMiddleDistal;

        HumanBodyBones ringProximal = right ? HumanBodyBones.RightRingProximal : HumanBodyBones.LeftRingProximal;
        HumanBodyBones ringIntermediate = right ? HumanBodyBones.RightRingIntermediate : HumanBodyBones.LeftRingIntermediate;
        HumanBodyBones ringDistal = right ? HumanBodyBones.RightRingDistal : HumanBodyBones.LeftRingDistal;

        HumanBodyBones littleProximal = right ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal;
        HumanBodyBones littleIntermediate = right ? HumanBodyBones.RightLittleIntermediate : HumanBodyBones.LeftLittleIntermediate;
        HumanBodyBones littleDistal = right ? HumanBodyBones.RightLittleDistal : HumanBodyBones.LeftLittleDistal;

        // All fingers use the same curl amount: no separate index or middle values.
        fingerBones = new FingerBone[]
        {
            new FingerBone(animator.GetBoneTransform(thumbProximal), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(thumbIntermediate), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(thumbDistal), allFingerCurl),

            new FingerBone(animator.GetBoneTransform(indexProximal), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(indexIntermediate), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(indexDistal), allFingerCurl),

            new FingerBone(animator.GetBoneTransform(middleProximal), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(middleIntermediate), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(middleDistal), allFingerCurl),

            new FingerBone(animator.GetBoneTransform(ringProximal), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(ringIntermediate), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(ringDistal), allFingerCurl),

            new FingerBone(animator.GetBoneTransform(littleProximal), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(littleIntermediate), allFingerCurl),
            new FingerBone(animator.GetBoneTransform(littleDistal), allFingerCurl),
        };
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;

        Gizmos.DrawSphere(lastTarget, 0.025f);
        Gizmos.DrawSphere(lastElbowHint, 0.02f);
        Gizmos.DrawLine(lastElbowHint, lastTarget);
    }
}
