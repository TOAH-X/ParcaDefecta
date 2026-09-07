using System;
using UnityEngine;
using UnityEngine.Scripting;
using Cysharp.Threading.Tasks;

/// <summary>
/// 画面遷移演出（トランジション）の Model 層。
/// 「今、画面が隠れているか」という状態を保持し、
/// 呼び出し元に「隠しきる／開けきる」までの待機を提供する。
/// View や Presenter の型は参照せず、イベントで Presenter に依頼し、完了報告を受け取る。
///
/// 使い方:
///   await TransitionManager.Instance.CoverAsync();   // 画面を隠しきるまで待つ
///   （シーン切替など）
///   await TransitionManager.Instance.UncoverAsync(); // 画面を開けきるまで待つ
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class TransitionManager : Singleton<TransitionManager>
{
    public enum TransitionState
    {
        Idle,       // 何も表示していない
        Covering,   // 隠している途中
        Covered,    // 隠しきった
        Uncovering, // 開けている途中
    }

    // 実行時確認用（Inspector で現在の状態を観察する）
    [SerializeField] private TransitionState state = TransitionState.Idle;

    /// <summary>現在の状態</summary>
    public TransitionState State => state;

    /// <summary>演出中（Idle 以外）なら true。遷移中の操作制限などに使う</summary>
    public bool IsTransitioning => state != TransitionState.Idle;

    /// <summary>Presenter が購読する。「隠して」の依頼</summary>
    public event Action OnCoverRequested;

    /// <summary>Presenter が購読する。「開けて」の依頼</summary>
    public event Action OnUncoverRequested;

    /// <summary>状態が変わったときに発火する。拡張用</summary>
    public event Action<TransitionState> OnStateChanged;

    // 呼び出し元を待たせるための完了通知
    private UniTaskCompletionSource coverTcs;
    private UniTaskCompletionSource uncoverTcs;

    /// <summary>
    /// 画面を隠しきるまで待つ。
    /// 既に隠れていれば即座に戻り、隠している途中なら同じ待機を返す。
    /// </summary>
    public UniTask CoverAsync()
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

        coverTcs = new UniTaskCompletionSource();
        SetState(TransitionState.Covering);

        if (OnCoverRequested == null)
        {
            // Canvas 未ロードなどで Presenter が居ない場合。演出なしで隠れた扱いにして呼び出し元を止めない
            Debug.LogWarning("[TransitionManager] Presenter が未登録のため、演出なしで Covered 扱いにします。");
            NotifyCovered();
            return UniTask.CompletedTask;
        }

        OnCoverRequested.Invoke();
        return coverTcs.Task;
    }

    /// <summary>
    /// 画面を開けきるまで待つ。
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

        if (OnUncoverRequested == null)
        {
            Debug.LogWarning("[TransitionManager] Presenter が未登録のため、演出なしで Idle 扱いにします。");
            NotifyUncovered();
            return UniTask.CompletedTask;
        }

        OnUncoverRequested.Invoke();
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
    [ContextMenu("Debug: Cover -> 1s -> Uncover")]
    private void DebugPlay()
    {
        DebugPlayAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid DebugPlayAsync(System.Threading.CancellationToken token)
    {
        Debug.Log("[TransitionManager] Debug: Cover 開始");
        await CoverAsync();
        Debug.Log("[TransitionManager] Debug: Covered");
        await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: token);
        Debug.Log("[TransitionManager] Debug: Uncover 開始");
        await UncoverAsync();
        Debug.Log("[TransitionManager] Debug: Idle");
    }
}
