using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// トランジション UI の Presenter 層。
/// Model（TransitionManager）からの依頼イベントを受けて、種類に対応する View を動かし、完了を Model に報告する。
/// TransitionCanvas プレハブのルートにアタッチして使用。
/// View は子オブジェクトから自動収集するため、演出の種類を増やしても本クラスの変更は不要。
/// </summary>
public class TransitionPresenter : MonoBehaviour
{
    // 種類ごとの View。Awake で子から収集する
    private readonly Dictionary<TransitionManager.TransitionType, TransitionViewBase> views = new();

    // OnDisable 時に Instance を叩くと終了処理中に再生成される恐れがあるため、購読時の参照を保持する
    private TransitionManager manager;

    private void Awake()
    {
        CollectViews();
    }

    private void OnEnable()
    {
        manager = TransitionManager.Instance;
        if (manager == null) return;

        manager.OnCoverRequested += HandleCover;
        manager.OnUncoverRequested += HandleUncover;

        // 途中参加時の同期。既に隠れている状態なら、その種類の View を隠した状態にしておく
        if (manager.State == TransitionManager.TransitionState.Covered
            && views.TryGetValue(manager.ActiveType, out var view))
        {
            view.SetCoveredImmediate(true);
        }
    }

    private void OnDisable()
    {
        if (manager == null) return;

        manager.OnCoverRequested -= HandleCover;
        manager.OnUncoverRequested -= HandleUncover;
        manager = null;
    }

    /// <summary>
    /// 子オブジェクトから TransitionViewBase を集めて種類ごとに登録する。
    /// </summary>
    private void CollectViews()
    {
        views.Clear();
        foreach (var view in GetComponentsInChildren<TransitionViewBase>(true))
        {
            if (views.ContainsKey(view.Type))
            {
                Debug.LogWarning($"[TransitionPresenter] {view.Type} の View が重複しています。'{view.name}' は無視します");
                continue;
            }
            views[view.Type] = view;
        }
    }

    private void HandleCover(TransitionManager.TransitionType type)
    {
        RunAsync(type, cover: true).Forget();
    }

    private void HandleUncover(TransitionManager.TransitionType type)
    {
        RunAsync(type, cover: false).Forget();
    }

    /// <summary>
    /// 種類に対応する View に演出を依頼し、終わったら Model に報告する。
    /// 新しい依頼で打ち切られた場合は報告しない（Model 側は新しい依頼の完了を待っている）。
    /// </summary>
    private async UniTaskVoid RunAsync(TransitionManager.TransitionType type, bool cover)
    {
        if (!views.TryGetValue(type, out var view))
        {
            Debug.LogWarning($"[TransitionPresenter] {type} に対応する View がありません。演出なしで完了報告します");
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
