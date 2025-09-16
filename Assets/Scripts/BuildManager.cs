using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
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
    }

    void PlaceObject()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;
        Vector2Int cell = gridManager.IsoWorldToCell(mw);

        if (!gridManager.IsCellFree(cell)) return;

        GameObject prefab = GetPrefabByBuildMode(currentMode);
        if (prefab == null) return;

        // === Дорога ===
        if (currentMode == BuildMode.Road)
        {
            Vector3 pos = gridManager.CellToIsoWorld(cell);
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);

            Road road = go.GetComponent<Road>();
            if (road == null) return;

            road.gridPos = cell;
            road.manager = gridManager;
            road.OnPlaced();

            if (go.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingLayerName = "World";
                sr.sortingOrder = -(int)(pos.y * 100);
            }

            gridManager.RegisterRoad(cell, road);
            gridManager.SetOccupied(cell, true, road);
            return; // ⚡️ не строим как обычное здание
        }

        // === Обычные здания ===
        PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
        var cost = poPrefab != null ? poPrefab.GetCostDict() : new Dictionary<string, int>();

        if (!ResourceManager.Instance.CanSpend(cost))
        {
            Debug.Log("Недостаточно ресурсов!");
            return;
        }

        // убираем grass/forest
        gridManager.ReplaceBaseTile(cell, null);

        Vector3 pos2 = gridManager.CellToIsoWorld(cell);
        GameObject go2 = Instantiate(prefab, pos2, Quaternion.identity);

        PlacedObject po = go2.GetComponent<PlacedObject>();
        if (po == null) return;

        po.gridPos = cell;
        po.manager = gridManager;
        po.OnPlaced();

        if (go2.TryGetComponent<SpriteRenderer>(out var sr2))
        {
            sr2.sortingLayerName = "World";
            sr2.sortingOrder = -(int)(pos2.y * 100);
        }

        ResourceManager.Instance.SpendResources(cost);
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
        }
    }
}
