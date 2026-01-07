using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextSizeApplier : MonoBehaviour
{
    [SerializeField] private Transform root; // если null — возьмём this.transform

    private readonly List<TMP_Text> texts = new List<TMP_Text>();
    private readonly Dictionary<TMP_Text, float> baseFontSize = new Dictionary<TMP_Text, float>();
    private readonly Dictionary<TMP_Text, float> baseFontSizeMin = new Dictionary<TMP_Text, float>();
    private readonly Dictionary<TMP_Text, float> baseFontSizeMax = new Dictionary<TMP_Text, float>();

    private void Awake()
    {
        RebuildCache();
    }

    // Если UI динамически создаётся — можно дергать вручную
    public void RebuildCache()
    {
        texts.Clear();
        baseFontSize.Clear();
        baseFontSizeMin.Clear();
        baseFontSizeMax.Clear();

        var r = root != null ? root : transform;
        r.GetComponentsInChildren(true, texts);

        foreach (var t in texts)
        {
            if (t == null) continue;

            baseFontSize[t] = t.fontSize;
            baseFontSizeMin[t] = t.fontSizeMin;
            baseFontSizeMax[t] = t.fontSizeMax;
        }
    }

    public void Apply(float scale)
    {
        scale = Mathf.Clamp(scale, 0.75f, 1.5f);

        // на случай если какие-то тексты появились позже
        if (texts.Count == 0) RebuildCache();

        foreach (var t in texts)
        {
            if (t == null) continue;

            if (baseFontSize.TryGetValue(t, out var baseSize))
                t.fontSize = baseSize * scale;

            // Если где-то включен AutoSize — подгоним диапазон тоже
            if (t.enableAutoSizing)
            {
                if (baseFontSizeMin.TryGetValue(t, out var min))
                    t.fontSizeMin = min * scale;

                if (baseFontSizeMax.TryGetValue(t, out var max))
                    t.fontSizeMax = max * scale;
            }

            t.SetAllDirty();
        }

        Canvas.ForceUpdateCanvases();
    }
}
