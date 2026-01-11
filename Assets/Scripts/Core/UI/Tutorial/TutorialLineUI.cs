using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialLineUI : MonoBehaviour
{
    [SerializeField] private Image checkboxImage;
    [SerializeField] private TMP_Text label;

    [Header("Checkbox Sprites")]
    [SerializeField] private Sprite uncheckedSprite;
    [SerializeField] private Sprite checkedSprite;

    public void SetText(string text)
    {
        if (label != null) label.text = text;
    }

    public void SetChecked(bool isChecked)
    {
        if (checkboxImage == null) return;
        checkboxImage.sprite = isChecked ? checkedSprite : uncheckedSprite;
    }

    // Опционально: чтобы шаги выглядели “зачёркнутыми”
    public void SetStrikethrough(bool on)
    {
        if (label == null) return;
        label.fontStyle = on ? FontStyles.Strikethrough : FontStyles.Normal;
    }
}