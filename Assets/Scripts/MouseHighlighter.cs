using UnityEngine;

public class MouseHighlighter : MonoBehaviour
{
    public GridManager gridManager;
    public BuildManager buildManager;
    public SpriteRenderer highlightSprite;
    public Color buildColor = Color.green;
    public Color cantBuildColor = Color.red;
    public Color demolishColor = Color.yellow;

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
            highlightSprite.color = demolishColor;
        else if (!gridManager.IsCellFree(cell))
            highlightSprite.color = cantBuildColor;
        else
            highlightSprite.color = buildColor;
    }
}