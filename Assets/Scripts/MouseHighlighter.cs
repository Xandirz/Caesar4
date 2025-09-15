using UnityEngine;

public class MouseHighlighter : MonoBehaviour
{
    public GridManager gridManager;
    public BuildManager buildManager;
    public SpriteRenderer highlightSprite;

    void Update()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2Int cell = gridManager.IsoWorldToCell(mouseWorld);
        Vector3 center  = gridManager.CellToIsoWorld(cell);

        // ставим в центр ромба
        if (highlightSprite != null)
            highlightSprite.transform.position = center;

        // цвета
        if (buildManager != null && buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
            highlightSprite.color = Color.blue;
        else if (!gridManager.IsCellFree(cell))
            highlightSprite.color = Color.red;
        else
            highlightSprite.color = Color.green;
    }
}