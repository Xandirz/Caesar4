using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [Header("References")]
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private GameObject tooltipBackground;

    [Header("Sizing")]
    [SerializeField] private float maxWidth = 420f;
    [SerializeField] private float maxHeight = 260f; // <-- НОВОЕ: максимум по высоте тултипа
    [SerializeField] private Vector2 padding = new Vector2(16f, 10f);

    [Tooltip("Сколько пикселей добавлять к ширине за каждую строку")]
    [SerializeField] private float widthPerLine = 14f;

    [Tooltip("Высота (в пикселях) на одну строку текста (в тултипе)")]
    [SerializeField] private float heightPerLine = 26f; // <-- НОВОЕ: высота на строку

    [Header("Positioning")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(16f, -16f);
    [SerializeField] private float screenMargin = 8f;

    private RectTransform textRect;
    private RectTransform bgRect;
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
        textRect = tooltipText != null ? tooltipText.rectTransform : null;
        bgRect = tooltipBackground != null ? tooltipBackground.GetComponent<RectTransform>() : null;

        if (tooltipText != null)
        {
            tooltipText.enableWordWrapping = true;
            tooltipText.overflowMode = TextOverflowModes.Overflow;
        }

        tooltipBackground?.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Show(string text, Vector2 screenPosition)
    {
        if (tooltipText == null) return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        tooltipBackground?.SetActive(true);

        if (lastText != text)
        {
            lastText = text;
            tooltipText.text = text;

            tooltipText.ForceMeshUpdate();
            RefreshSize();
        }

        // Pivot внутрь экрана
        Vector2 pivot = new Vector2(0f, 1f);
        if (screenPosition.x > Screen.width * 0.5f) pivot.x = 1f;
        if (screenPosition.y < Screen.height * 0.5f) pivot.y = 0f;
        rectTransform.pivot = pivot;

        Vector2 offset;
        offset.x = (pivot.x > 0.5f) ? -Mathf.Abs(cursorOffset.x) : Mathf.Abs(cursorOffset.x);
        offset.y = (pivot.y < 0.5f) ? Mathf.Abs(cursorOffset.y) : -Mathf.Abs(cursorOffset.y);

        rectTransform.position = screenPosition + offset;

        ClampToScreen();
    }

    public void Hide()
    {
        lastText = null;
        tooltipBackground?.SetActive(false);
        gameObject.SetActive(false);
    }

    public void UpdateText(string newText)
    {
        if (!gameObject.activeSelf || tooltipText == null) return;
        if (lastText == newText) return;

        lastText = newText;
        tooltipText.text = newText;

        tooltipText.ForceMeshUpdate();
        RefreshSize();
        ClampToScreen();
    }

    private void RefreshSize()
    {
        if (tooltipText == null || textRect == null) return;

        // preferred size для оценки кол-ва строк (при ограничении по ширине)
        Vector2 pref = tooltipText.GetPreferredValues(tooltipText.text, maxWidth, 0f);

        // Высота строки (для расчёта lines)
        float lineHeight = tooltipText.fontSize * tooltipText.lineSpacing;
        if (lineHeight <= 0f) lineHeight = tooltipText.fontSize;

        // Количество строк
        int lines = Mathf.Max(1, Mathf.CeilToInt(pref.y / lineHeight));

        // Ширина: базовая + прибавка за строки
        float extraWidth = lines * widthPerLine;
        float finalWidth = Mathf.Min(maxWidth, pref.x + extraWidth);

        // Высота: строго по lines * heightPerLine (но не выше maxHeight)
        float finalHeight = Mathf.Min(maxHeight, lines * heightPerLine);

        finalWidth = Mathf.Max(1f, finalWidth);
        finalHeight = Mathf.Max(1f, finalHeight);

        // Текст
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

        // Фон
        if (bgRect != null)
        {
            bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth + padding.x);
            bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight + padding.y);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void ClampToScreen()
    {
        if (rectTransform == null) return;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        float left = corners[0].x;
        float right = corners[2].x;
        float bottom = corners[0].y;
        float top = corners[2].y;

        float minX = screenMargin;
        float maxX = Screen.width - screenMargin;
        float minY = screenMargin;
        float maxY = Screen.height - screenMargin;

        Vector3 shift = Vector3.zero;

        if (left < minX) shift.x += (minX - left);
        if (right > maxX) shift.x -= (right - maxX);
        if (bottom < minY) shift.y += (minY - bottom);
        if (top > maxY) shift.y -= (top - maxY);

        if (shift != Vector3.zero)
            rectTransform.position += shift;
    }
}
