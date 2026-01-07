using System;
using System.Collections.Generic;
using UnityEngine;

#region Root save model

[Serializable]
public class GameSaveData
{
    // Увеличиваем версию, потому что меняем структуру меты
    public int version = 2;

    // Время сохранения (Unix seconds, UTC)
    public long savedAtUnix;

    // META "число": население People на момент сохранения
    public int people;

    // Основные данные игры
    public List<BuildingSaveData> buildings = new();
    public ResourceSaveData resources = new();
    public ResearchSaveData research = new();
    public List<BaseTileSaveData> baseTiles = new();
    public string previewFile; 
    public int mapW;
    public int mapH;

    public int sortingLayerId;
    public int sortingOrder;

    // BuildMode names
    public List<string> unlockedBuildings = new();
}

#endregion

#region Map / tiles

[Serializable]
public class BaseTileSaveData
{
    public int x, y;
    public BaseTileType type;
}

#endregion

#region Buildings

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
    // путь Transform внутри здания (например "House/AngryIcon")
    public string path;

    public int layerId;
    public int order;

    // важно для angryPrefab / иконок
    public bool activeSelf;
}

#endregion

#region Resources

[Serializable]
public class ResourceSaveData
{
    public List<ResourceIntKV> amounts = new();
    public List<ResourceIntKV> max = new();         // optional
    public List<ResourceFloatKV> buffers = new();
}

[Serializable]
public class ResourceIntKV
{
    public string key;
    public int value;
}

[Serializable]
public class ResourceFloatKV
{
    public string key;
    public float value;
}

#endregion

#region Research

[Serializable]
public class ResearchSaveData
{
    public List<string> completed = new();

    // при необходимости добавьте:
    // public string currentResearchId;
    // public float currentProgress;
}

#endregion
