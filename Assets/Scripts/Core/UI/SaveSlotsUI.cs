using System.Collections.Generic;
using UnityEngine;

public class SaveSlotsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] SaveSlotRowUI rowPrefab;
    [SerializeField] Transform rowsParent;

    [Header("Optional")]
    [SerializeField] int maxRows = 0; // 0 = без лимита

    readonly List<GameObject> spawned = new();

    void OnEnable()
    {
        Refresh();
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.OnSavesChanged += Refresh;
    }
    void OnDisable()
    {
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.OnSavesChanged -= Refresh;
    }
    public void Refresh()
    {
        if (SaveLoadManager.Instance == null)
            return;

        ClearRows();

        // ВАЖНО: тут должен быть SaveEntryInfo (не SaveMetaInfo)
        var saves = SaveLoadManager.Instance.ListSavesNewestFirst(); // List<SaveEntryInfo>

        int count = saves.Count;
        if (maxRows > 0)
            count = Mathf.Min(count, maxRows);

        for (int i = 0; i < count; i++)
        {
            var row = Instantiate(rowPrefab, rowsParent);
            row.Bind(saves[i]); // Bind(SaveEntryInfo)
            spawned.Add(row.gameObject);
        }
    }

    /// <summary>
    /// Кнопка "Сохранить" (ручное сохранение).
    /// Делает новое сохранение, не перезаписывая.
    /// </summary>
    public void OnSaveClicked()
    {
        if (SaveLoadManager.Instance == null)
            return;

        SaveLoadManager.Instance.SaveNew();
        Refresh();
    }

    void ClearRows()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawned.Clear();

        for (int i = rowsParent.childCount - 1; i >= 0; i--)
            Destroy(rowsParent.GetChild(i).gameObject);
    }
}