using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Road : PlacedObject
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.Road;
    public bool isConnectedToObelisk { get; set; } = false;

    [SerializeField] private GameObject notConnectedOverlay; // префаб-спрайт внутри Road, выключен по умолчанию

    
    private new Dictionary<string, int> cost = new()
    {
        { "Wood", 1 },
    };

    [Header("Road Sprites")]
    public Sprite Road_LeftRight;         // ──  (диагональ NE <-> SW)
    public Sprite Road_UpDown;            // │   (диагональ NW <-> SE)
    public Sprite Road_UpRight;           // угол: NW + NE
    public Sprite Road_UpLeft;            // угол: SW + NW
    public Sprite Road_DownRight;         // угол: NE + SE
    public Sprite Road_DownLeft;          // угол: SE + SW
    public Sprite Road_UpLeftRight;       // T без низа  (нет SE) → ↑←→
    public Sprite Road_DownLeftRight;     // T без верха (нет NW) → ↓←→
    public Sprite Road_LeftUpDown;        // T без права (нет NE) → ←↑↓
    public Sprite Road_RightUpDown;       // T без лева  (нет SW) → →↑↓
    public Sprite Road_UpDownLeftRight;   // крест

    [Header("Calibration")]
    [SerializeField, Tooltip("Поменять местами прямые NW↔SE и NE↔SW, если арты нарисованы наоборот")]
    private bool invertStraights = true;

    private SpriteRenderer sr;
    private RoadManager roadManager;
    
    

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        roadManager = FindObjectOfType<RoadManager>();
   

    }

 

    public override Dictionary<string, int> GetCostDict()
    {
        return cost;
    }
    
    public void ApplyConnectionVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // Основной цвет (можно оставить всегда белый)
        sr.color = Color.white;

        // Если есть оверлей — управляем его видимостью
        if (notConnectedOverlay != null)
        {
            notConnectedOverlay.SetActive(!isConnectedToObelisk);
        }
    }


    public override void OnPlaced()
    {
        if (roadManager != null)
        {
            roadManager.RegisterRoad(gridPos, this);
            roadManager.RefreshRoadAndNeighbors(gridPos);
            
            
        }
        
        AllBuildingsManager.Instance.MarkEffectsDirtyAround(gridPos, 8);

    }

    public override void OnRemoved()
    {
        // Возврат ресурсов при сносе — как было
        ResourceManager.Instance.RefundResources(cost);

        if (roadManager != null)
        {
            roadManager.UnregisterRoad(gridPos);
            roadManager.RefreshRoadAndNeighbors(gridPos);
        }

        base.OnRemoved();
    }

  public void UpdateRoadSprite(bool nw, bool ne, bool se, bool sw)
{
    if (sr == null) sr = GetComponent<SpriteRenderer>();

    // Прямые — две диагонали ромба. Если арты перепутаны, invertStraights чинит соответствие.
    Sprite straightNWSE = invertStraights ? Road_LeftRight : Road_UpDown;   // NW <-> SE
    Sprite straightNESW = invertStraights ? Road_UpDown   : Road_LeftRight; // NE <-> SW

    // Битовая маска: NW=1, NE=2, SE=4, SW=8
    int mask = (nw ? 1 : 0) | (ne ? 2 : 0) | (se ? 4 : 0) | (sw ? 8 : 0);

    switch (mask)
    {
        // 0 соседей — по договору рисуем прямую (NE<->SW)
        case 0:
            sr.sprite = straightNESW;
            break;

        // 1 сосед — прямая по соответствующей диагонали
        case 1: // NW
        case 4: // SE
            sr.sprite = straightNWSE; // NW <-> SE
            break;

        case 2: // NE
        case 8: // SW
            sr.sprite = straightNESW; // NE <-> SW
            break;

        // 2 соседа — прямая или угол
        case (1 | 4): // NW + SE
            sr.sprite = straightNWSE;
            break;

        case (2 | 8): // NE + SW
            sr.sprite = straightNESW;
            break;

        case (1 | 2): // NW + NE → верхний правый угол
            sr.sprite = Road_UpRight;
            break;

        case (2 | 4): // NE + SE → правый нижний угол
            sr.sprite = Road_DownRight;
            break;

        case (4 | 8): // SE + SW → нижний левый угол
            sr.sprite = Road_DownLeft;
            break;

        case (8 | 1): // SW + NW → верхний левый угол
            sr.sprite = Road_UpLeft;
            break;

        // 3 соседа — Т-образные (по отсутствующей стороне)
        case (2 | 4 | 8): // нет NW
            sr.sprite = Road_DownLeftRight;
            break;

        case (1 | 4 | 8): // нет NE
            sr.sprite = Road_LeftUpDown;
            break;

        case (1 | 2 | 8): // нет SE
            sr.sprite = Road_UpLeftRight;
            break;

        case (1 | 2 | 4): // нет SW
            sr.sprite = Road_RightUpDown;
            break;

        // 4 соседа — крест
        case (1 | 2 | 4 | 8):
            sr.sprite = Road_UpDownLeftRight;
            break;
    }

    // 🔥 Единая система сортировки
    if (BuildManager.Instance != null)
    {
        BuildManager.Instance.gridManager.ApplySorting(
            gridPos,
            SizeX,
            SizeY,
            sr,
            false, // не лес
            true   // дорога
        );
    }
    
    
  
}

}
