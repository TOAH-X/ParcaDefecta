using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using ParcaDefecta.System;

/// <summary>
/// 本体の死亡を扱う。死亡したら操作を止め、少し待ってからステージを最初からやり直す。
/// 死亡の原因は「deathHeight より下へ落ちた」のほか、外から Die() を呼んで増やせる。
/// 分身（AlterPlayer）には付けない。
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    [SerializeField, Tooltip("この高さより下に行ったら死亡する")] private float deathHeight = -500f;
    [SerializeField, Tooltip("死亡してからやり直すまでの秒数")] private float delayBeforeRestart = 0.3f;

    /// <summary>死亡していれば true</summary>
    public bool IsDead { get; private set; }

    void Update()
    {
        if (IsDead) return;

        if (transform.position.y < deathHeight)
        {
            Die();
        }
    }

    /// <summary>
    /// 本体を死亡させる。すでに死亡していれば何もしない。
    /// </summary>
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log("[PlayerDeath] 死亡しました。ステージをやり直します");

        DeathAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid DeathAsync(CancellationToken token)
    {
        // TODO: 死亡の演出（エフェクト・SE など）はここに差し込む

        // 待つ（ポーズ中は進めない）
        float timer = delayBeforeRestart;
        while (timer > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            if (token.IsCancellationRequested) return;

            float dt = TimeManager.Instance != null ? TimeManager.Instance.DeltaTime : Time.deltaTime;
            timer -= dt;
        }

        // やり直し。チェックポイントを作るときはここを差し替える
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ReloadCurrentStage();
        }
    }
}
