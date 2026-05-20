/*
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 時間停止に対応するステージ内のタイムマネージャー
/// </summary>
public class FieldTimeManager : SingletonMonoBehaviour<FieldTimeManager>
{
    private bool _isPausedByField;
    private bool _isLockedByMaster;
    private bool _wasEffectivePaused; // 前フレームの実質的な停止状態を記録

    private bool IsPaused => _isPausedByField || _isLockedByMaster;

    public float Time { get; private set; }

    public float DeltaTime => IsPaused ? 0f : UnityEngine.Time.deltaTime;

    /// <summary>
    /// trueで停止、falseで再開
    /// </summary>
    public event Action<bool> OnTimeChanged;

    protected override void Awake()
    {
        base.Awake();
        _wasEffectivePaused = IsPaused;

        UpdateTimeLoop().Forget();

        if (MasterTimeManager.Instance != null)
        {
            MasterTimeManager.Instance.OnTimeChanged += OnMasterTimeChanged;
        }
    }

    protected override void OnDestroy()
    {
        if (MasterTimeManager.Instance != null)
        {
            MasterTimeManager.Instance.OnTimeChanged -= OnMasterTimeChanged;
        }
        OnTimeChanged = null;
        base.OnDestroy();
    }

    private void OnMasterTimeChanged(bool isMasterStop)
    {
        _isLockedByMaster = isMasterStop;
        EvaluateStateChange();
    }

    public void Stop()
    {
        _isPausedByField = true;
        EvaluateStateChange();
    }

    public void Resume()
    {
        _isPausedByField = false;
        EvaluateStateChange();
    }

    /// <summary>
    /// 最終的な停止状態（IsPaused）に変化があった場合のみ、一元化してイベントを発火する
    /// </summary>
    private void EvaluateStateChange()
    {
        bool currentPaused = IsPaused;
        if (currentPaused != _wasEffectivePaused)
        {
            _wasEffectivePaused = currentPaused;
            OnTimeChanged?.Invoke(currentPaused);
        }
    }

    private async UniTaskVoid UpdateTimeLoop()
    {
        while (true)
        {
            if (!IsPaused)
            {
                Time += UnityEngine.Time.deltaTime;
            }
            await UniTask.Yield(PlayerLoopTiming.EarlyUpdate, destroyCancellationToken);
        }
    }

    /// <summary>
    /// フィールドの時間を基準として待機します
    /// </summary>
    public async UniTask<float> WaitSeconds(float waitTime, CancellationToken cancellationToken)
    {
        if (waitTime <= 0)
        {
            while (IsPaused)
            {
                await UniTask.Yield(PlayerLoopTiming.EarlyUpdate, cancellationToken);
            }
            return 0f;
        }

        float startFieldTime = Time;
        while (true)
        {
            await UniTask.Yield(PlayerLoopTiming.EarlyUpdate, cancellationToken);

            float elapsed = Time - startFieldTime;
            if (elapsed >= waitTime)
            {
                return elapsed - waitTime; // オーバーした時間を返す
            }
        }
    }
}
*/
/*
// 0からtargetTime秒までテキストを更新する
async UniTask SampleAsync(float targetTime = 3f)
{
    float time = 0f;
    while (time < targetTime)
    {
        text.SetText(time.ToString());
        time += FieldTimeManager.Instance.DeltaTime;
        await UniTask.Yield(destroyCancellationToken);
    }
    text.SetText(targetTime);
}
*/