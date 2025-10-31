using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public RoadManager roadManager;
    public List<GameObject> buildingPrefabs;

    public enum BuildMode
    {
        None, Road, House, LumberMill, Demolish, Well, Warehouse, Berry, Rock, Clay, Pottery, Hunter,
        Tools, Clothes, Crafts, Furniture, Wheat, Flour, Sheep, Weaver, Dairy, Bakery, Beans, Brewery,
        Coal, CopperOre, Copper, Market
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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            MouseHighlighter.Instance.HighlightRectangle(dragStartCell, dragEndCell, MouseHighlighter.Instance.demolishColor);
        }

        // отпускание — выполняем снос
        if (isSelecting && Input.GetMouseButtonUp(0))
        {
            isSelecting = false;
            MouseHighlighter.Instance.ClearHighlights();

            Vector2Int min = new(Mathf.Min(dragStartCell.x, dragEndCell.x), Mathf.Min(dragStartCell.y, dragEndCell.y));
            Vector2Int max = new(Mathf.Max(dragStartCell.x, dragEndCell.x), Mathf.Max(dragStartCell.y, dragEndCell.y));

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    DemolishAtCell(new Vector2Int(x, y));
                }
            }
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

        PlaceObject();
        lastPlacedCell = GetMouseCell();
    }

    if (Input.GetMouseButton(0) && currentMode != BuildMode.None)
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int cell = GetMouseCell();

        if (lastPlacedCell == null || cell != lastPlacedCell.Value)
        {
            PlaceObject();
            lastPlacedCell = cell;
        }
    }

    if (Input.GetMouseButtonUp(0))
    {
        lastPlacedCell = null;
    }

    // ПКМ — сброс режима
    if (Input.GetMouseButtonDown(1))
    {
        currentMode = BuildMode.None;
        MouseHighlighter.Instance.ClearHighlights();
    }
}




    private Vector2Int GetMouseCell()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;
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
            roadManager.UnregisterRoad(origin);

        CheckEffectsAfterDemolish(po);
        if (po is not Obelisk)
            Destroy(po.gameObject);
    }


    void PlaceObject()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;
        Vector2Int origin = gridManager.IsoWorldToCell(mw);

        GameObject prefab = GetPrefabByBuildMode(currentMode);
        if (prefab == null) return;

        PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
        if (poPrefab == null) return;

        int sizeX = poPrefab.SizeX;
        int sizeY = poPrefab.SizeY;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector2Int testPos = origin + new Vector2Int(x, y);
                if (!gridManager.IsCellFree(testPos))
                    return;
            }
        }

        var cost = poPrefab.GetCostDict();
        if (!ResourceManager.Instance.CanSpend(cost))
        {
            Debug.Log("Недостаточно ресурсов!");
            return;
        }

        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                gridManager.ReplaceBaseTile(origin + new Vector2Int(x, y), null);

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

        ResourceManager.Instance.SpendResources(cost);

        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                gridManager.SetOccupied(origin + new Vector2Int(x, y), true, po);

        if (po is Road road)
        {
            roadManager.RegisterRoad(origin, road);
            roadManager.RefreshRoadAndNeighbors(origin);
        }

        CheckEffects(po);
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

            po.hasRoadAccess = hasAccess;
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
                        if (IsInEffectSquare(w.gridPos, house.gridPos, w.buildEffectRadius))
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
                        if (IsInEffectSquare(m.gridPos, house.gridPos, m.buildEffectRadius))
                        {
                            house.SetMarketAccess(true);
                            hasMarket = true;
                        }
                    }
                }
            }

            if (!hasMarket)
                house.SetMarketAccess(false);
        }
    }

    private bool IsInEffectSquare(Vector2Int center, Vector2Int pos, int radius)
    {
        return Mathf.Abs(pos.x - center.x) <= radius &&
               Mathf.Abs(pos.y - center.y) <= radius;
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
        foreach (var p in buildingPrefabs)
        {
            var po = p.GetComponent<PlacedObject>();
            if (po != null && po.BuildMode == mode) return p;
        }
        return null;
    }

    // ✅ твой оригинальный DemolishObject — без изменений
    void DemolishObject()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;
        Vector2Int cell = gridManager.IsoWorldToCell(mw);

        if (gridManager.IsCellFree(cell))
        {
            Debug.Log("Здесь ничего нет!");
            return;
        }

        if (!gridManager.TryGetPlacedObject(cell, out var po) || po == null)
            return;

        if (po is Obelisk)
            return;

        Vector2Int origin = po.gridPos;
        int sizeX = po.SizeX;
        int sizeY = po.SizeY;

        po.OnRemoved();

        for (int dx = 0; dx < sizeX; dx++)
        {
            for (int dy = 0; dy < sizeY; dy++)
            {
                Vector2Int p = origin + new Vector2Int(dx, dy);
                gridManager.SetOccupied(p, false);
                gridManager.ReplaceBaseTile(p, gridManager.groundPrefab);
            }
        }

        if (po is Road)
            roadManager.UnregisterRoad(origin);

        CheckEffectsAfterDemolish(po);
        if (po is not Obelisk)
        {
            Destroy(po.gameObject);
        }
    }
}
