using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    // 今後の実績機能用のデータ定義（プレースホルダー）
    public List<AchievementSaveEntry> achievements = new List<AchievementSaveEntry>();
    
    // （拡張用）ステージクリア状況など、将来的に保存したい項目があればここに追記します
}

[Serializable]
public class AchievementSaveEntry
{
    public string id;
    public int currentValue;
    public bool isUnlocked;
}
