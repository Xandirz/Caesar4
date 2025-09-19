using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public RoadManager roadManager;
    public List<GameObject> buildingPrefabs;

    public enum BuildMode { None, Road, House, LumberMill, Demolish, Well, Warehouse }
    private BuildMode currentMode = BuildMode.None;

    public BuildMode CurrentMode => currentMode;
    public void SetBuildMode(BuildMode mode) => currentMode = mode;
    public static BuildManager Instance { get; private set; }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && currentMode != BuildMode.None)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (currentMode == BuildMode.Demolish) DemolishObject();
            else PlaceObject();
        }
        
        if(Input.GetMouseButtonDown(1))
        {
            currentMode = BuildMode.None;
            MouseHighlighter.Instance.ClearHighlights();
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
    {
        sr.sortingLayerName = "World";
        int bottomY = origin.y + po.SizeY - 1;
        sr.sortingOrder = -(bottomY * 1000 + origin.x);

        if (po is Road)
            sr.sortingOrder += 1;
    }

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

        if (gridManager.IsCellFree(cell))
        {
            Debug.Log("Здесь ничего нет!");
            return;
        }

        Vector3 center = gridManager.CellToIsoWorld(cell);
        Collider2D hit = Physics2D.OverlapPoint(center);

        if (hit && hit.TryGetComponent<PlacedObject>(out var po))
        {
            po.OnRemoved();
            Destroy(po.gameObject);

            int sizeX = po.SizeX;
            int sizeY = po.SizeY;
            Vector2Int origin = po.gridPos;

            // 🔥 Освобождаем ВСЕ клетки здания (например, 2×2 у склада)
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    Vector2Int pos = origin + new Vector2Int(x, y);
                    gridManager.SetOccupied(pos, false);
                    gridManager.ReplaceBaseTile(pos, gridManager.groundPrefab);
                }
            }

            // если это дорога → обновляем соседей
            if (po is Road)
                roadManager.UnregisterRoad(origin);
        }
    }


}
