using System;

[Serializable]
public struct SaveMetaInfo
{
    public string id;        // имя без .json
    public string path;      // полный путь
    public long savedAtUnix; // время
    public int people;       // население
    public bool corrupted;   // если не смогли прочитать
}