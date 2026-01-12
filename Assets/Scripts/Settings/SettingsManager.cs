using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // Это твой флаг
    public bool settingNeedHouse = true;

    [Header("Audio")]
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam = "MusicVol";

    [Header("UI Text Scaling")]
    [SerializeField] private TextSizeApplier textSizeApplier;

    private readonly Vector2Int[] resolutionPresets =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1366, 768),
        new Vector2Int(1280, 800),
    };

    private const string PrefKeyResolution = "resolution_preset_index";
    private const string PrefKeyFullscreen = "fullscreen";

    // ✅ НОВОЕ: ключ для settingNeedHouse
    private const string PrefKeyNeedHouse = "setting_need_house_v1";

    public GameSettings Current { get; private set; }

    private void Awake()
    {
        Debug.Log("[SettingsManager] Awake() вызван");

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SettingsManager] ⚠ Найден дубликат SettingsManager — уничтожаем текущий");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[SettingsManager] ✅ Instance установлен, объект помечен DontDestroyOnLoad");

        bool hadSaved = GameSettings.HasSaved();
        Debug.Log($"[SettingsManager] HasSaved = {hadSaved}");

        Current = GameSettings.Load();

        // ✅ НОВОЕ: грузим settingNeedHouse из PlayerPrefs (по умолчанию true)
        settingNeedHouse = PlayerPrefs.GetInt(PrefKeyNeedHouse, 1) == 1;

        Debug.Log(
            "[SettingsManager] Загружены настройки: " +
            $"Master={Current.masterVolume}, " +
            $"Music={Current.musicVolume}, " +
            $"MouseSens={Current.mouseSensitivity}, " +
            $"Fullscreen={Current.fullscreen}, " +
            $"NeedHouse={settingNeedHouse}"
        );

        Debug.Log("[SettingsManager] Применяем настройки к системам");
        ApplyAll(Current);

        // Если это первый запуск — запишем дефолты, чтобы UI всегда стартовал корректно
        if (!hadSaved)
        {
            Debug.Log("[SettingsManager] Первый запуск → сохраняем дефолтные настройки");
            Save(); // ✅ важно: тут сохранится и Current, и settingNeedHouse
        }

        Debug.Log("[SettingsManager] ✅ Awake() завершён");
    }

    public void ApplyAll(GameSettings s)
    {
        ApplyAudio(s.masterVolume, s.musicVolume);
        ApplyMouseSensitivity(s.mouseSensitivity);
    }

    public void ApplyAudio(float master01, float music01)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetMasterVolume(master01);
        AudioManager.Instance.SetMusicVolume(music01);

        Current.masterVolume = master01;
        Current.musicVolume = music01;
    }

    public void ApplyMouseSensitivity(float sens)
    {
        Current.mouseSensitivity = Mathf.Clamp(sens, 0.1f, 5f);
        // камера/инпут пусть читает SettingsManager.Instance.Current.mouseSensitivity
    }

    // ✅ НОВОЕ: публичный метод, чтобы UI/код менял флаг правильно
    public void SetNeedHouse(bool value, bool saveImmediately = true)
    {
        settingNeedHouse = value;

        if (saveImmediately)
            Save();
    }

    public void Save()
    {
        // сохраняем обычные настройки (JSON)
        Current.Save();

        // ✅ НОВОЕ: сохраняем settingNeedHouse отдельно
        PlayerPrefs.SetInt(PrefKeyNeedHouse, settingNeedHouse ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        Current = new GameSettings();
        settingNeedHouse = true; // ✅ дефолт
        ApplyAll(Current);
        Save();
    }
}
