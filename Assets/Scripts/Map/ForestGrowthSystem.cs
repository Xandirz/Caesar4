using UnityEngine;

public class ForestGrowthSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private AllBuildingsManager allBuildingsManager;

    [Header("Spread Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float chancePerTry = 0.15f;
    [SerializeField] private int triesPerTick = 10;

    [Header("Fallback (no forest on map)")]
    [Range(0f, 1f)]
    [SerializeField] private float fallbackSpawnChance = 0.2f;

    [Header("Rules")]
    [SerializeField] private bool onlyIfCellIsFree = true;
    [SerializeField] private bool avoidNearBuildings = false;
    [SerializeField] private int avoidRadius = 1;

    [Header("Neighborhood")]
    [SerializeField] private bool use8Neighbors = false; // ← то, о чём ты спросил

    private static readonly Vector2Int[] Neigh4 =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private static readonly Vector2Int[] Neigh8 =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1, 1), new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private void OnEnable()
    {
        if (allBuildingsManager != null)
            allBuildingsManager.OnEconomyTick += OnTick;
    }

    private void OnDisable()
    {
        if (allBuildingsManager != null)
            allBuildingsManager.OnEconomyTick -= OnTick;
    }

    public void OnTick()
    {
        if (grid == null || grid.forestPrefab == null || grid.groundPrefab == null)
            return;

        // === 1️⃣ Пробуем распространение от существующего леса ===
        for (int i = 0; i < triesPerTick; i++)
        {
            if (!TryPickRandomForestCell(out Vector2Int forestCell))
                break; // леса нет → fallback

            if (!TryFindFreeGroundNeighbor(forestCell, out Vector2Int growCell))
                continue;

            if (Random.value > chancePerTry)
                continue;

            grid.ReplaceBaseTile(growCell, grid.forestPrefab);
            return; // ✅ максимум 1 дерево за тик
        }

        // === 2️⃣ Fallback: если леса вообще нет ===
        if (Random.value > fallbackSpawnChance)
            return;

        TrySpawnForestSeed();
    }

    private bool TryPickRandomForestCell(out Vector2Int cell)
    {
        int w = grid.width;
        int h = grid.height;

        for (int i = 0; i < 30; i++)
        {
            Vector2Int c = new Vector2Int(Random.Range(0, w), Random.Range(0, h));
            if (grid.IsForestBaseTile(c))
            {
                cell = c;
                return true;
            }
        }

        cell = default;
        return false;
    }

    private bool TryFindFreeGroundNeighbor(Vector2Int forestCell, out Vector2Int growCell)
    {
        var dirs = use8Neighbors ? Neigh8 : Neigh4;

        // перемешиваем направления
        for (int i = 0; i < dirs.Length; i++)
        {
            int j = Random.Range(i, dirs.Length);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }

        foreach (var d in dirs)
        {
            Vector2Int c = forestCell + d;
            if (CanGrowOn(c))
            {
                growCell = c;
                return true;
            }
        }

        growCell = default;
        return false;
    }

    private void TrySpawnForestSeed()
    {
        int w = grid.width;
        int h = grid.height;

        for (int i = 0; i < 20; i++)
        {
            Vector2Int cell = new Vector2Int(Random.Range(0, w), Random.Range(0, h));
            if (!CanGrowOn(cell))
                continue;

            grid.ReplaceBaseTile(cell, grid.forestPrefab);
            return;
        }
    }

    private bool CanGrowOn(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= grid.width || cell.y < 0 || cell.y >= grid.height)
            return false;

        if (!grid.IsGroundBaseTile(cell))
            return false;

        if (grid.HasAnyPlacedObject(cell))
            return false;

        if (onlyIfCellIsFree && !grid.IsCellFree(cell))
            return false;

        if (avoidNearBuildings && IsNearAnyPlacedObject(cell, avoidRadius))
            return false;

        return true;
    }

    private bool IsNearAnyPlacedObject(Vector2Int cell, int r)
    {
        for (int dx = -r; dx <= r; dx++)
        for (int dy = -r; dy <= r; dy++)
        {
            Vector2Int p = cell + new Vector2Int(dx, dy);
            if (grid.HasAnyPlacedObject(p))
                return true;
        }
        return false;
    }
}
