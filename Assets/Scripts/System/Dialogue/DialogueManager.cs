using System;
using UnityEngine;
using UnityEngine.Scripting;
using Cysharp.Threading.Tasks;
using ParcaDefecta.System;

/// <summary>
/// 会話の Model 層。「どの会話の何行目を、どの状態で出しているか」を持ち、進行だけを決める。
/// 見た目(ウィンドウ、立ち絵、文字送り)は知らず、イベントで Presenter に依頼し、報告を受け取る。
/// 構造は TransitionManager と同じ(Model が event を持ち、Presenter が購読する)。
///
/// 1 行の流れ:
///   OnLineRequested(行) → [Typing: View が文字を送る] → NotifyLineShown() → [WaitingAdvance: 送り待ち] → Advance() → 次の行
///   Typing 中に Advance() が来たら OnCompleteRequested で「全部出して」を依頼し、View が出し終えたら NotifyLineShown() で WaitingAdvance になる。
///
/// 会話中はゲームを止める。PauseManager でポーズする(Player はポーズ中は入力を読まない)。
///
/// 使い方:
///   await DialogueManager.Instance.PlayAsync(data); // 会話が終わるまで待てる
///   DialogueManager.Instance.Advance();             // 送り(Presenter が入力を受けて呼ぶ)
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class DialogueManager : Singleton<DialogueManager>
{
    public enum DialogueState
    {
        Idle,           // 再生していない
        Typing,         // 行を表示中(文字送りの途中)
        WaitingAdvance, // 行を全部出し、送りを待っている
    }

    // 実行時確認用(Inspector で現在の状態を観察する)
    [SerializeField] private DialogueState state = DialogueState.Idle;
    [SerializeField] private DialogueData currentData;
    [SerializeField] private int currentIndex = -1;

    /// <summary>現在の状態</summary>
    public DialogueState State => state;

    /// <summary>再生中なら true。Player を止める判断などに使う</summary>
    public bool IsPlaying => state != DialogueState.Idle;

    /// <summary>再生中の会話。Idle なら null</summary>
    public DialogueData CurrentData => currentData;

    /// <summary>再生中の行の番号(0 始まり)。Idle なら -1</summary>
    public int CurrentIndex => currentIndex;

    /// <summary>Presenter が購読する。会話が始まった(ウィンドウを開く)</summary>
    public event Action<DialogueData> OnStarted;

    /// <summary>Presenter が購読する。この行を出して</summary>
    public event Action<DialogueLine> OnLineRequested;

    /// <summary>Presenter が購読する。文字送りを飛ばして行を全部出して</summary>
    public event Action OnCompleteRequested;

    /// <summary>Presenter が購読する。会話が終わった(ウィンドウを閉じる)</summary>
    public event Action OnFinished;

    /// <summary>状態が変わったときに発火する。拡張用</summary>
    public event Action<DialogueState> OnStateChanged;

    // 呼び出し元を待たせるための完了通知
    private UniTaskCompletionSource finishTcs;

    /// <summary>
    /// 会話を最初の行から再生し、終わるまで待つ。
    /// 再生中に呼ばれた場合は警告して無視する(2 つの会話を同時には出さない)。
    /// </summary>
    public UniTask PlayAsync(DialogueData data)
    {
        if (IsPlaying)
        {
            Debug.LogWarning($"[DialogueManager] 再生中のため '{(data != null ? data.name : "null")}' の再生要求を無視します");
            return UniTask.CompletedTask;
        }
        if (data == null || data.Lines.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] 会話データが無いか、行が 1 つもありません");
            return UniTask.CompletedTask;
        }

        currentData = data;
        currentIndex = 0;
        finishTcs = new UniTaskCompletionSource();

        SetGameStopped(true);
        OnStarted?.Invoke(data);
        ShowCurrentLine();

        return finishTcs.Task;
    }

    /// <summary>
    /// 送り。文字送りの途中なら全部出すよう依頼し、全部出ていれば次の行へ進む。最後の行なら終了する。
    /// Presenter が入力を受けて呼ぶ。
    /// </summary>
    public void Advance()
    {
        switch (state)
        {
            case DialogueState.Typing:
                if (OnCompleteRequested == null)
                {
                    // Presenter が居ないときは出し終えた扱いにして進行を止めない
                    NotifyLineShown();
                    return;
                }
                OnCompleteRequested.Invoke();
                return;

            case DialogueState.WaitingAdvance:
                currentIndex++;
                if (currentIndex < currentData.Lines.Count)
                {
                    ShowCurrentLine();
                }
                else
                {
                    Finish();
                }
                return;
        }
    }

    /// <summary>
    /// 会話を途中で閉じる。ステージ切替などで使う想定。Idle なら何もしない。
    /// </summary>
    public void Cancel()
    {
        if (!IsPlaying) return;

        Debug.Log($"[DialogueManager] '{currentData.name}' を {currentIndex} 行目で中断します");
        Finish();
    }

    /// <summary>
    /// Presenter が「行を全部出した」ことを報告する。送り待ちになる。
    /// </summary>
    public void NotifyLineShown()
    {
        if (state != DialogueState.Typing)
        {
            Debug.LogWarning($"[DialogueManager] NotifyLineShown が Typing 以外の状態({state})で呼ばれました。無視します");
            return;
        }

        SetState(DialogueState.WaitingAdvance);
    }

    private void ShowCurrentLine()
    {
        SetState(DialogueState.Typing);

        if (OnLineRequested == null)
        {
            // Canvas 未ロードなどで Presenter が居ない場合。表示なしで出し終えた扱いにし、Advance で進められるようにする
            Debug.LogWarning("[DialogueManager] Presenter が未登録のため、表示なしで進行します");
            NotifyLineShown();
            return;
        }

        OnLineRequested.Invoke(currentData.Lines[currentIndex]);
    }

    private void Finish()
    {
        currentData = null;
        currentIndex = -1;
        SetState(DialogueState.Idle);

        OnFinished?.Invoke();
        SetGameStopped(false);

        finishTcs?.TrySetResult();
        finishTcs = null;
    }

    private void SetState(DialogueState next)
    {
        if (state == next) return;
        state = next;
        OnStateChanged?.Invoke(state);
    }

    /// <summary>
    /// 会話中のゲーム停止。ポーズして時間を止める。Player はポーズ中は入力を読まないので、これだけで操作が止まる。
    /// 解除は次のフレームに回す。最後の送りのキー(ゲームパッド A など)はジャンプと同じなので、
    /// 同じフレームで解除すると Player がそのキーをジャンプとして拾ってしまう。
    /// </summary>
    private void SetGameStopped(bool stopped)
    {
        if (stopped)
        {
            PauseManager.Instance.Pause();
        }
        else
        {
            ResumeNextFrameAsync().Forget();
        }
    }

    private async UniTaskVoid ResumeNextFrameAsync()
    {
        await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

        // 待っている間に次の会話が始まっていたら、そちらが止めたままにする
        if (IsPlaying) return;
        PauseManager.Instance.Resume();
    }

    // ---- デバッグ用 ----

    [Header("デバッグ用(Play 中に右クリックメニューから再生)")]
    [SerializeField] private DialogueData debugData;

    /// <summary>
    /// Play 中に Inspector の右クリックから動作確認する。debugData を再生し、1 秒ごとに送る。
    /// Presenter が居なくても、行の内容をログに出して進行を確かめられる。
    /// </summary>
    [ContextMenu("Debug: Play debugData (1s ごとに送る)")]
    private void DebugPlay()
    {
        DebugPlayAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid DebugPlayAsync(System.Threading.CancellationToken token)
    {
        if (debugData == null)
        {
            Debug.LogWarning("[DialogueManager] Debug: debugData が設定されていません");
            return;
        }

        Action<DialogueLine> log = line => Debug.Log($"[DialogueManager] Debug: {currentIndex} 行目 [{line.Name}] {line.Text}");
        OnLineRequested += log;
        try
        {
            Debug.Log($"[DialogueManager] Debug: '{debugData.name}' 再生開始");
            UniTask playing = PlayAsync(debugData);

            while (IsPlaying)
            {
                await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: token);
                Advance();
            }

            await playing;
            Debug.Log("[DialogueManager] Debug: 再生終了");
        }
        finally
        {
            OnLineRequested -= log;
        }
    }
}
