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

    private readonly List<GameObject> staticHighlights = new();
    private readonly List<GameObject> hoverHighlights  = new();

    private PlacedObject hoveredObject;
    private BuildManager.BuildMode lastMode = BuildManager.BuildMode.None;
    private bool areaPreviewActive = false; // контроль радиуса превью

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

        // 🔥 режим сноса
        if (buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
        {
            if (hoveredObject != null && hoveredObject.TryGetComponent<SpriteRenderer>(out var oldSr))
                oldSr.color = Color.white;
            hoveredObject = null;

            gridManager.TryGetPlacedObject(cell, out var po);
            if (po != null && po.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.color = Color.red;
                hoveredObject = po;
            }

            ClearHoverHighlights();
            return;
        }

        // сбрасываем подсветку старого объекта
        if (hoveredObject != null && hoveredObject.TryGetComponent<SpriteRenderer>(out var resetSr))
            resetSr.color = Color.white;
        hoveredObject = null;

        // смена режима — пересоздаём подсветку
        if (buildManager.CurrentMode != lastMode)
        {
            lastMode = buildManager.CurrentMode;
            areaPreviewActive = false; // сбрасываем флаг

            ClearStaticHighlights();
            ClearHoverHighlights();

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
                    ShowBuildModeHighlights(highlightCells);
            }
        }

        if (buildManager.CurrentMode != BuildManager.BuildMode.None)
        {
            GameObject prefab = GetPrefabForCurrentMode();
            if (prefab == null) return;

            PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
            if (poPrefab == null) return;

            // если нет радиуса — динамическое превью
            if (poPrefab.buildEffectRadius == 0)
            {
                ClearHoverHighlights();
                CreateRectangleHighlight(cell, poPrefab.SizeX, poPrefab.SizeY);
            }
            else
            {
                // если есть радиус — рисуем один раз и оставляем
                if (!areaPreviewActive)
                {
                    ClearHoverHighlights();
                    CreateAreaPreview(cell, poPrefab.buildEffectRadius);
                    areaPreviewActive = true;
                }
            }

            // прозрачный спрайт поверх превью
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

    public void ClearHighlights()
    {
        ClearHoverHighlights();
        ClearStaticHighlights();
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

    void CreateRectangleHighlight(Vector2Int origin, int sizeX, int sizeY)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector2Int pos = origin + new Vector2Int(x, y);
                Vector3 worldPos = gridManager.CellToIsoWorld(pos);

                SpriteRenderer sr = Instantiate(highlightPrefab, worldPos, Quaternion.identity, transform);
                sr.color = gridManager.IsCellFree(pos) ? buildColor : cantBuildColor;
                hoverHighlights.Add(sr.gameObject);
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

    void CreateAreaPreview(Vector2Int centerCell, int radius)
    {
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
                hoverHighlights.Add(hl.gameObject);
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

    public void ShowBuildModeHighlights(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0) return;
        foreach (var c in cells)
        {
            Vector3 pos = gridManager.CellToIsoWorld(c);
            SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
            hl.color = new Color(1f, 0.9f, 0.4f, 0.45f);
            staticHighlights.Add(hl.gameObject);
        }
    }

    public void ShowEffectRadius(Vector2Int centerCell, int radius)
    {
        ClearHighlights();
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int c = new(centerCell.x + dx, centerCell.y + dy);
                Vector3 pos = gridManager.CellToIsoWorld(c);
                SpriteRenderer hl = Instantiate(highlightPrefab, pos, Quaternion.identity, transform);
                hl.color = (c == centerCell) ? centerHighlightColor : effectRadiusColor;
                hoverHighlights.Add(hl.gameObject);
            }
        }
    }
}
