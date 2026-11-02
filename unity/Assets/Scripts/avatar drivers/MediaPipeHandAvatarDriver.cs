using UnityEngine;

public class MediaPipeHandAvatarDriver : MonoBehaviour
{
    public HumanoidHandReferences handRefs;

    [Header("Smoothing")]
    public float smoothSpeed = 12f;

    [Header("Offsets")]
    public Vector3 thumbProximalOffsetEuler;
    public Vector3 thumbIntermediateOffsetEuler;
    public Vector3 thumbDistalOffsetEuler;

    public Vector3 indexProximalOffsetEuler;
    public Vector3 indexIntermediateOffsetEuler;
    public Vector3 indexDistalOffsetEuler;

    public Vector3 middleProximalOffsetEuler;
    public Vector3 middleIntermediateOffsetEuler;
    public Vector3 middleDistalOffsetEuler;

    public Vector3 ringProximalOffsetEuler;
    public Vector3 ringIntermediateOffsetEuler;
    public Vector3 ringDistalOffsetEuler;

    public Vector3 littleProximalOffsetEuler;
    public Vector3 littleIntermediateOffsetEuler;
    public Vector3 littleDistalOffsetEuler;

    private Vector3[] landmarks;
    private bool hasLandmarks;

    public void SetLandmarks(Vector3[] newLandmarks)
    {
        if (newLandmarks == null || newLandmarks.Length != 21)
        {
            hasLandmarks = false;
            return;
        }

        landmarks = newLandmarks;
        hasLandmarks = true;
    }

    private void LateUpdate()
    {
        if (!hasLandmarks || handRefs == null) return;

        Vector3 upHint = handRefs.rightHand != null ? handRefs.rightHand.up : Vector3.up;

        // Thumb
        DriveBone(handRefs.rightThumbProximal,  landmarks[1],  landmarks[2], upHint, thumbProximalOffsetEuler);
        DriveBone(handRefs.rightThumbIntermediate, landmarks[2],  landmarks[3], upHint, thumbIntermediateOffsetEuler);
        DriveBone(handRefs.rightThumbDistal, landmarks[3],  landmarks[4], upHint, thumbDistalOffsetEuler);

        // Index
        DriveBone(handRefs.rightIndexProximal, landmarks[5],  landmarks[6], upHint, indexProximalOffsetEuler);
        DriveBone(handRefs.rightIndexIntermediate, landmarks[6],  landmarks[7], upHint, indexIntermediateOffsetEuler);
        DriveBone(handRefs.rightIndexDistal, landmarks[7],  landmarks[8], upHint, indexDistalOffsetEuler);

        // Middle
        DriveBone(handRefs.rightMiddleProximal, landmarks[9],  landmarks[10], upHint, middleProximalOffsetEuler);
        DriveBone(handRefs.rightMiddleIntermediate, landmarks[10], landmarks[11], upHint, middleIntermediateOffsetEuler);
        DriveBone(handRefs.rightMiddleDistal, landmarks[11], landmarks[12], upHint, middleDistalOffsetEuler);

        // Ring
        DriveBone(handRefs.rightRingProximal, landmarks[13], landmarks[14], upHint, ringProximalOffsetEuler);
        DriveBone(handRefs.rightRingIntermediate, landmarks[14], landmarks[15], upHint, ringIntermediateOffsetEuler);
        DriveBone(handRefs.rightRingDistal, landmarks[15], landmarks[16], upHint, ringDistalOffsetEuler);

        // Little
        DriveBone(handRefs.rightLittleProximal, landmarks[17], landmarks[18], upHint, littleProximalOffsetEuler);
        DriveBone(handRefs.rightLittleIntermediate, landmarks[18], landmarks[19], upHint, littleIntermediateOffsetEuler);
        DriveBone(handRefs.rightLittleDistal, landmarks[19], landmarks[20], upHint, littleDistalOffsetEuler);
    }

    private void DriveBone(Transform bone, Vector3 from, Vector3 to, Vector3 upHint, Vector3 offsetEuler)
    {
        if (bone == null) return;

        Vector3 dir = (to - from);
        if (dir.sqrMagnitude < 0.000001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, upHint) * Quaternion.Euler(offsetEuler);

        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        bone.rotation = Quaternion.Slerp(bone.rotation, target, t);
    }
}