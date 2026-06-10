using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using ParcaDefecta.System;

public class PlayerSeparation : MonoBehaviour
{
    [SerializeField] float coolTime = 6.0f;
    [SerializeField] AlterPlayer alterPlayer;
    [SerializeField] PlayerMoverHistory playerMoverHistory;

    public float CoolTime => coolTime;
    public float CurrentTimer { get; private set; }
    public bool IsReady => CurrentTimer <= 0;

    public void Execute(Vector2 moveInput)
    {
        if (!IsReady) return;
        SeparationAsync(moveInput, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid SeparationAsync(Vector2 moveDirection, CancellationToken token)
    {
        Debug.Log("Separation");
        CurrentTimer = coolTime;

        alterPlayer.Separation(moveDirection);

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