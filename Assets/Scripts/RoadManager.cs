using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    private Dictionary<Vector2Int, Road> roads = new();
    
    private Vector2Int obeliskPos;
    private bool hasObelisk = false;

    public static RoadManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    // Зарегистрировать новую дорогу
    public void RegisterRoad(Vector2Int pos, Road road)
    {
        if (!roads.ContainsKey(pos))
            roads.Add(pos, road);
        
        RecalculateConnections(pos); 

    }

    // Удалить дорогу
    public void UnregisterRoad(Vector2Int pos)
    {
        if (roads.ContainsKey(pos))
            roads.Remove(pos);

        // можно выбрать ближайшую дорогу и от неё пересчитать
        foreach (var neighbor in new[] { pos + Vector2Int.up, pos + Vector2Int.down, pos + Vector2Int.left, pos + Vector2Int.right })
        {
            if (roads.ContainsKey(neighbor))
            {
                RecalculateConnections(neighbor);
                break;
            }
        }
    }

    // Проверка — есть ли дорога в клетке
    public bool IsRoadAt(Vector2Int pos)
    {
        return roads.ContainsKey(pos);
    }

    public void RecalculateConnections(Vector2Int startCell)
    {
        if (!roads.ContainsKey(startCell)) return;

        Queue<Vector2Int> frontier = new();
        HashSet<Vector2Int> visited = new();

        frontier.Enqueue(startCell);
        visited.Add(startCell);

        while (frontier.Count > 0)
        {
            var cur = frontier.Dequeue();
            if (!roads.ContainsKey(cur)) continue;

            Road road = roads[cur];

            // ✅ вызываем проверку заново для каждой дороги
            road.isConnectedToObelisk = IsConnectedToObelisk(cur);
            UpdateRoadAt(road.gridPos);

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var next = cur + dir;
                if (roads.ContainsKey(next) && !visited.Contains(next))
                {
                    frontier.Enqueue(next);
                    visited.Add(next);
                }
            }
        }
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
        
        road.isConnectedToObelisk = IsConnectedToObelisk(pos);
        
        
        foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int neighbor = pos + dir;
            if (BuildManager.Instance.gridManager.TryGetPlacedObject(neighbor, out var po) && po != null)
            {
                BuildManager.Instance.CheckEffects(po);
            }
        }
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
    
    public void UpdateBuildingAccessAround(Vector2Int pos)
    {
        Vector2Int[] neighbors =
        {
            pos,
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right
        };

        foreach (var n in neighbors)
        {
            if (BuildManager.Instance.gridManager.TryGetPlacedObject(n, out var po))
            {
                if (po != null && !(po is Road))
                {
                    BuildManager.Instance.CheckEffects(po); 
                }
            }
        }
    }


    public void RegisterObelisk(Vector2Int pos)
    {
        obeliskPos = pos;
        hasObelisk = true;
    }

    public bool IsConnectedToObelisk(Vector2Int roadCell)
    {
        if (!hasObelisk) return false;

        Queue<Vector2Int> frontier = new();
        HashSet<Vector2Int> visited = new();

        frontier.Enqueue(roadCell);
        visited.Add(roadCell);

        while (frontier.Count > 0)
        {
            var cur = frontier.Dequeue();

            // если рядом обелиск → true
            if (cur + Vector2Int.up == obeliskPos) return true;
            if (cur + Vector2Int.down == obeliskPos) return true;
            if (cur + Vector2Int.left == obeliskPos) return true;
            if (cur + Vector2Int.right == obeliskPos) return true;

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var next = cur + dir;
                if (roads.ContainsKey(next) && !visited.Contains(next))
                {
                    frontier.Enqueue(next);
                    visited.Add(next);
                }
            }
        }

        return false;
    }


}