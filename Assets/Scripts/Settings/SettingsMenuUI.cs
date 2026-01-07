using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle fullscreenToggle;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider mouseSensitivitySlider;

    [Header("External")]
    [SerializeField] private ResolutionSettings resolutionSettings;

    [Header("Panel Root")]
    [SerializeField] private GameObject settingsPanel; // корневая панель настроек (её будем выключать после инициализации)

    private Coroutine initRoutine;
    private bool isInitialized = false;
    private bool isInitializing = false;

    private void OnEnable()
    {
        Debug.Log("[SettingsMenuUI] OnEnable called");

        // Если уже инициализировались ранее — ничего не делаем (только UI включился снова)
        if (isInitialized)
        {
            Debug.Log("[SettingsMenuUI] Уже инициализированы, повторно не инициализируем");
            return;
        }

        if (initRoutine != null)
            StopCoroutine(initRoutine);

        initRoutine = StartCoroutine(InitWhenReady());
    }

    private IEnumerator InitWhenReady()
    {
        int frames = 0;

        while (SettingsManager.Instance == null)
        {
            frames++;

            if (frames == 1)
                Debug.LogWarning("[SettingsMenuUI] ⏳ Ждём SettingsManager.Instance...");

            if (frames >= 300) // ~5 секунд при 60fps
            {
                Debug.LogError("[SettingsMenuUI] ❌ SettingsManager не появился за 300 кадров. Проверь сцену/объекты.");
                yield break;
            }

            yield return null;
        }

        Debug.Log($"[SettingsMenuUI] ✅ SettingsManager появился через {frames} кадров → инициализируем UI");

        isInitializing = true;

        Debug.Log("[SettingsMenuUI] Отключаем события UI");
        HookEvents(false);

        Debug.Log("[SettingsMenuUI] Загружаем значения в UI");
        LoadToUI_WithoutNotify();

        Debug.Log("[SettingsMenuUI] Применяем значения к системам");
        ApplyCurrentToSystems();

        Debug.Log("[SettingsMenuUI] Подключаем события UI");
        HookEvents(true);

        isInitializing = false;
        isInitialized = true;

        Debug.Log("[SettingsMenuUI] ✅ Инициализация SettingsMenuUI завершена");

        // После успешной инициализации — выключаем панель, чтобы не мешала старту игры
        if (settingsPanel != null)
        {
            Debug.Log("[SettingsMenuUI] 🧩 Деактивируем settingsPanel после инициализации");
            settingsPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[SettingsMenuUI] settingsPanel не назначен в инспекторе — не могу деактивировать");
        }

        initRoutine = null;
    }

    private void OnDisable()
    {
        Debug.Log("[SettingsMenuUI] OnDisable");

        // ВАЖНО:
        // - НЕ отписываем события здесь, иначе при следующем открытии меню (isInitialized=true)
        //   подписки не восстановятся и слайдеры "перестанут работать".
        // - Корутину остановим только если мы ещё не успели инициализироваться.
        if (!isInitialized && initRoutine != null)
        {
            StopCoroutine(initRoutine);
            initRoutine = null;
        }
    }

    private void LoadToUI_WithoutNotify()
    {
        var s = SettingsManager.Instance.Current;

        Debug.Log($"[SettingsMenuUI] LoadToUI → Master={s.masterVolume}, Music={s.musicVolume}, MouseSens={s.mouseSensitivity}, Fullscreen={s.fullscreen}");

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(s.fullscreen);

        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(s.masterVolume);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(s.musicVolume);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.SetValueWithoutNotify(s.mouseSensitivity);
    }

    private void ApplyCurrentToSystems()
    {
        var s = SettingsManager.Instance.Current;

        // применяем сразу, чтобы эффект был без "подвинь слайдер"
        SettingsManager.Instance.ApplyAudio(s.masterVolume, s.musicVolume);
        SettingsManager.Instance.ApplyMouseSensitivity(s.mouseSensitivity);
    }

    private void HookEvents(bool hook)
    {
        Debug.Log(
            $"[SettingsMenuUI] HookEvents({hook}) → " +
            $"fullscreen={fullscreenToggle != null}, " +
            $"master={masterSlider != null}, " +
            $"music={musicSlider != null}, " +
            $"mouse={mouseSensitivitySlider != null}"
        );

        if (hook)
        {
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }
        else
        {
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
        }
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Debug.Log($"[SettingsMenuUI] OnFullscreenChanged → {isFullscreen} (isInitializing={isInitializing})");
        if (isInitializing) return;

        // ВАЖНО: переключаем через ResolutionSettings, чтобы он применил Screen.SetResolution и сохранил в PlayerPrefs.
        if (resolutionSettings != null)
            resolutionSettings.ToggleFullscreen();

        // синхронизируем Current с тем, что реально сохранилось
        var s = SettingsManager.Instance.Current;
        s.fullscreen = PlayerPrefs.GetInt("fullscreen", 1) == 1;

        SettingsManager.Instance.Save();

        // на всякий случай синхронизируем UI
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(s.fullscreen);
    }

    private void OnMasterChanged(float v)
    {
        Debug.Log($"[SettingsMenuUI] OnMasterChanged → {v} (isInitializing={isInitializing})");
        if (isInitializing) return;

        var s = SettingsManager.Instance.Current;
        s.masterVolume = Mathf.Clamp01(v);

        SettingsManager.Instance.ApplyAudio(s.masterVolume, s.musicVolume);
        SettingsManager.Instance.Save();
    }

    private void OnMusicChanged(float v)
    {
        Debug.Log($"[SettingsMenuUI] OnMusicChanged → {v} (isInitializing={isInitializing})");
        if (isInitializing) return;

        var s = SettingsManager.Instance.Current;
        s.musicVolume = Mathf.Clamp01(v);

        SettingsManager.Instance.ApplyAudio(s.masterVolume, s.musicVolume);
        SettingsManager.Instance.Save();
    }

    private void OnMouseSensitivityChanged(float v)
    {
        Debug.Log($"[SettingsMenuUI] OnMouseSensitivityChanged → {v} (isInitializing={isInitializing})");
        if (isInitializing) return;

        var s = SettingsManager.Instance.Current;
        s.mouseSensitivity = Mathf.Clamp(v, 0.1f, 5f);

        SettingsManager.Instance.ApplyMouseSensitivity(s.mouseSensitivity);
        SettingsManager.Instance.Save();
    }
}
