using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlacedObject : MonoBehaviour
{
    public Vector2Int gridPos;
    public GridManager manager;
    public virtual int buildEffectRadius => 0;
    public virtual int SizeX => 1;
    public virtual int SizeY => 1;
public virtual BuildManager.BuildMode BuildMode => BuildManager.BuildMode.None;
    // Стоимость задаётся в скрипте наследника
    public Dictionary<string,int> cost = new Dictionary<string,int>();

    // Получаем словарь стоимости для BuildManager/UI
    public virtual Dictionary<string,int> GetCostDict()
    {
        return new Dictionary<string,int>(cost);
    }

    public bool hasRoadAccess = false;

    [FormerlySerializedAs("requiresAdjacentWater")] [Header("Placement Rules")]
    public bool needWaterNearby = false;
    [SerializeField] public bool needHouseNearby = false;
    public bool NeedHouseNearby => needHouseNearby;
    public bool hasHouseNearby;
    
    public virtual void OnClicked()
    {
        var bm = FindObjectOfType<BuildManager>();
        if (bm != null && bm.CurrentMode != BuildManager.BuildMode.None &&
            bm.CurrentMode != BuildManager.BuildMode.Demolish)
            return;

        MouseHighlighter.Instance.ClearHighlights();

        if (InfoUI.Instance != null)
        {
            InfoUI.Instance.ShowInfo(this);

        }


    }

    public virtual void OnPlaced()
    {
      
    }

    public virtual void OnRemoved()
    {
        Destroy(gameObject);

    }
    
    public virtual List<Vector2Int> GetOccupiedCells()
    {
        List<Vector2Int> cells = new();
        for (int x = 0; x < SizeX; x++)
        for (int y = 0; y < SizeY; y++)
            cells.Add(new Vector2Int(gridPos.x + x, gridPos.y + y));
        return cells;
    }
    
    public bool HasAdjacentHouse()
    {
        if (manager == null) return false;

        for (int dx = 0; dx < SizeX; dx++)
        {
            for (int dy = 0; dy < SizeY; dy++)
            {
                Vector2Int cell = gridPos + new Vector2Int(dx, dy);

                // 4-соседи
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
        return manager.TryGetPlacedObject(cell, out var obj) && obj is House;
    }


}