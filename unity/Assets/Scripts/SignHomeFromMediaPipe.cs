using System;
using UnityEngine;

/// <summary>
/// Drives a Humanoid avatar to sign the BSL word "HOME" using MediaPipe landmark JSON.
///
/// Expected JSON shape:
/// {
///   "label": "home",
///   "frame_count": 201,
///   "frames": [ { "pose": [...], "left_hand": [...], "right_hand": [...] } ]
/// }
///
/// Setup:
/// 1. Put this script in Assets/Scripts.
/// 2. Put your home_002 MediaPipe JSON/Text file in Assets, then drag it into Media Pipe Json.
/// 3. Attach this script to the same GameObject as the Animator.
/// 4. Animator must be Humanoid and have an Animator Controller.
/// 5. Enable IK Pass on the Animator Controller Base Layer.
/// 6. Press Play. Press H to replay HOME.
/// </summary>
public class SignHomeFromMediaPipe : MonoBehaviour
{
    [Header("Input")]
    public TextAsset mediaPipeJson;
    public bool playOnStart = true;
    public KeyCode replayKey = KeyCode.H;

    [Header("Playback")]
    [Tooltip("The uploaded HOME file says it has 201 frames. 25fps gives about 8 seconds.")]
    public float sourceFps = 25f;
    public bool loop = false;
    public bool holdLastPose = true;
    [Range(0f, 1f)] public float smoothing = 0.30f;

    [Header("Which Hands")]
    public bool driveRightHand = true;
    public bool driveLeftHand = true;

    [Header("IK Weights")]
    [Range(0f, 1f)] public float handPositionWeight = 1f;
    [Range(0f, 1f)] public float handRotationWeight = 0.85f;
    [Range(0f, 1f)] public float elbowHintWeight = 1f;

    [Header("MediaPipe to Avatar Scale")]
    [Tooltip("Flips camera X so MediaPipe screen movement maps naturally onto the avatar.")]
    public bool mirrorX = true;

    [Tooltip("Left/right movement scale from MediaPipe screen coordinates.")]
    public float horizontalScale = 2.0f;

    [Tooltip("Up/down movement scale from MediaPipe screen coordinates.")]
    public float verticalScale = 1.65f;

    [Tooltip("Constant forward distance from chest. Increase if hands go into the body.")]
    public float forwardDepth = 0.36f;

    [Tooltip("Moves both hands up/down relative to chest.")]
    public float verticalOffset = 0.04f;

    [Tooltip("Moves both hands sideways. Usually keep small.")]
    public float horizontalOffset = 0.0f;

    [Tooltip("Extra forward depth for elbow hints. Increase if arms bend sideways/backwards.")]
    public float elbowForwardOffset = 0.32f;

    [Header("Hand Rotation")]
    public bool useMediaPipeHandRotation = true;

    [Tooltip("Right hand palm/wrist correction. Tune if palm faces the wrong way.")]
    public Vector3 rightHandRotationOffsetEuler = new Vector3(0f, 90f, -20f);

    [Tooltip("Left hand palm/wrist correction. Tune if palm faces the wrong way.")]
    public Vector3 leftHandRotationOffsetEuler = new Vector3(0f, -90f, 20f);

    [Header("Finger Curl From MediaPipe")]
    public bool driveFingers = true;

    [Tooltip("Most rigs curl fingers around local X. If fingers bend backwards, try (1,0,0), (0,-1,0), or (0,0,-1).")]
    public Vector3 fingerCurlAxis = new Vector3(-1f, 0f, 0f);

    public float minCurl = 0f;
    public float maxCurl = 65f;
    public float thumbExtraCurl = 5f;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool drawDebugTargets = true;

    private Animator animator;
    private Transform chest;

    private MediaPipeFile data;
    private bool isPlaying;
    private float timer;
    private float duration;

    private HandRuntime rightRuntime = new HandRuntime();
    private HandRuntime leftRuntime = new HandRuntime();

    private HandRig rightRig;
    private HandRig leftRig;

    [Serializable]
    public class Landmark
    {
        public float x;
        public float y;
        public float z;
        public float v;
    }

    [Serializable]
    public class FrameData
    {
        public Landmark[] pose;
        public Landmark[] left_hand;
        public Landmark[] right_hand;
    }

    [Serializable]
    public class MediaPipeFile
    {
        public string label;
        public string source_csv;
        public int frame_count;
        public FrameData[] frames;
    }

    private class HandRuntime
    {
        public Vector3 handTarget;
        public Vector3 elbowTarget;
        public Quaternion handRotation;
        public bool hasPose;
    }

