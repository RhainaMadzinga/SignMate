using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum BslPlaybackPhase
{
    Idle,
    LeadIn,
    Playing,
    ReturningToRest,
    Paused
}

public class BslSequencePlayer : MonoBehaviour
{
    [Header("Sequence Library")]
    public string manifestFileName = "bsl_gloss_manifest.json";
    public string defaultGloss = "hello";
    public string jsonFileName = "sign_sequence.json";

    [Header("Playback")]
    public float framesPerSecond = 30f;
    public bool autoPlayOnStart = true;
    public bool loop = false;
    public float leadInSeconds = 1f;
    public float returnToRestSeconds = 1f;

    public BslSequence sequence;
    public BslFrame currentFrame;
    public string currentGloss;
    public string[] availableGlosses = new string[0];

    public BslPlaybackPhase Phase { get; private set; } = BslPlaybackPhase.Idle;
    public float BlendWeight { get; private set; }
    public int CurrentFrameIndex => frameIndex;
    public bool HasSequence => sequence != null && sequence.frames != null && sequence.frames.Length > 0;
    public bool IsPlaying =>
        Phase == BslPlaybackPhase.LeadIn ||
        Phase == BslPlaybackPhase.Playing ||
        Phase == BslPlaybackPhase.ReturningToRest;

    private readonly Dictionary<string, BslGlossEntry> glossesByName =
        new Dictionary<string, BslGlossEntry>(StringComparer.OrdinalIgnoreCase);

    private BslGlossManifest manifest;
    private int frameIndex;
    private float frameTimer;
    private float transitionTimer;
    private BslPlaybackPhase pausedPhase = BslPlaybackPhase.Idle;

    private void Start()
    {
        LoadManifest();

        bool loaded = false;
        if (!string.IsNullOrWhiteSpace(defaultGloss))
        {
            loaded = LoadGloss(defaultGloss);
        }

        if (!loaded)
        {
            loaded = LoadLegacySequence();
        }

        if (loaded && autoPlayOnStart)
            Play();
    }

    private void Update()
    {
        if (!HasSequence || Phase == BslPlaybackPhase.Idle || Phase == BslPlaybackPhase.Paused)
            return;

        if (Phase == BslPlaybackPhase.LeadIn)
        {
            UpdateLeadIn();
            return;
        }

        if (Phase == BslPlaybackPhase.ReturningToRest)
        {
            UpdateReturnToRest();
            return;
        }

        BlendWeight = 1f;
        frameTimer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(framesPerSecond, 1f);

        while (frameTimer >= frameTime && Phase == BslPlaybackPhase.Playing)
        {
            frameTimer -= frameTime;
            AdvanceFrame();
        }
    }

    public bool LoadGloss(string glossName)
    {
        if (string.IsNullOrWhiteSpace(glossName))
        {
            Debug.LogWarning("Cannot load a blank BSL gloss.");
            return false;
        }

        if (!glossesByName.TryGetValue(glossName, out BslGlossEntry entry))
        {
            Debug.LogWarning("BSL gloss not found in manifest: " + glossName);
            return false;
        }

        string path = Path.Combine(Application.streamingAssetsPath, entry.file);
        return LoadSequence(path, entry.name, ResolveFramesPerSecond(entry));
    }

    public bool Play()
    {
        if (!HasSequence)
        {
            Debug.LogWarning("Cannot play a BSL sequence before one is loaded.");
            return false;
        }

        if (Phase == BslPlaybackPhase.Paused)
        {
            Phase = pausedPhase == BslPlaybackPhase.Paused ? BslPlaybackPhase.Playing : pausedPhase;
            return true;
        }

        frameIndex = 0;
        frameTimer = 0f;
        transitionTimer = 0f;
        currentFrame = sequence.frames[0];
        pausedPhase = BslPlaybackPhase.Idle;

        if (leadInSeconds > 0f)
        {
            BlendWeight = 0f;
            Phase = BslPlaybackPhase.LeadIn;
        }
        else
        {
            BlendWeight = 1f;
            Phase = BslPlaybackPhase.Playing;
        }

        return true;
    }

    public void Pause()
    {
        if (!IsPlaying)
            return;

        pausedPhase = Phase;
        Phase = BslPlaybackPhase.Paused;
    }

    public void Stop()
    {
        frameIndex = 0;
        frameTimer = 0f;
        transitionTimer = 0f;
        BlendWeight = 0f;
        pausedPhase = BslPlaybackPhase.Idle;
        Phase = BslPlaybackPhase.Idle;

        if (HasSequence)
            currentFrame = sequence.frames[0];
    }

