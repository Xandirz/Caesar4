using System.Collections.Generic;
using UnityEngine;

public class PlacedObject : MonoBehaviour
{
    public Vector2Int gridPos;
    public GridManager manager;
    public virtual int buildEffectRadius => 0;
    public virtual int SizeX => 1;
    public virtual int SizeY => 1;
public virtual BuildManager.BuildMode BuildMode => BuildManager.BuildMode.None;
    // Стоимость задаётся в скрипте наследника
    public Dictionary<string,int> cost = new Dictionary<string,int>();

    // Получаем словарь стоимости для BuildManager/UI
    public virtual Dictionary<string,int> GetCostDict()
    {
        return new Dictionary<string,int>(cost);
    }
    public virtual void OnClicked()
    {
        var bm = FindObjectOfType<BuildManager>();
        if (bm != null && bm.CurrentMode != BuildManager.BuildMode.None && 
            bm.CurrentMode != BuildManager.BuildMode.Demolish)
            return;

        MouseHighlighter.Instance.ClearHighlights();

        if (InfoUI.Instance != null)
            InfoUI.Instance.ShowInfo(gameObject.name);
    }

    public virtual void OnPlaced()
    {
      
    }
    public virtual void OnRemoved() { }
}