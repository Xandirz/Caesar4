using UnityEngine;

public class ResearchTreeResetButton : MonoBehaviour
{
    [SerializeField] private RectTransform root;

    private static readonly Vector2 DefaultPosition = new Vector2(-150f, 1500f);
    private static readonly Vector3 DefaultScale = Vector3.one;

    public void ResetTree()
    {
        if (root == null)
        {
            Debug.LogWarning("ResearchTreeResetButton: root is not assigned.");
            return;
        }

        root.anchoredPosition = DefaultPosition;
        root.localScale = DefaultScale;
    }
}