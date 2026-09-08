using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Cysharp.Threading.Tasks;

/// <summary>
/// ゲーム進行の手順を持つマネージャー。
/// 「いつ・どのシーンでステージを置くか」はここが決め、StageManager は ID を受けて生成するだけにする。
/// 扱う手順は「ゲーム開始（ゲームシーンへ切り替えてステージを置く）」と「ステージ切替（次へ・指定・やり直し）」。
/// どちらも暗幕（TransitionManager）で隠している間に StageManager を呼ぶ。StageManager は暗幕を知らない。
///
/// 使い方:
///   GameFlowManager.Instance.StartGame();          // 先頭ステージで開始
///   GameFlowManager.Instance.StartGame("Stage02"); // 指定ステージで開始
///   GameFlowManager.Instance.AdvanceToNextStage(); // 次のステージへ
///   GameFlowManager.Instance.ReloadCurrentStage(); // 現在のステージをやり直す
///   await GameFlowManager.Instance.StartGameAsync(); // 完了を待ちたい場合は Async 版
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class GameFlowManager : Singleton<GameFlowManager>
{
    // ゲームシーン名。シーン名の台帳（SceneKeys）を作ったらそちらへ移す（暫定）
    private const string GameSceneName = "DebugScene";

    // 暗幕の種類。ゲーム開始はシーン切替と同じ Fade、ステージ切替は Wipe
    private const TransitionManager.TransitionType StartTransitionType = TransitionManager.TransitionType.Fade;
    private const TransitionManager.TransitionType StageTransitionType = TransitionManager.TransitionType.Wipe;

    /// <summary>開始処理中なら true</summary>
    public bool IsStarting { get; private set; }

    /// <summary>ステージ切替中なら true</summary>
    public bool IsChangingStage { get; private set; }

    /// <summary>開始処理中またはステージ切替中なら true。新しい要求はこの間無視される</summary>
    public bool IsBusy => IsStarting || IsChangingStage;

    private void Start()
    {
        // エディタでゲームシーンを直接 Play した場合の吸収。
        // DontDestroyOnLoad のため起動時に 1 回だけ走り、タイトル経由の遷移では発火しない
        if (IsInGameScene())
        {
            StartGame();
        }
    }

    /// <summary>
    /// ゲームを開始する。ID が空なら先頭ステージ、指定があればそのステージ。完了を待たない版。
    /// </summary>
    public void StartGame(string stageId = null)
    {
        StartGameAsync(stageId).Forget();
    }

    /// <summary>
    /// ゲームを開始する。ID が空なら先頭ステージ、指定があればそのステージ。完了まで待てます。
    /// ゲームシーン以外にいる場合はゲームシーンへ切り替えてからステージを置く。
    /// </summary>
    public async UniTask StartGameAsync(string stageId = null)
    {
        if (IsBusy)
        {
            Debug.LogWarning("[GameFlowManager] 処理中のため開始要求を無視します");
            return;
        }

        IsStarting = true;
        try
        {
            // シーン切替とステージ配置を 1 枚の暗幕で包む。
            // 内側で SceneLoader が掛ける暗幕は参照カウントで合流するため、ステージ配置が終わるまで開かない
            await TransitionManager.Instance.RunWithTransitionAsync(async () =>
            {
                // ゲームシーンへ移動
                if (!IsInGameScene())
                {
                    await SceneLoader.Instance.ChangeSceneAsync(GameSceneName);

                    // SceneLoader が遷移中で要求を無視した場合など、移動できていなければ中断する
                    if (!IsInGameScene())
                    {
                        Debug.LogWarning($"[GameFlowManager] '{GameSceneName}' へ移動できなかったため開始を中断します");
                        return;
                    }
                }

                // ステージ ID の決定
                if (string.IsNullOrEmpty(stageId))
                {
                    stageId = await StageManager.Instance.GetFirstStageIdAsync();
                }
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

    /// <summary>次のステージへ進む。完了まで待てます</summary>
    public UniTask AdvanceToNextStageAsync()
    {
        return RunStageChangeAsync(() => StageManager.Instance.AdvanceToNextStageAsync());
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
        if (IsBusy)
        {
            Debug.LogWarning("[GameFlowManager] 処理中のためステージ切替要求を無視します");
            return;
        }

        IsChangingStage = true;
        try
        {
            await TransitionManager.Instance.RunWithTransitionAsync(changeAction, StageTransitionType);
        }
        finally
        {
            IsChangingStage = false;
        }
    }

    private static bool IsInGameScene()
    {
        return SceneManager.GetActiveScene().name == GameSceneName;
    }
}
