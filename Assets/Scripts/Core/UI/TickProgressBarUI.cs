using UnityEngine;
using UnityEngine.UI;

public class TickProgressBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage; // Image с Type = Filled

    private void Reset()
    {
        fillImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (fillImage == null) return;
        if (AllBuildingsManager.Instance == null) return;

        float interval = Mathf.Max(0.0001f, AllBuildingsManager.Instance.CheckInterval);
        float timer = Mathf.Clamp(AllBuildingsManager.Instance.CheckTimer, 0f, interval);

        fillImage.fillAmount = timer / interval;
    }
}