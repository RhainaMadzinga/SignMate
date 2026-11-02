using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolisticPoseSource : MonoBehaviour
{
    public Vector3[] poseLandmarks = new Vector3[33];
    public bool hasPose;

    public void SetPose(Vector3[] landmarks)
    {
        if (landmarks == null || landmarks.Length < 25)
        {
            hasPose = false;
            return;
        }

        poseLandmarks = landmarks;
        hasPose = true;
    }

    public Vector3 GetPoint(int index)
    {
        if (!hasPose || poseLandmarks == null || index < 0 || index >= poseLandmarks.Length)
            return Vector3.zero;

        return poseLandmarks[index];
    }
}