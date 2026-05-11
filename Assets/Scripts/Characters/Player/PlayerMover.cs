using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [Header("移動速度")]
    [SerializeField] float moveSpeed = 7.5f;
    [Header("ジャンプ力")]
    [SerializeField] float jumpForce = 12.0f;
    [Header("LayerのGround")]
    [SerializeField] LayerMask groundLayer;

    [SerializeField] Rigidbody2D rb2D;

    // 移動ベクトル
    private Vector2 moveDirection;
    // 向き(右を向いているか)
    private bool isLookRight;
    public bool IsLookRight => isLookRight;

    // 移動中か(run)
    private bool isMoving;
    public bool IsMoving => isMoving;

    // ジャンプ中か(jump)
    private bool isJumping;
    public bool IsJumping => isJumping;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // ジャンプ中かつ、落下中または静止しており、地面に接地した瞬間にフラグを解除
        if (isJumping && rb2D.linearVelocity.y <= 0.01f && IsGrounding())
        {
            isJumping = false;
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    /// <param name="入力"></param>
    public void Move(Vector2 input)
    {
        moveDirection = input * moveSpeed;
        float moveX = moveDirection.x;

        isMoving = moveDirection.magnitude > 0.1f; // 移動中かどうかを更新

        if (isMoving) // 入力が一定以上の場合のみ更新
        {
            moveDirection.Normalize(); // 斜め移動を一定速度にするために正規化
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90; // 回転角度を計算
            transform.Translate(moveSpeed * Time.deltaTime * moveDirection, Space.World); // 移動方向に沿って移動
        }
        // 向きの変更
        if (moveX > 0)
        {
            Direction(true);
        }
        else if (moveX < 0)
        {
            Direction(false);
        }
    }

    // 向きの変更
    private void Direction(bool isLookRightInDirection)
    {
        isLookRight = isLookRightInDirection;
        Vector3 playerScale = this.transform.localScale;

        // 右向き
        if (isLookRight == true)
        {
            playerScale.x = 1 * Mathf.Abs(playerScale.x);
            // 逆側に移動していた場合は止まる
            if (rb2D.linearVelocity.x < 0)
            {
                rb2D.linearVelocity = new Vector2(0, rb2D.linearVelocity.y);
            }
        }
        // 左向き
        else
        {
            playerScale.x = -1 * Mathf.Abs(playerScale.x);
            // 逆側に移動していた場合は止まる
            if (rb2D.linearVelocity.x > 0)
            {
                rb2D.linearVelocity = new Vector2(0, rb2D.linearVelocity.y);
            }
        }
        this.transform.localScale = playerScale;
    }

    // ジャンプ処理
    public void Jump()
    {
        //Rayの判定(地上にいるとき)
        if (IsGrounding() == true && rb2D.linearVelocity.y == 0)
        {
            // 通常ジャンプ
            rb2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isJumping = true;
        }
    }

    // 地面との接地判定
    private bool IsGrounding()
    {
        float rayLength = this.transform.localScale.x * 1.0f;

        Vector2 direction = transform.right * 0.9f;
        Vector2 startPos = transform.position + new Vector3(-rayLength / 2, -this.transform.localScale.y / 2 - 0.1f, 0);

        // デバッグ表示
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, rayLength, groundLayer);
        Debug.DrawRay(startPos, direction, Color.red);

        return hit.collider != null;
    }

    // 座標を直接変更（テレポート用）
    public void SetPositionAndRotation(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
        //rb2D.velocity = Vector2.zero; // テレポート後の慣性をリセット
    }

    /*
        // 打ち上げ
        public void Launch(Vector2 force)
        {
            // 一旦速度リセット（不要なら削除可）
            rb2D.linev arVelocity = Vector2.zero;

            // 力を加える
            rb2D.AddForce(force, ForceMode2D.Impulse);
        }
    */
}
