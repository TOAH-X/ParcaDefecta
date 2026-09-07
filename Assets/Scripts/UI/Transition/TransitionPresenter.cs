using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// トランジション UI の Presenter 層。
/// Model（TransitionManager）からの依頼イベントを受けて View を動かし、完了を Model に報告する。
/// TransitionCanvas プレハブのルートにアタッチして使用。
/// </summary>
public class TransitionPresenter : MonoBehaviour
{
    [SerializeField] private TransitionView view;

    // OnDisable 時に Instance を叩くと終了処理中に再生成される恐れがあるため、購読時の参照を保持する
    private TransitionManager manager;

    private void OnEnable()
    {
        manager = TransitionManager.Instance;
        if (manager == null) return;

        manager.OnCoverRequested += HandleCover;
        manager.OnUncoverRequested += HandleUncover;

        // 途中参加時の同期。既に隠れている状態なら暗幕を出しておく
        if (view != null)
        {
            view.SetCoveredImmediate(manager.State == TransitionManager.TransitionState.Covered);
        }
    }

    private void OnDisable()
    {
        if (manager == null) return;

        manager.OnCoverRequested -= HandleCover;
        manager.OnUncoverRequested -= HandleUncover;
        manager = null;
    }

    private void HandleCover()
    {
        RunAsync(cover: true).Forget();
    }

    private void HandleUncover()
    {
        RunAsync(cover: false).Forget();
    }

    /// <summary>
    /// View に演出を依頼し、終わったら Model に報告する。
    /// 新しい依頼で打ち切られた場合は報告しない（Model 側は新しい依頼の完了を待っている）。
    /// </summary>
    private async UniTaskVoid RunAsync(bool cover)
    {
        if (view == null)
        {
            Debug.LogWarning("[TransitionPresenter] view が設定されていません。演出なしで完了報告します");
            Report(cover);
            return;
        }

        try
        {
            var token = this.GetCancellationTokenOnDestroy();
            if (cover)
            {
                await view.CoverAsync(token);
            }
            else
            {
                await view.UncoverAsync(token);
            }
        }
        catch (System.OperationCanceledException)
        {
            return;
        }

        Report(cover);
    }

    private void Report(bool cover)
    {
        if (manager == null) return;

        if (cover)
        {
            manager.NotifyCovered();
        }
        else
        {
            manager.NotifyUncovered();
        }
    }
}
