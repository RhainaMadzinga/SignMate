using System;
using UnityEngine;

/// <summary>
/// Drives a Humanoid avatar to sign the BSL word "I" using MediaPipe landmark JSON.
/// 
/// Setup:
/// 1. Put the coordinate JSON file in Assets/Resources/ or anywhere in Assets.
/// 2. Drag that JSON file into the "Media Pipe Json" field in the Inspector.
/// 3. Attach this script to the same GameObject as your Animator.
/// 4. Animator must use a Humanoid Avatar and have an Animator Controller.
/// 5. Enable IK Pass on the Animator Controller layer if using IK mode.
/// 6. Press Play. Press the I key to replay.
/// </summary>
public class SignIFromMediaPipe : MonoBehaviour
{
    [Header("Input")]
    public TextAsset mediaPipeJson;
    public bool playOnStart = true;
    public KeyCode replayKey = KeyCode.I;

    [Header("Playback")]
    [Tooltip("Source landmark file has 70 frames. 25fps gives about 2.8 seconds.")]
    public float sourceFps = 25f;
    public bool loop = false;
    public bool holdLastPose = true;
    [Range(0f, 1f)] public float smoothing = 0.35f;

    [Header("IK")]
    [Range(0f, 1f)] public float handPositionWeight = 1f;
    [Range(0f, 1f)] public float handRotationWeight = 0.8f;
    [Range(0f, 1f)] public float elbowHintWeight = 1f;

    [Header("MediaPipe to Avatar Scale")]
    [Tooltip("Flips the camera x-axis so the signer's right hand maps to the avatar's right hand.")]
    public bool mirrorX = true;

    [Tooltip("Left/right movement scale from MediaPipe screen coordinates.")]
    public float horizontalScale = 2.1f;

    [Tooltip("Up/down movement scale from MediaPipe screen coordinates.")]
    public float verticalScale = 1.65f;

    [Tooltip("Constant forward distance from the chest. Increase this if the hand goes into the body.")]
    public float forwardDepth = 0.34f;

    [Tooltip("Extra height offset relative to the chest.")]
    public float verticalOffset = 0.03f;

    [Tooltip("Extra sideways offset. Positive usually moves the right hand outward.")]
    public float horizontalOffset = 0.02f;

    [Tooltip("Extra forward/back offset for elbow hint. Increase if the arm bends sideways.")]
    public float elbowForwardOffset = 0.25f;

    [Header("Hand Rotation")]
    public bool useMediaPipeHandRotation = true;

    [Tooltip("Tune this if the wrist/palm faces the wrong direction.")]
    public Vector3 handRotationOffsetEuler = new Vector3(0f, 90f, -20f);

    [Header("Finger Curl From MediaPipe")]
    public bool driveFingers = true;

    [Tooltip("Most rigs curl around local X. If fingers bend backwards, change to (-1,0,0), (0,-1,0), or (0,0,-1).")]
    public Vector3 fingerCurlAxis = new Vector3(-1f, 0f, 0f);

    [Tooltip("Minimum curl angle for extended fingers.")]
    public float minCurl = 0f;

    [Tooltip("Maximum curl angle for folded fingers.")]
    public float maxCurl = 65f;

    [Tooltip("Extra curl for the thumb.")]
    public float thumbExtraCurl = 5f;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool drawDebugTargets = true;

    private Animator animator;
    private Transform chest;
    private Transform rightHand;

    private MediaPipeFile data;
    private bool isPlaying;
    private float timer;
    private float duration;
    private Vector3 currentHandTarget;
    private Vector3 currentElbowTarget;
    private Quaternion currentHandRotation;
    private bool hasCurrentPose;

