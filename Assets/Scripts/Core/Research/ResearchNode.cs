using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ResearchNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    [Header("Node Colors")]
    [SerializeField] private Color completedColor = Color.white;
    [SerializeField] private Color availableColor = new Color(0.7f, 1f, 0.7f, 1f);
    [SerializeField] private Color lockedColor    = new Color(0.4f, 0.4f, 0.4f, 1f);

    public string Id { get; private set; }
    public string DisplayName { get; private set; }

    public bool IsCompleted { get; private set; }
    public bool IsAvailable { get; private set; }

    private System.Action<ResearchNode> onClick;

    // 🔥 СТАТИЧЕСКАЯ НОДА, НАД КОТОРОЙ ВИСИТ КУРСОР
    public static ResearchNode CurrentHoveredNode = null;

    /// <summary>
    /// Инициализация ноды.
    /// </summary>
    public void Init(string id, string displayName, Sprite icon, System.Action<ResearchNode> onClick)
    {
        Id = id;
        DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
        this.onClick = onClick;

        if (iconImage != null && icon != null)
            iconImage.sprite = icon;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClick?.Invoke(this));
        }

        SetState(false, false);
    }

    public void SetState(bool available, bool completed)
    {
        IsAvailable = available;
        IsCompleted = completed;

        if (button != null)
            button.interactable = available && !completed;

        if (backgroundImage != null)
        {
            if (completed)
                backgroundImage.color = completedColor;
            else if (available)
                backgroundImage.color = availableColor;
            else
                backgroundImage.color = lockedColor;
        }
    }

    public void SetIcon(Sprite icon)
    {
        if (iconImage != null)
            iconImage.sprite = icon;
    }

    // 🔥 Этот метод будет вызываться каждый тик для обновления тултипа
    public string GetTooltipText()
    {
        if (ResearchManager.Instance != null)
            return ResearchManager.Instance.BuildTooltipForNode(Id);

        return DisplayName;
    }

    // ===== Ховер =====
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUI.Instance == null)
            return;

        CurrentHoveredNode = this; // <— ВАЖНО

        TooltipUI.Instance.Show(GetTooltipText(), Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance == null) return;

        CurrentHoveredNode = null;

        TooltipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        if (TooltipUI.Instance == null) return;

        if (CurrentHoveredNode == this)
            CurrentHoveredNode = null;

        TooltipUI.Instance.Hide();
    }
}
