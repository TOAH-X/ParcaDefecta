using UnityEngine;
using R3;
using ParcaDefecta.System;

/// <summary>
/// 跳んでから着地するまでを見張る(Player と AlterPlayer に付ける)
/// </summary>
public class JumpTracker : MonoBehaviour
{
    [Header("ジャンプ直後に着地とみなさない時間 (秒)")]
    [SerializeField] float jumpLandGuardTime = 0.1f;
    [SerializeField] GroundChecker groundChecker;

    // ジャンプ中か(跳んでから着地するまで)
    private bool isJumping;
    public bool IsJumping => isJumping;

    // ジャンプしてからの経過時間(着地判定の誤検知防止用)
    private float jumpElapsed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (TimeManager.Instance == null) return;

        // TimeManagerのOnTickを購読して、ポーズ状態を反映した更新を行う
        TimeManager.Instance.OnTick
            .Subscribe(dt => PerformUpdate(dt))
            .AddTo(this);
    }

    private void PerformUpdate(float dt)
    {
        if (!isJumping) return;

        jumpElapsed += dt;

        // ジャンプ直後は足元がまだ地面に近いので、一定時間たってから接地でフラグを解除する
        // 速度で判定すると、上昇中の足場に乗ったときに解除されずジャンプできなくなる
        if (jumpElapsed >= jumpLandGuardTime && groundChecker.Check())
        {
            isJumping = false;
        }
    }

    // 跳んだときに呼ぶ(ジャンプ・打ち上げ)
    public void Begin()
    {
        isJumping = true;
        jumpElapsed = 0f;
    }

    // 状態を消す(AlterPlayer の分離し直しのとき用)
    public void Clear()
    {
        isJumping = false;
        jumpElapsed = 0f;
    }
}
