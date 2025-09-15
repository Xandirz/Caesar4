using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;

    public List<GameObject>  buildingPrefabs;
    public Color zoneOfAffectColor = Color.blue;
    public enum BuildMode { None, Road, House, LumberMill, Demolish, Well }
    private BuildMode currentMode = BuildMode.None;

    public BuildMode CurrentMode => currentMode;

    // Метод для UI кнопок
    public void SetBuildMode(BuildMode mode)
    {
        currentMode = mode;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && currentMode != BuildMode.None)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (currentMode == BuildMode.Demolish)
                DemolishObject();
            else
                PlaceObject();
        }

        // Если режим - колодец, визуально выделяем зону вокруг курсора
        if (currentMode == BuildMode.Well)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int cellPos = new Vector2Int(Mathf.FloorToInt(mouseWorld.x), Mathf.FloorToInt(mouseWorld.y));

            // Очистить предыдущие выделения зоны
              gridManager.ClearHighlight();

            // Выделить зону вокруг позиции под курсором
            gridManager.HighlightZone(cellPos, 4, zoneOfAffectColor);
        }
        else
        {
            gridManager.ClearHighlight();
        }
    }


    void PlaceObject()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int cellPos = gridManager.WorldToCell(mouseWorld);

        if (!gridManager.IsCellFree(cellPos))
            return;

        GameObject prefab = GetPrefabByBuildMode(currentMode);
        if (prefab == null) return;

        PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
        var costDict = poPrefab.GetCostDict();

        if (!ResourceManager.Instance.CanSpend(costDict))
        {
            Debug.Log("Недостаточно ресурсов для строительства!");
            return;
        }

        Vector3 placePos = gridManager.CellToWorld(cellPos);
        GameObject obj = Instantiate(prefab, placePos, Quaternion.identity);

        PlacedObject po = obj.GetComponent<PlacedObject>();
        po.gridPos = cellPos;
        po.manager = gridManager;

        ResourceManager.Instance.SpendResources(costDict);
        gridManager.SetOccupied(cellPos, true);

        po.OnPlaced();
    }

    GameObject GetPrefabByBuildMode(BuildManager.BuildMode mode)
    {
        foreach (var prefab in buildingPrefabs)
        {
            PlacedObject po = prefab.GetComponent<PlacedObject>();
            if (po != null && po.BuildMode == mode)
                return prefab;
        }
        return null;
    }




    void DemolishObject()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int cellPos = gridManager.WorldToCell(mouseWorld);

        if (gridManager.IsCellFree(cellPos))
        {
            Debug.Log("Здесь ничего нет!");
            return;
        }

        Vector3 cellWorld = gridManager.CellToWorld(cellPos);
        Collider2D hit = Physics2D.OverlapPoint(cellWorld);

        if (hit != null)
        {
            PlacedObject po = hit.GetComponent<PlacedObject>();
            if (po != null)
                po.OnRemoved();
        }
    }
}
