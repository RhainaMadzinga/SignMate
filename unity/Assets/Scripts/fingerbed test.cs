using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fingerbedtest : MonoBehaviour
{
    public HumanoidHandReferences handRefs;
    public float bend = 45f;

    private Quaternion p0;
    private Quaternion i0;
    private Quaternion d0;

    private void Start()
    {
        if (handRefs == null) return;

        if (handRefs.rightIndexProximal != null) p0 = handRefs.rightIndexProximal.localRotation;
        if (handRefs.rightIndexIntermediate != null) i0 = handRefs.rightIndexIntermediate.localRotation;
        if (handRefs.rightIndexDistal != null) d0 = handRefs.rightIndexDistal.localRotation;
    }

    private void Update()
    {
        if (handRefs == null) return;
        if (handRefs.rightIndexProximal == null || handRefs.rightIndexIntermediate == null || handRefs.rightIndexDistal == null) return;

        if (Input.GetKey(KeyCode.B))
        {
            handRefs.rightIndexProximal.localRotation = p0 * Quaternion.Euler(-bend, 0, 0);
            handRefs.rightIndexIntermediate.localRotation = i0 * Quaternion.Euler(-bend, 0, 0);
            handRefs.rightIndexDistal.localRotation = d0 * Quaternion.Euler(-bend, 0, 0);
        }
        else
        {
            handRefs.rightIndexProximal.localRotation = p0;
            handRefs.rightIndexIntermediate.localRotation = i0;
            handRefs.rightIndexDistal.localRotation = d0;
        }
    }
}

