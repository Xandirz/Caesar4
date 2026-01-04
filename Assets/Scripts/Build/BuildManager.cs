using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public RoadManager roadManager;
    [Header("Auto load prefabs from Resources")]
    [SerializeField] private string resourcesBuildingsFolder = "Prefabs/Buildings"; 
// путь внутри Assets/Resources (без "Resources/")

    private Dictionary<BuildMode, GameObject> prefabByMode;

    public enum BuildMode
    {
        None, Road, House, LumberMill, Demolish, Well, Warehouse, Berry, Rock, Clay, Pottery, Hunter,
        Tools, Clothes, Crafts, Furniture, Wheat, Flour, Sheep, Weaver, Dairy, Bakery, Beans, Brewery,
        Charcoal, CopperOre,  Market, Fish, Flax, Bee,Candle, Pig, Goat,Soap, Brick, Olive,OliveOil,Chicken,Cattle,
        Temple,Leather,TinOre,Copper,Bronze, Smithy, Herbs,Doctor,Vegetables,Grape,Wine,GoldOre,Gold,Bathhouse,
        
        Salt,Fruit,Jewelry, Sand, Ash,Glass, 
    }

    private BuildMode currentMode = BuildMode.None;
    public BuildMode CurrentMode => currentMode;
    public void SetBuildMode(BuildMode mode) => currentMode = mode;
    public static BuildManager Instance { get; private set; }

    private Vector2Int? lastPlacedCell = null;

    // === Зональный снос ===
    private bool isSelecting = false;
    public Vector2Int dragStartCell;
    private Vector2Int dragEndCell;
    
    [Header("Line Build Mode UI")]
    [SerializeField] private Button lineModeButton;         // сюда сама кнопка
    [SerializeField] private TMP_Text lineModeButtonText;   // сюда TMP текст внутри кнопки

    [SerializeField] private bool lineBuildMode = false;
    public bool IsLineBuildMode => lineBuildMode;
// --- Line build runtime ---
    private Vector2Int? lineAnchorCell = null;   // клетка, откуда началась "линия"
    private bool lineLockActive = false;
    private bool lockAxisX = false;              // true => фиксируем X (строим по Y), false => фиксируем Y (строим по X)


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (lineModeButton != null)
            lineModeButton.onClick.AddListener(ToggleLineBuildMode);

        SyncLineModeButtonText();

    }
    
    void Start()
    {
        BuildPrefabCache();
        
        
        UnlockBuilding(BuildMode.Road);
        UnlockBuilding(BuildMode.House);
        UnlockBuilding(BuildMode.Well);
        UnlockBuilding(BuildMode.Berry);
        UnlockBuilding(BuildMode.LumberMill);
        UnlockBuilding(BuildMode.Rock);
        UnlockBuilding(BuildMode.Fish);
    }
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

            var mode = po.BuildMode;

            // если дубликаты — последний перезапишет
            prefabByMode[mode] = prefab;
        }

        Debug.Log($"BuildManager: загружено префабов = {prefabByMode.Count} (из {prefabs.Length})");
    }


