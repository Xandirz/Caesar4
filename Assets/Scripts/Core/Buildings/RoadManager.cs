using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    public Dictionary<Vector2Int, Road> roads = new();
    private Dictionary<Vector2Int, bool> connected = new(); // 🔹 кэш подключённости

    private Vector2Int obeliskPos;
    private bool hasObelisk = false;
    private GridManager gridManager;

    public static RoadManager Instance { get; private set; }

    private static readonly Vector2Int[] dirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        gridManager =FindObjectOfType<GridManager>();
    }

    // ==================== Roads ====================
    public void RegisterRoad(Vector2Int pos, Road road)
    {
        if (!roads.ContainsKey(pos))
            roads[pos] = road;

        RecalculateConnections(); // 🔹 пересчитываем всю компоненту один раз
    }

    public void UnregisterRoad(Vector2Int pos)
    {
        if (roads.Remove(pos))
        {
            connected.Remove(pos);
            RecalculateConnections(); // 🔹 пересчёт после удаления
        }
    }

    public bool IsRoadAt(Vector2Int pos) => roads.ContainsKey(pos);

    // ==================== Connections ====================
    public void RecalculateConnections()
    {
        gridManager.UpdateConnectedRoadsFromRoadManager();
        // если нет обелиска — все дороги = false
        if (!hasObelisk)
        {
            foreach (var kvp in roads)
            {
                kvp.Value.isConnectedToObelisk = false;
                connected[kvp.Key] = false;
                kvp.Value.ApplyConnectionVisual();
                   
            }
            return;
        }

        // BFS от обелиска
        Queue<Vector2Int> frontier = new();
        HashSet<Vector2Int> visited = new();

        // обелиск не хранится как дорога, поэтому стартуем со всех соседних клеток
        foreach (var d in dirs)
        {
            var start = obeliskPos + d;
            if (roads.ContainsKey(start))
            {
                frontier.Enqueue(start);
                visited.Add(start);
            }
        }

        // все дороги по умолчанию = false
        foreach (var kvp in roads)
            connected[kvp.Key] = false;

        while (frontier.Count > 0)
        {
            var cur = frontier.Dequeue();
            if (!roads.TryGetValue(cur, out var road)) continue;

            // помечаем как подключённую
            connected[cur] = true;
            road.isConnectedToObelisk = true;
            road.ApplyConnectionVisual();

            foreach (var n in dirs)
            {
                var next = cur + n;
                if (roads.ContainsKey(next) && visited.Add(next))
                    frontier.Enqueue(next);
            }
        }

        // дороги, до которых не дошли, помечаем как не подключённые
        foreach (var kvp in roads)
        {
            if (!connected[kvp.Key])
            {
                kvp.Value.isConnectedToObelisk = false;
                kvp.Value.ApplyConnectionVisual();
            }
        }
    }

    public bool IsConnectedToObelisk(Vector2Int roadCell)
    {
        return connected.TryGetValue(roadCell, out var value) && value;
    }

    // ==================== Updates ====================
    public void UpdateRoadAt(Vector2Int pos)
    {
        if (!roads.TryGetValue(pos, out var road))
        {
            roads.Remove(pos);
            return;
        }

        bool hasNW = HasAnyPlacedObjectAt(pos + Vector2Int.up);
        bool hasNE = HasAnyPlacedObjectAt(pos + Vector2Int.right);
        bool hasSE = HasAnyPlacedObjectAt(pos + Vector2Int.down);
        bool hasSW = HasAnyPlacedObjectAt(pos + Vector2Int.left);

        foreach (var n in dirs)
        {
            var neighbor = pos + n;
            if (BuildManager.Instance.gridManager.TryGetPlacedObject(neighbor, out var po) && po != null)
                BuildManager.Instance.CheckEffects(po);
        }

        road.UpdateRoadSprite(hasNW, hasNE, hasSE, hasSW);
        road.ApplyConnectionVisual();
    }

    public void RefreshRoadAndNeighbors(Vector2Int pos)
    {
        UpdateRoadAt(pos);
        foreach (var n in dirs)
            UpdateRoadAt(pos + n);
    }

    public void UpdateBuildingAccessAround(Vector2Int pos)
    {
        foreach (var n in NeighborsWithCenter(pos))
        {
            if (BuildManager.Instance.gridManager.TryGetPlacedObject(n, out var po))
            {
                if (po != null && !(po is Road))
                    BuildManager.Instance.CheckEffects(po);
            }
        }
    }

    // ==================== Obelisk ====================
    public void RegisterObelisk(Vector2Int pos)
    {
        obeliskPos = pos;
        hasObelisk = true;
        RecalculateConnections();
        // 🔹 сразу обновляем всё при появлении обелиска
    }
    private bool HasAnyPlacedObjectAt(Vector2Int cell)
    {
        // любая сущность, которая заняла клетку (и дороги, и здания)
        if (BuildManager.Instance == null || BuildManager.Instance.gridManager == null) return false;
        return BuildManager.Instance.gridManager.TryGetPlacedObject(cell, out var po) && po != null;
    }
    public void RefreshRoadsAroundArea(Vector2Int origin, int sizeX, int sizeY)
    {
        for (int dx = 0; dx < sizeX; dx++)
        for (int dy = 0; dy < sizeY; dy++)
        {
            var cell = origin + new Vector2Int(dx, dy);

            // обновим дороги рядом с каждой клеткой здания
            RefreshRoadAndNeighbors(cell);
        }
    }

    // ==================== Helpers ====================
    private IEnumerable<Vector2Int> NeighborsWithCenter(Vector2Int pos)
    {
        yield return pos;
        foreach (var d in dirs) yield return pos + d;
    }
}
