using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildUIManager : MonoBehaviour
{
    public BuildManager buildManager;
    public GameObject buttonPrefab;
    public Transform buttonParent;
    private Button demolishButton;

    void Start()
    {
        CreateDemolishButton();
        CreateButtonsFromBuildManager();
    }

    void CreateDemolishButton()
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
        TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
        if (txt != null)
            txt.text = "Снос";

        demolishButton = btnObj.GetComponent<Button>();
        if (demolishButton != null)
        {
            demolishButton.onClick.AddListener(() =>
            {
                buildManager.SetBuildMode(BuildManager.BuildMode.Demolish);
                Debug.Log("Режим сноса активирован");
            });
        }
    }

    void CreateButtonsFromBuildManager()
    {
        foreach (var prefab in buildManager.buildingPrefabs)
        {
            if (prefab == null) continue;

            // Получаем PlacedObject для стоимости
            PlacedObject po = prefab.GetComponent<PlacedObject>();
            if (po == null) continue;

            var costDict = po.GetCostDict();
            string costText = GetCostText(costDict);
            string name = prefab.name;

            // Создаём кнопку
            GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = $"{name}\n{costText}";

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                BuildManager.BuildMode localMode = po.BuildMode;
                btn.onClick.AddListener(() => buildManager.SetBuildMode(localMode));
            }
        }
    }

    string GetCostText(Dictionary<string, int> costDict)
    {
        if (costDict == null || costDict.Count == 0) return "Стоимость: 0";

        string text = "";
        foreach (var kvp in costDict)
            text += $"{kvp.Key}: {kvp.Value} ";
        return text.Trim();
    }
}
