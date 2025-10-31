using UnityEngine;
using System.Collections;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance;

    [Header("Порог для открытия исследований")]
    public int housesToUnlockClay = 10;

    [Header("UI ссылки")]
    public ResearchUI researchUI; // сюда привяжем окно исследования

    private bool clayUnlocked = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // Проверяем условие — построено 10 домов
        if (!clayUnlocked && AllBuildingsManager.Instance != null)
        {
            int houseCount = AllBuildingsManager.Instance.GetBuildingCount(BuildManager.BuildMode.House);

            if (houseCount >= housesToUnlockClay)
            {
                clayUnlocked = true;

                // Показываем окно исследования
                if (researchUI != null)
                {
                    researchUI.ShowResearch(
                        "Новое исследование!",
                        "Вы открыли строительство глины (Clay).",
                        () =>
                        {
                            BuildManager.Instance.UnlockBuilding(BuildManager.BuildMode.Clay);
                        }
                    );
                }
             
            }
        }
    }
}