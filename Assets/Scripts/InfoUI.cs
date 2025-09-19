using UnityEngine;
using TMPro; // если используешь TextMeshPro

public class InfoUI : MonoBehaviour
{
    public static InfoUI Instance;

    [SerializeField] private GameObject infoPanel; // окно Info
    [SerializeField] private TMP_Text infoText;    // текст внутри окна

    void Awake()
    {
        Instance = this;
        infoPanel.SetActive(false); // прячем по умолчанию
    }

    public void ShowInfo(PlacedObject po)
    {
        infoPanel.SetActive(true);

        string text = po.name;

        // Если это дом — добавляем инфу про воду
        if (po is House house)
        {
            text += "\nВода: " + (house.HasWater ? "Есть" : "Нет");
        }

        infoText.text = text;
    }


    public void HideInfo()
    {
        infoPanel.SetActive(false);
    }
}