using System.Collections.Generic;
using UnityEngine;

public class LumberMill : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.LumberMill;

    // ⚡ Стоимость задаём прямо здесь
    public LumberMill()
    {
        cost = new Dictionary<string,int>
        {
            { "People", 4 }
        };
        production = new Dictionary<string, int>
        {
            { "Wood", 20 }
        };
  
        
        upgradeConsumptionLevel1 = new Dictionary<string, int>
        {
            { "Tools", 1 }
        };
        
        upgradeProductionBonusLevel1 = new Dictionary<string, int>
        {
            { "Wood", 20 }
        };
    }
    private void Awake()
    {
        level2Sprite = Resources.Load<Sprite>("Sprites/Buildings/Production/Lvl2/Lumber2");
    }
    public override Dictionary<string, int> GetCostDict() => cost;
}