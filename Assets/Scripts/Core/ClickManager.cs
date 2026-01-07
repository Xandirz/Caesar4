using UnityEngine;
using UnityEngine.EventSystems;

public class ClickManager : MonoBehaviour
{
    public GridManager gridManager;
    public InfoUI infoUI;
    public BuildManager buildManager;

    [Header("Iso click fix")]
    [SerializeField] private float clickYOffsetPixels = 24f; // подбирай 16–40

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        // если кликнули по UI — не обрабатываем клик по карте/зданиям
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int cell = GetMouseCellIsoAdjusted();

        PlacedObject po = null;
        gridManager.TryGetPlacedObject(cell, out po);

        if (po != null)
        {
            // показываем Info только если НЕ demolish
            if (buildManager != null && buildManager.CurrentMode != BuildManager.BuildMode.Demolish)
                po.OnClicked();
        }
        else
        {
            MouseHighlighter.Instance.ClearHighlights();
            if (infoUI != null)
                infoUI.HideInfo();
        }
    }

    private Vector2Int GetMouseCellIsoAdjusted()
    {
        Camera cam = Camera.main;
        Vector3 sp = Input.mousePosition;

        // ✅ ключевой фикс для изометрии: смещаем клик вниз, чтобы попадать в "основание" здания
        sp.y -= clickYOffsetPixels;

        Vector3 mw = cam.ScreenToWorldPoint(sp);
        mw.z = 0f;

        // пиксель-перфект как у остальной системы
        mw = gridManager.SnapToPixels(mw);

        return gridManager.IsoWorldToCell(mw);
    }
}