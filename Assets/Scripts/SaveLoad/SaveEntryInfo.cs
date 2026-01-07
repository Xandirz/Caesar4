using System;

[Serializable]
public struct SaveEntryInfo
{
    // ID сохранения = имя файла без расширения
    // пример: "save_2026-01-07_21-43-12-153"
    public string id;

    // Имя файла с расширением
    // пример: "save_2026-01-07_21-43-12-153.json"
    public string fileName;
    public string previewFile;

    // Полный путь к файлу
    public string fullPath;

    // true, если файл не удалось прочитать
    public bool corrupted;

    // META: население (People) на момент сохранения
    public int people;

    // META: время сохранения (Unix seconds, UTC)
    public long savedAtUnix;

    // Локальное время для отображения в UI
    public DateTimeOffset SavedAtLocal =>
        (savedAtUnix > 0)
            ? DateTimeOffset.FromUnixTimeSeconds(savedAtUnix).ToLocalTime()
            : DateTimeOffset.MinValue;
}