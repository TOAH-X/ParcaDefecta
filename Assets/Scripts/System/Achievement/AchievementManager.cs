using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class AchievementManager : Singleton<AchievementManager>, ISaveable
{
    // Addressablesからロードされる実績マスターデータ一覧
    private List<AchievementEntry> allAchievements = new List<AchievementEntry>();

    // 実績IDをキーとした、メモリ上の進捗管理用ディクショナリ
    private Dictionary<string, AchievementSaveEntry> progressMap = new Dictionary<string, AchievementSaveEntry>();

    // 実績が解除された時のイベント
    public System.Action<AchievementEntry> OnAchievementUnlocked;

    protected override void Awake()
    {
        base.Awake();
        // 非同期初期化を開始
        _ = InitializeAsync(this.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// Addressablesから実績データベースをロードし、初期化する。
    /// </summary>
    private async UniTaskVoid InitializeAsync(CancellationToken token)
    {
        try
        {
            // Addressables から AchievementDatabase をロード
            var db = await Addressables
                .LoadAssetAsync<AchievementDatabase>("AchievementDatabase")
                .ToUniTask(cancellationToken: token);

            if (db == null)
            {
                Debug.LogError("[AchievementManager] AchievementDatabase が見つかりません。Addressables設定を確認してください。");
                return;
            }

            if (db.achievements == null)
            {
                Debug.LogError("[AchievementManager] AchievementDatabase の achievements がnullです。");
                return;
            }

            allAchievements = db.achievements;
            InitializeProgress();

            // ロード完了後にセーブシステムに登録・復元
            if (SaveSystemManager.Instance != null)
            {
                SaveSystemManager.Instance.Register(this);
                SaveSystemManager.Instance.Load();
            }
        }
        catch (System.OperationCanceledException)
        {
            Debug.LogWarning("[AchievementManager] 初期化がキャンセルされました");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AchievementManager] 初期化エラー: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.Unregister(this);
        }
    }

    // 全実績の進捗データを初期状態で用意する
    private void InitializeProgress()
    {
        if (allAchievements == null || allAchievements.Count == 0)
        {
            Debug.LogWarning("[AchievementManager] InitializeProgress: allAchievementsがnullまたは空です。");
            return;
        }

        progressMap.Clear();
        foreach (var data in allAchievements)
        {
            if (data == null || string.IsNullOrEmpty(data.id) || progressMap.ContainsKey(data.id))
                continue;

            progressMap[data.id] = new AchievementSaveEntry
            {
                id = data.id,
                currentValue = 0,
                isUnlocked = false
            };
        }
    }

    /// <summary>
    /// 実績の進捗を加算し、解除条件を満たした場合は実績を解除する。
    /// 各ゲームロジックからこのメソッドを呼び出す。
    /// 例: AchievementManager.Instance.NotifyProgress(AchievementType.ClearStage, 1);
    /// </summary>
    public void NotifyProgress(AchievementType type, int amount = 1)
    {
        // 対象となる実績を種類でフィルタリングして進捗を加算
        var targets = allAchievements.Where(a => a.type == type);
        bool anyUpdated = false;

        foreach (var data in targets)
        {
            if (!progressMap.TryGetValue(data.id, out var entry)) continue;

            // 解除済みであればスキップ
            if (entry.isUnlocked) continue;

            entry.currentValue += amount;
            anyUpdated = true;

            // 目標値を達成したら解除
            if (entry.currentValue >= data.targetValue)
            {
                entry.isUnlocked = true;
                entry.currentValue = data.targetValue;
                Debug.Log($"[AchievementManager] 実績解除: {data.title}");

                // イベント発火
                OnAchievementUnlocked?.Invoke(data);
                Debug.Log($"[AchievementManager] NotifyProgress: type={type}, amount={amount}, anyUpdated={anyUpdated},entry.currentValue={entry.currentValue}");
            }
        }

        // 変更があった場合は自動セーブ
        if (anyUpdated && SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.Save();
        }
    }

    // ---- ISaveable の実装 ----

    public string SaveDataId => "achievement_manager";

    public void PopulateSaveData(GameSaveData saveData)
    {
        saveData.achievements = new List<AchievementSaveEntry>(progressMap.Values);
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.achievements == null) return;

        foreach (var entry in saveData.achievements)
        {
            if (progressMap.ContainsKey(entry.id))
            {
                progressMap[entry.id] = entry;
            }
        }
    }

    // ---- デバッグ用 ----

    /// <summary>
    /// 現在の実績進捗をすべてコンソールに出力する。
    /// </summary>
    [ContextMenu("Debug: Log All Progress")]
    public void DebugLogAllProgress()
    {
        foreach (var entry in progressMap.Values)
        {
            Debug.Log($"[Achievement] id={entry.id}, value={entry.currentValue}, unlocked={entry.isUnlocked}");
        }
    }
}
