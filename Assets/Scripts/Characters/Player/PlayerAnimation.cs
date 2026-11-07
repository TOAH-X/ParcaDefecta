using UnityEngine;
using ParcaDefecta.System;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] PlayerMover playerMover;
    [Header("空中とみなすまでの猶予 (秒)")]
    [SerializeField] float airDelay = 0.1f;

    // 地面から離れてからの経過時間(滞空モーションの判定用)
    private float airTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    void OnEnable()
    {
        playerMover.OnJumped += HandleJumped;
    }

    void OnDisable()
    {
        playerMover.OnJumped -= HandleJumped;
    }

    // 跳んだ瞬間にジャンプモーションのトリガーを送る
    private void HandleJumped()
    {
        animator.SetTrigger("jump");
    }

    // Update is called once per frame
    void Update()
    {
        // ポーズ中はパラメーター更新をスキップ
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        // 走るモーションの更新
        animator.SetBool("run", playerMover.IsMoving);
        // 接地状態の更新(滞空モーションの判定用)
        // 動く足場などで一瞬離れただけでは滞空にしないよう、離れてから airDelay 秒経つまでは接地扱いにする
        if (playerMover.IsGrounded)
        {
            airTimer = 0f;
        }
        else
        {
            airTimer += Time.deltaTime;
        }
        animator.SetBool("grounded", airTimer < airDelay);
        // ジャンプ中か・下向きに動いているかの更新(ジャンプモーションから抜ける判定用)
        animator.SetBool("jumping", playerMover.IsJumping);
        animator.SetBool("falling", playerMover.IsFalling);
    }
}
