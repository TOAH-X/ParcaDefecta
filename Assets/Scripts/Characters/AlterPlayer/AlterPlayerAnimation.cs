using UnityEngine;
using ParcaDefecta.System;

public class AlterPlayerAnimation : MonoBehaviour
{
    [SerializeField] AlterPlayerMover alterPlayerMover;
    [SerializeField] Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (animator == null) return;

        bool shouldAnimate = alterPlayerMover != null && alterPlayerMover.IsMoving;

        // ポーズ中または移動中でなければ、Animator の状態更新を完全に止める
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value)
        {
            animator.enabled = false;
            animator.SetBool("run", false);
            return;
        }

        animator.enabled = shouldAnimate;

        if (shouldAnimate)
        {
            animator.SetBool("run", true);
        }
        else
        {
            animator.SetBool("run", false);
        }
    }
}
