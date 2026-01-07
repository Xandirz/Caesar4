using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Human : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 1.5f;

    private GridManager gridManager;
    private SpriteRenderer sr;

    private Vector2Int currentCell;
    private Vector2Int? previousCell = null;
    private Vector3 targetPos;
    private bool hasTarget = false;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        sr = GetComponent<SpriteRenderer>();

        // Инициализация: ставим на ближайшую дорогу
        currentCell = gridManager.GetGridPositionFromWorld(transform.position);
        if (!IsRoad(currentCell))
        {
            // Если человек случайно появился не на дороге — телепортируем на ближайшую
            currentCell = FindNearestRoad(currentCell);
            transform.position = gridManager.GetWorldPositionFromGrid(currentCell);
        }

        ChooseNextTarget();
        gridManager.OnRoadNetworkChanged += OnRoadNetworkChanged;
    }

    void OnDestroy()
    {
        if (gridManager != null)
            gridManager.OnRoadNetworkChanged -= OnRoadNetworkChanged;
    }

    void Update()
    {
        if (!hasTarget) return;

        // === обновляем сортировку до движения ===
        if (gridManager != null && sr != null)
            gridManager.ApplySortingDynamic(transform.position, sr);

        // движение
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // направление спрайта
        float dir = targetPos.x - transform.position.x;
        if (Mathf.Abs(dir) > 0.05f)
            sr.flipX = dir < 0;

        // проверка достижения точки
        if (Vector3.Distance(transform.position, targetPos) < 0.02f)
        {
            previousCell = currentCell;
            currentCell = gridManager.GetGridPositionFromWorld(targetPos);
            ChooseNextTarget();
        }
    }


    // сортировка переносится в LateUpdate — после всех перемещений






    private void OnRoadNetworkChanged()
    {
        // если сеть дорог изменилась — обновляем цель
        if (!IsRoad(currentCell))
        {
            // человек стоял на удалённой дороге — телепортируем на ближайшую
            currentCell = FindNearestRoad(currentCell);
            transform.position = gridManager.GetWorldPositionFromGrid(currentCell);
        }

        ChooseNextTarget();
    }

    public void Initialize(GridManager gm)
    {
        gridManager = gm;
    }

    private void ChooseNextTarget()
    {
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(currentCell.x + 1, currentCell.y),
            new Vector2Int(currentCell.x - 1, currentCell.y),
            new Vector2Int(currentCell.x, currentCell.y + 1),
            new Vector2Int(currentCell.x, currentCell.y - 1)
        };

        // фильтруем только дороги
        neighbors.RemoveAll(n => !IsRoad(n));

        // убираем ячейку, с которой пришёл
        if (previousCell != null)
            neighbors.Remove(previousCell.Value);

        if (neighbors.Count == 0)
        {
            // если в тупике — разворачиваемся
            if (previousCell != null && IsRoad(previousCell.Value))
                neighbors.Add(previousCell.Value);
            else
            {
                hasTarget = false;
                return;
            }
        }

        // выбираем случайного соседа
        Vector2Int nextCell = neighbors[Random.Range(0, neighbors.Count)];
        targetPos = gridManager.GetWorldPositionFromGrid(nextCell);
        hasTarget = true;
    }

    private bool IsRoad(Vector2Int cell)
    {
        return gridManager != null &&
               gridManager.TryGetPlacedObject(cell, out var po) &&
               po is Road r &&
               r.isConnectedToObelisk;
    }

    private Vector2Int FindNearestRoad(Vector2Int start)
    {
        int searchRadius = 1;
        while (searchRadius < 20)
        {
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    Vector2Int check = new Vector2Int(start.x + dx, start.y + dy);
                    if (IsRoad(check))
                        return check;
                }
            }
            searchRadius++;
        }
        return start;
    }
}
