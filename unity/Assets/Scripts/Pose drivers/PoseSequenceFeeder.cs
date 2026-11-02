using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseSequenceFeeder : MonoBehaviour
{
    public BslSequencePlayer sequencePlayer;
    public MediaPipeUpperBodyAvatarDriver driver;

    public float positionScale = 2f;
    public float depthScale = 2f;

    private void LateUpdate()
    {
        if (sequencePlayer == null)
        {
            Debug.LogError("PoseSequenceFeeder: sequencePlayer is null");
            return;
        }

        if (driver == null)
        {
            Debug.LogError("PoseSequenceFeeder: driver is null");
            return;
        }

        if (sequencePlayer.currentFrame == null)
        {
            Debug.LogWarning("PoseSequenceFeeder: currentFrame is null");
            return;
        }

        var pose = sequencePlayer.currentFrame.pose;
        if (pose == null || pose.Length < 25)
        {
            Debug.LogWarning("PoseSequenceFeeder: pose is null or too short");
            return;
        }

        Debug.Log("PoseSequenceFeeder sending pose to " + driver.gameObject.name);

        Vector3[] converted = new Vector3[33];

        for (int i = 0; i < pose.Length && i < 33; i++)
        {
            converted[i] = new Vector3(
                -(pose[i].x - 0.5f) * positionScale,
                -(pose[i].y - 0.5f) * positionScale,
                -pose[i].z * depthScale
            );
        }

        driver.SetPoseLandmarks(converted);

        Debug.Log("pose length = " + pose.Length);
        Debug.Log("left shoulder x = " + pose[11].x);
        Debug.Log("right shoulder x = " + pose[12].x);
    }
}