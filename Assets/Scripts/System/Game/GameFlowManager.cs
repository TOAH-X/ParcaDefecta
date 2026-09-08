using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Cysharp.Threading.Tasks;

/// <summary>
/// ゲーム進行の手順を持つマネージャー。
/// 「いつ・どのシーンでステージを置くか」はここが決め、StageManager は ID を受けて生成するだけにする。
/// 現段階では「ゲームを開始する（ゲームシーンへ切り替えて、ステージを置く）」のみを扱う。
///
/// 使い方:
///   GameFlowManager.Instance.StartGame();          // 先頭ステージで開始
///   GameFlowManager.Instance.StartGame("Stage02"); // 指定ステージで開始
///   await GameFlowManager.Instance.StartGameAsync(); // 完了を待ちたい場合
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class GameFlowManager : Singleton<GameFlowManager>
{
    // ゲームシーン名。シーン名の台帳（SceneKeys）を作ったらそちらへ移す（暫定）
    private const string GameSceneName = "DebugScene";

    /// <summary>開始処理中なら true</summary>
    public bool IsStarting { get; private set; }

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
        if (IsStarting)
        {
            Debug.LogWarning("[GameFlowManager] 開始処理中のため要求を無視します");
            return;
        }

        IsStarting = true;
        try
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
        }
        finally
        {
            IsStarting = false;
        }
    }

    private static bool IsInGameScene()
    {
        return SceneManager.GetActiveScene().name == GameSceneName;
    }
}
