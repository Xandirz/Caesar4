using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public List<GameObject> buildingPrefabs;
    public Color zoneOfAffectColor = Color.cyan;

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
            else                                   PlaceObject();
        }

        if (currentMode == BuildMode.Well)
        {
            Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int cell = gridManager.IsoWorldToCell(mw);
            gridManager.ClearHighlight();
            gridManager.HighlightZone(cell, 4, zoneOfAffectColor);
        }
        else
        {
            gridManager.ClearHighlight();
        }
    }

    void PlaceObject()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int cell = gridManager.IsoWorldToCell(mw);
        if (!gridManager.IsCellFree(cell)) return;

        GameObject prefab = GetPrefabByBuildMode(currentMode);
        if (prefab == null) return;

        PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
        var cost = poPrefab != null ? poPrefab.GetCostDict() : new Dictionary<string,int>();
        if (!ResourceManager.Instance.CanSpend(cost))
        {
            Debug.Log("Недостаточно ресурсов!");
            return;
        }

        Vector3 pos = gridManager.CellToIsoWorld(cell);
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);

        // для изометрии корректная сортировка по y
        if (go.TryGetComponent<SpriteRenderer>(out var sr))
            sr.sortingOrder = -(int)(pos.y * 100);

        if (go.TryGetComponent<PlacedObject>(out var po))
        {
            po.gridPos = cell;
            po.manager = gridManager;
            po.OnPlaced();
        }

        ResourceManager.Instance.SpendResources(cost);
        PlacedObject poss = go.GetComponent<PlacedObject>();
        poss.gridPos = cell;
        poss.manager = gridManager;

        gridManager.SetOccupied(cell, true, po);

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
        Vector2Int cell = gridManager.IsoWorldToCell(mw);

        if (gridManager.IsCellFree(cell))
        {
            Debug.Log("Здесь ничего нет!");
            return;
        }

        Vector3 center = gridManager.CellToIsoWorld(cell);
        Collider2D hit = Physics2D.OverlapPoint(center);
        if (hit && hit.TryGetComponent<PlacedObject>(out var po))
            po.OnRemoved();
    }
}
