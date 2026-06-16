using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ステージ単体の定義データ。
/// </summary>
[System.Serializable]
public class StageInfo
{
    [SerializeField] private string id;
    [SerializeField] private StageData prefab;

    public string Id => id;
    public StageData Prefab => prefab;
}

/// <summary>
/// ステージのプレハブ一覧とプレイヤーのプレハブを保持するデータアセット。
/// </summary>
[CreateAssetMenu(fileName = "StageDatabase", menuName = "StageDatabase")]
public class StageDatabase : ScriptableObject
{
    [Header("Prefab Settings")]
    [SerializeField] private GameObject playerParentPrefab;
    public GameObject PlayerParentPrefab => playerParentPrefab;

    [Header("Stages")]
    [SerializeField] private List<StageInfo> stageDefinitions = new List<StageInfo>();
    public IReadOnlyList<StageInfo> StageDefinitions => stageDefinitions;

    /// <summary>
    /// 指定されたIDのステージ情報を取得します。
    /// </summary>
    public StageInfo GetStageInfo(string id) => stageDefinitions.FirstOrDefault(s => s.Id == id);

    /// <summary>
    /// 最初のステージIDを取得します。
    /// </summary>
    public string GetFirstStageId() => stageDefinitions.FirstOrDefault()?.Id;

    /// <summary>
    /// 指定されたIDの次のステージIDを取得します。
    /// </summary>
    public string GetNextStageId(string currentId)
    {
        int index = stageDefinitions.FindIndex(s => s.Id == currentId);
        if (index < 0 || index >= stageDefinitions.Count - 1) return null;
        return stageDefinitions[index + 1].Id;
    }

    private void OnValidate()
    {
        // IDの重複チェック（エディタ上での入力ミス防止）
        var duplicateIds = stageDefinitions
            .GroupBy(s => s.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var dupId in duplicateIds)
        {
            if (!string.IsNullOrEmpty(dupId))
            {
                Debug.LogError($"StageDatabase: ID '{dupId}' が重複しています。ユニークなIDを設定してください。");
            }
        }
    }
}