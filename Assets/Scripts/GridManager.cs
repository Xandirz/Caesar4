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
    [Header("Water")]
    public GameObject waterPrefab;
    public bool waterOnLastColumn = true;
    private readonly HashSet<Vector2Int> waterCells = new HashSet<Vector2Int>();

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

    
    private HashSet<Vector2Int> connectedRoads = new HashSet<Vector2Int>();
    private Vector2Int? obeliskPos;
    public event System.Action OnRoadNetworkChanged;
    
    
    
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
// Полная версия метода с водой в последнем столбце X
    // Обновлённый SpawnTiles:
    void SpawnTiles()
    {
        // сбрасываем кэш воды на случай пересоздания карты
        waterCells.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 pos = CellToIsoWorld(cell);

                pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
                pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;

                // 👉 если это последний столбец по X — ставим воду
                GameObject prefab = null;
                bool isForest = false;

                if (waterOnLastColumn && x == width - 1 && waterPrefab != null)
                {
                    prefab = waterPrefab;
                }
                else
                {
                    isForest = Random.value < forestChance;
                    prefab = isForest ? forestPrefab : groundPrefab;
                }

                if (prefab == null) continue;

                GameObject tile = Instantiate(prefab, pos, Quaternion.identity, transform);

                if (tile.TryGetComponent<SpriteRenderer>(out var sr))
                {
                    // для воды isForest = false, чтобы не применять «лесную» сортировку
                    ApplySorting(cell, 1, 1, sr, isForest, false);
                }

                baseTiles[cell] = tile;

                // ✅ отмечаем воду
                if (prefab == waterPrefab)
                {
                    waterCells.Add(cell);       // <— добавили клетку в набор воды
                    SetOccupied(cell, true);    // (по желанию) делаем непроходимой/незастраиваемой
                }
            }
        }

        SpawnObelisk();
    }



    public bool IsWaterCell(Vector2Int cell) => waterCells.Contains(cell);
 


    
// Статический (для дорог, зданий, деревьев и т.д.)
    // === Статический (дороги, здания и т.д.) ===
    public void ApplySorting(Vector2Int cell, int sizeX, int sizeY, SpriteRenderer sr, bool isForest = false, bool isRoad = false)
    {
        sr.sortingLayerName = "World";

        int bottomY = cell.y + sizeY - 1;
        sr.sortingOrder = -(bottomY * 1000 + cell.x);
    }

    
