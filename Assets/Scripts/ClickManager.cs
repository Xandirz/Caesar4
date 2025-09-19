using UnityEngine;

public class ClickManager : MonoBehaviour
{
    public GridManager gridManager;
    public InfoUI infoUI;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mw.z = 0f;
            Vector2Int cell = gridManager.IsoWorldToCell(mw);

            PlacedObject po = GetPlacedObject(cell);

            if (po != null)
            {
                po.OnClicked();
            }
            else
            {
                // клик по пустой клетке → очищаем выделение
                MouseHighlighter.Instance.ClearHighlights();
                infoUI.HideInfo();
            }
        }
    }

    // утилита для доступа к placedObjects
    PlacedObject GetPlacedObject(Vector2Int cell)
    {
        var field = typeof(GridManager).GetField("placedObjects",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var dict = (System.Collections.Generic.Dictionary<Vector2Int, PlacedObject>)field.GetValue(gridManager);

        if (dict.TryGetValue(cell, out var po))
            return po;

        return null;
    }
}