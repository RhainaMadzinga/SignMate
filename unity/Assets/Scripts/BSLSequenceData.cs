using System;


[Serializable]
public class Landmark4
{
    public float x;
    public float y;
    public float z;
    public float v;
}

[Serializable]
public class Landmark3
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class BslFrame
{
    public Landmark4[] pose;
    public Landmark3[] left_hand;
    public Landmark3[] right_hand;
    public string source_gloss;
    public int source_frame;
}

[Serializable]
public class BslSequence
{
    public int frame_count;
    public BslFrame[] frames;
}

[Serializable]
public class BslGlossEntry
{
    public string name;
    public string file;
    public int frame_count;
    public float fps;
}

[Serializable]
public class BslGlossManifest
{
    public float default_fps = 30f;
    public BslGlossEntry[] glosses;
}

