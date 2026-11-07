using UnityEngine;
using ParcaDefecta.System;

public class AlterPlayerAnimation : MonoBehaviour
{
    [SerializeField] AlterPlayerMover alterPlayerMover;
    [SerializeField] Animator animator;
    [Header("空中とみなすまでの猶予 (秒)")]
    [SerializeField] float airDelay = 0.1f;

    // 地面から離れてからの経過時間(滞空モーションの判定用)
    private float airTimer;
    // 前フレームで分離していたか(分離した瞬間の検出用)
    private bool wasSeparated;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    void OnEnable()
    {
        alterPlayerMover.OnLaunched += HandleLaunched;
    }

    void OnDisable()
    {
        alterPlayerMover.OnLaunched -= HandleLaunched;
    }

    // 打ち上げられた瞬間にジャンプモーションのトリガーを送る
    private void HandleLaunched()
    {
        // Animator を止めている間(追従中・ポーズ中)は送らない。古いトリガーが残り、動き出したときに誤って再生されるため
        if (!alterPlayerMover.IsSeparated) return;
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        animator.SetTrigger("jump");
    }

    // Update is called once per frame
    void Update()
    {
        if (animator == null || alterPlayerMover == null) return;

        // ポーズ中は Animator の状態更新を完全に止める
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value)
        {
            animator.enabled = false;
            return;
        }

        // 追従中はプレイヤーの過去の絵を写すので、Animator は止めておく
        if (!alterPlayerMover.IsSeparated)
        {
            animator.enabled = false;
            wasSeparated = false;
            return;
        }

        animator.enabled = true;

        // 分離した瞬間は、前回の分離のときの状態が残らないよう最初からやり直す
        if (!wasSeparated)
        {
            animator.Rebind();
            airTimer = 0f;
            wasSeparated = true;
        }

        // 走るモーションの更新
        animator.SetBool("run", alterPlayerMover.IsMoving);
        // 接地状態の更新(滞空モーションの判定用)
        // 動く足場などで一瞬離れただけでは滞空にしないよう、離れてから airDelay 秒経つまでは接地扱いにする
        if (alterPlayerMover.IsGrounded)
        {
            airTimer = 0f;
        }
        else
        {
            airTimer += Time.deltaTime;
        }
        animator.SetBool("grounded", airTimer < airDelay);
        // ジャンプ中か・下向きに動いているかの更新(ジャンプモーションから抜ける判定用)
        animator.SetBool("jumping", alterPlayerMover.IsJumping);
        animator.SetBool("falling", alterPlayerMover.IsFalling);
    }
}