// Динамический (для людей, животных и т.д.)
    // === Динамический (люди, животные) ===
    public void ApplySortingDynamic(Vector3 worldPos, SpriteRenderer sr)
    {
        sr.sortingLayerName = "World";

        // Сдвигаем pivot к ногам
        float footOffset = halfH * 0.9f;
        float adjustedY = worldPos.y - footOffset;

        // Изометрические координаты
        float wx = (worldPos.x - worldOrigin.x) / halfW;
        float wy = (adjustedY - worldOrigin.y) / halfH;

        float gx = (wx + wy) * 0.5f;
        float gy = (wy - wx) * 0.5f;

        int rowY   = Mathf.FloorToInt(gy);
        float frac = gy - rowY;
        int gridX  = Mathf.FloorToInt(gx);

        int baseOrder = -(rowY * 1000 + gridX);
        int interp    = Mathf.RoundToInt(frac * 1000f);

        // 🔧 вычисляем динамический оффсет относительно изометрической геометрии
        // Пропорция: чем "высше" тайл (tileHeightUnits / tileWidthUnits), тем больший сдвиг нужен
        int humanAboveRoadOffset = Mathf.RoundToInt((tileHeightUnits / tileWidthUnits) * 1000f * 0.3f);
        // 0.3f — коэффициент «высоты спрайта» (подбирается один раз, обычно 0.25–0.4)

        sr.sortingOrder = baseOrder - interp + humanAboveRoadOffset;
    }




    
    
   public float cellWidth = 1f;   // ширина изометрической клетки
    public float cellHeight = 0.5f; // высота изометрической клетки

    // Конвертирует координаты сетки в мировые (центр клетки)
    public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
    {
        float worldX = (gridPos.x - gridPos.y) * (cellWidth / 2f);
        float worldY = (gridPos.x + gridPos.y) * (cellHeight / 2f);
        return new Vector3(worldX, worldY, 0);
    }

    // Обратная конвертация: из мира в сетку
    public Vector2Int GetGridPositionFromWorld(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x / (cellWidth / 2f) + worldPos.y / (cellHeight / 2f)) / 2f);
        int y = Mathf.RoundToInt((worldPos.y / (cellHeight / 2f) - worldPos.x / (cellWidth / 2f)) / 2f);
        return new Vector2Int(x, y);
    }

    // Реальное построение пути по соединённым дорогам
    public List<Vector3> GetRoadPath()
    {
        HashSet<Vector2Int> roadTiles = new HashSet<Vector2Int>();
        foreach (var kvp in placedObjects)
        {
            if (kvp.Value != null && kvp.Value.name.Contains("Road"))
                roadTiles.Add(kvp.Key);
        }

        if (roadTiles.Count < 2)
            return new List<Vector3>();

        // Находим первую дорогу
        Vector2Int start = GetRandomRoadTile(roadTiles);

        // BFS по соседним дорогам
        List<Vector2Int> visited = new List<Vector2Int>();
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(start);
        visited.Add(start);

        Vector2Int[] dirs = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (q.Count > 0)
        {
            Vector2Int cur = q.Dequeue();
            foreach (var d in dirs)
            {
                Vector2Int next = cur + d;
                if (roadTiles.Contains(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    q.Enqueue(next);
                }
            }
        }

        // Переводим путь из сетки в мир
        List<Vector3> path = new List<Vector3>();
        foreach (var cell in visited)
        {
            Vector3 worldPos = GetWorldPositionFromGrid(cell);
            path.Add(worldPos);
        }

        return path;
    }

    private Vector2Int GetRandomRoadTile(HashSet<Vector2Int> roads)
    {
        int i = Random.Range(0, roads.Count);
        foreach (var r in roads)
            if (--i < 0) return r;
        return new Vector2Int(0, 0);
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
        go.name = prefab.name;
        po.OnPlaced();

        SetOccupied(center, true, po);

        RoadManager.Instance.RegisterObelisk(center);
        
        
        if (go.TryGetComponent<SpriteRenderer>(out var sr))
        {
            ApplySorting(center, 1, 1, sr, false, false);
        }
        
      ReplaceBaseTile(center, null);

    }

    // === Обновление пула дорог, соединённых с обелиском ===
    // === Обновление списка дорог, соединённых с обелиском ===
// === Обновление списка дорог, подключённых к обелиску ===
    public void UpdateConnectedRoadsFromRoadManager()
    {
        connectedRoads.Clear();

        foreach (var kvp in RoadManager.Instance.roads)
        {
            Road road = kvp.Value;
            if (road != null && road.isConnectedToObelisk)
                connectedRoads.Add(kvp.Key);
        }

        OnRoadNetworkChanged?.Invoke();
    }


// Возвращает упорядоченный маршрут по соединённым дорогам
    public List<Vector3> GetOrderedConnectedRoadWorldPositions()
    {
        List<Vector3> ordered = new List<Vector3>();

        if (connectedRoads == null || connectedRoads.Count == 0)
            return ordered;

        // Берём любую стартовую дорогу (ближе к центру)
        Vector2Int start = Vector2Int.zero;
        float minDist = float.MaxValue;
        Vector2Int center = new Vector2Int(width / 2, height / 2);

        foreach (var cell in connectedRoads)
        {
            float d = Vector2Int.Distance(center, cell);
            if (d < minDist)
            {
                minDist = d;
                start = cell;
            }
        }

        // BFS — последовательный обход сети дорог
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] dirs = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            ordered.Add(GetWorldPositionFromGrid(cur));

            foreach (var d in dirs)
            {
                Vector2Int next = cur + d;
                if (connectedRoads.Contains(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return ordered;
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
