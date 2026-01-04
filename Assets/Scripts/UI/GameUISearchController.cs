using UnityEngine;
using UnityEngine.EventSystems;

public class GameUISearchController : MonoBehaviour
{
    [Header("Search systems")]
    [SerializeField] private BuildUISearch buildSearch;
    [SerializeField] private ResourceUIManager resourceSearch;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // Снимаем фокус (важно для TMP_InputField)
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (buildSearch != null)
            buildSearch.ClearSearch();

        if (resourceSearch != null)
            resourceSearch.ClearSearch();
    }
}