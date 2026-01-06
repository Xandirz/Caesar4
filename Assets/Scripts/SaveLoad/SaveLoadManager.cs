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
        Debug.Log("save");
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "saves"));

        GameSaveData data = new GameSaveData
        {
            savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        data.mapW = gridManager.width;
        data.mapH = gridManager.height;
        data.baseTiles = gridManager.ExportBaseTiles();

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
    List<RendererSortingSaveData> ExportRendererSortings(Transform root)
    {
        var list = new List<RendererSortingSaveData>();
        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            list.Add(new RendererSortingSaveData
            {
                path = GetRelativePath(root, sr.transform),
                layerId = sr.sortingLayerID,
                order = sr.sortingOrder,
                activeSelf = sr.gameObject.activeSelf
            });
        }

        return list;
    }

    string GetRelativePath(Transform root, Transform t)
    {
        if (root == null || t == null) return "";
        if (t == root) return "";

        var stack = new List<string>();
        var cur = t;

        while (cur != null && cur != root)
        {
            stack.Add(cur.name);
            cur = cur.parent;
        }

        stack.Reverse();
        return string.Join("/", stack);
    }

    // --- split into parts below ---
    void SaveBuildings(GameSaveData data)
    {
        var grid = gridManager;

        foreach (var po in grid.GetAllUniquePlacedObjects())
        {
            if (po == null) continue;

            if (po.BuildMode == BuildManager.BuildMode.None)
            {
                Debug.LogWarning($"[SaveBuildings] Found PlacedObject with mode=None: {po.name} at {po.gridPos}");
                continue;
            }

            var b = new BuildingSaveData
            {
                mode = po.BuildMode.ToString(),
                x = po.gridPos.x,
                y = po.gridPos.y,
                stage = -1,
                paused = false,

                // ✅ сохраняем доступ к дороге
                hasRoadAccess = po.hasRoadAccess,

                // sorting "основного" SR (оставляем для совместимости/быстрого дебага)
                sortingLayerId = 0,
                sortingOrder = 0,

                // ✅ needsAreMet (по умолчанию: старые сейвы не ломаем)
                needsAreMet = false,

                // ✅ сортинг всех дочерних рендереров (включая angryPrefab) + активность
                renderSortings = ExportRendererSortings(po.transform)
            };

            // основной SR (если нужен)
            var sr = po.GetComponent<SpriteRenderer>();
            if (sr == null) sr = po.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                b.sortingLayerId = sr.sortingLayerID;
                b.sortingOrder = sr.sortingOrder;
            }

            // ProductionBuilding state
            if (po is ProductionBuilding pb)
            {
                b.stage = pb.CurrentStage;
                b.paused = pb.IsPaused;
            }

            // House state + ✅ needsAreMet
            if (po is House h)
            {
                b.stage = h.CurrentStage;

                b.needsAreMet = h.needsAreMet;
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

        // 0) Очистка текущего мира
        ClearCurrentWorld();

// 0.5) Базовая карта (baseTypes + визуальные тайлы) — ДО зданий
        gridManager.ImportBaseTiles(data.mapW, data.mapH, data.baseTiles);
        gridManager.RebuildBaseTileVisualsFromBaseTypes();



        
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
        if (!Enum.TryParse(b.mode, out BuildManager.BuildMode mode))
        {
            Debug.LogWarning($"[SaveLoad] Unknown BuildMode: {b.mode}");
            return;
        }

        if (mode == BuildManager.BuildMode.None)
        {
            Debug.LogWarning($"[SaveLoad] Skip mode=None at ({b.x},{b.y})");
            return;
        }

        var po = BuildManager.Instance.SpawnFromSave(mode, new Vector2Int(b.x, b.y));
        if (po == null) return;

        // ✅ APPLY state (из save -> в объект)
        if (po is ProductionBuilding pb)
        {
            if (b.stage >= 0)
                pb.SetStageFromSave(b.stage);

            pb.SetPaused(b.paused);
        }
        else if (po is House h)
        {
            if (b.stage >= 0)
                h.SetStageFromSave(b.stage);

            // ✅ restore needsAreMet (только если оно было сохранено)

            // Если у вас есть метод, который включает/выключает angryPrefab — вызови его здесь:
            // h.RefreshMoodVisual();
        }

        // ✅ restore hasRoadAccess (для всех зданий)
        po.hasRoadAccess = b.hasRoadAccess;

        // ✅ restore sorting "основного" (если вы уже делали)
        ApplySavedSorting(po, b);

        // ✅ КЛЮЧЕВОЕ: restore sorting + activeSelf для ВСЕХ дочерних рендереров (включая angryPrefab)
        // ДЕЛАЕМ В САМОМ КОНЦЕ, чтобы никакие ApplySorting/OnPlaced не перезатёрли.
        ApplyRendererSortings(po.transform, b.renderSortings);
    }



    void ApplyRendererSortings(Transform root, List<RendererSortingSaveData> saved)
    {
        if (saved == null || saved.Count == 0) return;

        foreach (var s in saved)
        {
            if (s == null) continue;

            var t = FindByPath(root, s.path);
            if (t == null) continue;

            // ✅ восстановить активность (важно для angryPrefab)
            t.gameObject.SetActive(s.activeSelf);

            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            sr.sortingLayerID = s.layerId;
            sr.sortingOrder = s.order;
        }
    }

    Transform FindByPath(Transform root, string path)
    {
        if (root == null) return null;
        if (string.IsNullOrEmpty(path)) return root;

        var parts = path.Split('/');
        Transform cur = root;

        foreach (var p in parts)
        {
            if (cur == null) return null;
            cur = cur.Find(p);
        }

        return cur;
    }



    void ApplySavedSorting(PlacedObject po, BuildingSaveData b)
    {
        if (po == null) return;

        // применяем ко всем SpriteRenderer внутри объекта (на случай составных)
        var renderers = po.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        foreach (var sr in renderers)
        {
            sr.sortingLayerID = b.sortingLayerId;
            sr.sortingOrder = b.sortingOrder;
        }
    }

    void ClearCurrentWorld()
    {
        var grid = gridManager;
        var all = grid.GetAllUniquePlacedObjects();

        foreach (var po in all)
        {
            if (po == null) continue;
            if (po is Obelisk) continue; 
            po.OnRemoved();
            Destroy(po.gameObject);
        }

        // Сбросить occupancy/placedObjects.
        // Лучше добавить метод в GridManager:
        grid.ClearAllPlacedObjectsAndOccupancy();
    }

}