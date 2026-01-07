using System.Collections.Generic;
using UnityEngine;

public class Obelisk : PlacedObject
{

    public override void OnRemoved()
    {
        // ⚡ Не даём снести Обелиск
        Debug.Log("Обелиск нельзя снести!");
    }
}