using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 実績データのエントリ（AchievementDatabase内に直接保持される）
/// </summary>
[System.Serializable]
public class AchievementEntry
{
    [Header("実績情報")]
    public string id;               // 一意なID（例: "clear_stage_1"）
    public string title;            // 実績名（例: "ステージ１クリア"）
    public string description;      // 説明文
    public Sprite icon;             // アイコン画像

    [Header("解除条件")]
    public AchievementType type;    // 解除トリガーの種類
    public int targetValue;         // 目標値（例: 1 = 1回達成、10 = 10回達成）
}

[CreateAssetMenu(menuName = "AchievementDatabase")]
public class AchievementDatabase : ScriptableObject
{
    [SerializeField] public List<AchievementEntry> achievements = new List<AchievementEntry>();
}
