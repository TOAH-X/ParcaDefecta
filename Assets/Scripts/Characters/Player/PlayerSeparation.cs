using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using ParcaDefecta.System;

public class PlayerSeparation : MonoBehaviour
{
    [SerializeField] float coolTime = 6.0f;
    [Header("解放中の分身の挙動")]
    [SerializeField, Tooltip("分身が分離している秒数。クールタイム以下にすること")]
    float separationDuration = 5.0f;
    [SerializeField, Tooltip("分離直後に分身が慣性で進む速さ")]
    float inertialSpeed = 5.0f;
    [SerializeField] AlterPlayer alterPlayer;
    [SerializeField] PlayerMoverHistory playerMoverHistory;

    public float CoolTime => coolTime;
    public float CurrentTimer { get; private set; }
    public bool IsReady => CurrentTimer <= 0;

    public void Execute(Vector2 moveInput)
    {
        if (!IsReady) return;
        // クールタイムが持続時間より短く設定されていても、分離中の二重実行はさせない
        if (alterPlayer.IsSeparated) return;
        SeparationAsync(moveInput, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid SeparationAsync(Vector2 moveDirection, CancellationToken token)
    {
        Debug.Log("Separation");
        CurrentTimer = coolTime;

        alterPlayer.Separation(moveDirection, separationDuration, inertialSpeed);

        while (CurrentTimer > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            if (token.IsCancellationRequested) return;

            float dt = TimeManager.Instance != null ? TimeManager.Instance.DeltaTime : Time.deltaTime;
            CurrentTimer -= dt;
        }

        CurrentTimer = 0;
    }

    private void OnValidate()
    {
        if (coolTime < separationDuration)
        {
            Debug.LogWarning($"[PlayerSeparation] coolTime({coolTime}) が separationDuration({separationDuration}) より短いです。分離中に再実行できてしまうため、coolTime 以上を推奨します", this);
        }
    }
}