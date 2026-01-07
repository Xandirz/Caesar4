using System;

[Serializable]
public struct SaveSlotInfo
{
    public int index;           // 0..9
    public string slotId;       // "slot1".."slot10"
    public bool exists;         // файл есть
    public bool corrupted;      // файл есть, но не прочитался
    public int saveNumber;      // число
    public long savedAtUnix;    // время

    public DateTimeOffset SavedAtLocal =>
        (savedAtUnix > 0)
            ? DateTimeOffset.FromUnixTimeSeconds(savedAtUnix).ToLocalTime()
            : DateTimeOffset.MinValue;
}