    private void LoadManifest()
    {
        glossesByName.Clear();
        availableGlosses = new string[0];

        string path = Path.Combine(Application.streamingAssetsPath, manifestFileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning("BSL gloss manifest not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        manifest = JsonUtility.FromJson<BslGlossManifest>(json);

        if (manifest == null || manifest.glosses == null)
        {
            Debug.LogWarning("Failed to parse BSL gloss manifest: " + path);
            return;
        }

        List<string> names = new List<string>();
        foreach (BslGlossEntry entry in manifest.glosses)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.name) || string.IsNullOrWhiteSpace(entry.file))
                continue;

            glossesByName[entry.name] = entry;
            names.Add(entry.name);
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        availableGlosses = names.ToArray();
    }

    private bool LoadLegacySequence()
    {
        if (string.IsNullOrWhiteSpace(jsonFileName))
            return false;

        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        if (!File.Exists(path))
            return false;

        return LoadSequence(path, Path.GetFileNameWithoutExtension(jsonFileName), framesPerSecond);
    }

    private bool LoadSequence(string path, string glossName, float fps)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("BSL sequence file not found: " + path);
            return false;
        }

        string json = File.ReadAllText(path);
        BslSequence loadedSequence = JsonUtility.FromJson<BslSequence>(json);

        Debug.Log("Loaded path: " + path);
        Debug.Log("JSON starts with: " + json.Substring(0, Mathf.Min(200, json.Length)));
        Debug.Log("loadedSequence null? " + (loadedSequence == null));

if (loadedSequence != null)
{
    Debug.Log("frames null? " + (loadedSequence.frames == null));
    if (loadedSequence.frames != null)
        Debug.Log("frames length = " + loadedSequence.frames.Length);
}

        if (loadedSequence == null || loadedSequence.frames == null || loadedSequence.frames.Length == 0)
        {
            Debug.LogWarning("Failed to load BSL sequence: " + path);
            return false;
        }

        if (loadedSequence.frame_count > 0 && loadedSequence.frame_count != loadedSequence.frames.Length)
        {
            Debug.LogWarning(
                "BSL sequence frame_count does not match the frame array for " +
                glossName +
                ". Using the frame array length."
            );
        }

        sequence = loadedSequence;
        currentFrame = loadedSequence.frames[0];
        Debug.Log("Loaded path: " + path);
        Debug.Log("Loaded frame count: " + loadedSequence.frames.Length);
        Debug.Log("Frame 0 pose null? " + (loadedSequence.frames[0].pose == null));
        Debug.Log("Frame 0 left_hand null? " + (loadedSequence.frames[0].left_hand == null));
        Debug.Log("Frame 0 right_hand null? " + (loadedSequence.frames[0].right_hand == null));

if (loadedSequence.frames[0].pose != null)
    Debug.Log("Frame 0 pose length = " + loadedSequence.frames[0].pose.Length);
        currentGloss = glossName;
        framesPerSecond = Mathf.Max(fps, 1f);
        Stop();

        Debug.Log("Loaded BSL gloss '" + glossName + "' with " + loadedSequence.frames.Length + " frames.");
        return true;
    }

    private float ResolveFramesPerSecond(BslGlossEntry entry)
    {
        if (entry != null && entry.fps > 0f)
            return entry.fps;

        if (manifest != null && manifest.default_fps > 0f)
            return manifest.default_fps;

        return framesPerSecond;
    }

    private void UpdateLeadIn()
    {
        transitionTimer += Time.deltaTime;
        BlendWeight = Mathf.Clamp01(transitionTimer / Mathf.Max(leadInSeconds, 0.0001f));

        if (BlendWeight >= 1f)
        {
            transitionTimer = 0f;
            Phase = BslPlaybackPhase.Playing;
        }
    }

    private void UpdateReturnToRest()
    {
        transitionTimer += Time.deltaTime;
        BlendWeight = 1f - Mathf.Clamp01(transitionTimer / Mathf.Max(returnToRestSeconds, 0.0001f));

        if (BlendWeight <= 0f)
        {
            Phase = BslPlaybackPhase.Idle;
            transitionTimer = 0f;
        }
    }

    private void AdvanceFrame()
    {
        if (frameIndex >= sequence.frames.Length - 1)
        {
            if (loop)
            {
                frameIndex = 0;
                currentFrame = sequence.frames[frameIndex];
                return;
            }

            BeginReturnToRest();
            return;
        }

        frameIndex++;
        currentFrame = sequence.frames[frameIndex];
    }

    private void BeginReturnToRest()
    {
        if (returnToRestSeconds <= 0f)
        {
            Stop();
            return;
        }

        transitionTimer = 0f;
        BlendWeight = 1f;
        Phase = BslPlaybackPhase.ReturningToRest;
    }
}
