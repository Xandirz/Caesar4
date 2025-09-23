using System.Collections.Generic;
using UnityEngine;

public class LumberMill : ProductionBuilding
{
    public override BuildManager.BuildMode BuildMode => BuildManager.BuildMode.LumberMill;
    public override string Resource => "Wood";

    // ⚡ Стоимость задаём прямо здесь
    private new Dictionary<string,int> cost = new()
    {
        { "Wood", 1 },
        { "People", 1 }
    };

    public override Dictionary<string, int> GetCostDict() => cost;
}