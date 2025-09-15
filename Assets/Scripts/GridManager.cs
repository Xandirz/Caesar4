
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public Grid grid; 
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;

    private bool[,] occupied;

    // Новый словарь для хранения объектов на клетках
    private Dictionary<Vector2Int, PlacedObject> placedObjects = new Dictionary<Vector2Int, PlacedObject>();

    // Позиция и объект клетки для временной подсветки
    private Vector2Int? highlightedCell = null;
    private Color highlightColor = Color.clear;

    void Awake()
    {
        occupied = new bool[width, height];
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

    // Устанавливаем занятость клетки и обновляем словарь placedObjects
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

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3Int cell = grid.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y);
    }

    public Vector3 CellToWorld(Vector2Int pos)
    {
        return grid.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));
    }

    // Визуальное выделение клетки, цвет задается, например, синим
    public void HighlightCell(Vector2Int pos, Color color)
    {
        if (!IsInsideMap(pos)) return;
        highlightedCell = pos;
        highlightColor = color;
    }
    public void HighlightZone(Vector2Int center, int size, Color color)
    {
        if (size <= 1) return; // если размер меньше 2 — подсвечивать нечего

        Vector2Int rightCell = new Vector2Int(center.x + 1, center.y);
        Vector2Int leftCell = new Vector2Int(center.x - 1, center.y);
        HighlightCell(rightCell, color);
        HighlightCell(leftCell, color);
    }







    // Метод для очистки подсветки
    public void ClearHighlight()
    {
        highlightedCell = null;
        highlightColor = Color.clear;
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;

        Gizmos.color = Color.gray;

        for (int x = 0; x <= width; x++)
        {
            Vector3 start = grid.CellToWorld(new Vector3Int(x, 0, 0));
            Vector3 end = grid.CellToWorld(new Vector3Int(x, height, 0));
            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= height; y++)
        {
            Vector3 start = grid.CellToWorld(new Vector3Int(0, y, 0));
            Vector3 end = grid.CellToWorld(new Vector3Int(width, y, 0));
            Gizmos.DrawLine(start, end);
        }

        // Рисуем выделение одной клетки, если есть
        if (highlightedCell.HasValue)
        {
            Gizmos.color = highlightColor;
            Vector3 cellCenter = CellToWorld(highlightedCell.Value);
            float halfSize = cellSize / 2f;
            Vector3 size = new Vector3(cellSize, cellSize, 0.1f);
            Gizmos.DrawCube(cellCenter, size);
        }
    }
}