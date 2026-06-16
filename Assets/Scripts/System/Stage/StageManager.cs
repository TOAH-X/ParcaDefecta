using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ステージ管理を行うマネージャー。
/// どのシーンから開始しても自動的に初期化され、StageDatabaseを解決します。
/// </summary>
public class StageManager : Singleton<StageManager>
{
    [Header("Data Source")]
    [SerializeField] private StageDatabase database;
    private const string DatabasePath = "StageDatabase";

    [Header("Runtime State")]
    [SerializeField] private string currentStageId;
    private GameObject _currentStageInstance;
    private GameObject _currentPlayerInstance;

    private bool _isLoading;

    /// <summary>
    /// [n=0を実現するための自動初期化]
    /// ゲーム開始時に自動的に呼び出され、インスタンスを生成します。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSystem()
    {
        // Instanceを呼ぶことで、Singleton基底クラスのロジックにより
        // GameObjectが生成され、DontDestroyOnLoadに登録されます。
        var _ = Instance;
    }

    protected override void Awake()
    {
        base.Awake();
        EnsureDatabaseLoaded();
    }

    /// <summary>
    /// データベースアセットが読み込まれていることを保証します。
    /// </summary>
    private void EnsureDatabaseLoaded()
    {
        if (database != null) return;

        // 1. まずResourcesから試行
        database = Resources.Load<StageDatabase>(DatabasePath);

        // 2. なければメモリ上（Preloaded Assets等）から検索
        if (database == null)
        {
            database = FindDatabaseInProject();
        }

        if (database == null)
        {
            Debug.LogError($"StageManager: StageDatabase が見つかりません。インスペクターにセットするか、Resources/{DatabasePath} に配置するか、Preloaded Assets に登録してください。");
        }
    }

    private StageDatabase FindDatabaseInProject()
    {
        // 1. Resourcesフォルダを使わず、現在メモリにロードされているアセットから探す
        // (Preloaded Assets に登録されている場合に有効)
        var dbs = Resources.FindObjectsOfTypeAll<StageDatabase>();
        if (dbs.Length > 0) return dbs[0];

        Debug.LogError("StageManager: StageDatabaseが見つかりません。Project SettingsのPreloaded Assetsに登録するか、Resourcesフォルダに配置してください。");
        return null;
    }

    /// <summary>
    /// ゲームを最初のステージから開始します（UIなどから呼び出し）。
    /// </summary>
    public void StartGame()
    {
        EnsureDatabaseLoaded();
        string firstId = database?.GetFirstStageId();
        if (!string.IsNullOrEmpty(firstId)) LoadStage(firstId);
    }

    /// <summary>
    /// これは使わない。
    /// 次のステージへ進みます（ゴール地点のトリガーなどから呼び出し）。
    /// </summary>
    public void AdvanceToNextStage()
    {
        if (database == null) return;
        string nextId = database.GetNextStageId(currentStageId);
        if (!string.IsNullOrEmpty(nextId)) LoadStage(nextId);
        else Debug.Log("全ステージをクリアしました！");
    }

    /// <summary>
    /// 現在のステージを最初からやり直します。
    /// </summary>
    public void ReloadCurrentStage()
    {
        if (string.IsNullOrEmpty(currentStageId))
        {
            Debug.LogWarning("StageManager: リロード対象のステージIDが設定されていません。");
            return;
        }
        LoadStage(currentStageId);
    }

    /// <summary>
    /// 指定されたIDのステージを読み込み、プレイヤーを配置します。
    /// </summary>
    public void LoadStage(string stageId)
    {
        if (_isLoading) return;

        EnsureDatabaseLoaded();
        if (database == null) return;

        var info = database.GetStageInfo(stageId);

        if (info == null || info.Prefab == null)
        {
            Debug.LogError($"StageManager: ID '{stageId}' のステージ定義が見つからないか、プレハブが未設定です。");
            return;
        }

        _isLoading = true;
        currentStageId = stageId;
        ProcessStageLoading(info.Prefab);
        _isLoading = false;
    }

    private void ProcessStageLoading(StageData stagePrefab)
    {
        // 古いインスタンスを破棄
        if (_currentPlayerInstance != null) Destroy(_currentPlayerInstance);
        if (_currentStageInstance != null) Destroy(_currentStageInstance);

        // ステージの生成
        StageData stageInstance = Instantiate(stagePrefab);
        _currentStageInstance = stageInstance.gameObject;

        // スポーン座標の取得 (StageDataから直接取得)
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (stageInstance.SpawnPoint != null)
        {
            spawnPos = stageInstance.SpawnPoint.position;
            spawnRot = stageInstance.SpawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning($"StageManager: プレハブ '{stagePrefab.name}' に SpawnPoint が設定されていません。");
        }

        // プレイヤーの生成
        if (database.PlayerParentPrefab != null)
        {
            _currentPlayerInstance = Instantiate(database.PlayerParentPrefab, spawnPos, spawnRot);
        }
    }
}