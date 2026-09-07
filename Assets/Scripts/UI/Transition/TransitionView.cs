using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// トランジション演出の View 層。
/// CanvasGroup の alpha を動かして画面を隠す／開けるだけを担当する。
/// Presenter からの指示以外では動かない。Model（TransitionManager）を参照しない。
/// </summary>
public class TransitionView : MonoBehaviour
{
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    // 進行中のフェードを打ち切るためのトークン
    private CancellationTokenSource cts;

    private void Awake()
    {
        // 起動時は開いた状態から始める
        SetCoveredImmediate(false);
    }

    /// <summary>
    /// 画面を隠す。先に入力を遮断してからフェードインする。
    /// </summary>
    public async UniTask CoverAsync(CancellationToken token)
    {
        if (panelCanvasGroup == null)
        {
            Debug.LogWarning("[TransitionView] panelCanvasGroup が設定されていません");
            return;
        }

        var linked = RestartToken(token);
        panelCanvasGroup.blocksRaycasts = true;
        await FadeAsync(panelCanvasGroup.alpha, 1f, linked);
    }

    /// <summary>
    /// 画面を開ける。フェードアウトが終わってから入力遮断を解除する。
    /// </summary>
    public async UniTask UncoverAsync(CancellationToken token)
    {
        if (panelCanvasGroup == null)
        {
            Debug.LogWarning("[TransitionView] panelCanvasGroup が設定されていません");
            return;
        }

        var linked = RestartToken(token);
        await FadeAsync(panelCanvasGroup.alpha, 0f, linked);
        panelCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// フェードなしで状態を合わせる。Presenter の初期同期用。
    /// </summary>
    public void SetCoveredImmediate(bool covered)
    {
        cts?.Cancel();
        if (panelCanvasGroup == null) return;

        panelCanvasGroup.alpha = covered ? 1f : 0f;
        panelCanvasGroup.blocksRaycasts = covered;
    }

    /// <summary>
    /// 進行中のフェードを打ち切り、外部トークンと連結した新しいトークンを返す。
    /// </summary>
    private CancellationToken RestartToken(CancellationToken external)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = CancellationTokenSource.CreateLinkedTokenSource(external);
        return cts.Token;
    }

    /// <summary>
    /// alpha を from から to へ動かす。
    /// 途中から再開しても速度が変わらないよう、残り距離に比例した時間で動かす。
    /// ポーズ中（timeScale = 0）でも動くよう unscaled 時間を使う。
    /// </summary>
    private async UniTask FadeAsync(float from, float to, CancellationToken token)
    {
        float duration = fadeDuration * Mathf.Abs(to - from);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        panelCanvasGroup.alpha = to;
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