    private class HandRig
    {
        public HumanBodyBones[][] bones;
        public Quaternion[][] initialRotations;
        public float[] maxLengths = new float[5];
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("[SignHomeFromMediaPipe] No Animator found. Attach this script to the avatar GameObject that has the Animator.");
            enabled = false;
            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogError("[SignHomeFromMediaPipe] Animator is not Humanoid. Set the model Rig to Humanoid.");
            enabled = false;
            return;
        }

        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);

        if (chest == null)
        {
            Debug.LogError("[SignHomeFromMediaPipe] Missing Chest/UpperChest/Spine mapping on the Humanoid Avatar.");
            enabled = false;
            return;
        }

        if (driveRightHand && animator.GetBoneTransform(HumanBodyBones.RightHand) == null)
        {
            Debug.LogError("[SignHomeFromMediaPipe] RightHand bone is missing from the Humanoid Avatar mapping.");
            enabled = false;
            return;
        }

        if (driveLeftHand && animator.GetBoneTransform(HumanBodyBones.LeftHand) == null)
        {
            Debug.LogError("[SignHomeFromMediaPipe] LeftHand bone is missing from the Humanoid Avatar mapping.");
            enabled = false;
            return;
        }

        rightRig = SetupFingerRig(true);
        leftRig = SetupFingerRig(false);
        LoadJson();
    }

    private void Start()
    {
        if (playOnStart)
            PlaySign();
    }

    private void Update()
    {
        if (Input.GetKeyDown(replayKey))
            PlaySign();

        if (!isPlaying || data == null || data.frames == null || data.frames.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= duration)
        {
            if (loop)
            {
                timer = 0f;
            }
            else
            {
                timer = duration;
                isPlaying = false;
            }
        }

        UpdateTargetsFromCurrentTime();
    }

    private void LateUpdate()
    {
        if (!driveFingers || data == null || data.frames == null || data.frames.Length == 0)
            return;

        if (!isPlaying && !holdLastPose)
            return;

        FrameData frame = GetFrameAtCurrentTime();
        if (driveRightHand) ApplyFingerCurl(frame.right_hand, rightRig);
        if (driveLeftHand) ApplyFingerCurl(frame.left_hand, leftRig);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (data == null || data.frames == null || data.frames.Length == 0)
            return;

        if (!isPlaying && !holdLastPose)
            return;

        if (!rightRuntime.hasPose && !leftRuntime.hasPose)
            UpdateTargetsFromCurrentTime();

        if (driveRightHand)
            ApplyIK(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, rightRuntime);

        if (driveLeftHand)
            ApplyIK(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, leftRuntime);
    }

    public void PlaySign()
    {
        if (data == null || data.frames == null || data.frames.Length == 0)
        {
            Debug.LogError("[SignHomeFromMediaPipe] Cannot play. No MediaPipe JSON loaded.");
            return;
        }

        timer = 0f;
        isPlaying = true;
        rightRuntime.hasPose = false;
        leftRuntime.hasPose = false;
        UpdateTargetsFromCurrentTime();

        if (showDebugLogs)
            Debug.Log("[SignHomeFromMediaPipe] Playing BSL word: HOME");
    }

    private void LoadJson()
    {
        if (mediaPipeJson == null)
        {
            Debug.LogError("[SignHomeFromMediaPipe] Drag your HOME MediaPipe JSON/Text file into the Media Pipe Json field.");
            enabled = false;
            return;
        }

        data = JsonUtility.FromJson<MediaPipeFile>(mediaPipeJson.text);

        if (data == null || data.frames == null || data.frames.Length == 0)
        {
            Debug.LogError("[SignHomeFromMediaPipe] JSON loaded, but no frames were found.");
            enabled = false;
            return;
        }

        duration = Mathf.Max(0.01f, data.frames.Length / Mathf.Max(1f, sourceFps));
        CalculateFingerReferenceLengths(data.frames, true, rightRig);
        CalculateFingerReferenceLengths(data.frames, false, leftRig);

        if (showDebugLogs)
            Debug.Log($"[SignHomeFromMediaPipe] Loaded label '{data.label}' with {data.frames.Length} frames. Duration: {duration:0.00}s");
    }

    private void UpdateTargetsFromCurrentTime()
    {
        FrameData frame = GetFrameAtCurrentTime();
        if (frame == null)
            return;

        if (driveRightHand)
        {
            Landmark wrist = GetSafeLandmark(frame.right_hand, 0) ?? GetSafeLandmark(frame.pose, 16);
            Landmark elbow = GetSafeLandmark(frame.pose, 14);
            UpdateOneHand(frame, wrist, elbow, true, rightRuntime);
        }

        if (driveLeftHand)
        {
            Landmark wrist = GetSafeLandmark(frame.left_hand, 0) ?? GetSafeLandmark(frame.pose, 15);
            Landmark elbow = GetSafeLandmark(frame.pose, 13);
            UpdateOneHand(frame, wrist, elbow, false, leftRuntime);
        }
    }

    private void UpdateOneHand(FrameData frame, Landmark wrist, Landmark elbow, bool right, HandRuntime runtime)
    {
        Vector3 handTarget = MediaPipePointToChestSpace(frame, wrist, forwardDepth);
        Vector3 fallbackElbow = handTarget + chest.TransformDirection(new Vector3(right ? 0.45f : -0.45f, -0.22f, elbowForwardOffset));
        Vector3 elbowTarget = elbow != null ? MediaPipePointToChestSpace(frame, elbow, forwardDepth + elbowForwardOffset) : fallbackElbow;

        Quaternion handRot = useMediaPipeHandRotation
            ? GetHandRotation(frame, right)
            : chest.rotation * Quaternion.Euler(right ? rightHandRotationOffsetEuler : leftHandRotationOffsetEuler);

        if (!runtime.hasPose || smoothing <= 0f)
        {
            runtime.handTarget = handTarget;
            runtime.elbowTarget = elbowTarget;
            runtime.handRotation = handRot;
            runtime.hasPose = true;
        }
        else
        {
            float lerp = 1f - Mathf.Pow(smoothing, Time.deltaTime * 30f);
            runtime.handTarget = Vector3.Lerp(runtime.handTarget, handTarget, lerp);
            runtime.elbowTarget = Vector3.Lerp(runtime.elbowTarget, elbowTarget, lerp);
            runtime.handRotation = Quaternion.Slerp(runtime.handRotation, handRot, lerp);
        }

        if (drawDebugTargets)
        {
            Debug.DrawLine(chest.position, runtime.handTarget, right ? Color.green : Color.cyan);
            Debug.DrawLine(chest.position, runtime.elbowTarget, Color.yellow);
        }
    }

    private void ApplyIK(AvatarIKGoal goal, AvatarIKHint elbowHint, HandRuntime runtime)
    {
        animator.SetIKPositionWeight(goal, handPositionWeight);
        animator.SetIKRotationWeight(goal, handRotationWeight);
        animator.SetIKPosition(goal, runtime.handTarget);
        animator.SetIKRotation(goal, runtime.handRotation);

        animator.SetIKHintPositionWeight(elbowHint, elbowHintWeight);
        animator.SetIKHintPosition(elbowHint, runtime.elbowTarget);
    }

    private FrameData GetFrameAtCurrentTime()
    {
        if (data == null || data.frames == null || data.frames.Length == 0)
            return null;

        float normalized = duration <= 0f ? 0f : Mathf.Clamp01(timer / duration);
        int index = Mathf.Clamp(Mathf.RoundToInt(normalized * (data.frames.Length - 1)), 0, data.frames.Length - 1);
        return data.frames[index];
    }

    private Vector3 MediaPipePointToChestSpace(FrameData frame, Landmark lm, float zForward)
    {
        if (lm == null)
            return chest.position;

        Landmark leftShoulder = GetSafeLandmark(frame.pose, 11);
        Landmark rightShoulder = GetSafeLandmark(frame.pose, 12);

        float centreX = 0.5f;
        float centreY = 0.5f;

        if (leftShoulder != null && rightShoulder != null)
        {
            centreX = (leftShoulder.x + rightShoulder.x) * 0.5f;
            centreY = (leftShoulder.y + rightShoulder.y) * 0.5f;
        }

        float dx = lm.x - centreX;
        float dy = centreY - lm.y;

        float localX = (mirrorX ? -dx : dx) * horizontalScale + horizontalOffset;
        float localY = dy * verticalScale + verticalOffset;

        return chest.TransformPoint(new Vector3(localX, localY, zForward));
    }

    private Vector3 MediaPipeVectorToChestSpace(FrameData frame, Landmark from, Landmark to)
    {
        if (from == null || to == null)
            return chest.forward;

        float dx = to.x - from.x;
        float dy = from.y - to.y;
        float dz = to.z - from.z;

        float localX = (mirrorX ? -dx : dx) * horizontalScale;
        float localY = dy * verticalScale;
        float localZ = -dz * 2f;

        Vector3 v = chest.TransformDirection(new Vector3(localX, localY, localZ));
        return v.sqrMagnitude < 0.0001f ? chest.forward : v.normalized;
    }

    private Quaternion GetHandRotation(FrameData frame, bool right)
    {
        Landmark[] hand = right ? frame.right_hand : frame.left_hand;
        Landmark wrist = GetSafeLandmark(hand, 0);
        Landmark indexMcp = GetSafeLandmark(hand, 5);
        Landmark pinkyMcp = GetSafeLandmark(hand, 17);
        Landmark middleMcp = GetSafeLandmark(hand, 9);

        Vector3 offsetEuler = right ? rightHandRotationOffsetEuler : leftHandRotationOffsetEuler;

        if (wrist == null || indexMcp == null || pinkyMcp == null || middleMcp == null)
            return chest.rotation * Quaternion.Euler(offsetEuler);

        Vector3 wristToMiddle = MediaPipeVectorToChestSpace(frame, wrist, middleMcp);
        Vector3 wristToIndex = MediaPipeVectorToChestSpace(frame, wrist, indexMcp);
        Vector3 wristToPinky = MediaPipeVectorToChestSpace(frame, wrist, pinkyMcp);

        Vector3 palmNormal = Vector3.Cross(wristToIndex, wristToPinky).normalized;
        if (!right) palmNormal = -palmNormal;
        if (palmNormal.sqrMagnitude < 0.001f) palmNormal = chest.forward;

        Quaternion mpRot = Quaternion.LookRotation(palmNormal, wristToMiddle);
        return mpRot * Quaternion.Euler(offsetEuler);
    }

    private Landmark GetSafeLandmark(Landmark[] landmarks, int index)
    {
        if (landmarks == null || index < 0 || index >= landmarks.Length)
            return null;
        return landmarks[index];
    }

    private HandRig SetupFingerRig(bool right)
    {
        HandRig rig = new HandRig();
        rig.bones = right
            ? new HumanBodyBones[][]
            {
                new HumanBodyBones[] { HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal },
                new HumanBodyBones[] { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal },
                new HumanBodyBones[] { HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal },
                new HumanBodyBones[] { HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal },
                new HumanBodyBones[] { HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal }
            }
            : new HumanBodyBones[][]
            {
                new HumanBodyBones[] { HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal },
                new HumanBodyBones[] { HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal },
                new HumanBodyBones[] { HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal },
                new HumanBodyBones[] { HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal },
                new HumanBodyBones[] { HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal }
            };

        rig.initialRotations = new Quaternion[rig.bones.Length][];
        for (int f = 0; f < rig.bones.Length; f++)
        {
            rig.initialRotations[f] = new Quaternion[rig.bones[f].Length];
            for (int j = 0; j < rig.bones[f].Length; j++)
            {
                Transform bone = animator.GetBoneTransform(rig.bones[f][j]);
                rig.initialRotations[f][j] = bone != null ? bone.localRotation : Quaternion.identity;
            }
            rig.maxLengths[f] = 0.001f;
        }
        return rig;
    }

    private void CalculateFingerReferenceLengths(FrameData[] frames, bool right, HandRig rig)
    {
        if (rig == null) return;

        int[] tips = { 4, 8, 12, 16, 20 };
        foreach (FrameData frame in frames)
        {
            Landmark[] hand = right ? frame.right_hand : frame.left_hand;
            Landmark wrist = GetSafeLandmark(hand, 0);
            if (wrist == null) continue;

            for (int i = 0; i < tips.Length; i++)
            {
                Landmark tip = GetSafeLandmark(hand, tips[i]);
                if (tip == null) continue;

                float d = Distance3(wrist, tip);
                if (d > rig.maxLengths[i]) rig.maxLengths[i] = d;
            }
        }
    }

    private void ApplyFingerCurl(Landmark[] hand, HandRig rig)
    {
        if (hand == null || rig == null) return;

        Landmark wrist = GetSafeLandmark(hand, 0);
        if (wrist == null) return;

        int[] tips = { 4, 8, 12, 16, 20 };
        Vector3 axis = fingerCurlAxis.sqrMagnitude < 0.001f ? Vector3.right : fingerCurlAxis.normalized;

        for (int finger = 0; finger < tips.Length; finger++)
        {
            Landmark tip = GetSafeLandmark(hand, tips[finger]);
            if (tip == null) continue;

            float length = Distance3(wrist, tip);
            float extension = Mathf.Clamp01(length / Mathf.Max(0.001f, rig.maxLengths[finger]));
            float curl = Mathf.Lerp(maxCurl, minCurl, extension);
            if (finger == 0) curl += thumbExtraCurl;

            for (int joint = 0; joint < rig.bones[finger].Length; joint++)
            {
                Transform bone = animator.GetBoneTransform(rig.bones[finger][joint]);
                if (bone == null) continue;

                float jointShare = joint == 0 ? 1f : 0.65f;
                bone.localRotation = rig.initialRotations[finger][joint] * Quaternion.AngleAxis(curl * jointShare, axis);
            }
        }
    }

    private float Distance3(Landmark a, Landmark b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
