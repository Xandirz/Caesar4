using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Map Size")]
    public int width = 20;
    public int height = 20;

    [Header("Tile Settings")]
    public Vector2Int tilePixels = new Vector2Int(64, 32); // размер тайла в пикселях
    public int pixelsPerUnit = 32; // PPU как в настройках спрайтов
    public Vector2 worldOrigin = Vector2.zero;

    [Header("Grid Visuals")]
    public Color lineColor = Color.white;
    public float lineWidth = 0.02f;
    public Material lineMaterial;

    private float tileWidthUnits;
    private float tileHeightUnits;
    private float halfW, halfH;

    private bool[,] occupied;
    private Dictionary<Vector2Int, PlacedObject> placedObjects = new();

    private Vector2Int? highlightedCell = null;
    private Color highlightColor = Color.clear;

    private readonly List<LineRenderer> gridLines = new List<LineRenderer>();

    void Awake()
    {
        RecalcUnits();
        occupied = new bool[width, height];
    }

    void Start()
    {
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

    private bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

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

    public PlacedObject GetPlacedObjectAtCell(Vector2Int pos)
    {
        placedObjects.TryGetValue(pos, out var po);
        return po;
    }

    // === Изометрические преобразования ===
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


    // === Подсветка ===
    public void HighlightCell(Vector2Int pos, Color color)
    {
        if (!IsInsideMap(pos)) return;
        highlightedCell = pos;
        highlightColor = color;
    }

    public void HighlightZone(Vector2Int center, int size, Color color)
    {
        if (size <= 1) return;
        HighlightCell(center + Vector2Int.right, color);
        HighlightCell(center + Vector2Int.left,  color);
        HighlightCell(center + Vector2Int.up,    color);
        HighlightCell(center + Vector2Int.down,  color);
    }

    public void ClearHighlight()
    {
        highlightedCell = null;
        highlightColor = Color.clear;
    }

    // === Построение сетки через LineRenderer ===
    void DrawIsoGrid()
    {
        // очистка старых линий
        foreach (var lr in gridLines)
            if (lr != null) Destroy(lr.gameObject);
        gridLines.Clear();

        // вертикальные линии
        for (int x = 0; x <= width; x++)
        {
            Vector3 start = CellToIsoWorld(new Vector2Int(x, 0));
            Vector3 end   = CellToIsoWorld(new Vector2Int(x, height));
            CreateLine(start, end);
        }

        // горизонтальные линии
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
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        gridLines.Add(lr);
    }
}
