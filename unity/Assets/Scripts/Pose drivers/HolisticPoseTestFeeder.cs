using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolisticPoseTestFeeder : MonoBehaviour
{
    public HolisticPoseSource source;

    private void Update()
    {
        if (source == null) return;

        Vector3[] pose = new Vector3[33];

        // Nose
        pose[0] = new Vector3(0f, 1.7f, 0.15f);

        // Shoulders
        pose[11] = new Vector3(-0.25f, 1.45f, 0f);
        pose[12] = new Vector3(0.25f, 1.45f, 0f);

        // Elbows
        pose[13] = new Vector3(-0.45f, 1.2f, 0.10f);
        pose[14] = new Vector3(0.45f, 1.2f, 0.10f);

        // Wrists
        pose[15] = new Vector3(-0.35f, 0.95f, 0.20f);
        pose[16] = new Vector3(0.35f, 0.95f, 0.20f);

        // Hips
        pose[23] = new Vector3(-0.18f, 1.0f, 0f);
        pose[24] = new Vector3(0.18f, 1.0f, 0f);

        source.SetPose(pose);
    }
}