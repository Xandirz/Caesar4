using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public RoadManager roadManager;
    public List<GameObject> buildingPrefabs;

    public enum BuildMode { None, Road, House, LumberMill, Demolish,Upgrade, Well, Warehouse, Berry, Rock, Clay, Pottery, Hunter,
        Tools, Clothes, Crafts, Furniture, Wheat, Flour, Sheep, Weaver, Dairy, Bakery, Beans, Brewery,
        Coal, CopperOre, Copper
    }
    private BuildMode currentMode = BuildMode.None;

    public BuildMode CurrentMode => currentMode;
    public void SetBuildMode(BuildMode mode) => currentMode = mode;
    public static BuildManager Instance { get; private set; }
    
    private Vector2Int? lastPlacedCell = null;



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    


   void Update()
{
    if (Input.GetMouseButtonDown(0) && currentMode != BuildMode.None)
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (currentMode == BuildMode.Demolish || currentMode == BuildMode.Upgrade)
        {
            Vector2Int cell = GetMouseCell();

            // Сначала очищаем старую подсветку
            MouseHighlighter.Instance.ClearHighlights();

            // Затем создаём новую подсветку
            MouseHighlighter.Instance.CreateSingleHighlight(cell);
        }

        else
        {
            PlaceObject();
            lastPlacedCell = GetMouseCell();
        }
    }

    // --- удержание ЛКМ ---
    if (Input.GetMouseButton(0) && currentMode != BuildMode.None)
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int cell = GetMouseCell();

        if (lastPlacedCell == null || cell != lastPlacedCell.Value)
        {
            if (currentMode == BuildMode.Demolish)
            {
                MouseHighlighter.Instance.CreateSingleHighlight(cell);
                DemolishObject();
            }
            else if (currentMode == BuildMode.Upgrade) // 🆕 массовое улучшение
            {
                MouseHighlighter.Instance.CreateSingleHighlight(cell);
                TryUpgradeObject(cell);
            }
            else
            {
                PlaceObject();
            }

            lastPlacedCell = cell;
        }
    }

    if (Input.GetMouseButtonUp(0))
    {
        lastPlacedCell = null;
        if (currentMode == BuildMode.Demolish || currentMode == BuildMode.Upgrade)
            MouseHighlighter.Instance.ClearHighlights();
    }

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

    private void TryUpgradeObject(Vector2Int cell)
    {
        if (!gridManager.TryGetPlacedObject(cell, out var po) || po == null)
            return;

        // Проверяем тип
        if (po is House house)
        {
            house.TryUpgrade();
        }
        else if (po is ProductionBuilding prod)
        {
            prod.TryUpgrade();
        }
        else
        {
            Debug.Log("Этот объект нельзя улучшить");
        }
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

    // Проверяем все клетки
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

    // Убираем базовые тайлы под всем объектом
    for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
            gridManager.ReplaceBaseTile(origin + new Vector2Int(x, y), null);

    // Вычисляем позицию
    Vector3 pos = gridManager.CellToIsoWorld(origin);
    pos.x = Mathf.Round(pos.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
    pos.y = Mathf.Round(pos.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;

    GameObject go = Instantiate(prefab, pos, Quaternion.identity);

    PlacedObject po = go.GetComponent<PlacedObject>();
    if (po == null) return;

    po.gridPos = origin;
    po.manager = gridManager;
    po.OnPlaced();

    if (go.TryGetComponent<SpriteRenderer>(out var sr))
        gridManager.ApplySorting(po.gridPos, po.SizeX, po.SizeY, sr, false, po is Road);


    ResourceManager.Instance.SpendResources(cost);

    // Помечаем все клетки как занятые
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
       if (!(po is Road)) // у дорог это не нужно
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
                       if (roadManager.IsRoadAt(n)&& roadManager.IsConnectedToObelisk(n))
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

           // Обновляем дома ВНУТРИ квадрата радиуса
           for (int dx = -r; dx <= r; dx++)
           {
               for (int dy = -r; dy <= r; dy++)
               {
                   Vector2Int p = c + new Vector2Int(dx, dy);
                   if (gridManager.TryGetPlacedObject(p, out var obj) && obj is House h)
                   {
                       h.SetWaterAccess(true);
                   }
               }
           }
       }
       else if (po is House house)
       {
           // Ищем ЛЮБОЙ колодец поблизости, который покрывает дом по тому же правилу квадрата
           bool hasWater = false;
           int searchRadius = 10; // разумная «рамка» поиска вокруг дома

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

           // Перебираем дома, которые находились в квадрате радиуса снесённого колодца
           for (int dx = -r; dx <= r; dx++)
           {
               for (int dy = -r; dy <= r; dy++)
               {
                   Vector2Int p = c + new Vector2Int(dx, dy);
                   if (gridManager.TryGetPlacedObject(p, out var obj) && obj is House h)
                   {
                       // Проверяем: остался ли ХОТЯ БЫ один другой колодец, покрывающий этот дом
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
           // ✅ новая логика для дорог
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

    void DemolishObject()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;
        Vector2Int cell = gridManager.IsoWorldToCell(mw);

        // 1) Если клетка свободна — сразу выходим
        if (gridManager.IsCellFree(cell))
        {
            Debug.Log("Здесь ничего нет!");
            return;
        }

        // 2) Узнаём, какой объект занимает ЭТУ клетку
        if (!gridManager.TryGetPlacedObject(cell, out var po) || po == null)
            return;

        if (po is Obelisk)
            return; // 🚫 нельзя сносить обелиск
        
        // 3) Берём origin и размер именно из объекта
        Vector2Int origin = po.gridPos;        // левый верхний угол твоего 2×2
        int sizeX = po.SizeX;                  // у склада 2
        int sizeY = po.SizeY;                  // у склада 2

        // 4) Хук объекта (рефанд ресурсов и т.п.)
        po.OnRemoved();

        // 5) Освобождаем ВСЕ клетки прямоугольника (0;0, 0;1, 1;0, 1;1)
        for (int dx = 0; dx < sizeX; dx++)
        {
            for (int dy = 0; dy < sizeY; dy++)
            {
                Vector2Int p = origin + new Vector2Int(dx, dy);
                gridManager.SetOccupied(p, false);
                gridManager.ReplaceBaseTile(p, gridManager.groundPrefab);
            }
        }

        // 6) Спец-логика для дорог
        if (po is Road)
            roadManager.UnregisterRoad(origin);

        // 7) Удаляем объект из сцены
        CheckEffectsAfterDemolish(po);
        if (po is not Obelisk)
        {
            Destroy(po.gameObject);

        }
        
    }



}
