using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotRowUI : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;   // Дата сохранения
    [SerializeField] TMP_Text metaText;    // Население
    [SerializeField] Button loadButton;
    [SerializeField] Button deleteButton;  // опционально, можно убрать
    [SerializeField] Image previewImage;

    string saveId; // имя файла без .json

    /// <summary>
    /// Привязка строки к сохранению
    /// </summary>
    public void Bind(SaveEntryInfo  info)
    {
        saveId = info.id;

        if (info.corrupted)
        {
            titleText.text = saveId;
            metaText.text = "Corrupted save";
            loadButton.interactable = false;
            if (deleteButton) deleteButton.interactable = true;
            return;
        }

        DateTimeOffset time =
            DateTimeOffset.FromUnixTimeSeconds(info.savedAtUnix).ToLocalTime();

        titleText.text = time.ToString("yyyy-MM-dd HH:mm:ss");
        metaText.text = $"Population: {info.people}";

        loadButton.interactable = true;

        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(OnLoadClicked);

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }
        
        if (previewImage != null)
        {
            previewImage.sprite = null;
            previewImage.enabled = false;

            if (!info.corrupted && !string.IsNullOrEmpty(info.previewFile))
            {
                string abs = Path.Combine(
                    Application.persistentDataPath,
                    "saves",
                    info.previewFile
                );

                if (File.Exists(abs))
                {
                    byte[] bytes = File.ReadAllBytes(abs);

                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        var sprite = Sprite.Create(
                            tex,
                            new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f),
                            100f
                        );

                        previewImage.sprite = sprite;
                        previewImage.enabled = true;
                    }
                    else
                    {
                        Destroy(tex);
                    }
                }
            }
        }


    }

    void OnLoadClicked()
    {
        SaveLoadManager.Instance.Load(saveId);
    }

    void OnDeleteClicked()
    {
        SaveLoadManager.Instance.DeleteSave(saveId);
        Destroy(gameObject);
    }
}