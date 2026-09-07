using System;
using UnityEngine;
using UnityEngine.Scripting;
using Cysharp.Threading.Tasks;

/// <summary>
/// 画面遷移演出（トランジション）の Model 層。
/// 「今、画面が隠れているか」という状態と「どの演出で隠したか」を保持し、
/// 呼び出し元に「隠しきる／開けきる」までの待機を提供する。
/// View や Presenter の型は参照せず、イベントで Presenter に依頼し、完了報告を受け取る。
///
/// 使い方:
///   await TransitionManager.Instance.RunWithTransitionAsync(async () => { /* 隠れている間の処理 */ });
///
/// 複数の系統（シーン遷移・ステージ遷移など）が同時に RunWithTransitionAsync を使っても、
/// 全員の処理が終わるまで画面は開かない（参照カウント方式）。
/// CoverAsync / UncoverAsync を直接呼ぶ経路はこのカウントの対象外なので、
/// 「隠したまま複数の処理を跨ぐ」ような特殊な場面に限って使うこと。
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class TransitionManager : Singleton<TransitionManager>
{
    /// <summary>
    /// 演出の種類。追加するときはここに 1 行足し、対応する TransitionViewBase 派生を Views/ に作る。
    /// None は演出なし（Presenter を呼ばず即完了）。
    /// </summary>
    public enum TransitionType
    {
        None,
        Fade,
        Wipe,
    }

    public enum TransitionState
    {
        Idle,       // 何も表示していない
        Covering,   // 隠している途中
        Covered,    // 隠しきった
        Uncovering, // 開けている途中
    }

    // 実行時確認用（Inspector で現在の状態を観察する）
    [SerializeField] private TransitionState state = TransitionState.Idle;
    [SerializeField] private TransitionType activeType = TransitionType.None;

    /// <summary>現在の状態</summary>
    public TransitionState State => state;

    /// <summary>現在（または直前）の演出で使っている種類。Uncover はこの種類で行う</summary>
    public TransitionType ActiveType => activeType;

    /// <summary>演出中（Idle 以外）なら true。遷移中の操作制限などに使う</summary>
    public bool IsTransitioning => state != TransitionState.Idle;

    /// <summary>Presenter が購読する。「この種類で隠して」の依頼</summary>
    public event Action<TransitionType> OnCoverRequested;

    /// <summary>Presenter が購読する。「この種類で開けて」の依頼</summary>
    public event Action<TransitionType> OnUncoverRequested;

    /// <summary>状態が変わったときに発火する。拡張用</summary>
    public event Action<TransitionState> OnStateChanged;

    // 呼び出し元を待たせるための完了通知
    private UniTaskCompletionSource coverTcs;
    private UniTaskCompletionSource uncoverTcs;

    // RunWithTransitionAsync の同時利用数。0 に戻った最後の 1 件だけが実際に開ける
    [SerializeField] private int runningCount = 0;

    /// <summary>
    /// 画面を隠している間に action を実行し、終わったら開ける。
    /// action が例外を投げても暗幕は開ける。
    /// 他の系統が同時に利用中なら、全員の action が終わるまで開けない。
    /// </summary>
    public async UniTask RunWithTransitionAsync(Func<UniTask> action, TransitionType type = TransitionType.Fade)
    {
        runningCount++;
        try
        {
            // 既に隠れていれば CoverAsync は即座に戻る（最初の 1 件だけが実際に隠す）
            await CoverAsync(type);
            await action();
        }
        finally
        {
            runningCount--;
            if (runningCount <= 0)
            {
                runningCount = 0;
                await UncoverAsync();
            }
        }
    }

    /// <summary>
    /// 画面を隠しきるまで待つ。
    /// 既に隠れていれば即座に戻り、隠している途中なら同じ待機を返す。
    /// </summary>
    public UniTask CoverAsync(TransitionType type = TransitionType.Fade)
    {
        switch (state)
        {
            case TransitionState.Covered:
                return UniTask.CompletedTask;
            case TransitionState.Covering:
                return coverTcs.Task;
        }

        // Uncovering 中の割り込み。開け待ちの呼び出し元は解放しておく（待ち続けさせない）
        if (state == TransitionState.Uncovering)
        {
            Debug.LogWarning("[TransitionManager] Uncover 中に Cover が要求されました。Uncover は中断されます。");
            uncoverTcs?.TrySetResult();
        }

        activeType = type;
        coverTcs = new UniTaskCompletionSource();
        SetState(TransitionState.Covering);

        if (type == TransitionType.None)
        {
            // 演出なし。Presenter を介さず即座に隠れた扱いにする
            NotifyCovered();
            return UniTask.CompletedTask;
        }

        if (OnCoverRequested == null)
        {
            // Canvas 未ロードなどで Presenter が居ない場合。演出なしで隠れた扱いにして呼び出し元を止めない
            Debug.LogWarning("[TransitionManager] Presenter が未登録のため、演出なしで Covered 扱いにします。");
            NotifyCovered();
            return UniTask.CompletedTask;
        }

        OnCoverRequested.Invoke(type);
        return coverTcs.Task;
    }

    /// <summary>
    /// 画面を開けきるまで待つ。Cover で使った種類で開ける。
    /// 既に開いていれば即座に戻り、開けている途中なら同じ待機を返す。
    /// </summary>
    public UniTask UncoverAsync()
    {
        switch (state)
        {
            case TransitionState.Idle:
                return UniTask.CompletedTask;
            case TransitionState.Uncovering:
                return uncoverTcs.Task;
        }

        // Covering 中の割り込み。隠し待ちの呼び出し元は解放しておく
        if (state == TransitionState.Covering)
        {
            Debug.LogWarning("[TransitionManager] Cover 中に Uncover が要求されました。Cover は中断されます。");
            coverTcs?.TrySetResult();
        }

        uncoverTcs = new UniTaskCompletionSource();
        SetState(TransitionState.Uncovering);

        if (activeType == TransitionType.None)
        {
            NotifyUncovered();
            return UniTask.CompletedTask;
        }

        if (OnUncoverRequested == null)
        {
            Debug.LogWarning("[TransitionManager] Presenter が未登録のため、演出なしで Idle 扱いにします。");
            NotifyUncovered();
            return UniTask.CompletedTask;
        }

        OnUncoverRequested.Invoke(activeType);
        return uncoverTcs.Task;
    }

    /// <summary>
    /// Presenter が「隠しきった」ことを報告する。
    /// </summary>
    public void NotifyCovered()
    {
        if (state != TransitionState.Covering)
        {
            Debug.LogWarning($"[TransitionManager] NotifyCovered が Covering 以外の状態({state})で呼ばれました。無視します。");
            return;
        }

        SetState(TransitionState.Covered);
        coverTcs?.TrySetResult();
    }

    /// <summary>
    /// Presenter が「開けきった」ことを報告する。
    /// </summary>
    public void NotifyUncovered()
    {
        if (state != TransitionState.Uncovering)
        {
            Debug.LogWarning($"[TransitionManager] NotifyUncovered が Uncovering 以外の状態({state})で呼ばれました。無視します。");
            return;
        }

        SetState(TransitionState.Idle);
        uncoverTcs?.TrySetResult();
    }

    private void SetState(TransitionState next)
    {
        if (state == next) return;
        state = next;
        OnStateChanged?.Invoke(state);
    }

    // ---- デバッグ用 ----

    /// <summary>
    /// Play 中に Inspector の右クリックから動作確認する。Cover → 1 秒待機 → Uncover。
    /// </summary>
    [ContextMenu("Debug: Fade (Cover -> 1s -> Uncover)")]
    private void DebugPlayFade()
    {
        DebugPlayAsync(TransitionType.Fade, this.GetCancellationTokenOnDestroy()).Forget();
    }

    [ContextMenu("Debug: Wipe (Cover -> 1s -> Uncover)")]
    private void DebugPlayWipe()
    {
        DebugPlayAsync(TransitionType.Wipe, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid DebugPlayAsync(TransitionType type, System.Threading.CancellationToken token)
    {
        Debug.Log($"[TransitionManager] Debug: {type} Cover 開始");
        await RunWithTransitionAsync(async () =>
        {
            Debug.Log($"[TransitionManager] Debug: {type} Covered");
            await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: token);
            Debug.Log($"[TransitionManager] Debug: {type} Uncover 開始");
        }, type);
        Debug.Log($"[TransitionManager] Debug: {type} Idle");
    }
}
