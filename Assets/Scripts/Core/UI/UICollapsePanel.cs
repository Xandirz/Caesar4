using UnityEngine;
using UnityEngine.UI;

public class UICollapsePanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject resourcesPanel;
    [SerializeField] private GameObject bottomPanel;

    [Header("Buttons")]
    [SerializeField] private Button toggleResourcesButton;
    [SerializeField] private Button toggleBottomButton;

    [Header("Symbols")]
    [SerializeField] private string openSymbol = "+";  // показать (панель скрыта)
    [SerializeField] private string closeSymbol = "-"; // скрыть (панель показана)

    private RectTransform resourcesRT;
    private RectTransform bottomRT;

    private Vector2 resourcesShownPos;
    private Vector2 bottomShownPos;

    private Vector2 resourcesHiddenPos;
    private Vector2 bottomHiddenPos;

    private CanvasGroup resourcesCG;
    private CanvasGroup bottomCG;

    private bool resourcesHidden = false;
    private bool bottomHidden = false;

    private void Awake()
    {
        if (resourcesPanel != null) resourcesRT = resourcesPanel.GetComponent<RectTransform>();
        if (bottomPanel != null) bottomRT = bottomPanel.GetComponent<RectTransform>();

        // CanvasGroup нужен, чтобы когда панель "за экраном", она не ловила клики
        if (resourcesPanel != null) resourcesCG = resourcesPanel.GetComponent<CanvasGroup>();
        if (bottomPanel != null) bottomCG = bottomPanel.GetComponent<CanvasGroup>();

        if (resourcesPanel != null && resourcesCG == null) resourcesCG = resourcesPanel.AddComponent<CanvasGroup>();
        if (bottomPanel != null && bottomCG == null) bottomCG = bottomPanel.AddComponent<CanvasGroup>();

        // запоминаем "показанное" положение (как в префабе)
        if (resourcesRT != null) resourcesShownPos = resourcesRT.anchoredPosition;
        if (bottomRT != null) bottomShownPos = bottomRT.anchoredPosition;

        // вычисляем "скрытое" положение: вниз на высоту панели (+ небольшой запас)
        resourcesHiddenPos = GetHiddenDownPos(resourcesRT, resourcesShownPos);
        bottomHiddenPos = GetHiddenDownPos(bottomRT, bottomShownPos);

        // подписки на кнопки
        if (toggleResourcesButton != null)
            toggleResourcesButton.onClick.AddListener(ToggleResources);

        if (toggleBottomButton != null)
            toggleBottomButton.onClick.AddListener(ToggleBottom);
    }

    private void Start()
    {
        // стартовые надписи: если панель открыта -> "-" (закрыть), если скрыта -> "+"
        UpdateButtonText(toggleResourcesButton, resourcesHidden ? openSymbol : closeSymbol);
        UpdateButtonText(toggleBottomButton, bottomHidden ? openSymbol : closeSymbol);

        // применим состояние (на случай если ты выставил флаги в инспекторе)
        ApplyResourcesState(resourcesHidden);
        ApplyBottomState(bottomHidden);
    }

    private void OnDestroy()
    {
        if (toggleResourcesButton != null)
            toggleResourcesButton.onClick.RemoveListener(ToggleResources);

        if (toggleBottomButton != null)
            toggleBottomButton.onClick.RemoveListener(ToggleBottom);
    }

    private void ToggleResources()
    {
        resourcesHidden = !resourcesHidden;
        ApplyResourcesState(resourcesHidden);
        UpdateButtonText(toggleResourcesButton, resourcesHidden ? openSymbol : closeSymbol);
    }

    private void ToggleBottom()
    {
        bottomHidden = !bottomHidden;
        ApplyBottomState(bottomHidden);
        UpdateButtonText(toggleBottomButton, bottomHidden ? openSymbol : closeSymbol);
    }

    private void ApplyResourcesState(bool hidden)
    {
        if (resourcesRT != null)
            resourcesRT.anchoredPosition = hidden ? resourcesHiddenPos : resourcesShownPos;

        ApplyCanvasGroup(resourcesCG, hidden);
    }

    private void ApplyBottomState(bool hidden)
    {
        if (bottomRT != null)
            bottomRT.anchoredPosition = hidden ? bottomHiddenPos : bottomShownPos;

        ApplyCanvasGroup(bottomCG, hidden);
    }

    private static void ApplyCanvasGroup(CanvasGroup cg, bool hidden)
    {
        if (cg == null) return;

        cg.alpha = hidden ? 0f : 1f;
        cg.blocksRaycasts = !hidden;
        cg.interactable = !hidden;
    }

    private static void UpdateButtonText(Button btn, string text)
    {
        if (btn == null) return;

        // "текст можно брать из button.text" — в Unity это обычно Text внутри кнопки
        var uiText = btn.GetComponentInChildren<Text>(true);
        if (uiText != null)
            uiText.text = text;
    }

    private static Vector2 GetHiddenDownPos(RectTransform rt, Vector2 shownPos)
    {
        if (rt == null) return shownPos;

        // реальная высота панели (с учётом масштаба)
        float h = rt.rect.height * rt.lossyScale.y;

        // небольшой запас, чтобы точно ушла за край
        float padding = 20f;

        return shownPos + new Vector2(0f, -(h + padding));
    }
}
