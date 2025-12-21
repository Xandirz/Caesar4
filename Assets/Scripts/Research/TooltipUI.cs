using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private GameObject tooltipBackground;

    private RectTransform rectTransform;
    private string lastText = null;

    // Настройки (можешь менять)
    [SerializeField] private Vector2 cursorOffset = new Vector2(16f, -16f);
    [SerializeField] private float screenMargin = 8f;

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

        // Включаем тултип
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (tooltipBackground != null && !tooltipBackground.activeSelf)
            tooltipBackground.SetActive(true);

        // Обновляем текст (если поменялся)
        if (lastText != text)
        {
            lastText = text;
            tooltipText.text = text;

            // Важно: пересчитать размеры после смены текста
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        // 1) Подбираем pivot так, чтобы тултип "рос" внутрь экрана
        // Правый верхний квадрант -> pivot (1,1), и т.д.
        Vector2 pivot = new Vector2(0f, 1f); // по умолчанию: тултип справа и ниже курсора
        if (screenPosition.x > Screen.width * 0.5f) pivot.x = 1f;
        if (screenPosition.y < Screen.height * 0.5f) pivot.y = 0f;
        rectTransform.pivot = pivot;

        // 2) Ставим рядом с курсором (учитывая pivot)
        Vector2 offset = cursorOffset;

        // если pivot справа — двигаем тултип влево от курсора
        if (pivot.x > 0.5f) offset.x = -Mathf.Abs(cursorOffset.x);
        else offset.x = Mathf.Abs(cursorOffset.x);

        // если pivot снизу — двигаем тултип вверх от курсора
        if (pivot.y < 0.5f) offset.y = Mathf.Abs(cursorOffset.y);
        else offset.y = -Mathf.Abs(cursorOffset.y);

        rectTransform.position = screenPosition + offset;

        // 3) Кламп к экрану
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
        if (!gameObject.activeSelf)
            return;

        if (tooltipText == null)
            return;

        if (lastText == newText)
            return;

        lastText = newText;
        tooltipText.text = newText;

        // пересчитать размеры после изменения текста
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // и снова убедиться, что тултип не вылез за экран
        ClampToScreen();
    }

    private void ClampToScreen()
    {
        if (rectTransform == null) return;

        // Получаем мировые углы (в Overlay-Canvas это фактически экранные координаты)
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        // corners: 0=BL,1=TL,2=TR,3=BR
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
