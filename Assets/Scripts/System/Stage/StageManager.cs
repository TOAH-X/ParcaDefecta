using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using R3;
using Cysharp.Threading.Tasks;
using UnityEngine.Scripting;

/// <summary>
/// ステージ管理を行うマネージャー。
/// ステージ一覧（StageDatabase）は覚えておかず、処理を始めるその時に「今いるシーンの一覧」を SceneLoader に聞く。
/// 一覧はシーンごとに SceneDatabase で指定する（試験版・本番・デバッグで別の一覧を使うため）。
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class StageManager : Singleton<StageManager>
{
    [Header("Runtime State")]
    [SerializeField] private string currentStageId;
    private GameObject _currentStageInstance;
    private GameObject _currentPlayerInstance;

    /// <summary>
    /// 今置かれているステージ。無ければ null。
    /// 通知（OnStageSpawned）より後に受け取りを始める部品が、今の状態を知るために使う。
    /// </summary>
    public StageData CurrentStage => _currentStageInstance != null ? _currentStageInstance.GetComponent<StageData>() : null;

    /// <summary>
    /// 今の Player の Transform（OnPlayerSpawned で渡すものと同じ）。無ければ null。
    /// 通知（OnPlayerSpawned）より後に受け取りを始める部品が、今の状態を知るために使う。
    /// </summary>
    public Transform CurrentPlayer
    {
        get
        {
            if (_currentPlayerInstance == null) return null;
            var player = _currentPlayerInstance.GetComponentInChildren<Player>();
            return player != null ? player.transform : null;
        }
    }

    private bool _isLoading;

    // ステージが生成されたことを通知するストリーム（Player の生成より先に流れる）
    private readonly Subject<StageData> _onStageSpawned = new();
    public Observable<StageData> OnStageSpawned => _onStageSpawned;

    // プレイヤーが生成されたことを通知するストリーム
    private readonly Subject<Transform> _onPlayerSpawned = new();
    public Observable<Transform> OnPlayerSpawned => _onPlayerSpawned;


    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 今いるシーンのステージ一覧を引く。無ければエラーを出して null。
    /// 覚えておかず、処理を始めるたびに呼ぶ（シーンの切り替えと順番がずれないようにするため）。
    /// </summary>
    private async UniTask<StageDatabase> GetCurrentDatabaseAsync()
    {
        var database = await SceneLoader.Instance.GetCurrentStageDatabaseAsync();
        if (database == null)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.LogError($"StageManager: 今のシーン '{sceneName}' にステージ一覧が指定されていません。SceneDatabase を確認してください。");
        }
        return database;
    }

    /// <summary>
    /// 今いるシーンのステージ一覧の、先頭ステージの ID を返します。見つからなければ null。
    /// </summary>
    public async UniTask<string> GetFirstStageIdAsync()
    {
        var database = await GetCurrentDatabaseAsync();
        return database?.GetFirstStageId();
    }

    /// <summary>
    /// 今のステージの次があるかを返します（今のシーンのステージ一覧で判断）。
    /// 一覧が引けないときは true を返し、判断を進める側（読み込み）でエラーを出させる。
    /// </summary>
    public async UniTask<bool> HasNextStageAsync()
    {
        var database = await GetCurrentDatabaseAsync();
        if (database == null) return true;
        return !string.IsNullOrEmpty(database.GetNextStageId(currentStageId));
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
        var database = await GetCurrentDatabaseAsync();
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
    /// 読み込み中に来た要求は無視します（ステージ一覧を引いている間も含む）。
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
            var database = await GetCurrentDatabaseAsync();
            if (database == null) return;

            var info = database.GetStageInfo(stageId);

            if (info == null || info.Prefab == null)
            {
                Debug.LogError($"StageManager: ID '{stageId}' のステージ定義が見つからないか、プレハブが未設定です。");
                return;
            }

            currentStageId = stageId;
            ProcessStageLoading(info.Prefab, database.PlayerParentPrefab);
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
    private void ProcessStageLoading(StageData stagePrefab, GameObject playerParentPrefab)
    {
        DestroyExistingStages();
        DestroyExistingPlayers();

        StageData stageInstance = SpawnStage(stagePrefab);
        // カメラの範囲などを先に決めてから Player を置くため、Player の生成より前に通知する
        _onStageSpawned.OnNext(stageInstance);
        SpawnPlayer(stageInstance, playerParentPrefab);
    }

    /// <summary>
    /// シーン内の StageData を持つオブジェクトをすべて破棄します。
    /// Destroy はフレームの終わりまで実際には消えないため、先に非表示にして、
    /// 同じフレームに作る新しいステージと重ならないようにする（例: ステージが持つ Global Light 2D が 2 つになるとエラーになる）。
    /// </summary>
    private void DestroyExistingStages()
    {
        foreach (var stage in FindObjectsByType<StageData>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            stage.gameObject.SetActive(false);
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
    private void SpawnPlayer(StageData stageInstance, GameObject playerParentPrefab)
    {
        if (playerParentPrefab == null)
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

        _currentPlayerInstance = Instantiate(playerParentPrefab, spawnPos, spawnRot);
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
        _onStageSpawned.OnCompleted();
        _onStageSpawned.Dispose();
        _onPlayerSpawned.OnCompleted();
        _onPlayerSpawned.Dispose();
    }
}