using UnityEngine;
using UnityEngine.UI;

public class ResolutionSettings : MonoBehaviour
{
    [SerializeField] private Dropdown dropdown;

    private bool fullscreen = true; // ⬅ стартуем в fullscreen

    private readonly Vector2Int[] presets =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1366, 768),
        new Vector2Int(1280, 800),
    };

    private const string PrefKeyResolution = "resolution_preset_index";
    private const string PrefKeyFullscreen = "fullscreen";

    private void Start()
    {
        // Заполняем dropdown, если пустой
        if (dropdown.options.Count == 0)
        {
            dropdown.ClearOptions();
            foreach (var p in presets)
                dropdown.options.Add(new Dropdown.OptionData($"{p.x}×{p.y}"));
        }

        // Загружаем fullscreen (по умолчанию true)
        fullscreen = PlayerPrefs.GetInt(PrefKeyFullscreen, 1) == 1;

        // Загружаем пресет разрешения
        int savedResolution = PlayerPrefs.GetInt(PrefKeyResolution, 0);
        savedResolution = Mathf.Clamp(savedResolution, 0, presets.Length - 1);

        dropdown.SetValueWithoutNotify(savedResolution);

        ApplyPreset(savedResolution);
    }

    private void Update()
    {
        // Переключение fullscreen по клавише F
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFullscreen();
        }
    }

    public void OnDropdownChanged(int index)
    {
        ApplyPreset(index);

        PlayerPrefs.SetInt(PrefKeyResolution, index);
        PlayerPrefs.Save();

        Debug.Log("Resolution index: " + index);
    }

    private void ToggleFullscreen()
    {
        fullscreen = !fullscreen;

        int index = dropdown.value;
        ApplyPreset(index);

        PlayerPrefs.SetInt(PrefKeyFullscreen, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Fullscreen: " + fullscreen);
    }

    private void ApplyPreset(int index)
    {
        var p = presets[Mathf.Clamp(index, 0, presets.Length - 1)];

        Screen.SetResolution(
            p.x,
            p.y,
            fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed
        );
    }
}
