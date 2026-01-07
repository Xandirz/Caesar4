using UnityEngine;

[System.Serializable]
public class GameSettings
{
    // Дефолты (важно!)
    public const float DEFAULT_MASTER = 1f;
    public const float DEFAULT_MUSIC = 0.8f;
    public const float DEFAULT_MOUSE = 1f;
    public const float DEFAULT_TEXT = 1f;

    public int resolutionIndex = 0;
    public bool fullscreen = true;

    public float masterVolume = DEFAULT_MASTER;   // 0..1
    public float musicVolume  = DEFAULT_MUSIC;    // 0..1
    public float mouseSensitivity = DEFAULT_MOUSE; // 0.1..5
    public float textScale = DEFAULT_TEXT;        // 0.75..1.5

    private const string KEY = "game_settings_v1";

    public static bool HasSaved() => PlayerPrefs.HasKey(KEY);

    public static GameSettings Load()
    {
        // Если сохранений нет — возвращаем нормальные дефолты
        if (!PlayerPrefs.HasKey(KEY))
        {
            var fresh = new GameSettings();
            fresh.ValidateAndFix();
            return fresh;
        }

        var json = PlayerPrefs.GetString(KEY, "");
        if (string.IsNullOrWhiteSpace(json))
        {
            var fresh = new GameSettings();
            fresh.ValidateAndFix();
            return fresh;
        }

        GameSettings s = null;
        try
        {
            s = JsonUtility.FromJson<GameSettings>(json);
        }
        catch
        {
            s = null;
        }

        if (s == null)
            s = new GameSettings();

        s.ValidateAndFix();
        return s;
    }

    public void ValidateAndFix()
    {
        // защита от NaN/Infinity
        if (!IsFinite(masterVolume)) masterVolume = DEFAULT_MASTER;
        if (!IsFinite(musicVolume))  musicVolume  = DEFAULT_MUSIC;
        if (!IsFinite(mouseSensitivity)) mouseSensitivity = DEFAULT_MOUSE;
        if (!IsFinite(textScale)) textScale = DEFAULT_TEXT;

        // Если у тебя исторически сохранялись нули — чинить
        // (иначе пользователь навсегда “залипнет” на 0 после первого бага)
        if (masterVolume <= 0f) masterVolume = DEFAULT_MASTER;
        if (musicVolume  < 0f)  musicVolume  = DEFAULT_MUSIC; // музыку можно 0, но отрицательное — нет

        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume  = Mathf.Clamp01(musicVolume);
        mouseSensitivity = Mathf.Clamp(mouseSensitivity, 0.1f, 5f);
        textScale = Mathf.Clamp(textScale, 0.75f, 1.5f);
    }

    private bool IsFinite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));

    public void Save()
    {
        ValidateAndFix();
        var json = JsonUtility.ToJson(this);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }
}
