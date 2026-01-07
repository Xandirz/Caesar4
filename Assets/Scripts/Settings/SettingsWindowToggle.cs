using UnityEngine;

public class SettingsWindowToggle : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    public void Toggle()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel != null && settingsPanel.activeSelf)
            settingsPanel.SetActive(false);
    }

    public void OpenClose()
    {
        if (settingsPanel == null) return;
        if (!settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            settingsPanel.SetActive(false);

        }
    }

 
}