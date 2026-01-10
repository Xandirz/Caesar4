using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }
    public GridManager gridManager;
    public static bool IsLoading { get; private set; }
    public event Action OnSavesChanged;
    [Header("Save Preview")]
    [SerializeField] float previewWorldSize = 12f;   // размер области в мире
    [SerializeField] int previewPx = 256;             // размер картинки (256x256)
    [SerializeField] LayerMask previewMask = ~0;      // какие слои рендерить
    [Header("Autosave")]
    [SerializeField] bool autosaveEnabled = true;
    [SerializeField] float autosaveIntervalSeconds = 300f; // 5 минут

    Coroutine autosaveRoutine;
    void Start()
    {
        if (autosaveEnabled)
            autosaveRoutine = StartCoroutine(AutosaveLoop());
    }

    void OnDestroy()
    {
        if (autosaveRoutine != null)
            StopCoroutine(autosaveRoutine);
    }

    // Папка для сейвов
    string SavesDir => Path.Combine(Application.persistentDataPath, "saves");

    // Путь к файлу по id (id = имя без расширения)
    string SavePath(string id) => Path.Combine(SavesDir, $"{id}.json");

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    string PreviewsDir => Path.Combine(SavesDir, "previews");

    void EnsurePreviewsDir()
    {
        if (!Directory.Exists(PreviewsDir))
            Directory.CreateDirectory(PreviewsDir);
    }

    void EnsureSavesDir()
    {
        if (!Directory.Exists(SavesDir))
            Directory.CreateDirectory(SavesDir);
    }

    void Update()
    {
        // F5 = добавить НОВОЕ сохранение
        if (Input.GetKeyUp(KeyCode.F5))
        {
            SaveNew();
        }

        // F9 = загрузить САМОЕ НОВОЕ сохранение (если есть)
        if (Input.GetKeyUp(KeyCode.F9))
        {
            LoadLatest();
        }
    }
    public string SaveNew()
    {
        string id = GenerateNewSaveId();
        Save(id);
        OnSavesChanged?.Invoke();
        return id;
    }
    System.Collections.IEnumerator AutosaveLoop()
    {
        float t = 0f;

        while (true)
        {
            // "минуты игры": Time.deltaTime обнуляется при timeScale=0
            t += Time.deltaTime;

            if (t >= autosaveIntervalSeconds)
            {
                t = 0f;

                // не автосейвим во время загрузки
                if (!IsLoading)
                    Autosave();
            }

            yield return null;
        }
    }
    void Autosave()
    {
        // новый файл каждый раз (чтобы не перезаписывать)
        string id = GenerateNewSaveId("autosave");
        Save(id);

        Debug.Log($"[Autosave] Saved: {id}");
    }

    Texture2D CaptureAreaAround(Vector3 centerWorld, float worldSize, int px, LayerMask cullingMask)
    {
        // временная камера
        var go = new GameObject("~SavePreviewCamera");
        var cam = go.AddComponent<Camera>();

        cam.orthographic = true;
        cam.orthographicSize = worldSize * 0.5f;   // worldSize = высота области в мире
        cam.aspect = 1f;                           // квадрат
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); // прозрачный фон
        cam.cullingMask = cullingMask;

        // 2D камера смотрит “в экран”
        go.transform.position = new Vector3(centerWorld.x, centerWorld.y, -10f);
        go.transform.rotation = Quaternion.identity;

        var rt = new RenderTexture(px, px, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, px, px), 0, 0);
        tex.Apply();

        // cleanup
        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(go);

        return tex;
    }
    string SavePreviewPng(string saveId)
    {
        EnsurePreviewsDir();
        return Path.Combine(PreviewsDir, $"{saveId}.png");
    }

    Obelisk FindObelisk()
    {
        // Самый простой способ: найти в сцене
        // (Если обелиск всегда один — ок)
        return FindAnyObjectByType<Obelisk>();
    }

    string CaptureAndWritePreview(string saveId)
    {
        var ob = FindObelisk();
        if (ob == null)
            return null;

        var tex = CaptureAreaAround(ob.transform.position, previewWorldSize, previewPx, previewMask);
        byte[] png = tex.EncodeToPNG();
        Destroy(tex);

        string absPath = SavePreviewPng(saveId);
        File.WriteAllBytes(absPath, png);

        // вернём относительный путь для сохранения в GameSaveData
        // например: "previews/save_....png"
        return Path.Combine("previews", $"{saveId}.png").Replace("\\", "/");
    }

    // ---------------------------
    // META helpers
    // ---------------------------

    int CapturePeople()
    {
        // People = население
        if (ResourceManager.Instance != null)
            return ResourceManager.Instance.GetResource("People");
        return 0;
    }

    /// <summary>
    /// Уникальный id для сейва (без .json)
    /// Пример: save_2026-01-07_21-43-12-153
    /// </summary>
    string GenerateNewSaveId(string prefix = "save")
    {
        EnsureSavesDir();
        return prefix + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
    }

    // ---------------------------
    // Public: List / Load / Delete
    // ---------------------------

    /// <summary>
    /// Список всех сейвов (новые сверху).
    /// Используется UI.
    /// </summary>
    public List<SaveEntryInfo> ListSavesNewestFirst()
    {
        EnsureSavesDir();

        var files = Directory.GetFiles(SavesDir, "*.json", SearchOption.TopDirectoryOnly);
        var list = new List<SaveEntryInfo>(files.Length);

        foreach (var path in files)
        {
            var info = new SaveEntryInfo
            {
                fullPath = path,
                fileName = Path.GetFileName(path),
                id = Path.GetFileNameWithoutExtension(path),
                corrupted = false,
                people = 0,
                savedAtUnix = 0
            };

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<GameSaveData>(json);

                info.people = data.people;
                info.savedAtUnix = data.savedAtUnix;
                info.previewFile = data.previewFile; // ← ВОТ СЮДА

            }
            catch
            {
                info.corrupted = true;
            }

            list.Add(info);
        }

        // сортировка: новые сверху
        list.Sort((a, b) =>
        {
            // если время есть в обоих — сортируем по нему
            if (a.savedAtUnix != 0 && b.savedAtUnix != 0)
                return b.savedAtUnix.CompareTo(a.savedAtUnix);

            // fallback: сортировка по времени файла
            var ta = File.GetLastWriteTimeUtc(a.fullPath);
            var tb = File.GetLastWriteTimeUtc(b.fullPath);
            return tb.CompareTo(ta);
        });

        return list;
    }

    /// <summary>
    /// Загрузить самое новое сохранение (если есть).
    /// </summary>
    public void LoadLatest()
    {
        var saves = ListSavesNewestFirst();
        if (saves.Count == 0)
        {
            Debug.LogWarning("[SaveLoad] No saves found.");
            return;
        }

        // пропускаем битые сверху, если вдруг
        foreach (var s in saves)
        {
            if (!s.corrupted)
            {
                Load(s.id);
                return;
            }
        }

        Debug.LogWarning("[SaveLoad] All saves are corrupted.");
    }

    /// <summary>
    /// Удалить сохранение по id (без расширения).
    /// </summary>
    public void DeleteSave(string id)
    {
        EnsureSavesDir();

        string path = SavePath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveLoad] Deleted: {path}");
        }
    }

    // ---------------------------
    // Save / Load (твоя логика мира)
    // ---------------------------

    /// <summary>
    /// Сохранить в файл saves/{id}.json
    /// id должен быть уникальным, чтобы не перезаписывать.
    /// </summary>
    public void Save(string id)
    {
        EnsureSavesDir();

        Debug.Log("[SaveLoad] save");

        GameSaveData data = new GameSaveData
        {
            // meta
            savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            people = CapturePeople()
        };

        // карта
        data.mapW = gridManager.width;
        data.mapH = gridManager.height;
        data.baseTiles = gridManager.ExportBaseTiles();
        data.previewFile = CaptureAndWritePreview(id);

        // 1) Buildings
        SaveBuildings(data);

        // 2) Resources
        SaveResources(data);

        // 3) Research + unlocks
        SaveResearchAndUnlocks(data);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath(id), json);

        Debug.Log($"[SaveLoad] Saved to: {SavePath(id)}");
    }

    /// <summary>
    /// Загрузить из файла saves/{id}.json
    /// </summary>
    public void Load(string id)
    {
        EnsureSavesDir();

        string path = SavePath(id);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveLoad] Save not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        ApplyLoadedData(data);
        Debug.Log($"[SaveLoad] Loaded from: {path}");
    }

    // ---------------------------
    // Renderer sorting export/import (как у тебя)
    // ---------------------------

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

    // ---------------------------
    // Save parts (твои методы)
    // ---------------------------

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

                hasRoadAccess = po.hasRoadAccess,

                sortingLayerId = 0,
                sortingOrder = 0,

                needsAreMet = false,

                renderSortings = ExportRendererSortings(po.transform)
            };

            var sr = po.GetComponent<SpriteRenderer>();
            if (sr == null) sr = po.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                b.sortingLayerId = sr.sortingLayerID;
                b.sortingOrder = sr.sortingOrder;
            }

            if (po is ProductionBuilding pb)
            {
                b.stage = pb.CurrentStage;
                b.paused = pb.IsPaused;
            }

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

        var max = rm.GetMaxResourcesCopy();
        foreach (var kv in max)
            data.resources.max.Add(new ResourceIntKV { key = kv.Key, value = kv.Value });
    }

    void RestoreResources(ResourceSaveData rs)
    {
        var amounts = new Dictionary<string, int>();
        foreach (var kv in rs.amounts)
            amounts[kv.key] = kv.value;

        var max = new Dictionary<string, int>();
        foreach (var kv in rs.max)
            max[kv.key] = kv.value;

        var buffers = new Dictionary<string, float>();
        foreach (var kv in rs.buffers)
            buffers[kv.key] = kv.value;

        ResourceManager.Instance.SetAllResources(amounts, max, buffers);
    }

    void SaveResearchAndUnlocks(GameSaveData data)
    {
        var research = ResearchManager.Instance;
        if (research != null)
        {
            data.research.completed = research.ExportCompletedResearch() ?? new List<string>();
        }
        else
        {
            data.research.completed = new List<string>();
        }

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

    // ---------------------------
    // Apply loaded state (твоя логика)
    // ---------------------------

    void ApplyLoadedData(GameSaveData data)
    {
        IsLoading = true;

        // 0) Очистка текущего мира
        ClearCurrentWorld();

        // 0.5) Базовая карта — ДО зданий
        gridManager.ImportBaseTiles(data.mapW, data.mapH, data.baseTiles);
        gridManager.RebuildBaseTileVisualsFromBaseTypes();

        // 1) Исследования
        ResearchManager.Instance.ImportState(data.research);

        // 2) Unlocked buildings
        BuildManager.Instance.ImportUnlockedBuildings(data.unlockedBuildings);

        // 3) Постройки: сначала дороги
        foreach (var b in data.buildings)
            if (b.mode == BuildManager.BuildMode.Road.ToString())
                SpawnAndApplyBuilding(b);

        foreach (var b in data.buildings)
            if (b.mode != BuildManager.BuildMode.Road.ToString())
                SpawnAndApplyBuilding(b);

        // 4) Ресурсы — В КОНЦЕ
        RestoreResources(data.resources);

        // 5) Финальные пересчёты/обновления UI
        ResourceManager.Instance.ApplyStorageLimits();
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

            // needsAreMet можно восстановить, если у тебя есть нужный метод/поле
            // h.needsAreMet = b.needsAreMet;  // если поле публичное
            // h.RefreshMoodVisual();
        }

        po.hasRoadAccess = b.hasRoadAccess;

        ApplySavedSorting(po, b);

        ApplyRendererSortings(po.transform, b.renderSortings);
    }

    void ApplyRendererSortings(Transform root, List<RendererSortingSaveData> saved)
    {
        if (saved == null || saved.Count == 0) return;

        foreach (var s in saved)
        {
            var t = FindByPath(root, s.path);
            if (t == null) continue;

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
        grid.ClearAllPlacedObjectsAndOccupancy();
    }
}
