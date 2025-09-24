using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    private Dictionary<Vector2Int, Road> roads = new();
    private Vector2Int obeliskPos;
    private bool hasObelisk = false;

    public static RoadManager Instance { get; private set; }

    private static readonly Vector2Int[] dirs = 
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==================== Roads ====================
    public void RegisterRoad(Vector2Int pos, Road road)
    {
        if (!roads.ContainsKey(pos))
            roads[pos] = road;

        RecalculateConnections(pos);
    }

    public void UnregisterRoad(Vector2Int pos)
    {
        if (roads.Remove(pos))
        {
            foreach (var n in dirs)
            {
                var neighbor = pos + n;
                if (roads.ContainsKey(neighbor))
                {
                    RecalculateConnections(neighbor);
                    break;
                }
            }
        }
    }

    public bool IsRoadAt(Vector2Int pos) => roads.ContainsKey(pos);

    // ==================== Connections ====================
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
            if (!roads.TryGetValue(cur, out var road)) continue;

            road.isConnectedToObelisk = IsConnectedToObelisk(cur);
            UpdateRoadAt(cur);

            foreach (var n in dirs)
            {
                var next = cur + n;
                if (roads.ContainsKey(next) && visited.Add(next))
                    frontier.Enqueue(next);
            }
        }
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
            foreach (var n in dirs)
                if (cur + n == obeliskPos) return true;

            foreach (var n in dirs)
            {
                var next = cur + n;
                if (roads.ContainsKey(next) && visited.Add(next))
                    frontier.Enqueue(next);
            }
        }

        return false;
    }

    // ==================== Updates ====================
    public void UpdateRoadAt(Vector2Int pos)
    {
        if (!roads.TryGetValue(pos, out var road))
        {
            roads.Remove(pos);
            return;
        }

        // NW, NE, SE, SW → соответствуют up, right, down, left
        bool hasNW = IsRoadAt(pos + Vector2Int.up);
        bool hasNE = IsRoadAt(pos + Vector2Int.right);
        bool hasSE = IsRoadAt(pos + Vector2Int.down);
        bool hasSW = IsRoadAt(pos + Vector2Int.left);

       
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
    }

    // ==================== Helpers ====================
    private IEnumerable<Vector2Int> NeighborsWithCenter(Vector2Int pos)
    {
        yield return pos;
        foreach (var d in dirs) yield return pos + d;
    }
}
