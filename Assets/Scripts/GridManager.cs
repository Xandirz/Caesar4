using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Map Size")]
    public int width = 20;
    public int height = 20;

    [Header("Tile Settings")]
    public Vector2Int tilePixels = new Vector2Int(64, 32);
    public int pixelsPerUnit = 64;
    public Vector2 worldOrigin = Vector2.zero;

    [Header("Tile Prefabs")]
    public GameObject groundPrefab;
    public GameObject forestPrefab;
    [Range(0, 1f)] public float forestChance = 0.2f;

    [Header("Grid Visuals")]
    public Color lineColor = Color.white;
    public float lineWidth = 0.02f;
    public Material lineMaterial;

    private float tileWidthUnits;
    private float tileHeightUnits;
    private float halfW, halfH;

    private bool[,] occupied;
    private Dictionary<Vector2Int, PlacedObject> placedObjects = new();
    private Dictionary<Vector2Int, GameObject> baseTiles = new(); // храним землю/лес

    private readonly List<LineRenderer> gridLines = new List<LineRenderer>();

    void Awake()
    {
        RecalcUnits();
        occupied = new bool[width, height];
    }

    void Start()
    {
        SpawnTiles();
        DrawIsoGrid();
    }

    void OnValidate()
    {
        RecalcUnits();
    }

    void RecalcUnits()
    {
        tileWidthUnits  = (float)tilePixels.x / Mathf.Max(1, pixelsPerUnit);
        tileHeightUnits = (float)tilePixels.y / Mathf.Max(1, pixelsPerUnit);
        halfW = tileWidthUnits * 0.5f;
        halfH = tileHeightUnits * 0.5f;
    }
// GridManager.cs
    public bool TryGetPlacedObject(Vector2Int cell, out PlacedObject po)
    {
        return placedObjects.TryGetValue(cell, out po);
    }

    // === Генерация карты (земля/лес) ===
    void SpawnTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 pos = CellToIsoWorld(cell);

                pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
                pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;

                bool isForest = Random.value < forestChance;
                GameObject prefab = isForest ? forestPrefab : groundPrefab;
                if (prefab == null) continue;

                GameObject tile = Instantiate(prefab, pos, Quaternion.identity, transform);

                
                
                if (tile.TryGetComponent<SpriteRenderer>(out var sr))
                    ApplySorting(cell, 1, 1, sr, isForest, false);
               


                baseTiles[cell] = tile;
            }
        }

        SpawnObelisk();
    }

    
    public void ApplySorting(Vector2Int cell, int sizeX, int sizeY, SpriteRenderer sr, bool isForest = false, bool isRoad = false)
    {
        sr.sortingLayerName = "World";

        int bottomY = cell.y + sizeY - 1;
        sr.sortingOrder = -(bottomY * 1000 + cell.x);

        if (isForest)
            sr.sortingOrder += 1;   // лес чуть выше травы

        if (isRoad)
            sr.sortingOrder -= 1;   // дорога всегда под зданиями и лесом
    }


    private void SpawnObelisk()
    {
        Vector2Int center = new Vector2Int(width / 2, height / 2);

        GameObject prefab = Resources.Load<GameObject>("Obelisk");
        if (prefab == null)
        {
            Debug.LogError("Obelisk prefab not found in Resources!");
            return;
        }

        Vector3 pos = CellToIsoWorld(center); // у тебя уже есть метод конвертации клеток в мировые координаты
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);

        var po = go.GetComponent<PlacedObject>();
        po.gridPos = center;
        po.manager = this;
        po.OnPlaced();

        SetOccupied(center, true, po);

        RoadManager.Instance.RegisterObelisk(center);
    }

    

    // === Замена базового тайла (grass/forest) ===
    // === Замена базового тайла (grass/forest) ===
    public void ReplaceBaseTile(Vector2Int pos, GameObject prefab)
    {
        if (baseTiles.TryGetValue(pos, out var oldTile))
        {
            if (oldTile != null) Destroy(oldTile);
            baseTiles.Remove(pos);
        }

        if (prefab != null)
        {
            Vector3 posWorld = CellToIsoWorld(pos);
            posWorld.x = Mathf.Round(posWorld.x * pixelsPerUnit) / pixelsPerUnit;
            posWorld.y = Mathf.Round(posWorld.y * pixelsPerUnit) / pixelsPerUnit;

            GameObject tile = Instantiate(prefab, posWorld, Quaternion.identity, transform);

            if (tile.TryGetComponent<SpriteRenderer>(out var sr))
                ApplySorting(pos, 1, 1, sr, prefab == forestPrefab, false);

            baseTiles[pos] = tile;
        }
    }


    // === Построение линий сетки через LineRenderer ===
    void DrawIsoGrid()
    {
        foreach (var lr in gridLines)
            if (lr != null) Destroy(lr.gameObject);
        gridLines.Clear();

        for (int x = 0; x <= width; x++)
        {
            Vector3 start = CellToIsoWorld(new Vector2Int(x, 0));
            Vector3 end   = CellToIsoWorld(new Vector2Int(x, height));
            CreateLine(start, end);
        }

        for (int y = 0; y <= height; y++)
        {
            Vector3 start = CellToIsoWorld(new Vector2Int(0, y));
            Vector3 end   = CellToIsoWorld(new Vector2Int(width, y));
            CreateLine(start, end);
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("GridLine");
        go.transform.parent = transform;

        var lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.sortingOrder = 10000; // линии поверх всего
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        gridLines.Add(lr);
    }

    // === Преобразования ===
    public Vector3 CellToIsoWorld(Vector2Int c)
    {
        float sx = worldOrigin.x + (c.x - c.y) * halfW;
        float sy = worldOrigin.y + (c.x + c.y) * halfH;
        return new Vector3(sx, sy, 0f);
    }

    public Vector2Int IsoWorldToCell(Vector3 w)
    {
        float wx = (w.x - worldOrigin.x) / halfW;
        float wy = (w.y - worldOrigin.y) / halfH;

        int x = Mathf.FloorToInt((wx + wy) * 0.5f);
        int y = Mathf.FloorToInt((wy - wx) * 0.5f);
        return new Vector2Int(x, y);
    }

    // === Логика занятости клеток ===
    private bool IsInsideMap(Vector2Int pos) =>
        pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    public bool IsCellFree(Vector2Int pos)
    {
        if (!IsInsideMap(pos)) return false;
        return !occupied[pos.x, pos.y];
    }

    public void SetOccupied(Vector2Int pos, bool value, PlacedObject obj = null)
    {
        if (IsInsideMap(pos))
        {
            occupied[pos.x, pos.y] = value;
            if (value && obj != null)
                placedObjects[pos] = obj;
            else if (!value)
                placedObjects.Remove(pos);
        }
    }
}
