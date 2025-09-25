using UnityEngine;
using UnityEngine.EventSystems;

public class ClickManager : MonoBehaviour
{
    public GridManager gridManager;
    public InfoUI infoUI;
    public BuildManager buildManager; 

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
                // 🔥 показываем Info только если НЕ demolish
                if (buildManager != null && buildManager.CurrentMode != BuildManager.BuildMode.Demolish)
                {
                    po.OnClicked();
                }
            }
            else
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    MouseHighlighter.Instance.ClearHighlights();
                    if (infoUI != null)
                        infoUI.HideInfo();
                }
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