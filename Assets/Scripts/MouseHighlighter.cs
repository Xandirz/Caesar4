using System.Collections.Generic;
using UnityEngine;

public class MouseHighlighter : MonoBehaviour
{
    public static MouseHighlighter Instance { get; private set; }

    public GridManager gridManager;
    public BuildManager buildManager;

    [Header("Highlight Prefab")]
    public SpriteRenderer highlightPrefab;

    [Header("Colors")]
    public Color buildColor = Color.green;
    public Color cantBuildColor = Color.red;
    public Color demolishColor = Color.yellow;

    [Header("Effect Radius Colors")]
    public Color effectRadiusColor = Color.cyan;   
    public Color centerHighlightColor = Color.yellow; 

    private List<GameObject> activeHighlights = new List<GameObject>();

    private PlacedObject hoveredObject;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

   void Update()
{
    if (gridManager == null || buildManager == null || highlightPrefab == null) return;

    Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    mouseWorld.z = 0f;
    Vector2Int cell = gridManager.IsoWorldToCell(mouseWorld);

    // 🔥 Режим сноса
    if (buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
    {
        // если ранее был выделен объект — сбрасываем ему цвет
        if (hoveredObject != null && hoveredObject.TryGetComponent<SpriteRenderer>(out var oldSr))
            oldSr.color = Color.white;
        hoveredObject = null;

        gridManager.TryGetPlacedObject(cell, out var po);
        if (po != null && po.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.red;
            hoveredObject = po;
        }

        return;
    }



    // --- если не Demolish и не Upgrade ---
    if (hoveredObject != null && hoveredObject.TryGetComponent<SpriteRenderer>(out var resetSr))
        resetSr.color = Color.white;
    hoveredObject = null;

    // 🔥 Логика подсветки для режимов строительства
    if (buildManager.CurrentMode != BuildManager.BuildMode.None)
    {
        ClearHighlights();

        GameObject prefab = GetPrefabForCurrentMode();
        if (prefab == null) return;

        PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
        if (poPrefab == null) return;

        // если есть зона действия → круг
        if (poPrefab.buildEffectRadius > 0 && poPrefab.SizeX == 1 && poPrefab.SizeY == 1)
        {
            CreateAreaPreview(cell, poPrefab.buildEffectRadius);
        }
        else
        {
            CreateRectangleHighlight(cell, poPrefab.SizeX, poPrefab.SizeY);
        }

        Vector3 pos = gridManager.CellToIsoWorld(cell);
        pos.x = Mathf.Round(pos.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
        pos.y = Mathf.Round(pos.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;

        if (poPrefab.TryGetComponent<SpriteRenderer>(out var prefabSr))
        {
            SpriteRenderer icon = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
            if (poPrefab is Road)
            {
                Road roadPrefab = (Road)poPrefab;
                icon.sprite = roadPrefab.Road_LeftRight;
            }
            else
            {
                icon.sprite = prefabSr.sprite;
            }
            icon.color = new Color(1f, 1f, 1f, 0.5f);
            activeHighlights.Add(icon.gameObject);
        }
    }
}



   public void HighlightRectangle(Vector2Int start, Vector2Int end, Color color)
   {
       ClearHighlights();
       int minX = Mathf.Min(start.x, end.x);
       int maxX = Mathf.Max(start.x, end.x);
       int minY = Mathf.Min(start.y, end.y);
       int maxY = Mathf.Max(start.y, end.y);

       for (int x = minX; x <= maxX; x++)
       for (int y = minY; y <= maxY; y++)
           CreateSingleHighlight(new Vector2Int(x, y));
   }
   
   
    // Подсветка прямоугольника под здание
    void CreateRectangleHighlight(Vector2Int origin, int sizeX, int sizeY)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector2Int pos = origin + new Vector2Int(x, y);
                Vector3 worldPos = gridManager.CellToIsoWorld(pos);

                SpriteRenderer sr = Instantiate(highlightPrefab, worldPos, Quaternion.identity, transform);

                if (!gridManager.IsCellFree(pos))
                    sr.color = cantBuildColor;
                else
                    sr.color = buildColor;

                activeHighlights.Add(sr.gameObject);
            }
        }
    }



    GameObject GetPrefabForCurrentMode()
    {
        foreach (var p in buildManager.buildingPrefabs)
        {
            var po = p.GetComponent<PlacedObject>();
            if (po != null && po.BuildMode == buildManager.CurrentMode)
                return p;
        }
        return null;
    }

    public void ClearHighlights()
    {
        foreach (var hl in activeHighlights)
        {
            if (hl != null)
                Destroy(hl);
        }
        activeHighlights.Clear();
    }

    // === Универсальное превью для зданий с зоной ===
    void CreateAreaPreview(Vector2Int centerCell, int radius)
    {
        // центр
        Vector3 centerPos = gridManager.CellToIsoWorld(centerCell);
        SpriteRenderer centerHl = Instantiate(highlightPrefab, centerPos, Quaternion.identity, transform);

        if (!gridManager.IsCellFree(centerCell))
            centerHl.color = cantBuildColor;
        else
            centerHl.color = buildColor;

        activeHighlights.Add(centerHl.gameObject);

        // радиус вокруг
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector2Int c = new Vector2Int(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = effectRadiusColor;

                activeHighlights.Add(hl.gameObject);
            }
        }
    }

    // === Подсветка одной клетки ===
    public void CreateSingleHighlight(Vector2Int cell)
    {
        Vector3 center = gridManager.CellToIsoWorld(cell);
        SpriteRenderer sr = Instantiate(highlightPrefab, center, Quaternion.identity, transform);

        if (buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
            sr.color = demolishColor;

        else if (!gridManager.IsCellFree(cell))
            sr.color = cantBuildColor;
        else
            sr.color = buildColor;

        activeHighlights.Add(sr.gameObject);
    }

    // === Подсветка зоны вокруг готового здания (например, Well) ===
    public void ShowEffectRadius(Vector2Int centerCell, int radius)
    {
        ClearHighlights();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int c = new Vector2Int(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);

                if (c == centerCell)
                    hl.color = centerHighlightColor;
                else
                    hl.color = effectRadiusColor;

                activeHighlights.Add(hl.gameObject);
            }
        }
    }
}
