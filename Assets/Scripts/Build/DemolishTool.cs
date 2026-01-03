using UnityEngine;

public class DemolishButton : MonoBehaviour
{
    public BuildManager buildManager;

    // Метод, который нужно повесить на кнопку UI через инспектор
    public void ActivateDemolishMode()
    {
        if (buildManager != null)
            buildManager.SetBuildMode(BuildManager.BuildMode.Demolish);
        else
            Debug.LogError("BuildManager не назначен в DemolishButton");
    }
}