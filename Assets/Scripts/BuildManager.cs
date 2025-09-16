using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public RoadManager roadManager;
    public List<GameObject> buildingPrefabs;

    public enum BuildMode { None, Road, House, LumberMill, Demolish, Well }
    private BuildMode currentMode = BuildMode.None;

    public BuildMode CurrentMode => currentMode;
    public void SetBuildMode(BuildMode mode) => currentMode = mode;

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
        Vector2Int cell = gridManager.IsoWorldToCell(mw);

        if (!gridManager.IsCellFree(cell)) return;

        GameObject prefab = GetPrefabByBuildMode(currentMode);
        if (prefab == null) return;

        PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
        var cost = poPrefab != null ? poPrefab.GetCostDict() : new Dictionary<string, int>();

        if (!ResourceManager.Instance.CanSpend(cost))
        {
            Debug.Log("Недостаточно ресурсов!");
            return;
        }

        // убираем grass/forest
        gridManager.ReplaceBaseTile(cell, null);

        // вычисляем мировые координаты с привязкой к пиксельной сетке
        Vector3 pos = gridManager.CellToIsoWorld(cell);
        pos.x = Mathf.Round(pos.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
        pos.y = Mathf.Round(pos.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);

        PlacedObject po = go.GetComponent<PlacedObject>();
        if (po == null) return;

        po.gridPos = cell;
        po.manager = gridManager;
        po.OnPlaced();

        if (go.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.sortingLayerName = "World";
            sr.sortingOrder = -(int)(pos.y * 100);

            // ⚡️ дороги должны быть выше земли/леса
            if (po is Road)
                sr.sortingOrder += 1;
        }

        // списываем ресурсы
        ResourceManager.Instance.SpendResources(cost);

        gridManager.SetOccupied(cell, true, po);

        // если дорога → регистрируем её в RoadManager
        if (po is Road road)
        {
            roadManager.RegisterRoad(cell, road);
            roadManager.RefreshRoadAndNeighbors(cell);
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

            // после сноса возвращаем grass
            gridManager.ReplaceBaseTile(cell, gridManager.groundPrefab);

            gridManager.SetOccupied(cell, false);

            // если это дорога → обновляем соседей через RoadManager
            if (po is Road)
                roadManager.UnregisterRoad(cell);
        }
    }
}
