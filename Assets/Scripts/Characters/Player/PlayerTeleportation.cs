using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using ParcaDefecta.System;

public class PlayerTeleportation : MonoBehaviour
{
    [SerializeField] float coolTime = 1.0f;
    [SerializeField] PlayerMover playerMover;
    [SerializeField] PlayerMoverHistory playerMoverHistory;
    [SerializeField] Transform alterPlayerTransform;

    public float CoolTime => coolTime;
    public float CurrentTimer { get; private set; }
    public bool IsReady => CurrentTimer <= 0;

    public void Execute()
    {
        if (!IsReady) return;
        TeleportationAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid TeleportationAsync(CancellationToken token)
    {
        Debug.Log("Teleportation");
        CurrentTimer = coolTime;

        // 移動と履歴リセット
        playerMover.SetPositionAndRotation(alterPlayerTransform.position, alterPlayerTransform.rotation);
        playerMoverHistory.ClearData();

        // カウントダウン
        while (CurrentTimer > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            if (token.IsCancellationRequested) return;

            float dt = TimeManager.Instance != null ? TimeManager.Instance.DeltaTime : Time.deltaTime;
            CurrentTimer -= dt;
        }

        CurrentTimer = 0;
    }
}