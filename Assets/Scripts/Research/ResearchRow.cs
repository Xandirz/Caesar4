using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ResearchRow : MonoBehaviour
{
    [SerializeField] private TMP_Text textLabel;
    [SerializeField] private Button okButton;

    private string id;
    private Action onOk;
    public void SetText(string t) { textLabel.text = t; }

    // Инициализация строки (показываем только условие)
    public void Setup(string id, string conditionText, Action onOk)
    {
        this.id = id;
        this.onOk = onOk;

        if (textLabel != null)
            textLabel.text = conditionText;

        if (okButton != null)
        {
            okButton.gameObject.SetActive(false); // кнопку покажет UI, когда условие выполнится
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(() =>
            {
                onOk?.Invoke();
            });
        }
    }

    // Показываем/скрываем кнопку ОК + лёгкая визуальная подсветка
    public void SetAvailable(bool available)
    {
        if (okButton != null) okButton.gameObject.SetActive(available);
        if (textLabel != null) textLabel.color = available ? Color.green : Color.white;
    }

    // Меняем строку на «описание открытия», кнопку прячем
    public void ShowCompleted(string discoveryText)
    {
        if (textLabel != null)
        {
            // жирным id чтобы было видно, что открыто
            textLabel.text = $"<b>{id}</b>: {discoveryText}";
            textLabel.color = Color.white;
        }
        if (okButton != null) okButton.gameObject.SetActive(false);
    }
}