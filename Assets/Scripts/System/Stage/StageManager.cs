using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using UnityEngine.Scripting;

/// <summary>
/// ステージ管理を行うマネージャー。
/// どのシーンから開始しても自動的に初期化され、StageDatabaseを解決します。
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class StageManager : Singleton<StageManager>
{
    [Header("Data Source")]
    [SerializeField] private StageDatabase database;

    [Header("Runtime State")]
    [SerializeField] private string currentStageId;
    private GameObject _currentStageInstance;
    private GameObject _currentPlayerInstance;

    private bool _isLoading;
    private bool _isDatabaseLoading;

    // プレイヤーが生成されたことを通知するストリーム
    private readonly Subject<Transform> _onPlayerSpawned = new();
    public Observable<Transform> OnPlayerSpawned => _onPlayerSpawned;


    protected override void Awake()
    {
        base.Awake();
    }

    private async void Start()
    {
        // データベースを先読みしておく。
        // ステージを置くタイミングは GameFlowManager が決めるため、ここでは読み込みだけ行う
        await EnsureDatabaseLoadedAsync();
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
            Debug.Log($"StageManager: Addressable Key '{AddressableKeys.StageDatabase}' を使用して StageDatabase のロードを開始します。");

            var handle = Addressables.LoadAssetAsync<StageDatabase>(AddressableKeys.StageDatabase);
            await handle.ToUniTask();

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                database = handle.Result;
                if (database != null)
                {
                    Debug.Log($"StageManager: '{AddressableKeys.StageDatabase}' のロードに成功しました。アセット名: {database.name}");
                }
                else
                {
                    Debug.LogError($"StageManager: '{AddressableKeys.StageDatabase}' のロードは成功しましたが、結果のデータベースが null です。Addressables グループの設定を確認してください。");
                }
            }
            else
            {
                // 詳細なエラー理由を出力
                Debug.LogError($"StageManager: '{AddressableKeys.StageDatabase}' のロードに失敗しました。Status: {handle.Status}");
                if (handle.OperationException != null)
                {
                    Debug.LogException(handle.OperationException);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"StageManager: Addressablesでの '{AddressableKeys.StageDatabase}' ロード中に例外が発生しました。Keyが正しいか、Addressable Groupsでアドレスが設定されているか確認してください。\nError: {e.Message}");
        }
        finally
        {
            _isDatabaseLoading = false;
        }
    }

    /// <summary>
    /// 先頭ステージの ID を返します。データベース未読込なら読み込みを待ちます。見つからなければ null。
    /// </summary>
    public async UniTask<string> GetFirstStageIdAsync()
    {
        await EnsureDatabaseLoadedAsync();
        return database?.GetFirstStageId();
    }

    /// <summary>
    /// 次のステージへ進みます。完了を待たない版。
    /// </summary>
    public void AdvanceToNextStage()
    {
        AdvanceToNextStageAsync().Forget();
    }

    /// <summary>
    /// 次のステージへ進みます。完了まで待てます。次が無ければ何もしません。
    /// </summary>
    public async UniTask AdvanceToNextStageAsync()
    {
        await EnsureDatabaseLoadedAsync();
        if (database == null) return;

        string nextId = database.GetNextStageId(currentStageId);
        if (string.IsNullOrEmpty(nextId))
        {
            Debug.Log("全ステージをクリアしました！");
            return;
        }

        await LoadStageAsync(nextId);
    }

    /// <summary>
    /// 現在のステージを最初からやり直します。完了を待たない版。
    /// </summary>
    public void ReloadCurrentStage()
    {
        ReloadCurrentStageAsync().Forget();
    }

    /// <summary>
    /// 現在のステージを最初からやり直します。完了まで待てます。
    /// </summary>
    public async UniTask ReloadCurrentStageAsync()
    {
        if (string.IsNullOrEmpty(currentStageId))
        {
            Debug.LogWarning("StageManager: リロード対象のステージIDが設定されていません。");
            return;
        }

        await LoadStageAsync(currentStageId);
    }

    /// <summary>
    /// 指定されたIDのステージを読み込み、プレイヤーを配置します。完了を待たない版。
    /// </summary>
    public void LoadStage(string stageId)
    {
        LoadStageAsync(stageId).Forget();
    }

    /// <summary>
    /// 指定されたIDのステージを読み込み、プレイヤーを配置します。完了まで待てます。
    /// 読み込み中に来た要求は無視します（データベースの読み込み待ちも含む）。
    /// </summary>
    public async UniTask LoadStageAsync(string stageId)
    {
        if (_isLoading)
        {
            Debug.LogWarning($"StageManager: ステージ読み込み中のため '{stageId}' への要求を無視します。");
            return;
        }

        _isLoading = true;
        try
        {
            await EnsureDatabaseLoadedAsync();
            if (database == null) return;

            var info = database.GetStageInfo(stageId);

            if (info == null || info.Prefab == null)
            {
                Debug.LogError($"StageManager: ID '{stageId}' のステージ定義が見つからないか、プレハブが未設定です。");
                return;
            }

            currentStageId = stageId;
            ProcessStageLoading(info.Prefab);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// ステージ生成の統合処理。既存のステージと Player をすべて破棄してから、新しいステージと Player を生成します。
    /// 手置きのオブジェクトも自分が生成したものも区別せず破棄するため、生成後は常にステージ 1 つ・Player 1 体になります。
    /// </summary>
    private void ProcessStageLoading(StageData stagePrefab)
    {
        DestroyExistingStages();
        DestroyExistingPlayers();

        StageData stageInstance = SpawnStage(stagePrefab);
        SpawnPlayer(stageInstance);
    }

    /// <summary>
    /// シーン内の StageData を持つオブジェクトをすべて破棄します。
    /// </summary>
    private void DestroyExistingStages()
    {
        foreach (var stage in FindObjectsByType<StageData>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Destroy(stage.gameObject);
        }
        _currentStageInstance = null;
    }

    /// <summary>
    /// シーン内の Player をすべて探し、それぞれのルート（PlayerParent）を破棄します。
    /// </summary>
    private void DestroyExistingPlayers()
    {
        var roots = new HashSet<GameObject>();
        foreach (var player in FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            roots.Add(player.transform.root.gameObject);
        }
        foreach (var root in roots)
        {
            Destroy(root);
        }
        _currentPlayerInstance = null;
    }

    /// <summary>
    /// ステージを生成して保持します。
    /// </summary>
    private StageData SpawnStage(StageData stagePrefab)
    {
        StageData stageInstance = Instantiate(stagePrefab);
        _currentStageInstance = stageInstance.gameObject;
        return stageInstance;
    }

    /// <summary>
    /// ステージの SpawnPoint に Player を生成して保持し、生成を通知します。
    /// </summary>
    private void SpawnPlayer(StageData stageInstance)
    {
        if (database.PlayerParentPrefab == null)
        {
            Debug.LogError("StageManager: StageDatabase に PlayerParentPrefab が設定されていません。これが原因でプレイヤーが生成されません。");
            return;
        }

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
            Debug.LogWarning($"StageManager: ステージ '{stageInstance.name}' に SpawnPoint が設定されていません。");
        }

        _currentPlayerInstance = Instantiate(database.PlayerParentPrefab, spawnPos, spawnRot);
        Debug.Log($"StageManager: プレイヤーを生成しました。位置: {spawnPos}");

        // 生成されたPlayerコンポーネントを起点に通知を発行
        var player = _currentPlayerInstance.GetComponentInChildren<Player>();
        if (player != null)
        {
            _onPlayerSpawned.OnNext(player.transform);
        }
    }

    private void OnDestroy()
    {
        _onPlayerSpawned.OnCompleted();
        _onPlayerSpawned.Dispose();
    }
}