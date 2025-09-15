using UnityEngine;

public class MouseHighlighter : MonoBehaviour
{
    public GridManager gridManager;
    public BuildManager buildManager;
    public SpriteRenderer highlightSprite;

    void Update()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f; // для Grid.WorldToCell
        Vector2Int cellPos = gridManager.WorldToCell(mouseWorld);
        Vector3 cellCenter = gridManager.CellToWorld(cellPos);

        highlightSprite.transform.position = cellCenter;

        // меняем цвет
        if (buildManager.CurrentMode == BuildManager.BuildMode.Demolish)
        {
            highlightSprite.color = Color.blue;
        }
        else if (!gridManager.IsCellFree(cellPos))
        {
            highlightSprite.color = Color.red;
        }
        else
        {
            highlightSprite.color = Color.green;
        }
    }

}