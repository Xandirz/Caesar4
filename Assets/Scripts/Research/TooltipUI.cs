using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private GameObject tooltipBackground;

    private RectTransform rectTransform;
    private string lastText = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rectTransform = GetComponent<RectTransform>();

        if (tooltipBackground != null)
            tooltipBackground.SetActive(false);

        gameObject.SetActive(false);
    }

    public void Show(string text, Vector2 screenPosition)
    {
        if (tooltipText == null)
        {
            Debug.LogWarning("TooltipUI.Show: tooltipText == null");
            return;
        }

        lastText = text;
        tooltipText.text = text;

        gameObject.SetActive(true);
        tooltipBackground?.SetActive(true);

        if (rectTransform != null)
            rectTransform.position = screenPosition + new Vector2(16f, -16f);
    }

    public void UpdateText(string text)
    {
        if (!gameObject.activeSelf) return;
        if (tooltipText == null) return;

        if (lastText == text) return;    // ничего не изменилось

        lastText = text;
        tooltipText.text = text;
    }

    public void Hide()
    {
        lastText = null;
        tooltipBackground?.SetActive(false);
        gameObject.SetActive(false);
    }
}