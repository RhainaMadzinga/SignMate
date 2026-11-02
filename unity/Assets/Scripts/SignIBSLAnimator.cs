using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SignIBSLAnimator : MonoBehaviour
{
    public enum DominantHand { Right, Left }

    [Header("Playback")]
    public bool playOnStart = true;
    public KeyCode replayKey = KeyCode.I;
    public float signDuration = 1.15f;
    public DominantHand dominantHand = DominantHand.Right;
    public bool holdLastPose = false;

    [Header("Smooth Start / End")]
    public float startFromNormalDuration = 0.35f;
    public float returnToNormalDuration = 0.45f;

    [Header("IK Weights")]
    [Range(0f, 1f)] public float handPositionWeight = 1f;
    [Range(0f, 1f)] public float handRotationWeight = 0.85f;
    [Range(0f, 1f)] public float elbowHintWeight = 1f;

    [Header("Body-relative Hand Targets")]
    [Tooltip("Start position before the sign begins. Relative to chest.")]
    public Vector3 startOffset = new Vector3(0.24f, -0.10f, 0.42f);

    [Tooltip("Pointing position at the chest/self. Relative to chest.")]
    public Vector3 selfPointOffset = new Vector3(0.08f, -0.06f, 0.22f);

    [Tooltip("Final return position after the sign. Relative to chest.")]
    public Vector3 finishOffset = new Vector3(0.22f, -0.14f, 0.38f);

    [Header("Body Collision Safety")]
    public float minForwardFromChest = 0.18f;
    public float minSideFromCenter = 0.04f;

    [Header("Elbow Bend Control")]
    [Tooltip("X pushes elbow outward. Z brings elbow forward. Y moves elbow up/down.")]
    public Vector3 elbowHintOffset = new Vector3(0.55f, -0.20f, 0.45f);

    [Header("Hand Rotation")]
    [Tooltip("Hand rotation while pointing to self. Tune Y/Z if index points the wrong way.")]
    public Vector3 pointHandRotationOffsetEuler = new Vector3(5f, 100f, -20f);

    [Tooltip("Extra wrist twist when the index reaches the chest.")]
    public Vector3 contactExtraRotationEuler = new Vector3(8f, 0f, -18f);

    [Header("Finger Shape")]
    [Tooltip("Most rigs curl fingers around local X. If fingers bend backwards, use (1,0,0) instead.")]
    public Vector3 fingerCurlAxis = new Vector3(-1f, 0f, 0f);

    [Tooltip("Index finger stays straight for pointing.")]
    public float indexCurl = 0f;

    [Tooltip("Middle/ring/little fingers curl into a pointing hand shape.")]
    public float foldedFingerCurl = 45f;

    [Tooltip("Thumb curl amount.")]
    public float thumbCurl = 15f;

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
            Debug.LogError("[SignIBSLAnimator] No Animator found. Put this script on the avatar GameObject with the Animator.");
            enabled = false;
            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogError("[SignIBSLAnimator] Animator is not Humanoid. Change the model Rig to Humanoid.");
            enabled = false;
            return;
        }

        chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);

        if (chest == null)
        {
            Debug.LogError("[SignIBSLAnimator] Missing Chest/Spine bone in Humanoid Avatar mapping.");
            enabled = false;
            return;
        }

        CacheFingerBones();

        if (showDebugLogs)
            Debug.Log("[SignIBSLAnimator] Ready. Press I to replay the BSL I/self sign.");
    }

    private void Start()
    {
        if (playOnStart)
            PlayI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(replayKey))
            PlayI();

        if (!isPlaying)
            return;

        timer += Time.deltaTime;

        float totalDuration = Mathf.Max(0f, startFromNormalDuration) + signDuration + Mathf.Max(0f, returnToNormalDuration);
        if (timer >= totalDuration)
        {
            timer = totalDuration;
            isPlaying = false;
        }
    }

    public void PlayI()
    {
        timer = 0f;
        isPlaying = true;

        if (showDebugLogs)
            Debug.Log("[SignIBSLAnimator] Playing BSL I/self sign.");
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || chest == null)
            return;

        bool active = isPlaying || (holdLastPose && timer > 0f);
        float activeWeight = active ? GetOverallControlWeight() : 0f;

        float t = GetSignProgress();
        Vector3 target = GetIHandPosition(t);
        Quaternion rotation = GetIHandRotation(t);
        Vector3 elbowHintPosition = GetElbowHintPosition();

        lastTarget = target;
        lastElbowHint = elbowHintPosition;

        AvatarIKGoal handGoal = dominantHand == DominantHand.Right ? AvatarIKGoal.RightHand : AvatarIKGoal.LeftHand;
        AvatarIKHint elbowHint = dominantHand == DominantHand.Right ? AvatarIKHint.RightElbow : AvatarIKHint.LeftElbow;

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

        bool active = isPlaying || (holdLastPose && timer > 0f);
        float t = GetSignProgress();
        float shapeWeight = active ? FingerShapeWeight(t) * GetOverallControlWeight() : 0f;

        Vector3 axis = fingerCurlAxis.sqrMagnitude < 0.0001f ? Vector3.right : fingerCurlAxis.normalized;

        for (int i = 0; i < fingerBones.Length; i++)
        {
            FingerBone fb = fingerBones[i];
            if (fb.bone == null) continue;

            Quaternion curl = Quaternion.AngleAxis(fb.curlAmount * shapeWeight, axis);
            fb.bone.localRotation = fb.startLocalRotation * curl;
        }
    }

    private float GetSignProgress()
    {
        float startDuration = Mathf.Max(0f, startFromNormalDuration);
        return Mathf.Clamp01((timer - startDuration) / Mathf.Max(0.01f, signDuration));
    }

    private float GetOverallControlWeight()
    {
        if (holdLastPose && !isPlaying && timer > 0f)
            return 1f;

        float startDuration = Mathf.Max(0f, startFromNormalDuration);
        float returnDuration = Mathf.Max(0.001f, returnToNormalDuration);
        float signEndTime = startDuration + signDuration;

        // Fade in at the beginning so the hand/fingers do not snap into the sign.
        if (startDuration > 0f && timer < startDuration)
            return Ease(timer / startDuration);

        // Full control during the actual sign.
        if (timer <= signEndTime)
            return 1f;

        // Fade out at the end so the avatar returns to normal smoothly.
        float returnT = Mathf.Clamp01((timer - signEndTime) / returnDuration);
        return 1f - Ease(returnT);
    }

    private Vector3 GetIHandPosition(float t)
    {
        float side = dominantHand == DominantHand.Right ? 1f : -1f;

        Vector3 startPos = BodyPoint(chest, new Vector3(startOffset.x * side, startOffset.y, startOffset.z));
        Vector3 selfPointPos = BodyPoint(chest, new Vector3(selfPointOffset.x * side, selfPointOffset.y, selfPointOffset.z));
        Vector3 finishPos = BodyPoint(chest, new Vector3(finishOffset.x * side, finishOffset.y, finishOffset.z));

        Vector3 target;

        // BSL I/self motion:
        // 0.00-0.30: hand moves from ready space to chest
        // 0.30-0.62: hold point to self
        // 0.62-1.00: hand returns to ready space
        if (t < 0.30f)
        {
            target = Vector3.Lerp(startPos, selfPointPos, Ease(t / 0.30f));
        }
        else if (t < 0.62f)
        {
            target = selfPointPos;
        }
        else
        {
            target = Vector3.Lerp(selfPointPos, finishPos, Ease((t - 0.62f) / 0.38f));
        }

        return KeepHandOutsideBody(target, side);
    }

    private Quaternion GetIHandRotation(float t)
    {
        float side = dominantHand == DominantHand.Right ? 1f : -1f;

        Quaternion baseFacing = Quaternion.LookRotation(transform.forward, transform.up);

        Vector3 euler = pointHandRotationOffsetEuler;

        float contactWeight;
        if (t < 0.30f)
            contactWeight = Ease(t / 0.30f);
        else if (t < 0.62f)
            contactWeight = 1f;
        else
            contactWeight = 1f - Ease((t - 0.62f) / 0.38f);

        euler += contactExtraRotationEuler * contactWeight;

        return baseFacing * Quaternion.Euler(
            euler.x,
            euler.y * side,
            euler.z * side
        );
    }

    private Vector3 GetElbowHintPosition()
    {
        float side = dominantHand == DominantHand.Right ? 1f : -1f;
        return BodyPoint(chest, new Vector3(elbowHintOffset.x * side, elbowHintOffset.y, elbowHintOffset.z));
    }

    private Vector3 KeepHandOutsideBody(Vector3 target, float side)
    {
        if (chest == null)
            return target;

        Vector3 chestLocal = chest.InverseTransformPoint(target);

        // For this sign the finger should point at the chest, so do not push too far forward.
        chestLocal.z = Mathf.Max(chestLocal.z, minForwardFromChest);

        // Keep it slightly on the dominant hand side, but allow it near centre so it can point to self.
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

    private float Ease(float value)
    {
        value = Mathf.Clamp01(value);
        if (easing != null)
            return easing.Evaluate(value);
        return Mathf.SmoothStep(0f, 1f, value);
    }

    private float FingerShapeWeight(float t)
    {
        if (t < 0.15f)
            return Ease(t / 0.15f);

        if (t > 0.88f)
            return 1f - Ease((t - 0.88f) / 0.12f);

        return 1f;
    }

    private void CacheFingerBones()
    {
        bool right = dominantHand == DominantHand.Right;

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

        fingerBones = new FingerBone[]
        {
            new FingerBone(animator.GetBoneTransform(thumbProximal), thumbCurl),
            new FingerBone(animator.GetBoneTransform(thumbIntermediate), thumbCurl),
            new FingerBone(animator.GetBoneTransform(thumbDistal), thumbCurl),

            new FingerBone(animator.GetBoneTransform(indexProximal), indexCurl),
            new FingerBone(animator.GetBoneTransform(indexIntermediate), indexCurl),
            new FingerBone(animator.GetBoneTransform(indexDistal), indexCurl),

            new FingerBone(animator.GetBoneTransform(middleProximal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(middleIntermediate), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(middleDistal), foldedFingerCurl),

            new FingerBone(animator.GetBoneTransform(ringProximal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(ringIntermediate), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(ringDistal), foldedFingerCurl),

            new FingerBone(animator.GetBoneTransform(littleProximal), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(littleIntermediate), foldedFingerCurl),
            new FingerBone(animator.GetBoneTransform(littleDistal), foldedFingerCurl),
        };
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;

        Gizmos.DrawSphere(lastTarget, 0.025f);
        Gizmos.DrawSphere(lastElbowHint, 0.025f);
        Gizmos.DrawLine(lastElbowHint, lastTarget);
    }
}
