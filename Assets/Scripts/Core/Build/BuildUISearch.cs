using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildUISearch : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BuildUIManager buildUI;
    [SerializeField] private TMP_InputField searchInput;

    [Header("UI")]
    [SerializeField] private GameObject tabsRoot;              // панель табов (только табы)
    [SerializeField] private GameObject searchResultsPanel;    // панель результатов поиска
    [SerializeField] private Transform searchResultsParent;    // content (VerticalLayoutGroup)

    [Header("Search Options")]
    [SerializeField] private bool matchContainsToo = true;     // "oil" найдёт OliveOil
    [SerializeField] private bool sortAlphabetically = true;

    // кэшируем все кнопки строительства (которые уже созданы BuildUIManager)
    private readonly List<GameObject> allBuildButtons = new();
    private readonly Dictionary<GameObject, (Transform parent, int index)> original = new();

    private bool isSearchMode;

    private void Awake()
    {
        if (searchInput != null)
        {
            searchInput.onValueChanged.RemoveAllListeners();
            searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        if (searchResultsPanel != null)
            searchResultsPanel.SetActive(false);
    }

    private void Start()
    {
        // Важно: BuildUIManager должен уже успеть создать кнопки (он делает это в Start()).
        // Поэтому кэшируем в Start (после Awake).
        CacheBuildButtons();
    }

    private void CacheBuildButtons()
    {
        allBuildButtons.Clear();
        original.Clear();

        if (buildUI == null || buildUI.buttonParent == null)
        {
            Debug.LogError("[BuildUISearch] buildUI or buildUI.buttonParent is not set!");
            return;
        }

        // Берём ВСЕ кнопки зданий строго из buttonParent
        foreach (Transform child in buildUI.buttonParent)
        {
            if (child == null) continue;

            GameObject go = child.gameObject;
            allBuildButtons.Add(go);

            original[go] = (go.transform.parent, go.transform.GetSiblingIndex());
        }

        // На всякий случай
        if (allBuildButtons.Count == 0)
            Debug.LogWarning("[BuildUISearch] No build buttons found under buildUI.buttonParent. Are they created?");
    }

    private void OnSearchChanged(string raw)
    {
        string q = (raw ?? "").Trim();

        // если кнопки ещё не закэшились (например, порядок запуска) — попробуем ещё раз
        if (allBuildButtons.Count == 0)
            CacheBuildButtons();

        if (string.IsNullOrEmpty(q))
        {
            ExitSearchMode();
            return;
        }

        EnterSearchMode(q);
    }

    private void EnterSearchMode(string query)
    {
        isSearchMode = true;

        if (tabsRoot != null) tabsRoot.SetActive(false);
        if (searchResultsPanel != null) searchResultsPanel.SetActive(true);

        string q = query.ToLowerInvariant();

        // Очистим контейнер результатов (ничего не destroy)
        if (searchResultsParent != null)
        {
            for (int i = searchResultsParent.childCount - 1; i >= 0; i--)
            {
                searchResultsParent.GetChild(i).SetParent(buildUI.buttonParent, false);
            }
        }

        // Сначала скрываем все
        foreach (var go in allBuildButtons)
            if (go != null) go.SetActive(false);

        // Собираем совпадения
        var matched = new List<(GameObject go, string name)>(64);

        foreach (var go in allBuildButtons)
        {
            if (go == null) continue;

            // ✅ 0) фильтр: НЕ показываем неоткрытые (locked)
            var btn = go.GetComponent<UnityEngine.UI.Button>();
            if (btn == null || !btn.interactable)
                continue;

            // (опционально) если хочешь показывать только реально "доступные/видимые" сейчас
            // if (!go.activeInHierarchy) continue;

            // имя берём из TMP_Text на кнопке
            var txt = go.GetComponentInChildren<TMP_Text>(true);
            string name = txt != null ? txt.text : go.name;
            string n = name.ToLowerInvariant();

            bool ok = n.StartsWith(q) || (matchContainsToo && n.Contains(q));
            if (!ok) continue;

            matched.Add((go, name));
        }


        if (sortAlphabetically)
            matched.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

        // Переносим найденные в один список и показываем
        foreach (var item in matched)
        {
            if (searchResultsParent != null)
                item.go.transform.SetParent(searchResultsParent, false);

            item.go.SetActive(true);
        }
    }

    private void ExitSearchMode()
    {
        if (!isSearchMode) return;
        isSearchMode = false;

        // Возвращаем кнопки туда, где они были (buttonParent)
        foreach (var kv in original)
        {
            var go = kv.Key;
            if (go == null) continue;

            var (parent, index) = kv.Value;
            if (parent == null) continue;

            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(index);
            go.SetActive(true);
        }

        if (searchResultsPanel != null) searchResultsPanel.SetActive(false);
        if (tabsRoot != null) tabsRoot.SetActive(true);

        // BuildUIManager сам решает какие кнопки показывать в активном табе,
        // поэтому просто просим его обновить видимость.
        if (BuildUIManager.Instance != null)
            BuildUIManager.Instance.RefreshAllLocksAndTabs();
    }
    
    
    public void ClearSearch()
    {
        if (searchInput == null) return;

        searchInput.SetTextWithoutNotify(string.Empty);
        OnSearchChanged(string.Empty);
    }
}
