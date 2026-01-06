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
    public List<BaseTileSaveData> baseTiles = new();
    public int mapW;
    public int mapH;
    public int sortingLayerId;
    public int sortingOrder;

    public List<string> unlockedBuildings = new(); // BuildMode names
}
[Serializable]
public class BaseTileSaveData
{
    public int x, y;
    public BaseTileType type;
}




[Serializable]
public class BuildingSaveData
{
    public string mode;   // BuildMode name
    public int x;
    public int y;
    public int sortingLayerId;
    public int sortingOrder;
    public bool hasRoadAccess;
    public bool needsAreMet;
    public List<RendererSortingSaveData> renderSortings = new();


    // optional state:
    public int stage = -1;     // -1 => not used
    public bool paused = false;
}
[Serializable]
public class RendererSortingSaveData
{
    public string path;        // путь Transform внутри здания (например "House/AngryIcon")
    public int layerId;
    public int order;
    public bool activeSelf;    // важно для angryPrefab
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