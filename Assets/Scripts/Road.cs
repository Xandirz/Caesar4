using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Road : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Road;

    private new Dictionary<string, int> cost = new()
    {
        { "Wood", 1 },
    };

    [Header("Road Sprites")]
    public Sprite straight;   // прямая (N-S)
    public Sprite corner;     // угол (N+E)
    public Sprite tJunction;  // тройник (N+E+W, без S)
    public Sprite cross;      // перекресток
    public Sprite end;        // конец дороги (N)

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override Dictionary<string, int> GetCostDict() => cost;

    public override void OnPlaced()
    {
        if (manager != null)
        {
            manager.RegisterRoad(gridPos, this);

            // обновляем себя и соседей
            manager.UpdateRoadAt(gridPos);
            manager.UpdateRoadAt(gridPos + Vector2Int.up);
            manager.UpdateRoadAt(gridPos + Vector2Int.down);
            manager.UpdateRoadAt(gridPos + Vector2Int.left);
            manager.UpdateRoadAt(gridPos + Vector2Int.right);
        }
    }

    public override void OnRemoved()
    {
        ResourceManager.Instance.RefundResources(cost);

        if (manager != null)
        {
            manager.SetOccupied(gridPos, false);
            manager.UnregisterRoad(gridPos);

            // обновляем соседей
            manager.UpdateRoadAt(gridPos + Vector2Int.up);
            manager.UpdateRoadAt(gridPos + Vector2Int.down);
            manager.UpdateRoadAt(gridPos + Vector2Int.left);
            manager.UpdateRoadAt(gridPos + Vector2Int.right);
        }

        Destroy(gameObject);
    }

    public void UpdateRoadSprite(bool n, bool e, bool s, bool w)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.flipX = false; // сбрасываем всегда

        int connections = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);

        switch (connections)
        {
            case 0:
                sr.sprite = end;
                break;

            case 1:
                sr.sprite = end;
                if (s) { /* вниз = тот же спрайт */ }
                else if (e) sr.flipX = true; // вправо
                else if (w) sr.flipX = true; // влево (тоже flipX)
                break;

            case 2:
                if (n && s)
                {
                    sr.sprite = straight; // вертикаль
                }
                else if (e && w)
                {
                    sr.sprite = straight; // горизонталь
                    sr.flipX = true;
                }
                else
                {
                    sr.sprite = corner;
                    if (e && s)
                    {
                        // используем corner (N+E), смещений нет
                        // визуально подходит для E+S
                    }
                    else if (s && w)
                    {
                        sr.flipX = true; // поворот corner в S+W
                    }
                    else if (w && n)
                    {
                        sr.flipX = true; // поворот corner в W+N
                    }
                }
                break;

            case 3:
                sr.sprite = tJunction;
                if (!s)
                {
                    // база (N+E+W)
                }
                else if (!n)
                {
                    // T без N (E+S+W) → используем тот же спрайт
                }
                else if (!w)
                {
                    sr.flipX = true; // T без W
                }
                else if (!e)
                {
                    sr.flipX = true; // T без E
                }
                break;

            case 4:
                sr.sprite = cross;
                break;
        }

        // сортировка по Y
        sr.sortingLayerName = "World";
        sr.sortingOrder = -(int)(transform.position.y * 100);
    }
}
