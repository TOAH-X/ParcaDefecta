using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

/// <summary>
/// ステージ管理を行うマネージャー。
/// どのシーンから開始しても自動的に初期化され、StageDatabaseを解決します。
/// </summary>
public class StageManager : Singleton<StageManager>
{
    [Header("Data Source")]
    [SerializeField] private StageDatabase database;
    private const string DatabaseAddress = "StageDatabase";

    [Header("Runtime State")]
    [SerializeField] private string currentStageId;
    private GameObject _currentStageInstance;
    private GameObject _currentPlayerInstance;

    private bool _isLoading;
    private bool _isDatabaseLoading;

    // プレイヤーが生成されたことを通知するストリーム
    private readonly Subject<Transform> _onPlayerSpawned = new();
    public Observable<Transform> OnPlayerSpawned => _onPlayerSpawned;


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
    }

    private async void Start()
    {
        // データベースをAddressablesからロード
        await EnsureDatabaseLoadedAsync();

        // 安全策：もしシーンに既にPlayerやStageが存在している場合は管理下に置く
        // これにより、手動配置されたプレイヤーが最初のステージロード時に正しく破棄・置換されます。
        if (_currentPlayerInstance == null && Player.Instance != null)
        {
            // PlayerParentを取得
            _currentPlayerInstance = Player.Instance.transform.root.gameObject;
        }

        // ステージについても同様に、手動配置されている場合は管理下に置く
        if (_currentStageInstance == null)
        {
            StageData existingStage = FindAnyObjectByType<StageData>();
            if (existingStage != null)
            {
                _currentStageInstance = existingStage.gameObject;
            }
        }

        // デバッグ用：初期ステージが設定されていなければ開始
        if (string.IsNullOrEmpty(currentStageId))
        {
            StartGame();
        }
    }

    private async UniTask EnsureDatabaseLoadedAsync()
    {
        if (database != null) return;

        // 重複ロードを防止
        if (_isDatabaseLoading)
        {
            await UniTask.WaitUntil(() => database != null || !_isDatabaseLoading, PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
            return;
        }

        _isDatabaseLoading = true;

        // システムの初期化。既に終わっていればすぐ返ります。
        var initHandle = Addressables.InitializeAsync();
        await initHandle.ToUniTask();

        try
        {
            Debug.Log($"StageManager: Addressable Key '{DatabaseAddress}' を使用して StageDatabase のロードを開始します。");

            var handle = Addressables.LoadAssetAsync<StageDatabase>(DatabaseAddress);
            await handle.ToUniTask();

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                database = handle.Result;
                if (database != null)
                {
                    Debug.Log($"StageManager: '{DatabaseAddress}' のロードに成功しました。アセット名: {database.name}");
                }
                else
                {
                    Debug.LogError($"StageManager: '{DatabaseAddress}' のロードは成功しましたが、結果のデータベースが null です。Addressables グループの設定を確認してください。");
                }
            }
            else
            {
                // 詳細なエラー理由を出力
                Debug.LogError($"StageManager: '{DatabaseAddress}' のロードに失敗しました。Status: {handle.Status}");
                if (handle.OperationException != null)
                {
                    Debug.LogException(handle.OperationException);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"StageManager: Addressablesでの '{DatabaseAddress}' ロード中に例外が発生しました。Keyが正しいか、Addressable Groupsでアドレスが設定されているか確認してください。\nError: {e.Message}");
        }
        finally
        {
            _isDatabaseLoading = false;
        }
    }

    /// <summary>
    /// ゲームを最初のステージから開始します（UIなどから呼び出し）。
    /// </summary>
    public async void StartGame()
    {
        await EnsureDatabaseLoadedAsync();
        string firstId = database?.GetFirstStageId();
        if (!string.IsNullOrEmpty(firstId)) LoadStage(firstId);
    }

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
        LoadStageAsync(stageId).Forget();
    }

    private async UniTaskVoid LoadStageAsync(string stageId)
    {
        await EnsureDatabaseLoadedAsync();
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
            Debug.Log($"StageManager: プレイヤーを生成しました。位置: {spawnPos}");

            // 生成されたPlayerコンポーネントを起点に通知を発行
            var player = _currentPlayerInstance.GetComponentInChildren<Player>();
            if (player != null)
            {
                _onPlayerSpawned.OnNext(player.transform);
            }
        }
        else
        {
            Debug.LogError("StageManager: StageDatabase に PlayerParentPrefab が設定されていません。これが原因でプレイヤーが生成されません。");
        }
    }

    private void OnDestroy()
    {
        _onPlayerSpawned.OnCompleted();
        _onPlayerSpawned.Dispose();
    }
}