using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public long savedAtUnix;

    public List<BuildingSaveData> buildings = new();
    public ResourceSaveData resources = new();
    public ResearchSaveData research = new();

    public List<string> unlockedBuildings = new(); // BuildMode names
}

[Serializable]
public class BuildingSaveData
{
    public string mode;   // BuildMode name
    public int x;
    public int y;

    // optional state:
    public int stage = -1;     // -1 => not used
    public bool paused = false;
}

[Serializable]
public class ResourceSaveData
{
    public List<ResourceIntKV> amounts = new();
    public List<ResourceIntKV> max = new();         // optional
    public List<ResourceFloatKV> buffers = new();
}

[Serializable] public class ResourceIntKV { public string key; public int value; }
[Serializable] public class ResourceFloatKV { public string key; public float value; }

[Serializable]
public class ResearchSaveData
{
    public List<string> completed = new();
    // при необходимости добавьте:
    // public string currentResearchId;
    // public float currentProgress;
}