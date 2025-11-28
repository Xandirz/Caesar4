using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchUI : MonoBehaviour
{
    public static ResearchUI Instance;

    [Header("Основная панель")]
    [SerializeField] private GameObject panel;

    [Header("Тултип")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Popup открытия")]
    [SerializeField] private GameObject discoveryPopupPanel;
    [SerializeField] private TMP_Text discoveryTitleText;
    [SerializeField] private TMP_Text discoveryBodyText;
    [SerializeField] private Button discoveryOkButton;

    private bool isVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panel != null) panel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        if (discoveryPopupPanel != null) discoveryPopupPanel.SetActive(false);

        if (discoveryOkButton != null)
        {
            discoveryOkButton.onClick.RemoveAllListeners();
            discoveryOkButton.onClick.AddListener(() =>
            {
                if (discoveryPopupPanel != null)
                    discoveryPopupPanel.SetActive(false);
            });
        }
    }

    public void TogglePanel()
    {
        if (panel == null) return;
        isVisible = !isVisible;
        panel.SetActive(isVisible);
    }

    public void ShowTooltip(string text, Vector2 screenPosition)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);

        // позиционируем тултип около мыши (если это UI под Screen Space Overlay)
        RectTransform rt = tooltipPanel.transform as RectTransform;
        if (rt != null)
        {
            rt.position = screenPosition + new Vector2(16f, -16f);
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(false);
    }

    public void ShowDiscoveryPopup(string title, string body)
    {
        if (discoveryPopupPanel == null || discoveryTitleText == null || discoveryBodyText == null)
            return;

        discoveryTitleText.text = title;
        discoveryBodyText.text = body;
        discoveryPopupPanel.SetActive(true);
    }
}
