using UnityEngine;

public class FingerTestBend : MonoBehaviour
{
    public HumanoidHandReferences handRefs;
    public float bendAngle = 45f;

    private void Update()
    {
        if (handRefs == null || handRefs.rightIndexProximal == null) return;

        if (Input.GetKey(KeyCode.B))
        {
            handRefs.rightIndexProximal.localRotation = Quaternion.Euler(0f, -bendAngle, 0f);
            handRefs.rightIndexIntermediate.localRotation = Quaternion.Euler(0f, -bendAngle, 0f);
            handRefs.rightIndexDistal.localRotation = Quaternion.Euler(0f, -bendAngle, 0f);
        }
    }
}