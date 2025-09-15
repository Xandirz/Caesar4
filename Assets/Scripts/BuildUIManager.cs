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
    // Все префабы зданий для создания кнопок
    public GameObject[] buildingPrefabs; 

    void Start()
    {
        CreateDemolishButton();
        CreateButtonsFromPrefabs();
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
    void CreateButtonsFromPrefabs()
    {
        foreach (var prefab in buildingPrefabs)
        {
            // Получаем PlacedObject для стоимости
            PlacedObject po = prefab.GetComponent<PlacedObject>();
            if (po == null) continue;

            var costDict = po.GetCostDict();
            string costText = GetCostText(costDict);

            // Название объекта (можно заменить на поле в prefab или компоненте)
            string name = prefab.name;

            // Создаем кнопку
            GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = $"{name}\n{costText}";

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                // Определяем тип здания для BuildManager.BuildMode - предполагается, что у PlacedObject или prefab есть способ определить BuildMode
                BuildManager.BuildMode mode = GetBuildModeFromPrefab(prefab);

                BuildManager.BuildMode localMode = mode;
                btn.onClick.AddListener(() => buildManager.SetBuildMode(localMode));
            }
        }
    }

    string GetCostText(Dictionary<string,int> costDict)
    {
        if (costDict == null || costDict.Count == 0) return "Стоимость: 0";

        string text = "";
        foreach (var kvp in costDict)
            text += $"{kvp.Key}: {kvp.Value} ";
        return text.Trim();
    }

    BuildManager.BuildMode GetBuildModeFromPrefab(GameObject prefab)
    {
        var placedObject = prefab.GetComponent<PlacedObject>();
        if (placedObject != null)
            return placedObject.BuildMode;
        return BuildManager.BuildMode.None;
    }
}