    private HumanBodyBones[][] fingerBones;
    private Quaternion[][] initialFingerRotations;
    private float[] maxFingerLengths = new float[5];

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

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("[SignIFromMediaPipe] No Animator found. Attach this script to the avatar GameObject that has the Animator.");
            enabled = false;
            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogError("[SignIFromMediaPipe] Animator is not Humanoid. Set your model Rig to Humanoid.");
            enabled = false;
            return;
        }

        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null)
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if (chest == null)
            chest = animator.GetBoneTransform(HumanBodyBones.Spine);

        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (chest == null || rightHand == null)
        {
            Debug.LogError("[SignIFromMediaPipe] Missing Chest/Spine or RightHand bone mapping on the Humanoid Avatar.");
            enabled = false;
            return;
        }

        SetupFingerBones();
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
        if (driveFingers && data != null && data.frames != null && data.frames.Length > 0 && (isPlaying || holdLastPose))
        {
            ApplyFingerCurl(GetInterpolatedFrame());
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (data == null || data.frames == null || data.frames.Length == 0)
            return;

        if (!isPlaying && !holdLastPose)
            return;

        if (!hasCurrentPose)
            UpdateTargetsFromCurrentTime();

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, handPositionWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, handRotationWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, currentHandTarget);
        animator.SetIKRotation(AvatarIKGoal.RightHand, currentHandRotation);

        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, elbowHintWeight);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow, currentElbowTarget);
    }

    public void PlaySign()
    {
        if (data == null || data.frames == null || data.frames.Length == 0)
        {
            Debug.LogError("[SignIFromMediaPipe] Cannot play. No MediaPipe JSON loaded.");
            return;
        }

        timer = 0f;
        isPlaying = true;
        hasCurrentPose = false;
        UpdateTargetsFromCurrentTime();

        if (showDebugLogs)
            Debug.Log("[SignIFromMediaPipe] Playing BSL word: I");
    }

    private void LoadJson()
    {
        if (mediaPipeJson == null)
        {
            Debug.LogError("[SignIFromMediaPipe] Drag your i_005_mediapipe JSON file into the Media Pipe Json field.");
            enabled = false;
            return;
        }

        data = JsonUtility.FromJson<MediaPipeFile>(mediaPipeJson.text);

        if (data == null || data.frames == null || data.frames.Length == 0)
        {
            Debug.LogError("[SignIFromMediaPipe] JSON loaded, but no frames were found.");
            enabled = false;
            return;
        }

        duration = Mathf.Max(0.01f, data.frames.Length / Mathf.Max(1f, sourceFps));
        CalculateFingerReferenceLengths();

        if (showDebugLogs)
        {
            Debug.Log($"[SignIFromMediaPipe] Loaded label '{data.label}' with {data.frames.Length} frames. Duration: {duration:0.00}s");
        }
    }

    private void UpdateTargetsFromCurrentTime()
    {
        FrameData frame = GetInterpolatedFrame();
        if (frame == null)
            return;

        // MediaPipe pose indices:
        // 11 = left shoulder, 12 = right shoulder, 14 = right elbow, 16 = right wrist.
        Landmark wrist = GetSafeLandmark(frame.right_hand, 0);
        if (wrist == null)
            wrist = GetSafeLandmark(frame.pose, 16);

        Landmark rightElbow = GetSafeLandmark(frame.pose, 14);

        Vector3 handTarget = MediaPipePointToChestSpace(wrist, forwardDepth);
        Vector3 elbowTarget = rightElbow != null
            ? MediaPipePointToChestSpace(rightElbow, forwardDepth + elbowForwardOffset)
            : handTarget + chest.TransformDirection(new Vector3(0.45f, -0.2f, elbowForwardOffset));

        Quaternion handRot = useMediaPipeHandRotation
            ? GetHandRotation(frame)
            : chest.rotation * Quaternion.Euler(handRotationOffsetEuler);

        if (!hasCurrentPose || smoothing <= 0f)
        {
            currentHandTarget = handTarget;
            currentElbowTarget = elbowTarget;
            currentHandRotation = handRot;
            hasCurrentPose = true;
        }
        else
        {
            float lerp = 1f - Mathf.Pow(smoothing, Time.deltaTime * 30f);
            currentHandTarget = Vector3.Lerp(currentHandTarget, handTarget, lerp);
            currentElbowTarget = Vector3.Lerp(currentElbowTarget, elbowTarget, lerp);
            currentHandRotation = Quaternion.Slerp(currentHandRotation, handRot, lerp);
        }

        if (drawDebugTargets)
        {
            Debug.DrawLine(chest.position, currentHandTarget, Color.green);
            Debug.DrawLine(chest.position, currentElbowTarget, Color.yellow);
        }
    }

    private FrameData GetInterpolatedFrame()
    {
        if (data == null || data.frames == null || data.frames.Length == 0)
            return null;

        // For now we use nearest frame. This keeps JsonUtility simple and stable.
        float normalized = duration <= 0f ? 0f : Mathf.Clamp01(timer / duration);
        int index = Mathf.Clamp(Mathf.RoundToInt(normalized * (data.frames.Length - 1)), 0, data.frames.Length - 1);
        return data.frames[index];
    }

    private Vector3 MediaPipePointToChestSpace(Landmark lm, float zForward)
    {
        if (lm == null)
            return chest.position;

        Landmark leftShoulder = GetSafeLandmark(GetInterpolatedFrame().pose, 11);
        Landmark rightShoulder = GetSafeLandmark(GetInterpolatedFrame().pose, 12);

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

        Vector3 local = new Vector3(localX, localY, zForward);
        return chest.TransformPoint(local);
    }

    private Vector3 MediaPipeVectorToChestSpace(Landmark from, Landmark to)
    {
        if (from == null || to == null)
            return Vector3.forward;

        float dx = to.x - from.x;
        float dy = from.y - to.y;
        float dz = to.z - from.z;

        float localX = (mirrorX ? -dx : dx) * horizontalScale;
        float localY = dy * verticalScale;
        float localZ = -dz * 2f;

        Vector3 v = chest.TransformDirection(new Vector3(localX, localY, localZ));
        if (v.sqrMagnitude < 0.0001f)
            return chest.forward;

        return v.normalized;
    }

    private Quaternion GetHandRotation(FrameData frame)
    {
        Landmark wrist = GetSafeLandmark(frame.right_hand, 0);
        Landmark indexMcp = GetSafeLandmark(frame.right_hand, 5);
        Landmark pinkyMcp = GetSafeLandmark(frame.right_hand, 17);
        Landmark middleMcp = GetSafeLandmark(frame.right_hand, 9);

        if (wrist == null || indexMcp == null || pinkyMcp == null || middleMcp == null)
            return chest.rotation * Quaternion.Euler(handRotationOffsetEuler);

        Vector3 wristToMiddle = MediaPipeVectorToChestSpace(wrist, middleMcp);
        Vector3 wristToIndex = MediaPipeVectorToChestSpace(wrist, indexMcp);
        Vector3 wristToPinky = MediaPipeVectorToChestSpace(wrist, pinkyMcp);

        Vector3 palmNormal = Vector3.Cross(wristToIndex, wristToPinky).normalized;
        if (palmNormal.sqrMagnitude < 0.001f)
            palmNormal = chest.forward;

        Quaternion mpRot = Quaternion.LookRotation(palmNormal, wristToMiddle);
        return mpRot * Quaternion.Euler(handRotationOffsetEuler);
    }

    private Landmark GetSafeLandmark(Landmark[] landmarks, int index)
    {
        if (landmarks == null || index < 0 || index >= landmarks.Length)
            return null;

        return landmarks[index];
    }

    private void SetupFingerBones()
    {
        fingerBones = new HumanBodyBones[][]
        {
            new HumanBodyBones[] { HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal },
            new HumanBodyBones[] { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal },
            new HumanBodyBones[] { HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal },
            new HumanBodyBones[] { HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal },
            new HumanBodyBones[] { HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal }
        };

        initialFingerRotations = new Quaternion[fingerBones.Length][];

        for (int f = 0; f < fingerBones.Length; f++)
        {
            initialFingerRotations[f] = new Quaternion[fingerBones[f].Length];

            for (int j = 0; j < fingerBones[f].Length; j++)
            {
                Transform bone = animator.GetBoneTransform(fingerBones[f][j]);
                initialFingerRotations[f][j] = bone != null ? bone.localRotation : Quaternion.identity;
            }
        }
    }

    private void CalculateFingerReferenceLengths()
    {
        for (int i = 0; i < maxFingerLengths.Length; i++)
            maxFingerLengths[i] = 0.001f;

        int[] tips = { 4, 8, 12, 16, 20 };

        foreach (FrameData frame in data.frames)
        {
            Landmark wrist = GetSafeLandmark(frame.right_hand, 0);
            if (wrist == null)
                continue;

            for (int i = 0; i < tips.Length; i++)
            {
                Landmark tip = GetSafeLandmark(frame.right_hand, tips[i]);
                if (tip == null)
                    continue;

                float d = Distance3(wrist, tip);
                if (d > maxFingerLengths[i])
                    maxFingerLengths[i] = d;
            }
        }
    }

    private void ApplyFingerCurl(FrameData frame)
    {
        if (frame == null || frame.right_hand == null)
            return;

        Landmark wrist = GetSafeLandmark(frame.right_hand, 0);
        if (wrist == null)
            return;

        int[] tips = { 4, 8, 12, 16, 20 };
        Vector3 axis = fingerCurlAxis.sqrMagnitude < 0.001f ? Vector3.right : fingerCurlAxis.normalized;

        for (int finger = 0; finger < tips.Length; finger++)
        {
            Landmark tip = GetSafeLandmark(frame.right_hand, tips[finger]);
            if (tip == null)
                continue;

            float length = Distance3(wrist, tip);
            float extension = Mathf.Clamp01(length / Mathf.Max(0.001f, maxFingerLengths[finger]));

            // When the fingertip is close to the wrist, the finger is curled.
            float curl = Mathf.Lerp(maxCurl, minCurl, extension);
            if (finger == 0)
                curl += thumbExtraCurl;

            for (int joint = 0; joint < fingerBones[finger].Length; joint++)
            {
                Transform bone = animator.GetBoneTransform(fingerBones[finger][joint]);
                if (bone == null)
                    continue;

                float jointShare = joint == 0 ? 1f : 0.65f;
                bone.localRotation = initialFingerRotations[finger][joint] * Quaternion.AngleAxis(curl * jointShare, axis);
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
