using System;
using UnityEngine;
using UnityEngine.Scripting;
using Cysharp.Threading.Tasks;
using R3;

/// <summary>
/// ゲーム進行の手順を持つマネージャー。
/// 「いつ・どのシーンでステージを置くか」はここが決め、StageManager は ID を受けて生成するだけにする。
/// 扱う手順は「ゲーム開始（ゲームシーンへ切り替えてステージを置く）」と「ステージ切替（次へ・指定・やり直し）」。
/// どちらも暗幕（TransitionManager）で隠している間に StageManager を呼ぶ。StageManager は暗幕を知らない。
/// 「ステージを置くシーン」は、シーンの種類が Game か Debug のシーン（SceneDatabase で登録）。
///
/// 使い方:
///   GameFlowManager.Instance.StartGame();          // 先頭ステージで開始（ゲームシーンにいなければ Game のシーンへ移る）
///   GameFlowManager.Instance.AdvanceToNextStage(); // 次のステージへ
///   GameFlowManager.Instance.ReloadCurrentStage(); // 現在のステージをやり直す
///   GameFlowManager.Instance.ReturnToTitle();      // タイトルへ戻る
///   await GameFlowManager.Instance.StartGameAsync(); // 完了を待ちたい場合は Async 版
/// ボタンから呼ぶときは GameFlowUIBridge を使う。
///
/// 次へ進もうとして次のステージが無かったときは、OnStagesEnded を流して「行き止まり」の状態になる
/// （ステージの切り替え・やり直しを受け付けない。タイトルへ戻るか、開始し直すと解ける）。
/// それをクリアとして扱うかはここでは決めない（試験版では TrialClearView が受け取ってクリア画面を出す）。
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class GameFlowManager : Singleton<GameFlowManager>
{
    // 暗幕の種類。ゲーム開始はシーン切替と同じ Fade、ステージ切替は Wipe
    private const TransitionManager.TransitionType StartTransitionType = TransitionManager.TransitionType.Fade;
    private const TransitionManager.TransitionType StageTransitionType = TransitionManager.TransitionType.Wipe;

    /// <summary>開始処理中なら true</summary>
    public bool IsStarting { get; private set; }

    /// <summary>ステージ切替中なら true</summary>
    public bool IsChangingStage { get; private set; }

    /// <summary>開始処理中またはステージ切替中なら true。新しい要求はこの間無視される</summary>
    public bool IsBusy => IsStarting || IsChangingStage;

    /// <summary>
    /// 次のステージが無く、行き止まりになっていれば true。
    /// この間はステージの切り替え・やり直しを受け付けない。タイトルへ戻るか、開始し直すと false に戻る。
    /// </summary>
    public bool HasStagesEnded { get; private set; }

    // 次へ進もうとして、次のステージが無かったことを知らせる（クリアかどうかは受け取る側が決める）
    private readonly Subject<Unit> _onStagesEnded = new();
    public Observable<Unit> OnStagesEnded => _onStagesEnded;

    private async void Start()
    {
        // エディタでゲームシーンを直接 Play した場合の吸収。
        // DontDestroyOnLoad のため起動時に 1 回だけ走り、タイトル経由の遷移では発火しない
        if (await IsInStageSceneAsync())
        {
            StartGame();
        }
    }

    /// <summary>
    /// ゲームを先頭ステージから開始する。完了を待たない版。
    /// </summary>
    public void StartGame()
    {
        StartGameAsync().Forget();
    }

    /// <summary>
    /// ゲームを先頭ステージから開始する。完了まで待てます。
    /// ステージを置くシーン以外にいる場合は、Game のシーンへ切り替えてからステージを置く。
    /// どのステージから始めるかは引数で受け取らない（将来は StageSessionManager に持たせて、ここで読む予定）。
    /// </summary>
    public async UniTask StartGameAsync()
    {
        if (IsBusy)
        {
            Debug.LogWarning("[GameFlowManager] 処理中のため開始要求を無視します");
            return;
        }

        IsStarting = true;
        HasStagesEnded = false;
        try
        {
            // 会話が出ていれば閉じる。ステージごと入れ替わるので、会話ウィンドウだけ残らないようにする
            DialogueManager.Instance.Cancel();

            // シーン切替とステージ配置を 1 枚の暗幕で包む。
            // 内側で SceneLoader が掛ける暗幕は参照カウントで合流するため、ステージ配置が終わるまで開かない
            await TransitionManager.Instance.RunWithTransitionAsync(async () =>
            {
                // ステージを置くシーンへ移動
                if (!await IsInStageSceneAsync())
                {
                    await SceneLoader.Instance.ChangeSceneAsync(SceneType.Game);

                    // SceneLoader が遷移中で要求を無視した場合など、移動できていなければ中断する
                    if (!await IsInStageSceneAsync())
                    {
                        Debug.LogWarning($"[GameFlowManager] 種類 '{SceneType.Game}' のシーンへ移動できなかったため開始を中断します");
                        return;
                    }
                }

                // 先頭ステージの ID
                string stageId = await StageManager.Instance.GetFirstStageIdAsync();
                if (string.IsNullOrEmpty(stageId))
                {
                    Debug.LogError("[GameFlowManager] 開始するステージ ID を決められませんでした。StageDatabase を確認してください");
                    return;
                }

                // ステージ配置
                await StageManager.Instance.LoadStageAsync(stageId);
            }, StartTransitionType);
        }
        finally
        {
            IsStarting = false;
        }
    }

    // ---- ステージ切替 ----

    /// <summary>指定ステージへ切り替える。完了を待たない版</summary>
    public void ChangeStage(string stageId)
    {
        ChangeStageAsync(stageId).Forget();
    }

    /// <summary>指定ステージへ切り替える。完了まで待てます</summary>
    public UniTask ChangeStageAsync(string stageId)
    {
        return RunStageChangeAsync(() => StageManager.Instance.LoadStageAsync(stageId));
    }

    /// <summary>次のステージへ進む。完了を待たない版</summary>
    public void AdvanceToNextStage()
    {
        AdvanceToNextStageAsync().Forget();
    }

    /// <summary>
    /// 次のステージへ進む。完了まで待てます。
    /// 次のステージが無ければ、暗幕は掛けずに OnStagesEnded を流して行き止まりの状態になる。
    /// </summary>
    public async UniTask AdvanceToNextStageAsync()
    {
        if (IsBusy || HasStagesEnded)
        {
            Debug.LogWarning("[GameFlowManager] 処理中または行き止まりのため、次のステージへの要求を無視します");
            return;
        }

        if (!await StageManager.Instance.HasNextStageAsync())
        {
            HasStagesEnded = true;
            DialogueManager.Instance.Cancel();
            _onStagesEnded.OnNext(Unit.Default);
            return;
        }

        await RunStageChangeAsync(() => StageManager.Instance.AdvanceToNextStageAsync());
    }

    // ---- タイトルへ ----

    /// <summary>タイトルへ戻る。完了を待たない版</summary>
    public void ReturnToTitle()
    {
        ReturnToTitleAsync().Forget();
    }

    /// <summary>
    /// タイトルへ戻り、行き止まりの状態を解く。完了まで待てます。
    /// ポーズはここでは解かない（暗転中にゲームが動き出さないように）。シーンが変わったときに TimeManager が解く。
    /// </summary>
    public async UniTask ReturnToTitleAsync()
    {
        if (IsBusy)
        {
            Debug.LogWarning("[GameFlowManager] 処理中のためタイトルへの要求を無視します");
            return;
        }

        DialogueManager.Instance.Cancel();
        HasStagesEnded = false;

        await SceneLoader.Instance.ChangeSceneAsync(SceneType.Title);
    }

    /// <summary>現在のステージをやり直す。完了を待たない版</summary>
    public void ReloadCurrentStage()
    {
        ReloadCurrentStageAsync().Forget();
    }

    /// <summary>現在のステージをやり直す。完了まで待てます</summary>
    public UniTask ReloadCurrentStageAsync()
    {
        return RunStageChangeAsync(() => StageManager.Instance.ReloadCurrentStageAsync());
    }

    /// <summary>
    /// ステージ切替の共通手順。処理中なら無視し、暗幕で隠している間に切替処理を実行する。
    /// 暗幕が閉じきるまで古いステージはそのまま残り、閉じてから差し替わる。
    /// </summary>
    private async UniTask RunStageChangeAsync(Func<UniTask> changeAction)
    {
        if (IsBusy || HasStagesEnded)
        {
            Debug.LogWarning("[GameFlowManager] 処理中または行き止まりのためステージ切替要求を無視します");
            return;
        }

        IsChangingStage = true;
        try
        {
            // 会話が出ていれば閉じる。ステージごと入れ替わるので、会話ウィンドウだけ残らないようにする
            DialogueManager.Instance.Cancel();

            await TransitionManager.Instance.RunWithTransitionAsync(changeAction, StageTransitionType);
        }
        finally
        {
            IsChangingStage = false;
        }
    }

    /// <summary>
    /// 今いるシーンが「ステージを置くシーン」（種類が Game か Debug）なら true。
    /// SceneDatabase に登録されていないシーンは false。
    /// </summary>
    private static async UniTask<bool> IsInStageSceneAsync()
    {
        SceneType? type = await SceneLoader.Instance.GetCurrentSceneTypeAsync();
        return type == SceneType.Game || type == SceneType.Debug;
    }

    private void OnDestroy()
    {
        _onStagesEnded.OnCompleted();
        _onStagesEnded.Dispose();
    }
}
