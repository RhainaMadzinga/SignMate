using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HumanoidBslSequenceDriver : MonoBehaviour
{
    [Header("Playback Source")]
    public BslSequencePlayer sequencePlayer;

    [Header("Input Coordinates")]
    public bool mirrorInputX = true;
    public bool invertInputZ = true;

    [Header("Motion")]
    public float rotationSmoothing = 14f;

    [Header("Gloss Motion")]
    public bool useGlossReferenceMotion = true;
    public Vector3 glossMotionGain = new Vector3(2.15f, 1.28f, 1.85f);
    public float elbowMotionGain = 1.42f;
    public float elbowFollowWristGain = 0.34f;

    [Header("Finger Motion")]
    public float handRotationWeight = 1f;
    public float fingerRotationWeight = 1f;
    public Vector3 fingerSegmentWeights = new Vector3(1f, 0.92f, 0.82f);

    [Header("Optional Hand Offsets")]
    public Vector3 leftHandOffsetEuler;
    public Vector3 rightHandOffsetEuler;

    private Animator animator;
    private ArmBindings leftArm;
    private ArmBindings rightArm;
    private HandBindings leftHand;
    private HandBindings rightHand;
    private BslSequence cachedSequence;
    private readonly Dictionary<string, GlossReference> glossReferences =
        new Dictionary<string, GlossReference>(StringComparer.OrdinalIgnoreCase);
    private bool ready;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
        {
            Debug.LogError("HumanoidBslSequenceDriver requires a valid Humanoid Animator avatar.");
            enabled = false;
            return;
        }

        leftArm = CreateArmBindings(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
        rightArm = CreateArmBindings(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
        leftHand = CreateHandBindings(true);
        rightHand = CreateHandBindings(false);

        ready = leftArm.IsUsable || rightArm.IsUsable || leftHand.IsUsable || rightHand.IsUsable;
        if (!ready)
        {
            Debug.LogError("HumanoidBslSequenceDriver could not find any usable humanoid arm or hand bones.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (sequencePlayer == null)
            sequencePlayer = FindObjectOfType<BslSequencePlayer>();
    }

    private void LateUpdate()
    {
        if (!ready)
            return;

        float blendWeight = sequencePlayer != null ? sequencePlayer.BlendWeight : 0f;
        BslFrame frame = sequencePlayer != null ? sequencePlayer.currentFrame : null;
        EnsureGlossReferences();

        if (frame == null || blendWeight <= 0f)
        {
            ApplyRestPose();
            return;
        }

        ApplyArmPose(leftArm, frame, true, blendWeight);
        ApplyArmPose(rightArm, frame, false, blendWeight);
        ApplyHandPose(leftHand, frame.left_hand, true, blendWeight, leftHandOffsetEuler);
        ApplyHandPose(rightHand, frame.right_hand, false, blendWeight, rightHandOffsetEuler);
    }

    private ArmBindings CreateArmBindings(HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones handBone)
    {
        Transform upper = animator.GetBoneTransform(upperBone);
        Transform lower = animator.GetBoneTransform(lowerBone);
        Transform hand = animator.GetBoneTransform(handBone);

        return new ArmBindings
        {
            upper = CreateBinding(upper, lower),
            lower = CreateBinding(lower, hand)
        };
    }

    private HandBindings CreateHandBindings(bool isLeft)
    {
        HumanBodyBones handBone = isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        HumanBodyBones thumbProximal = isLeft ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal;
        HumanBodyBones thumbIntermediate = isLeft ? HumanBodyBones.LeftThumbIntermediate : HumanBodyBones.RightThumbIntermediate;
        HumanBodyBones thumbDistal = isLeft ? HumanBodyBones.LeftThumbDistal : HumanBodyBones.RightThumbDistal;
        HumanBodyBones indexProximal = isLeft ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal;
        HumanBodyBones indexIntermediate = isLeft ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate;
        HumanBodyBones indexDistal = isLeft ? HumanBodyBones.LeftIndexDistal : HumanBodyBones.RightIndexDistal;
        HumanBodyBones middleProximal = isLeft ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal;
        HumanBodyBones middleIntermediate = isLeft ? HumanBodyBones.LeftMiddleIntermediate : HumanBodyBones.RightMiddleIntermediate;
        HumanBodyBones middleDistal = isLeft ? HumanBodyBones.LeftMiddleDistal : HumanBodyBones.RightMiddleDistal;
        HumanBodyBones ringProximal = isLeft ? HumanBodyBones.LeftRingProximal : HumanBodyBones.RightRingProximal;
        HumanBodyBones ringIntermediate = isLeft ? HumanBodyBones.LeftRingIntermediate : HumanBodyBones.RightRingIntermediate;
        HumanBodyBones ringDistal = isLeft ? HumanBodyBones.LeftRingDistal : HumanBodyBones.RightRingDistal;
        HumanBodyBones littleProximal = isLeft ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal;
        HumanBodyBones littleIntermediate = isLeft ? HumanBodyBones.LeftLittleIntermediate : HumanBodyBones.RightLittleIntermediate;
        HumanBodyBones littleDistal = isLeft ? HumanBodyBones.LeftLittleDistal : HumanBodyBones.RightLittleDistal;

        Transform hand = animator.GetBoneTransform(handBone);
        Transform middleRoot = animator.GetBoneTransform(middleProximal);

        return new HandBindings
        {
            hand = CreateBinding(hand, middleRoot),
            thumb = CreateFingerBindings(thumbProximal, thumbIntermediate, thumbDistal),
            index = CreateFingerBindings(indexProximal, indexIntermediate, indexDistal),
            middle = CreateFingerBindings(middleProximal, middleIntermediate, middleDistal),
            ring = CreateFingerBindings(ringProximal, ringIntermediate, ringDistal),
            little = CreateFingerBindings(littleProximal, littleIntermediate, littleDistal)
        };
    }

    private FingerBindings CreateFingerBindings(
        HumanBodyBones proximalBone,
        HumanBodyBones intermediateBone,
        HumanBodyBones distalBone
    )
    {
        Transform proximal = animator.GetBoneTransform(proximalBone);
        Transform intermediate = animator.GetBoneTransform(intermediateBone);
        Transform distal = animator.GetBoneTransform(distalBone);

        return new FingerBindings
        {
            proximal = CreateBinding(proximal, intermediate),
            intermediate = CreateBinding(intermediate, distal),
            distal = CreateBinding(distal, null)
        };
    }

    private BoneBinding CreateBinding(Transform bone, Transform nextBone)
    {
        if (bone == null)
            return new BoneBinding();

        Vector3 direction = nextBone != null ? nextBone.position - bone.position : bone.forward;
        if (direction.sqrMagnitude < 0.000001f)
            direction = bone.forward;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, ResolveUp(direction, transform.up));
        return new BoneBinding
        {
            bone = bone,
            restRotation = bone.rotation,
            lookOffset = Quaternion.Inverse(lookRotation) * bone.rotation
        };
    }

    private void EnsureGlossReferences()
    {
        BslSequence sequence = sequencePlayer != null ? sequencePlayer.sequence : null;
        if (sequence == cachedSequence)
            return;

        cachedSequence = sequence;
        glossReferences.Clear();

        if (sequence == null || sequence.frames == null)
            return;

        foreach (BslFrame frame in sequence.frames)
        {
            if (frame == null || frame.pose == null)
                continue;

            string gloss = FrameGloss(frame);
            if (!glossReferences.TryGetValue(gloss, out GlossReference reference))
            {
                reference = new GlossReference();
                glossReferences[gloss] = reference;
            }

            CaptureSideReference(reference.left, frame, true);
            CaptureSideReference(reference.right, frame, false);
        }
    }

    private void CaptureSideReference(SideReference reference, BslFrame frame, bool isLeft)
    {
        if (reference.hasReference || frame.pose == null)
            return;

        int shoulderIndex = isLeft ? 11 : 12;
        int elbowIndex = isLeft ? 13 : 14;
        int wristIndex = isLeft ? 15 : 16;
        if (frame.pose.Length <= wristIndex)
            return;

        reference.shoulder = ToRaw(frame.pose[shoulderIndex]);
        reference.elbow = ToRaw(frame.pose[elbowIndex]);
        reference.wrist = ToRaw(frame.pose[wristIndex]);
        reference.hasReference = true;
    }

    private void ApplyArmPose(ArmBindings bindings, BslFrame frame, bool isLeft, float blendWeight)
    {
        Landmark4[] pose = frame != null ? frame.pose : null;
        int shoulderIndex = isLeft ? 11 : 12;
        int elbowIndex = isLeft ? 13 : 14;
        int wristIndex = isLeft ? 15 : 16;
        if (pose == null || pose.Length <= wristIndex)
            return;

        Vector3 shoulder = ToRaw(pose[shoulderIndex]);
        Vector3 elbow = ToRaw(pose[elbowIndex]);
        Vector3 wrist = ToRaw(pose[wristIndex]);

        if (useGlossReferenceMotion && TryGetSideReference(frame, isLeft, out SideReference reference))
        {
            Vector3 originalWrist = wrist;
            wrist = AmplifyRawPoint(wrist, reference.wrist, glossMotionGain);
            elbow = reference.elbow + (elbow - reference.elbow) * Mathf.Max(elbowMotionGain, 0f);
            elbow += (wrist - originalWrist) * elbowFollowWristGain;
        }

        if (TryConvertDirection(elbow - shoulder, out Vector3 upperDirection))
            ApplyDirection(bindings.upper, upperDirection, transform.up, blendWeight, Vector3.zero);

        if (TryConvertDirection(wrist - elbow, out Vector3 lowerDirection))
            ApplyDirection(bindings.lower, lowerDirection, transform.up, blendWeight, Vector3.zero);
    }

    private void ApplyHandPose(
        HandBindings bindings,
        Landmark3[] landmarks,
        bool isLeft,
        float blendWeight,
        Vector3 handOffsetEuler
    )
    {
        if (landmarks == null || landmarks.Length < 21)
            return;

        if (!TryGetHandBasis(landmarks, isLeft, out Vector3 handForward, out Vector3 palmNormal))
            return;

        ApplyDirection(bindings.hand, handForward, palmNormal, blendWeight * handRotationWeight, handOffsetEuler);

        ApplyFingerPose(bindings.thumb, landmarks, 1, 2, 3, 4, palmNormal, blendWeight * fingerRotationWeight);
        ApplyFingerPose(bindings.index, landmarks, 5, 6, 7, 8, palmNormal, blendWeight * fingerRotationWeight);
        ApplyFingerPose(bindings.middle, landmarks, 9, 10, 11, 12, palmNormal, blendWeight * fingerRotationWeight);
        ApplyFingerPose(bindings.ring, landmarks, 13, 14, 15, 16, palmNormal, blendWeight * fingerRotationWeight);
        ApplyFingerPose(bindings.little, landmarks, 17, 18, 19, 20, palmNormal, blendWeight * fingerRotationWeight);
    }

    private void ApplyFingerPose(
        FingerBindings bindings,
        Landmark3[] landmarks,
        int a,
        int b,
        int c,
        int d,
        Vector3 upHint,
        float blendWeight
    )
    {
        if (TryGetHandDirection(landmarks, a, b, out Vector3 proximalDirection))
            ApplyDirection(bindings.proximal, proximalDirection, upHint, blendWeight * fingerSegmentWeights.x, Vector3.zero);

        if (TryGetHandDirection(landmarks, b, c, out Vector3 intermediateDirection))
            ApplyDirection(bindings.intermediate, intermediateDirection, upHint, blendWeight * fingerSegmentWeights.y, Vector3.zero);

        if (TryGetHandDirection(landmarks, c, d, out Vector3 distalDirection))
            ApplyDirection(bindings.distal, distalDirection, upHint, blendWeight * fingerSegmentWeights.z, Vector3.zero);
    }

    private void ApplyDirection(
        BoneBinding binding,
        Vector3 direction,
        Vector3 upHint,
        float blendWeight,
        Vector3 offsetEuler
    )
    {
        if (!binding.IsUsable || direction.sqrMagnitude < 0.000001f)
            return;

        Vector3 safeUp = ResolveUp(direction, upHint);
        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized, safeUp) *
            binding.lookOffset *
            Quaternion.Euler(offsetEuler);
        Quaternion blendedTarget = Quaternion.Slerp(binding.restRotation, targetRotation, Mathf.Clamp01(blendWeight));

        float t = 1f - Mathf.Exp(-Mathf.Max(rotationSmoothing, 0f) * Time.deltaTime);
        binding.bone.rotation = Quaternion.Slerp(binding.bone.rotation, blendedTarget, t);
    }

    private void ApplyRestPose()
    {
        ApplyRest(leftArm.upper);
        ApplyRest(leftArm.lower);
        ApplyRest(rightArm.upper);
        ApplyRest(rightArm.lower);
        ApplyRest(leftHand.hand);
        ApplyRest(rightHand.hand);
        ApplyFingerRest(leftHand.thumb);
        ApplyFingerRest(leftHand.index);
        ApplyFingerRest(leftHand.middle);
        ApplyFingerRest(leftHand.ring);
        ApplyFingerRest(leftHand.little);
        ApplyFingerRest(rightHand.thumb);
        ApplyFingerRest(rightHand.index);
        ApplyFingerRest(rightHand.middle);
        ApplyFingerRest(rightHand.ring);
        ApplyFingerRest(rightHand.little);
    }

    private void ApplyFingerRest(FingerBindings bindings)
    {
        ApplyRest(bindings.proximal);
        ApplyRest(bindings.intermediate);
        ApplyRest(bindings.distal);
    }

    private void ApplyRest(BoneBinding binding)
    {
        if (!binding.IsUsable)
            return;

        float t = 1f - Mathf.Exp(-Mathf.Max(rotationSmoothing, 0f) * Time.deltaTime);
        binding.bone.rotation = Quaternion.Slerp(binding.bone.rotation, binding.restRotation, t);
    }

    private bool TryGetSideReference(BslFrame frame, bool isLeft, out SideReference reference)
    {
        reference = null;
        if (frame == null)
            return false;

        if (!glossReferences.TryGetValue(FrameGloss(frame), out GlossReference glossReference))
            return false;

        reference = isLeft ? glossReference.left : glossReference.right;
        return reference != null && reference.hasReference;
    }

    private string FrameGloss(BslFrame frame)
    {
        if (frame != null && !string.IsNullOrWhiteSpace(frame.source_gloss))
            return frame.source_gloss;

        if (sequencePlayer != null && !string.IsNullOrWhiteSpace(sequencePlayer.currentGloss))
            return sequencePlayer.currentGloss;

        return "sequence";
    }

    private Vector3 ToRaw(Landmark4 landmark)
    {
        return new Vector3(landmark.x, landmark.y, landmark.z);
    }

    private Vector3 AmplifyRawPoint(Vector3 point, Vector3 reference, Vector3 gain)
    {
        Vector3 delta = point - reference;
        return reference + new Vector3(delta.x * gain.x, delta.y * gain.y, delta.z * gain.z);
    }

    private bool TryGetPoseDirection(Landmark4[] landmarks, int fromIndex, int toIndex, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (landmarks == null || landmarks.Length <= Mathf.Max(fromIndex, toIndex))
            return false;

        return TryConvertDirection(ToRaw(landmarks[toIndex]) - ToRaw(landmarks[fromIndex]), out direction);
    }

    private bool TryGetHandDirection(Landmark3[] landmarks, int fromIndex, int toIndex, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (landmarks == null || landmarks.Length <= Mathf.Max(fromIndex, toIndex))
            return false;

        Landmark3 from = landmarks[fromIndex];
        Landmark3 to = landmarks[toIndex];
        return TryConvertDirection(new Vector3(to.x - from.x, to.y - from.y, to.z - from.z), out direction);
    }

    private bool TryGetHandBasis(Landmark3[] landmarks, bool isLeft, out Vector3 forward, out Vector3 palmNormal)
    {
        forward = Vector3.zero;
        palmNormal = Vector3.zero;

        if (!TryGetHandDirection(landmarks, 0, 9, out forward))
            return false;

        if (!TryGetHandDirection(landmarks, 0, 5, out Vector3 indexDirection))
            return false;

        if (!TryGetHandDirection(landmarks, 0, 17, out Vector3 littleDirection))
            return false;

        palmNormal = Vector3.Cross(indexDirection, littleDirection).normalized;
        if (isLeft)
            palmNormal = -palmNormal;

        return palmNormal.sqrMagnitude > 0.000001f;
    }

    private bool TryConvertDirection(Vector3 rawDirection, out Vector3 direction)
    {
        Vector3 source = new Vector3(
            mirrorInputX ? -rawDirection.x : rawDirection.x,
            -rawDirection.y,
            invertInputZ ? -rawDirection.z : rawDirection.z
        );

        if (source.sqrMagnitude < 0.000001f)
        {
            direction = Vector3.zero;
            return false;
        }

        direction =
            transform.right * source.x +
            transform.up * source.y +
            transform.forward * source.z;
        direction.Normalize();
        return true;
    }

    private Vector3 ResolveUp(Vector3 direction, Vector3 upHint)
    {
        Vector3 safeUp = upHint.sqrMagnitude > 0.000001f ? upHint.normalized : transform.up;
        Vector3 normalizedDirection = direction.sqrMagnitude > 0.000001f ? direction.normalized : transform.forward;

        if (Mathf.Abs(Vector3.Dot(normalizedDirection, safeUp)) > 0.98f)
        {
            safeUp = Mathf.Abs(Vector3.Dot(normalizedDirection, transform.forward)) < 0.98f
                ? transform.forward
                : transform.right;
        }

        return safeUp.normalized;
    }

    private class BoneBinding
    {
        public Transform bone;
        public Quaternion restRotation;
        public Quaternion lookOffset;
        public bool IsUsable => bone != null;
    }

    private class ArmBindings
    {
        public BoneBinding upper = new BoneBinding();
        public BoneBinding lower = new BoneBinding();
        public bool IsUsable => upper.IsUsable || lower.IsUsable;
    }

    private class FingerBindings
    {
        public BoneBinding proximal = new BoneBinding();
        public BoneBinding intermediate = new BoneBinding();
        public BoneBinding distal = new BoneBinding();
        public bool IsUsable => proximal.IsUsable || intermediate.IsUsable || distal.IsUsable;
    }

    private class HandBindings
    {
        public BoneBinding hand = new BoneBinding();
        public FingerBindings thumb = new FingerBindings();
        public FingerBindings index = new FingerBindings();
        public FingerBindings middle = new FingerBindings();
        public FingerBindings ring = new FingerBindings();
        public FingerBindings little = new FingerBindings();
        public bool IsUsable =>
            hand.IsUsable ||
            thumb.IsUsable ||
            index.IsUsable ||
            middle.IsUsable ||
            ring.IsUsable ||
            little.IsUsable;
    }

    private class SideReference
    {
        public bool hasReference;
        public Vector3 shoulder;
        public Vector3 elbow;
        public Vector3 wrist;
    }

    private class GlossReference
    {
        public SideReference left = new SideReference();
        public SideReference right = new SideReference();
    }
}
