using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ResearchNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    public string Id { get; private set; }
    public string DisplayName { get; private set; }

    public bool IsCompleted { get; private set; }
    public bool IsAvailable { get; private set; }

    private System.Action<ResearchNode> onClick;

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
            if (completed)           backgroundImage.color = Color.white;
            else if (available)      backgroundImage.color = new Color(0.7f, 1f, 0.7f);
            else                     backgroundImage.color = new Color(0.4f, 0.4f, 0.4f);
        }
    }

    // ===== Ховер: тултип =====
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer ENTER on " + name);

        if (TooltipUI.Instance == null)
        {
            Debug.LogWarning("TooltipUI.Instance == null — в сцене нет активного TooltipUI");
            return;
        }

        string text = DisplayName;
        if (ResearchManager.Instance != null)
        {
            text = ResearchManager.Instance.BuildTooltipForNode(Id);
        }

        // Берём позицию мыши
        Vector2 screenPos = Input.mousePosition;

        TooltipUI.Instance.Show(text, screenPos);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance == null) return;
        TooltipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        if (TooltipUI.Instance == null) return;
        TooltipUI.Instance.Hide();
    }
}
