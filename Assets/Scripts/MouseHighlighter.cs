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
    public Color sameTypeColor = Color.magenta;
    public Color noiseRadiusColor = Color.red;

    [Header("Effect Radius Colors")]
    public Color effectRadiusColor = Color.cyan;
    public Color centerHighlightColor = Color.yellow;

    // Offsets (чем больше, тем "выше" поверх тайлов/зданий)
    private const int ORDER_STATIC = 600;   // подсветка уже построенных объектов
    private const int ORDER_AREA = 700;     // радиусы (effect/noise)
    private const int ORDER_CELL = 900;     // подсветка клетки(ок) под курсором
    private const int ORDER_GHOST = 1200;   // прозрачный спрайт (ghost)

    private readonly List<GameObject> staticHighlights = new();
    private readonly List<GameObject> hoverHighlights = new();
    private readonly List<GameObject> effectHighlights = new();

    private PlacedObject hoveredObject;
    private BuildManager.BuildMode lastMode = BuildManager.BuildMode.None;
    private BuildManager.BuildMode lastHighlightMode = BuildManager.BuildMode.None;

    private bool effectRadiusVisible = false;
    private Vector2Int lastEffectCenter;
    private int lastEffectRadius = 0;

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
        if (gridManager == null || buildManager == null || highlightPrefab == null)
            return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        mouseWorld = SnapToPixels(mouseWorld);

        Vector2Int cell = gridManager.IsoWorldToCell(mouseWorld);

        // === 🔥 РЕЖИМ СНОСА ===
        if (buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
        {
            if (Input.GetMouseButton(0))
            {
                if (buildManager.dragStartCell != Vector2Int.zero)
                    HighlightRectangle(buildManager.dragStartCell, cell, demolishColor);
                return;
            }

            // Подсветка одного здания при наведении (как было)
            if (gridManager.TryGetPlacedObject(cell, out var po) && po != null)
            {
                if (hoveredObject != po)
                {
                    if (hoveredObject != null && hoveredObject.TryGetComponent<SpriteRenderer>(out var oldSr))
                        oldSr.color = Color.white;

                    hoveredObject = po;
                    if (po.TryGetComponent<SpriteRenderer>(out var sr))
                        sr.color = Color.red;
                }
            }
            else if (hoveredObject != null)
            {
                if (hoveredObject.TryGetComponent<SpriteRenderer>(out var sr))
                    sr.color = Color.white;
                hoveredObject = null;
            }

            return;
        }

        // === Сброс цвета при переходе в другие режимы ===
        if (hoveredObject != null && hoveredObject.TryGetComponent<SpriteRenderer>(out var resetSr))
            resetSr.color = Color.white;
        hoveredObject = null;

        // === При смене режима ===
        if (buildManager.CurrentMode != lastMode)
        {
            lastMode = buildManager.CurrentMode;
            ClearStaticHighlights();
            ClearHoverHighlights();
            ClearEffectHighlights();
            effectRadiusVisible = false;

            if (buildManager.CurrentMode != BuildManager.BuildMode.None && AllBuildingsManager.Instance != null)
            {
                List<Vector2Int> highlightCells = new();
                foreach (var building in AllBuildingsManager.Instance.GetAllBuildings())
                {
                    if (building == null) continue;
                    if (building.BuildMode == buildManager.CurrentMode)
                        highlightCells.AddRange(building.GetOccupiedCells());
                }

                if (highlightCells.Count > 0)
                    ShowBuildModeHighlights(highlightCells, buildManager.CurrentMode);
            }
        }

        // === 🔹 Режим строительства ===
        if (buildManager.CurrentMode != BuildManager.BuildMode.None)
        {
            GameObject prefab = GetPrefabForCurrentMode();
            if (prefab == null) return;

            PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
            if (poPrefab == null) return;

            ClearHoverHighlights();

            if (poPrefab.buildEffectRadius == 0)
                CreateRectangleHighlight(cell, poPrefab);
            else
                CreateAreaPreview(cell, poPrefab.buildEffectRadius, effectRadiusColor);

            // 🔴 ПРЕДПРОСМОТР ШУМА
            var prodPrefab = poPrefab as ProductionBuilding;
            if (prodPrefab != null && prodPrefab.isNoisy)
                CreateAreaPreview(cell, prodPrefab.noiseRadius, noiseRadiusColor);

            // Ghost (прозрачный спрайт здания поверх)
            SpawnGhost(cell, poPrefab);
        }
        else
        {
            ClearHoverHighlights();
        }
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

    private Vector3 SnapToPixels(Vector3 w)
    {
        w.x = Mathf.Round(w.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
        w.y = Mathf.Round(w.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
        return w;
    }

    /// <summary>
    /// Единая точка создания подсветки с корректным Sorting.
    /// ВАЖНО: orderOffset должен быть > 0, чтобы лежать поверх тайла клетки.
    /// </summary>
    private SpriteRenderer SpawnHighlight(Vector2Int cell, Vector3 worldPos, Color color, int orderOffset, List<GameObject> bucket)
    {
        var sr = Instantiate(highlightPrefab, worldPos, Quaternion.identity, transform);
        sr.color = color;

        sr.sortingLayerName = "World";
        sr.sortingOrder = gridManager.GetBaseSortOrder(cell) + orderOffset;

        bucket.Add(sr.gameObject);
        return sr;
    }

    private void SpawnGhost(Vector2Int cell, PlacedObject poPrefab)
    {
        Vector3 pos = gridManager.CellToIsoWorld(cell);
        pos = SnapToPixels(pos);

        if (!poPrefab.TryGetComponent<SpriteRenderer>(out var prefabSr))
            return;

        SpriteRenderer icon = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);

        icon.sortingLayerName = "World";
        icon.sortingOrder = gridManager.GetBaseSortOrder(cell) + ORDER_GHOST;

        if (poPrefab is Road roadPrefab)
            icon.sprite = roadPrefab.Road_LeftRight;
        else
            icon.sprite = prefabSr.sprite;

        icon.color = new Color(1f, 1f, 1f, 0.5f);
        hoverHighlights.Add(icon.gameObject);
    }

    public void ClearHighlights()
    {
        ClearHoverHighlights();
        ClearStaticHighlights();
        ClearEffectHighlights();
        effectRadiusVisible = false;
    }

    public void HighlightRectangle(Vector2Int start, Vector2Int end, Color color)
    {
        ClearHoverHighlights();

        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int c = new(x, y);
                Vector3 worldPos = gridManager.CellToIsoWorld(c);
                SpawnHighlight(c, worldPos, color, ORDER_CELL, hoverHighlights);
            }
        }
    }

    // (оставляем как было — не используем, но не удаляем)
    private void ShowNoisePreview(Vector2Int centerCell, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Vector2Int c = new(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                var hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = noiseRadiusColor;

                // FIX: чтобы не пропадало на больших Y
                hl.sortingLayerName = "World";
                hl.sortingOrder = gridManager.GetBaseSortOrder(c) + ORDER_AREA;

                effectHighlights.Add(hl.gameObject);
            }
        }
    }

    public void ClearHoverHighlights()
    {
        foreach (var hl in hoverHighlights)
            if (hl != null) Destroy(hl);
        hoverHighlights.Clear();
    }

    public void ClearStaticHighlights()
    {
        foreach (var hl in staticHighlights)
            if (hl != null) Destroy(hl);
        staticHighlights.Clear();
    }

    public void ClearEffectHighlights()
    {
        foreach (var hl in effectHighlights)
            if (hl != null) Destroy(hl);
        effectHighlights.Clear();
    }

    void CreateRectangleHighlight(Vector2Int origin, PlacedObject poPrefab)
    {
        int sizeX = poPrefab.SizeX;
        int sizeY = poPrefab.SizeY;

        bool adjacencyOk = buildManager.IsAdjacencyOk(poPrefab, origin);

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector2Int c = origin + new Vector2Int(x, y);
                Vector3 worldPos = gridManager.CellToIsoWorld(c);

                bool free = gridManager.IsCellFree(c);
                Color col = (free && adjacencyOk) ? buildColor : cantBuildColor;

                SpawnHighlight(c, worldPos, col, ORDER_CELL, hoverHighlights);
            }
        }
    }

    // универсальный предпросмотр зоны (effect/noise)
    void CreateAreaPreview(Vector2Int centerCell, int radius, Color areaColor)
    {
        ClearEffectHighlights();

        // центр (в hover)
        Vector3 centerPos = gridManager.CellToIsoWorld(centerCell);
        Color centerColor = gridManager.IsCellFree(centerCell) ? buildColor : cantBuildColor;
        SpawnHighlight(centerCell, centerPos, centerColor, ORDER_CELL, hoverHighlights);

        // радиус (в effect)
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector2Int c = new(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                SpawnHighlight(c, pos, areaColor, ORDER_AREA, effectHighlights);
            }
        }
    }

    public void CreateSingleHighlight(Vector2Int cell)
    {
        Vector3 center = gridManager.CellToIsoWorld(cell);

        Color col;
        if (buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
            col = demolishColor;
        else
            col = gridManager.IsCellFree(cell) ? buildColor : cantBuildColor;

        SpawnHighlight(cell, center, col, ORDER_CELL, hoverHighlights);
    }

    public void ShowBuildModeHighlights(List<Vector2Int> cells, BuildManager.BuildMode mode, List<Vector2Int> selectedCells = null)
    {
        ClearStaticHighlights();

        foreach (var c in cells)
        {
            Vector3 pos = gridManager.CellToIsoWorld(c);
            SpawnHighlight(c, pos, sameTypeColor, ORDER_STATIC, staticHighlights);
        }

        if (selectedCells != null)
        {
            foreach (var c in selectedCells)
            {
                Vector3 pos = gridManager.CellToIsoWorld(c);
                SpawnHighlight(c, pos, centerHighlightColor, ORDER_STATIC + 50, staticHighlights);
            }
        }
    }

    GameObject GetPrefabForCurrentMode()
    {
        if (buildManager == null) return null;

        // если у тебя CurrentMode = BuildManager.BuildMode
        return BuildManager.Instance != null 
            ? BuildManager.Instance.GetPrefabByMode(buildManager.CurrentMode)
            : buildManager.GetPrefabByMode(buildManager.CurrentMode);
    }

    public void ToggleEffectRadius(Vector2Int centerCell, int radius)
    {
        if (effectRadiusVisible && centerCell == lastEffectCenter && radius == lastEffectRadius)
        {
            ClearEffectHighlights();
            effectRadiusVisible = false;
            return;
        }

        ShowEffectRadius(centerCell, radius);
        effectRadiusVisible = true;
        lastEffectCenter = centerCell;
        lastEffectRadius = radius;
    }

    public void ShowEffectRadius(Vector2Int centerCell, int radius)
    {
        ClearEffectHighlights();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int c = new(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                Color col = (c == centerCell) ? centerHighlightColor : effectRadiusColor;
                SpawnHighlight(c, pos, col, ORDER_AREA, effectHighlights);
            }
        }

        effectRadiusVisible = true;
        lastEffectCenter = centerCell;
        lastEffectRadius = radius;
    }

    // В MouseHighlighter.cs — рядом с ShowEffectRadius(...)
    public void ShowNoiseRadius(Vector2Int centerCell, int radius)
    {
        ClearEffectHighlights();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int c = new(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                Color col = (c == centerCell) ? centerHighlightColor : noiseRadiusColor;
                SpawnHighlight(c, pos, col, ORDER_AREA, effectHighlights);
            }
        }

        effectRadiusVisible = true;
        lastEffectCenter = centerCell;
        lastEffectRadius = radius;
    }
}
