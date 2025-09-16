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

        if (buildManager.CurrentMode != BuildManager.BuildMode.None)
        {
            ClearHighlights();

            // получаем префаб для текущего режима
            GameObject prefab = GetPrefabForCurrentMode();
            if (prefab == null) return;

            PlacedObject poPrefab = prefab.GetComponent<PlacedObject>();
            int radius = poPrefab != null ? poPrefab.buildEffectRadius : 0;

            if (radius > 0)
                CreateAreaPreview(cell, radius);
            else
                CreateSingleHighlight(cell);
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
    void CreateSingleHighlight(Vector2Int cell)
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
