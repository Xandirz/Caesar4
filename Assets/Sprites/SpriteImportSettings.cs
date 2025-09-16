using UnityEngine;
using UnityEditor;

public class SpriteImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        TextureImporter importer = (TextureImporter)assetImporter;

        // ограничим для папки Sprites
        if (!assetPath.Contains("/Sprites/")) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Загружаем настройки
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        // Pivot = Bottom Center
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);

        // Применяем
        importer.SetTextureSettings(settings);

        // Формат RGBA32
        var platformSettings = new TextureImporterPlatformSettings
        {
            name = "DefaultTexturePlatform",
            overridden = true,
            format = TextureImporterFormat.RGBA32,
            maxTextureSize = 2048,
            compressionQuality = 100
        };
        importer.SetPlatformTextureSettings(platformSettings);
    }
}