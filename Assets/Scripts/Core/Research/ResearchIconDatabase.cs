using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Research/Icon Database")]
public class ResearchIconDatabase : ScriptableObject
{
    [Header("Resources folder path (without 'Resources/')")]
    [SerializeField] private string resourcesFolder = "Sprites/Research";

    private Dictionary<string, Sprite> cache;

    public Sprite Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (cache == null)
            BuildCache();

        return cache.TryGetValue(id, out var s) ? s : null;
    }

    private void BuildCache()
    {
        cache = new Dictionary<string, Sprite>();

        var sprites = Resources.LoadAll<Sprite>(resourcesFolder);
        for (int i = 0; i < sprites.Length; i++)
        {
            var s = sprites[i];
            if (s == null) continue;

            // если есть дубликаты имён — последний перезапишет
            cache[s.name] = s;
        }
    }

    public void WarmUp() => BuildCache(); // можно вызвать в Start, чтобы прогреть кэш
}