using UnityEngine;
[RequireComponent(typeof(Animator))]
public class SignTomorrowBSLAnimator : MonoBehaviour
{
    public enum DominantHand { Right, Left }

    [Header("Body Collision Safety")]
     public float minForwardFromChest = 0.28f;
    public float minSideFromCenter = 0.18f;

    [Header("Playback")]
    public bool playOnStart = true;
    public KeyCode replayKey = KeyCode.T;
    public float signDuration = 1.55f;
    public DominantHand dominantHand = DominantHand.Right;
    public bool holdLastPose = true;

    [Header("IK Weights")]
    [Range(0f, 1f)] public float handPositionWeight = 1f;
    [Range(0f, 1f)] public float handRotationWeight = 0.9f;
    [Range(0f, 1f)] public float elbowHintWeight = 1f;

    [Header("Body-relative Hand Targets")]
    [Tooltip("Start position before the sign begins. This is relative to the chest.")]
    public Vector3 startOffset = new Vector3(0.18f, -0.18f, 0.34f);

    [Tooltip("Contact/near-contact point beside cheek/jaw. This is relative to the head.")]
    public Vector3 cheekOffset = new Vector3(0.20f, -0.08f, 0.18f);

    [Tooltip("Forward/downward movement after the hand leaves the cheek. This is relative to the chest.")]
    public Vector3 forwardOffset = new Vector3(0.28f, -0.20f, 0.72f);

    [Tooltip("Final resting position after the sign. This is relative to the chest.")]
    public Vector3 finishOffset = new Vector3(0.18f, -0.32f, 0.42f);

    [Header("Elbow Bend Control")]
    [Tooltip("Controls elbow direction. X pushes elbow outward. Z brings elbow forward. Y moves elbow up/down.")]
    public Vector3 elbowHintOffset = new Vector3(0.60f, -0.24f, 0.46f);

    [Header("Hand Rotation")]
    [Tooltip("Tune if the palm/fingers face the wrong way. Y turns palm, Z twists wrist, X tilts up/down.")]
    public Vector3 cheekHandRotationOffsetEuler = new Vector3(8f, 95f, -28f);

    [Tooltip("Rotation while the hand moves forward from the cheek.")]
    public Vector3 forwardHandRotationOffsetEuler = new Vector3(10f, 78f, -18f);

    [Tooltip("Extra Z wrist rotation after the finger reaches the cheek.")]
    public float cheekContactZTwist = -35f;

    [Tooltip("Extra X wrist rotation after the finger reaches the cheek.")]
    public float cheekContactXTwist = 20f;


    [Header("Finger Shape")]
    [Tooltip("Most rigs curl fingers around local X. If fingers bend backwards, use (-1,0,0) or try (0,-1,0).")]
    public Vector3 fingerCurlAxis = new Vector3(-1f, 0f, 0f);

    [Tooltip("Index should stay mostly extended for Tomorrow.")]
    public float indexCurl = 0f;

    [Tooltip("Soft fold for middle/ring/little fingers.")]
    public float foldedFingerCurl = 30f;

    [Tooltip("Small thumb curl.")]
    public float thumbCurl = 8f;

    [Header("Smoothing")]
    public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool drawDebugGizmos = true;

