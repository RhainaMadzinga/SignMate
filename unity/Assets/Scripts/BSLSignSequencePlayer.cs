using System.Collections;
using UnityEngine;

/// <summary>
/// Plays your BSL sign scripts in order.
/// Attach this to the same avatar GameObject that has:
/// SignIBSLAnimator, SignHomeBSLAnimator, SignTomorrowBSLAnimator, SignGoBSLAnimator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class BSLSignSequencePlayer : MonoBehaviour
{
    [System.Serializable]
    public class SignStep
    {
        [Tooltip("Name shown in the Console.")]
        public string signName = "I";

        [Tooltip("The GameObject/component target. Usually this avatar GameObject.")]
        public GameObject targetObject;

        [Tooltip("The public method on the sign script, e.g. PlayI, PlayHome, PlayTomorrow, PlayGo.")]
        public string playMethodName = "PlayI";

        [Tooltip("How long to wait before playing the next sign. Include sign duration + return-to-normal time.")]
        public float waitTime = 1.7f;
    }

    [Header("Playback")]
    public bool playOnStart = false;
    public KeyCode playSequenceKey = KeyCode.Space;
    public bool loop = false;

    [Tooltip("Pause between each word/sign.")]
    public float gapBetweenSigns = 0.15f;

    [Header("Sentence Order")]
    public SignStep[] sequence;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentSequence;

    private void Reset()
    {
        // Default order: I HOME TOMORROW GO.
        // You can reorder these in the Inspector.
        sequence = new SignStep[]
        {
            new SignStep { signName = "I",        playMethodName = "PlayI",        waitTime = 1.65f },
            new SignStep { signName = "HOME",     playMethodName = "PlayHome",     waitTime = 1.85f },
            new SignStep { signName = "TOMORROW", playMethodName = "PlayTomorrow", waitTime = 2.05f },
            new SignStep { signName = "GO",       playMethodName = "PlayGo",       waitTime = 1.65f },
        };
    }

    private void Awake()
    {
        // If targetObject is empty, default it to this avatar GameObject.
        if (sequence != null)
        {
            for (int i = 0; i < sequence.Length; i++)
            {
                if (sequence[i] != null && sequence[i].targetObject == null)
                    sequence[i].targetObject = gameObject;
            }
        }
    }

    private void Start()
    {
        if (playOnStart)
            PlaySequence();
    }

    private void Update()
    {
        if (Input.GetKeyDown(playSequenceKey))
            PlaySequence();
    }

    public void PlaySequence()
    {
        if (currentSequence != null)
            StopCoroutine(currentSequence);

        currentSequence = StartCoroutine(PlaySequenceRoutine());
    }

    public void StopSequence()
    {
        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
            currentSequence = null;
        }
    }

    private IEnumerator PlaySequenceRoutine()
    {
        if (sequence == null || sequence.Length == 0)
        {
            Debug.LogWarning("[BSLSignSequencePlayer] No signs in sequence.");
            yield break;
        }

        do
        {
            for (int i = 0; i < sequence.Length; i++)
            {
                SignStep step = sequence[i];
                if (step == null)
                    continue;

                GameObject target = step.targetObject != null ? step.targetObject : gameObject;

                if (showDebugLogs)
                    Debug.Log("[BSLSignSequencePlayer] Playing sign: " + step.signName + " using method: " + step.playMethodName);

                // SendMessage is used so this works with all your separate sign scripts
                // without needing direct references to each class.
                target.SendMessage(step.playMethodName, SendMessageOptions.DontRequireReceiver);

                yield return new WaitForSeconds(Mathf.Max(0.01f, step.waitTime));

                if (gapBetweenSigns > 0f)
                    yield return new WaitForSeconds(gapBetweenSigns);
            }
        }
        while (loop);

        currentSequence = null;
    }
}
