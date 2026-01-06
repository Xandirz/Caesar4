using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }
    public GridManager gridManager;
    public static bool IsLoading { get; private set; }

    string SavePath(string slot) =>
        Path.Combine(Application.persistentDataPath, "saves", $"{slot}.json");

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            Save();

        if (Input.GetKeyDown(KeyCode.F9))
            Load();
    }


    public void Save(string slot = "slot1")
    {
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "saves"));

        GameSaveData data = new GameSaveData
        {
            savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // 1) Buildings
        SaveBuildings(data);

        // 2) Resources
        SaveResources(data);

        // 3) Research + unlocks
        SaveResearchAndUnlocks(data);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath(slot), json);
        Debug.Log($"Saved to: {SavePath(slot)}");
    }

    public void Load(string slot = "slot1")
    {
        string path = SavePath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Save not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        ApplyLoadedData(data);
        Debug.Log($"Loaded from: {path}");
    }

    // --- split into parts below ---
    void SaveBuildings(GameSaveData data)
    {
        var grid = gridManager;      // или ссылка
        foreach (var po in grid.GetAllUniquePlacedObjects())
        {
            if (po == null) continue;

            var b = new BuildingSaveData
            {
                mode = po.BuildMode.ToString(), // важный момент: строка
                x = po.gridPos.x,
                y = po.gridPos.y,
                stage = -1,
                paused = false
            };

            // ProductionBuilding state
            if (po is ProductionBuilding pb)
            {
                b.stage = pb.CurrentStage;
                // добавьте публичный getter IsPaused (см. шаг 7)
                b.paused = pb.IsPaused;
            }

            // House state
            if (po is House h)
            {
                b.stage = h.CurrentStage;
            }

            data.buildings.Add(b);
        }
    }

    void SaveResources(GameSaveData data)
    {
        var rm = ResourceManager.Instance;

        var amounts = rm.GetResourcesCopy();
        foreach (var kv in amounts)
            data.resources.amounts.Add(new ResourceIntKV { key = kv.Key, value = kv.Value });

        var buffers = rm.GetBuffersCopy();
        foreach (var kv in buffers)
            data.resources.buffers.Add(new ResourceFloatKV { key = kv.Key, value = kv.Value });

        // optional: maxResources
        var max = rm.GetMaxResourcesCopy();
        foreach (var kv in max)
            data.resources.max.Add(new ResourceIntKV { key = kv.Key, value = kv.Value });
    }
    void RestoreResources(ResourceSaveData rs)
    {
        var amounts = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var kv in rs.amounts)
            amounts[kv.key] = kv.value;

        var max = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var kv in rs.max)
            max[kv.key] = kv.value;

        var buffers = new System.Collections.Generic.Dictionary<string, float>();
        foreach (var kv in rs.buffers)
            buffers[kv.key] = kv.value;

        ResourceManager.Instance.SetAllResources(amounts, max, buffers);
    }

    private void SaveResearchAndUnlocks(GameSaveData data)
    {
        // Research
        var research = ResearchManager.Instance;
        if (research != null)
        {
            // Implement these methods in ResearchManager
            data.research.completed = research.ExportCompletedResearch() ?? new List<string>();
        }
        else
        {
            data.research.completed = new List<string>();
        }

        // Unlocked buildings
        var build = BuildManager.Instance;
        if (build != null)
        {
            data.unlockedBuildings = build.ExportUnlockedBuildings() ?? new List<string>();
        }
        else
        {
            data.unlockedBuildings = new List<string>();
        }
    }

    void ApplyLoadedData(GameSaveData data)
    {
        IsLoading = true;

        // 0) Очистка текущего мира
        ClearCurrentWorld();

        // 1) Исследования
        ResearchManager.Instance.ImportState(data.research);

        // 2) Unlocked buildings (если не выводится из исследований)
        BuildManager.Instance.ImportUnlockedBuildings(data.unlockedBuildings);

        // 3) Постройки: лучше сначала дороги
        foreach (var b in data.buildings)
            if (b.mode == BuildManager.BuildMode.Road.ToString())
                SpawnAndApplyBuilding(b);

        foreach (var b in data.buildings)
            if (b.mode != BuildManager.BuildMode.Road.ToString())
                SpawnAndApplyBuilding(b);

        // 4) Ресурсы — В КОНЦЕ, чтобы перезатереть возможные изменения от OnPlaced()
        RestoreResources(data.resources);

        // 5) Финальные пересчёты/клемпы/обновления UI
        ResourceManager.Instance.ApplyStorageLimits(); // у вас уже есть:contentReference[oaicite:23]{index=23}
        if (ResourceUIManager.Instance != null)
            ResourceUIManager.Instance.ForceUpdateUI();

        IsLoading = false;
    }
    void SpawnAndApplyBuilding(BuildingSaveData b)
    {
        if (!Enum.TryParse<BuildManager.BuildMode>(b.mode, out var mode))
            return;

        var po = BuildManager.Instance.SpawnFromSave(mode, new Vector2Int(b.x, b.y));
        if (po == null) return;

        // apply per-building state
        if (po is ProductionBuilding pb)
        {
            b.stage = pb.CurrentStage;
            b.paused = pb.IsPaused; 

        }
        if (po is House h)
        {
            if (b.stage >= 0)
            {
                h.CurrentStage = b.stage;

            }
        }
    }
    void ClearCurrentWorld()
    {
        var grid = gridManager;
        var all = grid.GetAllUniquePlacedObjects();

        foreach (var po in all)
        {
            if (po == null) continue;
            po.OnRemoved();
            Destroy(po.gameObject);
        }

        // Сбросить occupancy/placedObjects.
        // Лучше добавить метод в GridManager:
        grid.ClearAllPlacedObjectsAndOccupancy();
    }

}