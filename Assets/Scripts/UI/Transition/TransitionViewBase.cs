using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// トランジション演出 View の抽象基底。
/// 具象クラスは Views/ フォルダに置き、自分が担当する TransitionType を Type で名乗る。
/// Presenter は起動時に子から本クラスを収集し、依頼された Type の View を動かす。
/// Model（TransitionManager）を参照しない。
/// </summary>
public abstract class TransitionViewBase : MonoBehaviour
{
    [SerializeField] protected float duration = 0.5f;

    // 進行中の演出を打ち切るためのトークン
    private CancellationTokenSource cts;

    /// <summary>この View が担当する演出の種類</summary>
    public abstract TransitionManager.TransitionType Type { get; }

    /// <summary>画面を隠す。完了まで待てる</summary>
    public abstract UniTask CoverAsync(CancellationToken token);

    /// <summary>画面を開ける。完了まで待てる</summary>
    public abstract UniTask UncoverAsync(CancellationToken token);

    /// <summary>演出なしで状態を合わせる。Presenter の初期同期用</summary>
    public abstract void SetCoveredImmediate(bool covered);

    /// <summary>
    /// 進行中の演出を打ち切り、外部トークンと連結した新しいトークンを返す。
    /// </summary>
    protected CancellationToken RestartToken(CancellationToken external)
    {
        CancelCurrent();
        cts = CancellationTokenSource.CreateLinkedTokenSource(external);
        return cts.Token;
    }

    /// <summary>
    /// 進行中の演出を打ち切る。SetCoveredImmediate などから使う。
    /// </summary>
    protected void CancelCurrent()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    /// <summary>
    /// 数値を from から to へ duration 基準で動かし、毎フレーム apply に渡す。
    /// 途中から再開しても速度が変わらないよう、残り距離に比例した時間で動かす。
    /// ポーズ中（timeScale = 0）でも動くよう unscaled 時間を使う。
    /// </summary>
    protected async UniTask TweenAsync(float from, float to, Action<float> apply, CancellationToken token)
    {
        float length = duration * Mathf.Abs(to - from);
        float elapsed = 0f;

        while (elapsed < length)
        {
            elapsed += Time.unscaledDeltaTime;
            apply(Mathf.Lerp(from, to, elapsed / length));
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        apply(to);
    }

    protected virtual void OnDestroy()
    {
        CancelCurrent();
    }
}
