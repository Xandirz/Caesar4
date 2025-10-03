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
        
    }

    public override Dictionary<string, int> GetCostDict() => cost;
}