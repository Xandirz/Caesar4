using UnityEngine;
using UnityEditor;

public class SpriteImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        TextureImporter importer = (TextureImporter)assetImporter;

        if (!assetPath.Contains("/Sprites/"))
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Устанавливаем режим спрайта
        importer.spriteImportMode = SpriteImportMode.Single;

        // Устанавливаем pivot напрямую
        importer.spritePivot = new Vector2(0.5f, 0f);

        // Также настраиваем через TextureImporterSettings, чтобы точно применилось
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);

        importer.SetTextureSettings(settings);

        // Настройки платформы
        TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
        platformSettings.overridden = true;
        platformSettings.format = TextureImporterFormat.RGBA32;
        platformSettings.maxTextureSize = 2048;
        platformSettings.compressionQuality = 100;
        importer.SetPlatformTextureSettings(platformSettings);
    }
}