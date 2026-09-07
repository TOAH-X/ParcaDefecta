using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// ワイプ演出の View。Image Type を Filled にした画像の fillAmount を動かして画面を隠す／開ける。
/// 塗りの方向は Image 側の Fill Method / Fill Origin で決める。
/// </summary>
public class WipeTransitionView : TransitionViewBase
{
    [SerializeField] private Image wipeImage;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    public override TransitionManager.TransitionType Type => TransitionManager.TransitionType.Wipe;

    private void Awake()
    {
        // 起動時は開いた状態から始める
        SetCoveredImmediate(false);
    }

    /// <summary>
    /// 先に入力を遮断してから塗りつぶす。
    /// </summary>
    public override async UniTask CoverAsync(CancellationToken token)
    {
        if (!IsConfigured()) return;

        var linked = RestartToken(token);
        panelCanvasGroup.blocksRaycasts = true;
        await TweenAsync(wipeImage.fillAmount, 1f, f => wipeImage.fillAmount = f, linked);
    }

    /// <summary>
    /// 塗りを消してから入力遮断を解除する。
    /// </summary>
    public override async UniTask UncoverAsync(CancellationToken token)
    {
        if (!IsConfigured()) return;

        var linked = RestartToken(token);
        await TweenAsync(wipeImage.fillAmount, 0f, f => wipeImage.fillAmount = f, linked);
        panelCanvasGroup.blocksRaycasts = false;
    }

    public override void SetCoveredImmediate(bool covered)
    {
        CancelCurrent();
        if (!IsConfigured()) return;

        wipeImage.fillAmount = covered ? 1f : 0f;
        panelCanvasGroup.blocksRaycasts = covered;
    }

    private bool IsConfigured()
    {
        if (wipeImage == null)
        {
            Debug.LogWarning("[WipeTransitionView] wipeImage が設定されていません");
            return false;
        }
        if (panelCanvasGroup == null)
        {
            Debug.LogWarning("[WipeTransitionView] panelCanvasGroup が設定されていません");
            return false;
        }
        return true;
    }
}