    private Animator animator;
    private Transform head;
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
            Debug.LogError("[SignTomorrowBSLAnimator] No Animator found. Put this script on the avatar GameObject with the Animator.");
            enabled = false;
            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogError("[SignTomorrowBSLAnimator] Animator is not Humanoid. Change the model Rig to Humanoid.");
            enabled = false;
            return;
        }

        head = animator.GetBoneTransform(HumanBodyBones.Head);
        chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);

        if (head == null || chest == null)
        {
            Debug.LogError("[SignTomorrowBSLAnimator] Missing Head/Chest bones in the Humanoid Avatar mapping.");
            enabled = false;
            return;
        }

        CacheFingerBones();

        if (showDebugLogs)
        {
            Debug.Log("[SignTomorrowBSLAnimator] Ready. Press T to replay TOMORROW.");
            Debug.Log("[SignTomorrowBSLAnimator] Animator Controller assigned: " + (animator.runtimeAnimatorController != null));
        }
    }

    private void Start()
    {
        if (playOnStart)
            PlayTomorrow();
    }

    private void Update()
    {
        if (Input.GetKeyDown(replayKey))
            PlayTomorrow();

        if (!isPlaying)
            return;

        timer += Time.deltaTime;
        if (timer >= signDuration)
        {
            timer = signDuration;
            isPlaying = false;
        }
    }

    public void PlayTomorrow()
    {
        timer = 0f;
        isPlaying = true;

        if (showDebugLogs)
            Debug.Log("[SignTomorrowBSLAnimator] Playing BSL TOMORROW.");
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || head == null || chest == null)
            return;

        bool active = isPlaying || (holdLastPose && timer > 0f);
        float activeWeight = active ? 1f : 0f;

        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, signDuration));
        Vector3 target = GetTomorrowHandPosition(t);
        Quaternion rotation = GetTomorrowHandRotation(t);
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
        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, signDuration));
        float shapeWeight = active ? FingerShapeWeight(t) : 0f;

        Vector3 axis = fingerCurlAxis.sqrMagnitude < 0.0001f ? Vector3.right : fingerCurlAxis.normalized;

        for (int i = 0; i < fingerBones.Length; i++)
        {
            FingerBone fb = fingerBones[i];
            if (fb.bone == null) continue;

            Quaternion curl = Quaternion.AngleAxis(fb.curlAmount * shapeWeight, axis);
            fb.bone.localRotation = fb.startLocalRotation * curl;
        }
    }

    private Vector3 GetTomorrowHandPosition(float t)
    {
        float side = dominantHand == DominantHand.Right ? 1f : -1f;

        Vector3 startPos = BodyPoint(chest, new Vector3(startOffset.x * side, startOffset.y, startOffset.z));
        Vector3 cheekPos = BodyPoint(head, new Vector3(cheekOffset.x * side, cheekOffset.y, cheekOffset.z));
        Vector3 forwardPos = BodyPoint(chest, new Vector3(forwardOffset.x * side, forwardOffset.y, forwardOffset.z));
        Vector3 finishPos = BodyPoint(chest, new Vector3(finishOffset.x * side, finishOffset.y, finishOffset.z));

        Vector3 target;

        // BSL TOMORROW motion:
        // 0.00-0.25: hand rises from chest area
        // 0.25-0.55: hand reaches cheek, wrist twists and holds
        // 0.55-0.74: hand moves forward and slightly downward
        // 0.74-1.00: hand settles down
        if (t < 0.25f)
        {
            target = Vector3.Lerp(startPos, cheekPos, Ease(t / 0.25f));
        }
        else if (t < 0.55f)
        {
            target = cheekPos;
        }
        else if (t < 0.74f)
        {
            target = Vector3.Lerp(cheekPos, forwardPos, Ease((t - 0.55f) / 0.19f));
        }
        else
        {
            target = Vector3.Lerp(forwardPos, finishPos, Ease((t - 0.74f) / 0.26f));
        }

        // Stop the hand going through the avatar.
        target = KeepHandOutsideBody(target, side);
        return target;
    }

    private Quaternion GetTomorrowHandRotation(float t)
  {
    float side = dominantHand == DominantHand.Right ? 1f : -1f;

    Quaternion baseFacing = Quaternion.LookRotation(transform.forward, transform.up);

    Vector3 cheekEuler = cheekHandRotationOffsetEuler;
    Vector3 forwardEuler = forwardHandRotationOffsetEuler;

    float xTwistWeight = 0f;
    float zTwistWeight = 0f;

    if (t < 0.25f)
    {
        xTwistWeight = 0f;
        zTwistWeight = 0f;
    }
    else if (t < 0.35f)
    {
        float twistIn = Ease((t - 0.25f) / 0.10f);
        xTwistWeight = twistIn;
        zTwistWeight = twistIn;
    }
    else if (t < 0.55f)
    {
        xTwistWeight = 1f;
        zTwistWeight = 1f;
    }
    else if (t < 0.74f)
    {
        float downProgress = Ease((t - 0.55f) / 0.19f);

        // Keeps rotating as the hand comes down
        xTwistWeight = Mathf.Lerp(1f, 1.6f, downProgress);
        zTwistWeight = Mathf.Lerp(1f, 1.6f, downProgress);
    }
    else
    {
        float settleProgress = Ease((t - 0.74f) / 0.26f);

        xTwistWeight = Mathf.Lerp(1.6f, 0f, settleProgress);
        zTwistWeight = Mathf.Lerp(1.6f, 0f, settleProgress);
    }

    cheekEuler.x += cheekContactXTwist * xTwistWeight;
    cheekEuler.z += cheekContactZTwist * zTwistWeight;

    forwardEuler.x += cheekContactXTwist * xTwistWeight;
    forwardEuler.z += cheekContactZTwist * zTwistWeight;

    Quaternion cheekRot = baseFacing * Quaternion.Euler(
        cheekEuler.x,
        cheekEuler.y * side,
        cheekEuler.z * side
    );

    Quaternion forwardRot = baseFacing * Quaternion.Euler(
        forwardEuler.x,
        forwardEuler.y * side,
        forwardEuler.z * side
    );

    float blend = Mathf.Clamp01((t - 0.55f) / 0.19f);
    return Quaternion.Slerp(cheekRot, forwardRot, Ease(blend));
}

      private Vector3 KeepHandOutsideBody(Vector3 target, float side)
{
    if (chest == null)
        return target;

    Vector3 chestLocal = chest.InverseTransformPoint(target);

    // Z = forward from the chest. Increase this if the hand still clips.
    chestLocal.z = Mathf.Max(chestLocal.z, minForwardFromChest);

    // X = side from the body centre. Keeps right hand on right side,
    // and left hand on left side.
    if (side > 0f)
    {
        chestLocal.x = Mathf.Max(chestLocal.x, minSideFromCenter);
    }
    else
    {
        chestLocal.x = Mathf.Min(chestLocal.x, -minSideFromCenter);
    }

    return chest.TransformPoint(chestLocal);
}

   
    private Vector3 GetElbowHintPosition()
    {
        float side = dominantHand == DominantHand.Right ? 1f : -1f;
        return BodyPoint(chest, new Vector3(elbowHintOffset.x * side, elbowHintOffset.y, elbowHintOffset.z));
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
