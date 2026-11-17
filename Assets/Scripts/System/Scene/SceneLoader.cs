using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

/// <summary>
/// シーン遷移の窓口。シーン遷移は必ずトランジション演出を通る。
/// 行き先はシーン名ではなくシーンの種類（SceneType）で受け取り、SceneDatabase で実際のシーンに変換する。
/// 遷移中に来た要求は無視する（多重ロード防止）。
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class SceneLoader : Singleton<SceneLoader>
{
    /// <summary>遷移中なら true。演出の開始から終了までを含む</summary>
    public bool IsChanging { get; private set; }

    private SceneDatabase database;
    private bool isDatabaseLoading;

    private async void Start()
    {
        // シーンの一覧表を先読みしておく
        await EnsureDatabaseLoadedAsync();
    }

    /// <summary>
    /// シーンの一覧表（SceneDatabase）を読み込む。読み込み済みならすぐ返る。
    /// </summary>
    private async UniTask EnsureDatabaseLoadedAsync()
    {
        if (database != null) return;

        // 重複ロードを防止
        if (isDatabaseLoading)
        {
            await UniTask.WaitUntil(() => database != null || !isDatabaseLoading, PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
            return;
        }

        isDatabaseLoading = true;
        try
        {
            await Addressables.InitializeAsync().ToUniTask();

            var handle = Addressables.LoadAssetAsync<SceneDatabase>(AddressableKeys.SceneDatabase);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                database = handle.Result;
            }
            else
            {
                Debug.LogError($"[SceneLoader] '{AddressableKeys.SceneDatabase}' の読み込みに失敗しました。Status: {handle.Status}。Addressables のアドレス設定を確認してください。");
                if (handle.OperationException != null) Debug.LogException(handle.OperationException);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SceneLoader] '{AddressableKeys.SceneDatabase}' の読み込み中に例外が発生しました: {e.Message}");
        }
        finally
        {
            isDatabaseLoading = false;
        }
    }

    /// <summary>
    /// 今いるシーンの種類を返す。SceneDatabase に登録されていないシーンなら null。
    /// </summary>
    public async UniTask<SceneType?> GetCurrentSceneTypeAsync()
    {
        await EnsureDatabaseLoadedAsync();
        if (database == null) return null;

        string current = SceneManager.GetActiveScene().name;
        return database.TryGetSceneType(current, out var type) ? type : (SceneType?)null;
    }

    /// <summary>
    /// 今いるシーンで遊ぶステージ一覧を返す（SceneDatabase の、そのシーンの行に指定したもの）。無ければ null。
    /// 前もって覚えておかず、使うその時に呼ぶ（シーンの切り替えと順番がずれないようにするため）。
    /// </summary>
    public async UniTask<StageDatabase> GetCurrentStageDatabaseAsync()
    {
        await EnsureDatabaseLoadedAsync();
        if (database == null) return null;

        string current = SceneManager.GetActiveScene().name;
        return database.TryGetStageDatabase(current, out var stageDatabase) ? stageDatabase : null;
    }

    /// <summary>
    /// 指定した種類のシーンへ、演出で画面を隠してから移り、完了後に画面を開けます。完了まで待てます。
    /// </summary>
    public async UniTask ChangeSceneAsync(SceneType type, TransitionManager.TransitionType transitionType = TransitionManager.TransitionType.Fade)
    {
        await EnsureDatabaseLoadedAsync();
        if (database == null) return;

        if (!database.TryGetSceneName(type, out string sceneName))
        {
            Debug.LogError($"[SceneLoader] 種類 '{type}' のシーンが SceneDatabase に登録されていません");
            return;
        }

        await ChangeSceneByNameAsync(sceneName, transitionType);
    }

    /// <summary>
    /// ChangeSceneAsync の完了を待たない版。Button の OnClick など戻り値を扱えない場所から使います。
    /// </summary>
    public void ChangeScene(SceneType type, TransitionManager.TransitionType transitionType = TransitionManager.TransitionType.Fade)
    {
        ChangeSceneAsync(type, transitionType).Forget();
    }

    /// <summary>
    /// シーン名で移る。外からは種類で頼む形にそろえるため、中だけで使う。
    /// </summary>
    private async UniTask ChangeSceneByNameAsync(string sceneName, TransitionManager.TransitionType transitionType)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoader] シーン名が空のため遷移しません");
            return;
        }

        if (IsChanging)
        {
            Debug.LogWarning($"[SceneLoader] 遷移中のため '{sceneName}' への要求を無視します");
            return;
        }

        IsChanging = true;
        try
        {
            await TransitionManager.Instance.RunWithTransitionAsync(
                () => SceneManager.LoadSceneAsync(sceneName).ToUniTask(),
                transitionType);
        }
        finally
        {
            IsChanging = false;
        }
    }
}
