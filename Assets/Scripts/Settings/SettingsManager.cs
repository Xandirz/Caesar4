using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    public bool settingNeedHouse = true; 

    [Header("Audio")]
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam = "MusicVol";

    [Header("UI Scale Root (optional)")]
    // Если используешь Canvas Scaler: можно управлять scaleFactor.
    // Или оставь null и мы будем менять TMP font size через отдельный контроллер.
    [Header("UI Text Scaling")]
    [SerializeField] private TextSizeApplier textSizeApplier;
// Presets (как в твоём ResolutionSettings)
    private readonly Vector2Int[] resolutionPresets =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1366, 768),
        new Vector2Int(1280, 800),
    };

    private const string PrefKeyResolution = "resolution_preset_index";
    private const string PrefKeyFullscreen = "fullscreen";

    public GameSettings Current { get; private set; }

    private void Awake()
    {
        Debug.Log("[SettingsManager] Awake() вызван");

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[SettingsManager] ⚠ Найден дубликат SettingsManager — уничтожаем текущий"
            );
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[SettingsManager] ✅ Instance установлен, объект помечен DontDestroyOnLoad");

        bool hadSaved = GameSettings.HasSaved();
        Debug.Log($"[SettingsManager] HasSaved = {hadSaved}");

        Current = GameSettings.Load();

        Debug.Log(
            "[SettingsManager] Загружены настройки: " +
            $"Master={Current.masterVolume}, " +
            $"Music={Current.musicVolume}, " +
            $"MouseSens={Current.mouseSensitivity}, " +
            $"Fullscreen={Current.fullscreen}"
        );

        Debug.Log("[SettingsManager] Применяем настройки к системам");
        ApplyAll(Current);

        // Если это первый запуск — запишем дефолты, чтобы UI всегда стартовал корректно
        if (!hadSaved)
        {
            Debug.Log("[SettingsManager] Первый запуск → сохраняем дефолтные настройки");
            Current.Save();
        }

        Debug.Log("[SettingsManager] ✅ Awake() завершён");
    }


    public void ApplyAll(GameSettings s)
    {
        ApplyAudio(s.masterVolume, s.musicVolume);
        ApplyMouseSensitivity(s.mouseSensitivity);
    }



    // Перевод 0..1 в децибелы для микшера
    public void ApplyAudio(float master01, float music01)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetMasterVolume(master01);
        AudioManager.Instance.SetMusicVolume(music01);

        Current.masterVolume = master01;
        Current.musicVolume = music01;
    }




    // Точку применения чувствительности лучше сделать в твоём контроллере камеры.
    public void ApplyMouseSensitivity(float sens)
    {
        Current.mouseSensitivity = Mathf.Clamp(sens, 0.1f, 5f);
        // Ничего не делаем тут напрямую: камера/инпут пусть читает SettingsManager.Instance.Current.mouseSensitivity
    }

    // Вариант 1: через Canvas scaleFactor (быстро, удобно для UI)




    public void Save()
    {
        Current.Save();
    }

    public void ResetToDefaults()
    {
        Current = new GameSettings();
        ApplyAll(Current);
        Save();
    }
}
