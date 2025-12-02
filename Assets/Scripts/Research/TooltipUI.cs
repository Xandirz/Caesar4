using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private GameObject tooltipBackground;

    private RectTransform rectTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rectTransform = GetComponent<RectTransform>();

        if (tooltipText == null)
        {
            Debug.LogError("TooltipUI: tooltipText не назначен в инспекторе!");
        }

        if (tooltipBackground == null)
        {
            Debug.LogWarning("TooltipUI: tooltipBackground не назначен в инспекторе");
        }

        // Выключаем при старте
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

        tooltipText.text = text;

        // Включаем объект тултипа
        gameObject.SetActive(true);

        if (tooltipBackground != null)
            tooltipBackground.SetActive(true);

        if (rectTransform != null)
        {
            rectTransform.position = screenPosition + new Vector2(16f, -16f);
        }
    }

    public void Hide()
    {
        if (tooltipBackground != null)
            tooltipBackground.SetActive(false);

        gameObject.SetActive(false);
    }
}