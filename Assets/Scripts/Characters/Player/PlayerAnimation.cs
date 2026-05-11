using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] PlayerMover playerMover;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // 走るモーションの更新
        animator.SetBool("run", playerMover.IsMoving);
        // ジャンプモーションの更新
        animator.SetBool("jump", playerMover.IsJumping);
    }
}
