using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ResourceUIManager : MonoBehaviour
{
    public static ResourceUIManager Instance;

    public TMP_Text resourceText; // один TextMeshPro для всех ресурсов

    private Dictionary<string, int> resources = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // обновление или добавление ресурса
    public void SetResource(string name, int amount)
    {
        resources[name] = amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        string text = "";
        foreach (var kvp in resources)
        {
            text += $"{kvp.Key}: {kvp.Value}\n";
        }

        if (resourceText != null)
            resourceText.text = text;
    }
}