void Update()
{
    // === 🔥 РЕЖИМ СНОСА ===
    if (currentMode == BuildMode.Demolish)
    {
        // начало выделения
        if (Input.GetMouseButtonDown(0))
        {
            dragStartCell = GetMouseCell();
            dragEndCell = dragStartCell;
            isSelecting = true;
            MouseHighlighter.Instance.ClearHighlights();
        }

        // во время выделения — подсвечиваем прямоугольник
        if (isSelecting && Input.GetMouseButton(0))
        {
            dragEndCell = GetMouseCell();
            MouseHighlighter.Instance.HighlightRectangle(
                dragStartCell, dragEndCell,
                MouseHighlighter.Instance.demolishColor
            );
        }

        // отпускание — выполняем снос
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

        // ПКМ — отмена выделения
        if (Input.GetMouseButtonDown(1))
        {
            isSelecting = false;
            MouseHighlighter.Instance.ClearHighlights();
            currentMode = BuildMode.None;
        }

        return;
    }

    // === 🏗️ СТРОИТЕЛЬСТВО ===
    if (Input.GetMouseButtonDown(0) && currentMode != BuildMode.None)
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int raw = GetMouseCell();

        // старт линии
        if (lineBuildMode)
        {
            lineAnchorCell = raw;
            lineLockActive = false;
        }

        // первый объект (в якоре)
        PlaceObjectAtCell(raw);
        lastPlacedCell = raw;
    }

    if (Input.GetMouseButton(0) && currentMode != BuildMode.None)
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int raw = GetMouseCell();
        Vector2Int cell = GetLineCell(raw); // <-- фиксация оси

        if (lastPlacedCell == null || cell != lastPlacedCell.Value)
        {
            PlaceObjectAtCell(cell);
            lastPlacedCell = cell;
        }
    }

    if (Input.GetMouseButtonUp(0))
    {
        lastPlacedCell = null;

        // сброс линии
        lineAnchorCell = null;
        lineLockActive = false;
    }

    // ПКМ — сброс режима
    if (Input.GetMouseButtonDown(1))
    {
        currentMode = BuildMode.None;
        MouseHighlighter.Instance.ClearHighlights();

        // на всякий — сброс линии
        lineAnchorCell = null;
        lineLockActive = false;
    }
}



  private HashSet<BuildMode> unlockedBuildings = new();

  public bool IsBuildingUnlocked(BuildMode mode)
  {
      return unlockedBuildings.Contains(mode);
  }

  public void UnlockBuilding(BuildMode mode)
  {
      if (unlockedBuildings.Contains(mode))
          return;
      
      unlockedBuildings.Add(mode);
      
      if (BuildUIManager.Instance != null)
      {
          BuildUIManager.Instance.EnableBuildingButton(mode);
      }
      

      Debug.Log($"Разблокировано здание: {mode}");

  }


    private Vector2Int GetMouseCell()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;
        mw = gridManager.SnapToPixels(mw);
        return gridManager.IsoWorldToCell(mw);

    }
    
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
        if (po is not Obelisk)
            Destroy(po.gameObject);
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDemolish();
        }

    }


    void PlaceObject()
    {
        Vector2Int origin = GetMouseCell();
        PlaceObjectAtCell(origin);
    }

 


    public bool HasAdjacentWater(Vector2Int origin, int sizeX, int sizeY)
    {
        // обходим все клетки, которые займёт здание
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector2Int cell = origin + new Vector2Int(x, y);

                // 4-соседей этой клетки
                Vector2Int up    = cell + Vector2Int.up;
                Vector2Int down  = cell + Vector2Int.down;
                Vector2Int left  = cell + Vector2Int.left;
                Vector2Int right = cell + Vector2Int.right;

                // проверяем через GridManager.IsWaterCell
                if (gridManager.IsWaterCell(up)    ||
                    gridManager.IsWaterCell(down)  ||
                    gridManager.IsWaterCell(left)  ||
                    gridManager.IsWaterCell(right))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    // BuildManager.cs
    public bool HasAdjacentMountain(Vector2Int origin, int sizeX, int sizeY)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector2Int cell = origin + new Vector2Int(x, y);

                Vector2Int up    = cell + Vector2Int.up;
                Vector2Int down  = cell + Vector2Int.down;
                Vector2Int left  = cell + Vector2Int.left;
                Vector2Int right = cell + Vector2Int.right;

                if (gridManager.IsMountainCell(up) ||
                    gridManager.IsMountainCell(down) ||
                    gridManager.IsMountainCell(left) ||
                    gridManager.IsMountainCell(right))
                    return true;
            }
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

        // NEW
        if (poPrefab.needMountainsNearby)
            ok &= HasAdjacentMountain(origin, sx, sy);

        return ok;
    }

    private bool HasAdjacentHouse(Vector2Int origin, int sizeX, int sizeY)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector2Int cell = origin + new Vector2Int(x, y);
                Vector2Int up    = cell + Vector2Int.up;
                Vector2Int down  = cell + Vector2Int.down;
                Vector2Int left  = cell + Vector2Int.left;
                Vector2Int right = cell + Vector2Int.right;

                if (IsHouseAt(up) || IsHouseAt(down) || IsHouseAt(left) || IsHouseAt(right))
                    return true;
            }
        }
        return false;
    }

    private bool IsHouseAt(Vector2Int cell)
    {
        return gridManager.TryGetPlacedObject(cell, out var obj) && obj is House;
    }


    public void CheckEffects(PlacedObject po)
    {
        if (!(po is Road))
        {
            bool hasAccess = false;

            for (int dx = 0; dx < po.SizeX && !hasAccess; dx++)
            {
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
            }

            bool prev = po.hasRoadAccess;
            po.hasRoadAccess = hasAccess;

            if (prev != hasAccess)
            {
                po.OnRoadAccessChanged(hasAccess);
            }

        }

        if (po is Road road)
        {
            bool connected = roadManager.IsConnectedToObelisk(po.gridPos);
            road.isConnectedToObelisk = connected;
            roadManager.UpdateBuildingAccessAround(road.gridPos);
        }

        if (po is Well well)
        {
            int r = well.buildEffectRadius;
            Vector2Int c = well.gridPos;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    Vector2Int p = c + new Vector2Int(dx, dy);
                    if (gridManager.TryGetPlacedObject(p, out var obj) && obj is House h)
                        h.SetWaterAccess(true);
                }
            }
        }
        else if (po is House house)
        {
            bool hasWater = false;
            int searchRadius = 10;

            for (int dx = -searchRadius; dx <= searchRadius && !hasWater; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius && !hasWater; dy++)
                {
                    Vector2Int p = house.gridPos + new Vector2Int(dx, dy);
                    if (gridManager.TryGetPlacedObject(p, out var obj) && obj is Well w)
                    {
                        if (w.hasRoadAccess && IsInEffectSquare(w.gridPos, house.gridPos, w.buildEffectRadius))
                        {
                            house.SetWaterAccess(true);
                            hasWater = true;
                        }
                    }

                }
            }

            if (!hasWater)
                house.SetWaterAccess(false);

            bool hasMarket = false;
            for (int dx = -searchRadius; dx <= searchRadius && !hasMarket; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius && !hasMarket; dy++)
                {
                    Vector2Int p = house.gridPos + new Vector2Int(dx, dy);
                    if (gridManager.TryGetPlacedObject(p, out var obj) && obj is Market m)
                    {
                        if (m.hasRoadAccess && IsInEffectSquare(m.gridPos, house.gridPos, m.buildEffectRadius))
                        {
                            house.SetMarketAccess(true);
                            hasMarket = true;
                        }
                    }

                }
            }

            if (!hasMarket)
                house.SetMarketAccess(false);
            
            
            bool hasTemple = false;
            for (int dx = -searchRadius; dx <= searchRadius && !hasTemple; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius && !hasTemple; dy++)
                {
                    Vector2Int p = house.gridPos + new Vector2Int(dx, dy);
                    if (gridManager.TryGetPlacedObject(p, out var obj) && obj is Temple t)
                    {
                        if (t.hasRoadAccess && IsInEffectSquare(t.gridPos, house.gridPos, t.buildEffectRadius))
                        {
                            house.SetTempleAccess(true);
                            hasTemple = true;
                        }
                    }

                }
            }

            if (!hasTemple)
                house.SetTempleAccess(false);
            
            bool hasDoctor = false;
            for (int dx = -searchRadius; dx <= searchRadius && !hasDoctor; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius && !hasDoctor; dy++)
                {
                    Vector2Int p = house.gridPos + new Vector2Int(dx, dy);
                    if (gridManager.TryGetPlacedObject(p, out var obj) && obj is Doctor d)
                    {
                        if (d.hasRoadAccess && IsInEffectSquare(d.gridPos, house.gridPos, d.buildEffectRadius))
                        {
                            house.SetDoctorAccess(true);
                            hasDoctor = true;
                        }
                    }
                }
            }

            if (!hasDoctor)
                house.SetDoctorAccess(false);

            bool hasBathhouse = false;
            for (int dx = -searchRadius; dx <= searchRadius && !hasBathhouse; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius && !hasBathhouse; dy++)
                {
                    Vector2Int p = house.gridPos + new Vector2Int(dx, dy);
                    if (gridManager.TryGetPlacedObject(p, out var obj) && obj is Bathhouse b)
                    {
                        if (b.hasRoadAccess && IsInEffectSquare(b.gridPos, house.gridPos, b.buildEffectRadius))
                        {
                            house.SetBathhouseAccess(true);
                            hasBathhouse = true;
                        }
                    }
                }
            }

            if (!hasBathhouse)
                house.SetBathhouseAccess(false);


        }
    }

    private bool IsInEffectSquare(Vector2Int center, Vector2Int pos, int radius)
    {
        return Mathf.Abs(pos.x - center.x) <= radius &&
               Mathf.Abs(pos.y - center.y) <= radius;
    }
    
    // Внутри BuildManager (любой раздел класса)
    private void RecheckRoadAccessForAllBuildings()
    {
        if (AllBuildingsManager.Instance == null) return;
        foreach (var b in AllBuildingsManager.Instance.GetAllBuildings())
        {
            if (b == null) continue;
            CheckEffects(b); // заново проверяем соседние дороги и connected-to-obelisk
        }
    }

    public void CheckEffectsForHousesInRadius(Vector2Int center, int radius)
    {
        if (gridManager == null) return;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int p = center + new Vector2Int(dx, dy);
                if (gridManager.TryGetPlacedObject(p, out var obj) && obj is House h)
                {
                    CheckEffects(h);
                }
            }
        }
    }

    private void CheckEffectsAfterDemolish(PlacedObject po)
    {
        if (po is Well well)
        {
            int r = well.buildEffectRadius;
            Vector2Int c = well.gridPos;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    Vector2Int p = c + new Vector2Int(dx, dy);
                    if (gridManager.TryGetPlacedObject(p, out var obj) && obj is House h)
                    {
                        bool stillHas = false;
                        int searchRadius = 10;

                        for (int sx = -searchRadius; sx <= searchRadius && !stillHas; sx++)
                        {
                            for (int sy = -searchRadius; sy <= searchRadius && !stillHas; sy++)
                            {
                                Vector2Int s = h.gridPos + new Vector2Int(sx, sy);
                                if (gridManager.TryGetPlacedObject(s, out var maybe) && maybe is Well otherWell)
                                {
                                    if (IsInEffectSquare(otherWell.gridPos, h.gridPos, otherWell.buildEffectRadius))
                                        stillHas = true;
                                }
                            }
                        }

                        h.SetWaterAccess(stillHas);
                    }
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
            {
                if (gridManager.TryGetPlacedObject(n, out var obj) && obj != null && !(obj is Road))
                {
                    CheckEffects(obj);
                }
            }
        }
    }

    GameObject GetPrefabByBuildMode(BuildMode mode)
    {
        if (prefabByMode == null || prefabByMode.Count == 0)
            BuildPrefabCache();

        return prefabByMode.TryGetValue(mode, out var prefab) ? prefab : null;
    }

    private float lastPopupTime = -999f;

    [SerializeField] private float popupCooldown = 0.35f;

