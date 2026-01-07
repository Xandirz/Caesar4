using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle fullscreenToggle;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;

    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider textSizeSlider;

    [Header("External")]
    [SerializeField] private ResolutionSettings resolutionSettings;

    private bool isInitializing;

    private void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        isInitializing = true;
        HookEvents(false);                 // на всякий случай

        LoadToUI_WithoutNotify();          // выставляем значения НЕ вызывая onValueChanged
        ApplyCurrentToSystems();           // сразу применяем, чтобы звук/текст работали

        HookEvents(true);                  // и только потом слушатели
        isInitializing = false;
    }

    private void OnDisable()
    {
        HookEvents(false);
    }

    private void LoadToUI_WithoutNotify()
    {
        var s = SettingsManager.Instance.Current;

        // Toggle
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(s.fullscreen);

        // Sliders
        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(s.masterVolume);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(s.musicVolume);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.SetValueWithoutNotify(s.mouseSensitivity);

        if (textSizeSlider != null)
            textSizeSlider.SetValueWithoutNotify(s.textScale);
    }

    private void ApplyCurrentToSystems()
    {
        var s = SettingsManager.Instance.Current;

        // применяем звук и текст сразу (иначе будет "не работает пока не подвинуть")
        SettingsManager.Instance.ApplyAudio(s.masterVolume, s.musicVolume);
        SettingsManager.Instance.ApplyTextScale(s.textScale);
        SettingsManager.Instance.ApplyMouseSensitivity(s.mouseSensitivity);
    }

    private void HookEvents(bool hook)
    {
        if (fullscreenToggle == null || masterSlider == null || musicSlider == null ||
            mouseSensitivitySlider == null || textSizeSlider == null)
            return;

        if (hook)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            textSizeSlider.onValueChanged.AddListener(OnTextScaleChanged);
        }
        else
        {
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
            textSizeSlider.onValueChanged.RemoveListener(OnTextScaleChanged);
        }
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        if (isInitializing) return;

        // ВАЖНО: мы не выставляем fullscreen напрямую, а просим ResolutionSettings переключить режим,
        // чтобы он же применил Screen.SetResolution и сохранил в PlayerPrefs.
        if (resolutionSettings != null)
            resolutionSettings.ToggleFullscreen();

        // синхронизируем наш SettingsManager.Current (чтобы UI/сейв были консистентны)
        var s = SettingsManager.Instance.Current;
        s.fullscreen = PlayerPrefs.GetInt("fullscreen", 1) == 1;
        SettingsManager.Instance.Save();

        // Чтобы сам Toggle соответствовал реальному состоянию (на всякий случай)
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(s.fullscreen);
    }

    private void OnMasterChanged(float v)
    {
        if (isInitializing) return;

        var s = SettingsManager.Instance.Current;
        s.masterVolume = Mathf.Clamp01(v);

        SettingsManager.Instance.ApplyAudio(s.masterVolume, s.musicVolume);
        SettingsManager.Instance.Save();
    }

    private void OnMusicChanged(float v)
    {
        if (isInitializing) return;

        var s = SettingsManager.Instance.Current;
        s.musicVolume = Mathf.Clamp01(v);

        SettingsManager.Instance.ApplyAudio(s.masterVolume, s.musicVolume);
        SettingsManager.Instance.Save();
    }

    private void OnMouseSensitivityChanged(float v)
    {
        if (isInitializing) return;

        var s = SettingsManager.Instance.Current;
        s.mouseSensitivity = Mathf.Clamp(v, 0.1f, 5f);

        SettingsManager.Instance.ApplyMouseSensitivity(s.mouseSensitivity);
        SettingsManager.Instance.Save();
    }

    private void OnTextScaleChanged(float v)
    {
        if (isInitializing) return;

        var s = SettingsManager.Instance.Current;
        s.textScale = Mathf.Clamp(v, 0.75f, 1.5f);

        SettingsManager.Instance.ApplyTextScale(s.textScale);
        SettingsManager.Instance.Save();
    }
}
