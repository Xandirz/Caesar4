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

    public void ShowInfo(string buildingName)
    {
        infoPanel.SetActive(true);
        infoText.text = buildingName;
    }

    public void HideInfo()
    {
        infoPanel.SetActive(false);
    }
}