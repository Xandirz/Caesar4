using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    private Dictionary<Vector2Int, Road> roads = new();

    // Зарегистрировать новую дорогу
    public void RegisterRoad(Vector2Int pos, Road road)
    {
        if (!roads.ContainsKey(pos))
            roads.Add(pos, road);
    }

    // Удалить дорогу
    public void UnregisterRoad(Vector2Int pos)
    {
        if (roads.ContainsKey(pos))
            roads.Remove(pos);
    }

    // Проверка — есть ли дорога в клетке
    public bool IsRoadAt(Vector2Int pos)
    {
        return roads.ContainsKey(pos);
    }

    // Обновить дорогу в точке
    public void UpdateRoadAt(Vector2Int pos)
    {
        if (!roads.TryGetValue(pos, out var road) || road == null)
        {
            roads.Remove(pos);
            return;
        }

        // Изометрия: up=NW, right=NE, down=SE, left=SW
        bool hasNW = IsRoadAt(pos + Vector2Int.up);
        bool hasNE = IsRoadAt(pos + Vector2Int.right);
        bool hasSE = IsRoadAt(pos + Vector2Int.down);
        bool hasSW = IsRoadAt(pos + Vector2Int.left);

// Порядок строго (nw, ne, se, sw)
        road.UpdateRoadSprite(hasNW, hasNE, hasSE, hasSW);
    }



    // Обновить дорогу + соседей
    public void RefreshRoadAndNeighbors(Vector2Int pos)
    {
        UpdateRoadAt(pos);
        UpdateRoadAt(pos + Vector2Int.up);
        UpdateRoadAt(pos + Vector2Int.down);
        UpdateRoadAt(pos + Vector2Int.left);
        UpdateRoadAt(pos + Vector2Int.right);
    }
}