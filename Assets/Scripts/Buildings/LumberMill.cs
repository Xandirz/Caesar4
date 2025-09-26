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
            { "People", 1 }
        };
        production = new Dictionary<string, int>
        {
            { "Wood", 5 }
        };
        
    }

    public override Dictionary<string, int> GetCostDict() => cost;
}