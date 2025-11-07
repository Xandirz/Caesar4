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
        Vector2Int cell = gridManager.IsoWorldToCell(mouseWorld);

        // === 🔥 РЕЖИМ СНОСА ===
        if (buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
        {
            // 🔴 выделение области сноса при зажатой ЛКМ
            if (Input.GetMouseButton(0))
            {
                if (buildManager.dragStartCell != Vector2Int.zero)
                {
                    HighlightRectangle(buildManager.dragStartCell, cell, demolishColor);
                }
                return;
            }

            // 🔴 Подсветка одного здания при наведении
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

            return; // не трогаем другие подсветки
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
            {
                CreateRectangleHighlight(cell, poPrefab);

            }
            else
            {
                CreateAreaPreview(cell, poPrefab.buildEffectRadius);
            }

            // 🔴 ПРЕДПРОСМОТР ШУМА: тем же методом, но красным
            var prodPrefab = poPrefab as ProductionBuilding;
            if (prodPrefab != null && prodPrefab.isNoisy)
            {
                CreateAreaPreview(cell, prodPrefab.noiseRadius, noiseRadiusColor);
            }

            // прозрачный спрайт здания поверх
            Vector3 pos = gridManager.CellToIsoWorld(cell);
            pos.x = Mathf.Round(pos.x * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;
            pos.y = Mathf.Round(pos.y * gridManager.pixelsPerUnit) / gridManager.pixelsPerUnit;

            if (poPrefab.TryGetComponent<SpriteRenderer>(out var prefabSr))
            {
                SpriteRenderer icon = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                if (poPrefab is Road roadPrefab)
                    icon.sprite = roadPrefab.Road_LeftRight;
                else
                    icon.sprite = prefabSr.sprite;

                icon.color = new Color(1f, 1f, 1f, 0.5f);
                hoverHighlights.Add(icon.gameObject);
            }
        }
        else
        {
            ClearHoverHighlights();
        }
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

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
                Vector2Int cell = new(x, y);
                Vector3 worldPos = gridManager.CellToIsoWorld(cell);

                SpriteRenderer hl = Instantiate(highlightPrefab, worldPos, Quaternion.identity, transform);
                hl.color = color;
                hoverHighlights.Add(hl.gameObject);
            }
        }
    }

    // (оставляем как было — не используем, но не удаляем)
    private void ShowNoisePreview(Vector2Int centerCell, int radius)
    {
        // не чистим hover, это просто доп. слой поверх прямоугольника
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Vector2Int c = new(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                var hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = noiseRadiusColor;
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

        // один раз проверяем доп. условия (вода/дом)
        bool adjacencyOk = buildManager.IsAdjacencyOk(poPrefab, origin);

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector2Int cell = origin + new Vector2Int(x, y);
                Vector3 worldPos = gridManager.CellToIsoWorld(cell);

                var hl = Instantiate(highlightPrefab, worldPos, Quaternion.identity, transform);
                bool free = gridManager.IsCellFree(cell);
                hl.color = (free && adjacencyOk) ? buildColor : cantBuildColor;
                hoverHighlights.Add(hl.gameObject);
            }
        }
    }

    // оригинальный предпросмотр (бирюзовый)
    void CreateAreaPreview(Vector2Int centerCell, int radius)
    {
        ClearEffectHighlights();

        Vector3 centerPos = gridManager.CellToIsoWorld(centerCell);
        SpriteRenderer centerHl = Instantiate(highlightPrefab, centerPos, Quaternion.identity, transform);
        centerHl.color = gridManager.IsCellFree(centerCell) ? buildColor : cantBuildColor;
        hoverHighlights.Add(centerHl.gameObject);

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector2Int c = new(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = effectRadiusColor;
                effectHighlights.Add(hl.gameObject);
            }
        }
    }

    // перегруз для произвольного цвета зоны (используем для шума — красный)
    void CreateAreaPreview(Vector2Int centerCell, int radius, Color areaColor)
    {
        ClearEffectHighlights();

        Vector3 centerPos = gridManager.CellToIsoWorld(centerCell);
        SpriteRenderer centerHl = Instantiate(highlightPrefab, centerPos, Quaternion.identity, transform);
        centerHl.color = gridManager.IsCellFree(centerCell) ? buildColor : cantBuildColor;
        hoverHighlights.Add(centerHl.gameObject);

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector2Int c = new(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);

                SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = areaColor; // ← ключевая разница
                effectHighlights.Add(hl.gameObject);
            }
        }
    }

    public void CreateSingleHighlight(Vector2Int cell)
    {
        Vector3 center = gridManager.CellToIsoWorld(cell);
        SpriteRenderer sr = Instantiate(highlightPrefab, center, Quaternion.identity, transform);
        if (buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
            sr.color = demolishColor;
        else
            sr.color = gridManager.IsCellFree(cell) ? buildColor : cantBuildColor;
        hoverHighlights.Add(sr.gameObject);
    }

    public void ShowBuildModeHighlights(List<Vector2Int> cells, BuildManager.BuildMode mode, List<Vector2Int> selectedCells = null)
    {
        ClearStaticHighlights();

        // — остальные здания (мягкий жёлтый)
        foreach (var c in cells)
        {
            Vector3 pos = gridManager.CellToIsoWorld(c);
            SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
            hl.color = sameTypeColor;
            staticHighlights.Add(hl.gameObject);
        }

        // — выбранное здание (другой цвет)
        if (selectedCells != null)
        {
            foreach (var c in selectedCells)
            {
                Vector3 pos = gridManager.CellToIsoWorld(c);
                SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = centerHighlightColor; 
                staticHighlights.Add(hl.gameObject);
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
                SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = (c == centerCell) ? centerHighlightColor : effectRadiusColor;
                effectHighlights.Add(hl.gameObject);
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
                SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = (c == centerCell) ? centerHighlightColor : noiseRadiusColor; // центр — жёлтый, радиус — красный
                effectHighlights.Add(hl.gameObject);
            }
        }

        effectRadiusVisible = true;
        lastEffectCenter = centerCell;
        lastEffectRadius = radius;
    }

}