// Смещение попапа рядом с клеткой (в пикселях экрана)
    [SerializeField] private float popupOffsetPixelsX = 0f;
    [SerializeField] private float popupOffsetPixelsY = 0f;

// Отступ от краёв экрана (в пикселях)
    [SerializeField] private float popupScreenMarginPixels = 24f;
    private void ShowBuildFailPopupAtCell(Vector2Int cell, string msg, MessagePopUp.Style style = MessagePopUp.Style.Error)
    {
        if (Time.time - lastPopupTime < popupCooldown) return;
        lastPopupTime = Time.time;

        Camera cam = Camera.main;
        if (cam == null || gridManager == null) return;

        // 1) World позиция клетки
        Vector3 cellWorld = gridManager.CellToIsoWorld(cell);

        // 2) Переводим в экранные пиксели (ВАЖНО: сохраняем depth в screen.z)
        Vector3 screen = cam.WorldToScreenPoint(cellWorld);

        // Если точка за камерой — ничего не показываем
        if (screen.z <= 0f) return;

        // 3) Делаем смещение "рядом" (слегка вбок + вверх), с небольшим рандомом
        float dx = Random.Range(-popupOffsetPixelsX, popupOffsetPixelsX);
        float dy = Random.Range(popupOffsetPixelsY * 0.7f, popupOffsetPixelsY * 1.2f);

        screen.x += dx;
        screen.y += dy;

        // 4) Clamp в пределах экрана (чтобы всегда было видно)
        screen.x = Mathf.Clamp(screen.x, popupScreenMarginPixels, Screen.width - popupScreenMarginPixels);
        screen.y = Mathf.Clamp(screen.y, popupScreenMarginPixels, Screen.height - popupScreenMarginPixels);

        // 5) Назад в world на ТОЙ ЖЕ глубине (screen.z!)
        Vector3 spawnWorld = cam.ScreenToWorldPoint(screen);

        // 6) Пиксель-перфект (как у зданий)
        spawnWorld.x = Mathf.Round(spawnWorld.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
        spawnWorld.y = Mathf.Round(spawnWorld.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;

        MessagePopUp.Create(spawnWorld, msg, style);
    }

    public void ToggleLineBuildMode()
    {
        lineBuildMode = !lineBuildMode;
        SyncLineModeButtonText();
    }

    private void SyncLineModeButtonText()
    {
        if (lineModeButtonText != null)
            lineModeButtonText.text = lineBuildMode ? "Line" : "Default";
    }

    private Vector2Int GetLineCell(Vector2Int rawCell)
    {
        if (!lineBuildMode)
            return rawCell;

        // если якоря ещё нет — считаем текущую клетку якорем
        if (lineAnchorCell == null)
            lineAnchorCell = rawCell;

        Vector2Int a = lineAnchorCell.Value;

        // определяем ось фиксации, когда ушли дальше чем на 0 клеток
        if (!lineLockActive)
        {
            int dx = Mathf.Abs(rawCell.x - a.x);
            int dy = Mathf.Abs(rawCell.y - a.y);

            if (dx != 0 || dy != 0)
            {
                // если dx >= dy -> фиксируем Y (строим по X), иначе фиксируем X (строим по Y)
                lockAxisX = dy > dx;   // dy больше -> фиксируем X
                lineLockActive = true;
            }
        }

        if (!lineLockActive)
            return rawCell;

        // применяем фиксацию
        if (lockAxisX)
            return new Vector2Int(a.x, rawCell.y);   // фикс X, меняем Y
        else
            return new Vector2Int(rawCell.x, a.y);   // фикс Y, меняем X
    }
private void PlaceObjectAtCell(Vector2Int origin)
{
    GameObject prefab = GetPrefabByBuildMode(currentMode);
    if (prefab == null) return;

    PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
    if (poPrefab == null) return;

    int sizeX = poPrefab.SizeX;
    int sizeY = poPrefab.SizeY;

    // --- 1) Проверка свободного места ---
    for (int x = 0; x < sizeX; x++)
    {
        for (int y = 0; y < sizeY; y++)
        {
            Vector2Int testPos = origin + new Vector2Int(x, y);
            if (!gridManager.IsCellFree(testPos))
            {
                ShowBuildFailPopupAtCell(origin, "Can't build here", MessagePopUp.Style.Error);
                return;
            }
        }
    }

    // --- 2) Проверка условий соседства ---
    if (poPrefab.needWaterNearby && !HasAdjacentWater(origin, sizeX, sizeY))
    {
        ShowBuildFailPopupAtCell(origin, "Need to place near water", MessagePopUp.Style.Warning);
        return;
    }

    if (poPrefab.NeedHouseNearby && !HasAdjacentHouse(origin, sizeX, sizeY))
    {
        ShowBuildFailPopupAtCell(origin, "Need to place near houses", MessagePopUp.Style.Warning);
        return;
    }

    if (poPrefab.needMountainsNearby && !HasAdjacentMountain(origin, sizeX, sizeY))
    {
        ShowBuildFailPopupAtCell(origin, "Need to place near mountains", MessagePopUp.Style.Warning);
        return;
    }

    // --- 3) Проверка ресурсов ---
    var cost = poPrefab.GetCostDict();
    if (!ResourceManager.Instance.CanSpend(cost))
    {
        ShowBuildFailPopupAtCell(origin, "Not enough resources", MessagePopUp.Style.Error);
        return;
    }

    // --- 4) Убираем базовые тайлы под объектом ---
    for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
            gridManager.ReplaceBaseTile(origin + new Vector2Int(x, y), null);

    // --- 5) Ставим объект ---
    Vector3 pos = gridManager.CellToIsoWorld(origin);
    pos.x = Mathf.Round(pos.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
    pos.y = Mathf.Round(pos.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;

    GameObject go = Instantiate(prefab, pos, Quaternion.identity);
    PlacedObject po = go.GetComponent<PlacedObject>();
    if (po == null) return;

    po.gridPos = origin;
    po.manager = gridManager;
    go.name = prefab.name;
    po.OnPlaced();

    if (go.TryGetComponent<SpriteRenderer>(out var sr))
        gridManager.ApplySorting(po.gridPos, po.SizeX, po.SizeY, sr, false, po is Road);

    // --- 6) Списываем ресурсы ---
    ResourceManager.Instance.SpendResources(cost);

    // --- 7) Отмечаем клетки занятыми ---
    for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
            gridManager.SetOccupied(origin + new Vector2Int(x, y), true, po);

    // --- 8) Дороги / эффекты ---
    if (po is Road road)
    {
        roadManager.RegisterRoad(origin, road);
        roadManager.RefreshRoadAndNeighbors(origin);
        RecheckRoadAccessForAllBuildings();
    }

    if (AudioManager.Instance != null)
        AudioManager.Instance.PlayBuild();

    CheckEffects(po);
}
public GameObject GetPrefabByMode(BuildMode mode)
{
    if (prefabByMode == null || prefabByMode.Count == 0)
        BuildPrefabCache();

    return prefabByMode.TryGetValue(mode, out var prefab) ? prefab : null;
}

}
