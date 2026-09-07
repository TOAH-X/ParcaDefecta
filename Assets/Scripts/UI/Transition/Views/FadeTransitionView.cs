using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// フェード演出の View。CanvasGroup の alpha を動かして画面を隠す／開ける。
/// </summary>
public class FadeTransitionView : TransitionViewBase
{
    [SerializeField] private CanvasGroup panelCanvasGroup;

    public override TransitionManager.TransitionType Type => TransitionManager.TransitionType.Fade;

    private void Awake()
    {
        // 起動時は開いた状態から始める
        SetCoveredImmediate(false);
    }

    /// <summary>
    /// 先に入力を遮断してからフェードインする。
    /// </summary>
    public override async UniTask CoverAsync(CancellationToken token)
    {
        if (panelCanvasGroup == null)
        {
            Debug.LogWarning("[FadeTransitionView] panelCanvasGroup が設定されていません");
            return;
        }

        var linked = RestartToken(token);
        panelCanvasGroup.blocksRaycasts = true;
        await TweenAsync(panelCanvasGroup.alpha, 1f, a => panelCanvasGroup.alpha = a, linked);
    }

    /// <summary>
    /// フェードアウトが終わってから入力遮断を解除する。
    /// </summary>
    public override async UniTask UncoverAsync(CancellationToken token)
    {
        if (panelCanvasGroup == null)
        {
            Debug.LogWarning("[FadeTransitionView] panelCanvasGroup が設定されていません");
            return;
        }

        var linked = RestartToken(token);
        await TweenAsync(panelCanvasGroup.alpha, 0f, a => panelCanvasGroup.alpha = a, linked);
        panelCanvasGroup.blocksRaycasts = false;
    }

    public override void SetCoveredImmediate(bool covered)
    {
        CancelCurrent();
        if (panelCanvasGroup == null) return;

        panelCanvasGroup.alpha = covered ? 1f : 0f;
        panelCanvasGroup.blocksRaycasts = covered;
    }
}
