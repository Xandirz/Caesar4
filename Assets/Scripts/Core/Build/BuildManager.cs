using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public RoadManager roadManager;

    [Header("Auto load prefabs from Resources")]
    [SerializeField] private string resourcesBuildingsFolder = "Prefabs/Buildings"; // Assets/Resources/...

    private Dictionary<BuildMode, GameObject> prefabByMode;

    public enum BuildMode
    {
        None, Road, House, LumberMill, Demolish, Well, Warehouse, Berry, Rock, Clay, Pottery, Hunter,
        Tools, Clothes, Crafts, Furniture, Wheat, Flour, Sheep, Weaver, Dairy, Bakery, Beans, Brewery,
        Charcoal, CopperOre, Market, Fish, Flax, Bee, Candle, Pig, Goat, Soap, Brick, Olive, OliveOil, Chicken, Cattle,
        Temple, Leather, TinOre, Copper, Bronze, Smithy, Herbs, Doctor, Vegetables, Grape, Wine, GoldOre, Gold, Bathhouse,
        Salt, Fruit, Jewelry, Sand, Ash, Glass,
    }

    private BuildMode currentMode = BuildMode.None;
    public BuildMode CurrentMode => currentMode;
    public void SetBuildMode(BuildMode mode) => currentMode = mode;

    public static BuildManager Instance { get; private set; }

    private Vector2Int? lastPlacedCell;

    // === Зональный снос ===
    private bool isSelecting;
    public Vector2Int dragStartCell;
    private Vector2Int dragEndCell;

    [Header("Line Build Mode UI")]
    [SerializeField] private Button lineModeButton;
    [SerializeField] private TMP_Text lineModeButtonText;

    [SerializeField] private bool lineBuildMode;
    public bool IsLineBuildMode => lineBuildMode;

    // --- Line build runtime ---
    private Vector2Int? lineAnchorCell;
    private bool lineLockActive;
    private bool lockAxisX;

    // --- Line build counter (reset each drag) ---
    private int linePlaceCount;
    private bool lineSessionActive;

    private HashSet<BuildMode> unlockedBuildings = new();

    private float lastPopupTime = -999f;
    [SerializeField] private float popupCooldown = 0f;

    [SerializeField] private float popupOffsetPixelsX = 0f;
    [SerializeField] private float popupOffsetPixelsY = 0f;
    [SerializeField] private float popupScreenMarginPixels = 24f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (lineModeButton != null)
            lineModeButton.onClick.AddListener(ToggleLineBuildMode);

        SyncLineModeButtonText();
    }

    void Start()
    {
        BuildPrefabCache();

        UnlockBuilding(BuildMode.Road, showHighlight: false);
        UnlockBuilding(BuildMode.House, showHighlight: false);
               UnlockBuilding(BuildMode.Well, showHighlight: false);
               UnlockBuilding(BuildMode.Berry, showHighlight: false);
               UnlockBuilding(BuildMode.LumberMill, showHighlight: false);
               UnlockBuilding(BuildMode.Rock, showHighlight: false); 
               UnlockBuilding(BuildMode.Fish, showHighlight: false);
    }

    // =========================
    // ======= UPDATE ==========
    // =========================
    void Update()
    {
        if (currentMode == BuildMode.Demolish)
        {
            UpdateDemolish();
            return;
        }

        if (currentMode == BuildMode.None)
            return;

        if (Input.GetMouseButtonDown(0))
            TryStartBuild();

        if (Input.GetMouseButton(0))
            TryContinueBuild();

        if (Input.GetMouseButtonUp(0))
            ResetLineSession();

        if (Input.GetMouseButtonDown(1))
        {
            currentMode = BuildMode.None;
            MouseHighlighter.Instance.ClearHighlights();
            ResetLineSession();
        }
    }

    private void UpdateDemolish()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragStartCell = GetMouseCell();
            dragEndCell = dragStartCell;
            isSelecting = true;
            MouseHighlighter.Instance.ClearHighlights();
        }

        if (isSelecting && Input.GetMouseButton(0))
        {
            dragEndCell = GetMouseCell();
            MouseHighlighter.Instance.HighlightRectangle(
                dragStartCell, dragEndCell,
                MouseHighlighter.Instance.demolishColor
            );
        }

        if (isSelecting && Input.GetMouseButtonUp(0))
        {
            isSelecting = false;
            MouseHighlighter.Instance.ClearHighlights();

            Vector2Int min = new(Mathf.Min(dragStartCell.x, dragEndCell.x), Mathf.Min(dragStartCell.y, dragEndCell.y));
            Vector2Int max = new(Mathf.Max(dragStartCell.x, dragEndCell.x), Mathf.Max(dragStartCell.y, dragEndCell.y));

            for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
                DemolishAtCell(new Vector2Int(x, y));
        }

        if (Input.GetMouseButtonDown(1))
        {
            isSelecting = false;
            MouseHighlighter.Instance.ClearHighlights();
            currentMode = BuildMode.None;
        }
    }

    private void TryStartBuild()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int raw = GetMouseCell();

        if (lineBuildMode)
        {
            lineAnchorCell = raw;
            lineLockActive = false;

            lineSessionActive = true;
            linePlaceCount = 0;
        }

        PlaceObjectAtCell(raw);
        lastPlacedCell = raw;
    }

    private void TryContinueBuild()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int raw = GetMouseCell();
        Vector2Int cell = GetLineCell(raw);

        if (lastPlacedCell == null)
        {
            PlaceObjectAtCell(cell);
            lastPlacedCell = cell;
            return;
        }

        if (cell != lastPlacedCell.Value)
        {
            PlaceLineSegment(lastPlacedCell.Value, cell);
            lastPlacedCell = cell;
        }
    }

    private void ResetLineSession()
    {
        lastPlacedCell = null;
        lineAnchorCell = null;
        lineLockActive = false;

        lineSessionActive = false;
        linePlaceCount = 0;
    }

    // =========================
    // ===== PREFABS ===========
    // =========================
    private void BuildPrefabCache()
    {
        prefabByMode = new Dictionary<BuildMode, GameObject>();

        var prefabs = Resources.LoadAll<GameObject>(resourcesBuildingsFolder);
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError($"BuildManager: не найдено префабов в Resources/{resourcesBuildingsFolder}");
            return;
        }

        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;

            var po = prefab.GetComponent<PlacedObject>();
            if (po == null)
            {
                Debug.LogWarning($"BuildManager: prefab '{prefab.name}' без PlacedObject — пропускаю");
                continue;
            }

            prefabByMode[po.BuildMode] = prefab; // дубликаты перезапишутся
        }

        Debug.Log($"BuildManager: загружено префабов = {prefabByMode.Count} (из {prefabs.Length})");
    }

    GameObject GetPrefabByBuildMode(BuildMode mode)
    {
        if (prefabByMode == null || prefabByMode.Count == 0)
            BuildPrefabCache();

        return prefabByMode.TryGetValue(mode, out var prefab) ? prefab : null;
    }

    public GameObject GetPrefabByMode(BuildMode mode) => GetPrefabByBuildMode(mode);

    // =========================
    // ===== UNLOCKED ==========
    // =========================
    public List<string> ExportUnlockedBuildings()
    {
        var list = new List<string>();
        foreach (var m in unlockedBuildings)
            list.Add(m.ToString());
        return list;
    }

    public void ImportUnlockedBuildings(List<string> modes)
    {
        unlockedBuildings.Clear();
        if (modes == null) return;

        foreach (var s in modes)
            if (Enum.TryParse<BuildMode>(s, out var mode))
                UnlockBuilding(mode, showHighlight: false);
        
        
        
    }

    public bool IsBuildingUnlocked(BuildMode mode) => unlockedBuildings.Contains(mode);

    public void UnlockBuilding(BuildMode mode, bool showHighlight = true)
    {
        if (!unlockedBuildings.Add(mode))
            return;

        if (BuildUIManager.Instance != null)
            BuildUIManager.Instance.EnableBuildingButton(mode, showHighlight);

        Debug.Log($"Разблокировано здание: {mode}");
    }

    // =========================
    // ===== LINE MODE =========
    // =========================
    public void ToggleLineBuildMode()
    {
        lineBuildMode = !lineBuildMode;
        SyncLineModeButtonText();
    }

    private void SyncLineModeButtonText()
    {
        if (lineModeButtonText != null)
            lineModeButtonText.text = lineBuildMode ? "Line" : "Brush";
    }

    private Vector2Int GetLineCell(Vector2Int rawCell)
    {
        if (!lineBuildMode)
            return rawCell;

        if (lineAnchorCell == null)
            lineAnchorCell = rawCell;

        Vector2Int a = lineAnchorCell.Value;

        if (!lineLockActive)
        {
            int dx = Mathf.Abs(rawCell.x - a.x);
            int dy = Mathf.Abs(rawCell.y - a.y);

            if (dx != 0 || dy != 0)
            {
                lockAxisX = dy > dx;   // dy больше -> фиксируем X (строим по Y)
                lineLockActive = true;
            }
        }

        if (!lineLockActive)
            return rawCell;

        return lockAxisX ? new Vector2Int(a.x, rawCell.y) : new Vector2Int(rawCell.x, a.y);
    }

    private void PlaceLineSegment(Vector2Int from, Vector2Int to)
    {
        if (from == to) return;

        if (from.x == to.x)
        {
            int stepY = (to.y > from.y) ? 1 : -1;
            for (int y = from.y + stepY; y != to.y + stepY; y += stepY)
                PlaceObjectAtCell(new Vector2Int(from.x, y));
            return;
        }

        if (from.y == to.y)
        {
            int stepX = (to.x > from.x) ? 1 : -1;
            for (int x = from.x + stepX; x != to.x + stepX; x += stepX)
                PlaceObjectAtCell(new Vector2Int(x, from.y));
            return;
        }

        PlaceObjectAtCell(to);
    }

    // =========================
    // ===== INPUT/GRID ========
    // =========================
    private Vector2Int GetMouseCell()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;
        mw = gridManager.SnapToPixels(mw);
        return gridManager.IsoWorldToCell(mw);
    }

    void PlaceObject()
    {
        Vector2Int origin = GetMouseCell();
        PlaceObjectAtCell(origin);
    }

    // =========================
    // ===== BUILD PLACE =======
    // =========================
    private bool PlaceObjectAtCell(Vector2Int origin)
    {
        GameObject prefab = GetPrefabByBuildMode(currentMode);
        if (prefab == null) return false;

        PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
        if (poPrefab == null) return false;

        int sx = poPrefab.SizeX;
        int sy = poPrefab.SizeY;

        bool Fail(string msg, MessagePopUp.Style style)
        {
            ShowBuildFailPopupAtCell(origin, msg, style);
            return false;
        }

        // 1) free check
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
            if (!gridManager.IsCellFree(origin + new Vector2Int(x, y)))
                return Fail("Can't build here", MessagePopUp.Style.Error);

        // 2) adjacency
        if (poPrefab.needWaterNearby && !HasAdjacentWater(origin, sx, sy))
            return Fail("Need to place near water", MessagePopUp.Style.Warning);

        if (poPrefab.NeedHouseNearby && !HasAdjacentHouse(origin, sx, sy))
            return Fail("Need to place near houses", MessagePopUp.Style.Warning);

        if (poPrefab.needMountainsNearby && !HasAdjacentMountain(origin, sx, sy))
            return Fail("Need to place near mountains", MessagePopUp.Style.Warning);

        // 3) resources
        var cost = poPrefab.GetCostDict();
        if (!ResourceManager.Instance.CanSpend(cost))
            return Fail("Not enough resources", MessagePopUp.Style.Error);

        // 4) clear base tiles
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
            gridManager.ReplaceBaseTile(origin + new Vector2Int(x, y), null);

        // 5) spawn
        Vector3 pos = gridManager.CellToIsoWorld(origin);
        pos.x = Mathf.Round(pos.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
        pos.y = Mathf.Round(pos.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        PlacedObject po = go.GetComponent<PlacedObject>();
        if (po == null) { Destroy(go); return false; }

        po.gridPos = origin;
        po.manager = gridManager;
        go.name = prefab.name;
        po.OnPlaced();

        if (go.TryGetComponent<SpriteRenderer>(out var sr))
            gridManager.ApplySorting(po.gridPos, po.SizeX, po.SizeY, sr, false, po is Road);

        // 6) spend
        ResourceManager.Instance.SpendResources(cost);

        // 7) occupy
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
            gridManager.SetOccupied(origin + new Vector2Int(x, y), true, po);

        // 8) road/effects
        if (po is Road road)
        {
            roadManager.RegisterRoad(origin, road);
            roadManager.RefreshRoadAndNeighbors(origin);
            RecheckRoadAccessForAllBuildings();
        }

        AudioManager.Instance?.PlayBuild();
        CheckEffects(po);

        // line counter popup (same behavior)
        if (lineBuildMode && lineSessionActive)
        {
            linePlaceCount++;
            ShowBuildFailPopupAtCell(origin, linePlaceCount.ToString(), MessagePopUp.Style.Warning);
        }

        return true;
    }

    // =========================
    // ===== DEMOLISH ==========
    // =========================
    private void DemolishAtCell(Vector2Int cell)
    {
        if (gridManager.IsCellFree(cell))
            return;

        if (!gridManager.TryGetPlacedObject(cell, out var po) || po == null)
            return;

        if (po is Obelisk)
            return;

        Vector2Int origin = po.gridPos;
        int sizeX = po.SizeX;
        int sizeY = po.SizeY;

        po.OnRemoved();

        for (int dx = 0; dx < sizeX; dx++)
        for (int dy = 0; dy < sizeY; dy++)
        {
            Vector2Int p = origin + new Vector2Int(dx, dy);
            gridManager.SetOccupied(p, false);
            gridManager.ReplaceBaseTile(p, gridManager.groundPrefab);
        }

        if (po is Road)
        {
            roadManager.UnregisterRoad(origin);
            RecheckRoadAccessForAllBuildings();
        }

        CheckEffectsAfterDemolish(po);
        Destroy(po.gameObject);

        AudioManager.Instance?.PlayDemolish();
    }

    // =========================
    // ===== ADJACENCY =========
    // =========================
    public bool HasAdjacentWater(Vector2Int origin, int sizeX, int sizeY)
    {
        for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
        {
            Vector2Int cell = origin + new Vector2Int(x, y);
            Vector2Int up = cell + Vector2Int.up;
            Vector2Int down = cell + Vector2Int.down;
            Vector2Int left = cell + Vector2Int.left;
            Vector2Int right = cell + Vector2Int.right;

            if (gridManager.IsWaterCell(up) ||
                gridManager.IsWaterCell(down) ||
                gridManager.IsWaterCell(left) ||
                gridManager.IsWaterCell(right))
                return true;
        }
        return false;
    }

    public bool HasAdjacentMountain(Vector2Int origin, int sizeX, int sizeY)
    {
        for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
        {
            Vector2Int cell = origin + new Vector2Int(x, y);
            Vector2Int up = cell + Vector2Int.up;
            Vector2Int down = cell + Vector2Int.down;
            Vector2Int left = cell + Vector2Int.left;
            Vector2Int right = cell + Vector2Int.right;

            if (gridManager.IsMountainCell(up) ||
                gridManager.IsMountainCell(down) ||
                gridManager.IsMountainCell(left) ||
                gridManager.IsMountainCell(right))
                return true;
        }
        return false;
    }

    public bool IsAdjacencyOk(PlacedObject poPrefab, Vector2Int origin)
    {
        if (poPrefab == null) return false;

        bool ok = true;
        int sx = poPrefab.SizeX;
        int sy = poPrefab.SizeY;

        if (poPrefab.needWaterNearby)
            ok &= HasAdjacentWater(origin, sx, sy);

        if (poPrefab.NeedHouseNearby)
            ok &= HasAdjacentHouse(origin, sx, sy);

        if (poPrefab.needMountainsNearby)
            ok &= HasAdjacentMountain(origin, sx, sy);

        return ok;
    }

    private bool HasAdjacentHouse(Vector2Int origin, int sizeX, int sizeY)
    {
        if (!SettingsManager.Instance.settingNeedHouse)
            return true;

        for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
        {
            Vector2Int cell = origin + new Vector2Int(x, y);
            Vector2Int up = cell + Vector2Int.up;
            Vector2Int down = cell + Vector2Int.down;
            Vector2Int left = cell + Vector2Int.left;
            Vector2Int right = cell + Vector2Int.right;

            if (IsHouseAt(up) || IsHouseAt(down) || IsHouseAt(left) || IsHouseAt(right))
                return true;
        }

        return false;
    }

    private bool IsHouseAt(Vector2Int cell)
        => gridManager.TryGetPlacedObject(cell, out var obj) && obj is House;

    // =========================
    // ===== POPUPS ============
    // =========================
    private void ShowBuildFailPopupAtCell(Vector2Int cell, string msg, MessagePopUp.Style style = MessagePopUp.Style.Error)
    {
        if (Time.time - lastPopupTime < popupCooldown) return;
        lastPopupTime = Time.time;

        Camera cam = Camera.main;
        if (cam == null || gridManager == null) return;

        Vector3 cellWorld = gridManager.CellToIsoWorld(cell);
        Vector3 screen = cam.WorldToScreenPoint(cellWorld);
        if (screen.z <= 0f) return;

        float dx = Random.Range(-popupOffsetPixelsX, popupOffsetPixelsX);
        float dy = Random.Range(popupOffsetPixelsY * 0.7f, popupOffsetPixelsY * 1.2f);

        screen.x = Mathf.Clamp(screen.x + dx, popupScreenMarginPixels, Screen.width - popupScreenMarginPixels);
        screen.y = Mathf.Clamp(screen.y + dy, popupScreenMarginPixels, Screen.height - popupScreenMarginPixels);

        Vector3 spawnWorld = cam.ScreenToWorldPoint(screen);
        spawnWorld.x = Mathf.Round(spawnWorld.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
        spawnWorld.y = Mathf.Round(spawnWorld.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;

        MessagePopUp.Create(spawnWorld, msg, style);
    }

    // =========================
    // ===== EFFECTS ===========
    // =========================
    public void CheckEffects(PlacedObject po)
    {
        if (!(po is Road))
        {
            bool hasAccess = false;

            for (int dx = 0; dx < po.SizeX && !hasAccess; dx++)
            for (int dy = 0; dy < po.SizeY && !hasAccess; dy++)
            {
                Vector2Int cell = po.gridPos + new Vector2Int(dx, dy);
                Vector2Int[] neighbors =
                {
                    cell + Vector2Int.up,
                    cell + Vector2Int.down,
                    cell + Vector2Int.left,
                    cell + Vector2Int.right
                };

                foreach (var n in neighbors)
                {
                    if (roadManager.IsRoadAt(n) && roadManager.IsConnectedToObelisk(n))
                    {
                        hasAccess = true;
                        break;
                    }
                }
            }

            bool prev = po.hasRoadAccess;
            po.hasRoadAccess = hasAccess;

            if (prev != hasAccess)
                po.OnRoadAccessChanged(hasAccess);
        }

        if (po is Road road)
        {
            bool connected = roadManager.IsConnectedToObelisk(po.gridPos);
            if (connected) TutorialEvents.RaiseRoadConnectedToObelisk();

            road.isConnectedToObelisk = connected;
            roadManager.UpdateBuildingAccessAround(road.gridPos);
        }

        if (po is Well well)
        {
            int r = well.buildEffectRadius;
            Vector2Int c = well.gridPos;
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                Vector2Int p = c + new Vector2Int(dx, dy);
                if (gridManager.TryGetPlacedObject(p, out var obj) && obj is House h)
                    h.SetWaterAccess(true);
            }
        }
        else if (po is House house)
        {
            ApplyHouseAccess(house);
        }
    }

    private void ApplyHouseAccess(House house)
    {
        int searchRadius = 10;

        bool HasInRadius<T>(Func<T, bool> predicate = null) where T : PlacedObject
        {
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                Vector2Int p = house.gridPos + new Vector2Int(dx, dy);
                if (gridManager.TryGetPlacedObject(p, out var obj) && obj is T t)
                {
                    if (predicate == null || predicate(t))
                        return true;
                }
            }
            return false;
        }

        bool WellOk(Well w) => w.hasRoadAccess && IsInEffectSquare(w.gridPos, house.gridPos, w.buildEffectRadius);
        bool MarketOk(Market m) => m.hasRoadAccess && IsInEffectSquare(m.gridPos, house.gridPos, m.buildEffectRadius);
        bool TempleOk(Temple t) => t.hasRoadAccess && IsInEffectSquare(t.gridPos, house.gridPos, t.buildEffectRadius);
        bool DoctorOk(Doctor d) => d.hasRoadAccess && IsInEffectSquare(d.gridPos, house.gridPos, d.buildEffectRadius);
        bool BathOk(Bathhouse b) => b.hasRoadAccess && IsInEffectSquare(b.gridPos, house.gridPos, b.buildEffectRadius);

        house.SetWaterAccess(HasInRadius<Well>(WellOk));
        house.SetMarketAccess(HasInRadius<Market>(MarketOk));
        house.SetTempleAccess(HasInRadius<Temple>(TempleOk));
        house.SetDoctorAccess(HasInRadius<Doctor>(DoctorOk));
        house.SetBathhouseAccess(HasInRadius<Bathhouse>(BathOk));
    }

    private bool IsInEffectSquare(Vector2Int center, Vector2Int pos, int radius)
        => Mathf.Abs(pos.x - center.x) <= radius && Mathf.Abs(pos.y - center.y) <= radius;

    private void RecheckRoadAccessForAllBuildings()
    {
        if (AllBuildingsManager.Instance == null) return;
        foreach (var b in AllBuildingsManager.Instance.GetAllBuildings())
            if (b != null) CheckEffects(b);
    }

    public void CheckEffectsForHousesInRadius(Vector2Int center, int radius)
    {
        if (gridManager == null) return;

        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            Vector2Int p = center + new Vector2Int(dx, dy);
            if (gridManager.TryGetPlacedObject(p, out var obj) && obj is House h)
                CheckEffects(h);
        }
    }

    private void CheckEffectsAfterDemolish(PlacedObject po)
    {
        if (po is Well well)
        {
            int r = well.buildEffectRadius;
            Vector2Int c = well.gridPos;

            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                Vector2Int p = c + new Vector2Int(dx, dy);
                if (gridManager.TryGetPlacedObject(p, out var obj) && obj is House h)
                {
                    bool stillHas = false;
                    int searchRadius = 10;

                    for (int sx = -searchRadius; sx <= searchRadius && !stillHas; sx++)
                    for (int sy = -searchRadius; sy <= searchRadius && !stillHas; sy++)
                    {
                        Vector2Int s = h.gridPos + new Vector2Int(sx, sy);
                        if (gridManager.TryGetPlacedObject(s, out var maybe) && maybe is Well otherWell)
                            if (IsInEffectSquare(otherWell.gridPos, h.gridPos, otherWell.buildEffectRadius))
                                stillHas = true;
                    }

                    h.SetWaterAccess(stillHas);
                }
            }
        }

        if (po is Road)
        {
            Vector2Int origin = po.gridPos;
            Vector2Int[] neighbors =
            {
                origin + Vector2Int.up,
                origin + Vector2Int.down,
                origin + Vector2Int.left,
                origin + Vector2Int.right
            };

            foreach (var n in neighbors)
                if (gridManager.TryGetPlacedObject(n, out var obj) && obj != null && !(obj is Road))
                    CheckEffects(obj);
        }
    }

    // =========================
    // ===== SAVE SPAWN =========
    // =========================
    public PlacedObject SpawnFromSave(BuildMode mode, Vector2Int origin)
    {
        Debug.Log($"[SpawnFromSave] mode={mode} origin={origin}");

        GameObject prefab = GetPrefabByBuildMode(mode);
        if (prefab == null)
        {
            Debug.LogError($"[SpawnFromSave] prefab == null for mode={mode}. Fix GetPrefabByMode mapping.");
            return null;
        }

        PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
        if (poPrefab == null)
        {
            Debug.LogError($"[SpawnFromSave] Prefab has no PlacedObject: {prefab.name}");
            return null;
        }

        int sizeX = poPrefab.SizeX;
        int sizeY = poPrefab.SizeY;

        Vector3 spawnPos = gridManager.CellToIsoWorld(origin);
        spawnPos = gridManager.SnapToPixels(spawnPos);

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        go.name = prefab.name;

        PlacedObject po = go.GetComponent<PlacedObject>();
        if (po == null)
        {
            Debug.LogError($"[SpawnFromSave] Instantiated object has no PlacedObject: {go.name}");
            Destroy(go);
            return null;
        }

        po.gridPos = origin;
        po.manager = gridManager;

        bool isRoad = (mode == BuildMode.Road);
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
            gridManager.ApplySorting(origin, sizeX, sizeY, sr, false, isRoad);

        for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
        {
            var p = origin + new Vector2Int(x, y);
            gridManager.SetOccupied(p, true, po);
            gridManager.ReplaceBaseTile(p, null);
        }

        po.OnPlaced();

        Debug.Log($"[SpawnFromSave] OK spawned {mode} at {origin} world={spawnPos}");
        return po;
    }
}
