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
    [SerializeField] BoxCollider2D boxCollider;

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

        IsGrounding();
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
        // 消さないこと
        /*
        float rayLength = this.transform.localScale.x * 1.0f;

        Vector2 direction = transform.right * 0.9f;
        Vector2 startPos = transform.position + new Vector3(-rayLength / 2, -this.transform.localScale.y / 2 - 0.1f, 0);

        // デバッグ表示
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, rayLength, groundLayer);
        Debug.DrawRay(startPos, direction, Color.red);

        return hit.collider != null;
        */

        /*
        if (boxCollider == null)
        {
            // Colliderが見つからない場合は、安全のためfalseを返す
            return false;
        }

        // Colliderの境界情報を取得
        Bounds bounds = boxCollider.bounds;

        // レイキャストの原点: コライダーの下端中央
        // bounds.center.x はコライダーの中心X座標
        // bounds.min.y はコライダーの下端Y座標
        Vector2 rayOrigin = new Vector2(bounds.center.x, bounds.min.y);

        // レイキャストの方向: 真下
        Vector2 rayDirection = Vector2.down;

        // レイキャストの長さ: コライダーの境界からわずかに下へ伸ばす
        // 例えば、0.05f 程度の短い距離で十分
        float rayLength = 0.05f; // 地面とのわずかな隙間を検出するための長さ

        // デバッグ表示 (Sceneビューで確認できる)
        Debug.DrawRay(rayOrigin, rayDirection * rayLength, Color.green);

        // レイキャストを実行
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, groundLayer);

        // ヒットしたコライダーがあれば接地していると判断
        return hit.collider != null;
        */
        Vector2 thisSize = new Vector2(Mathf.Abs(this.transform.localScale.x), Mathf.Abs(this.transform.localScale.y));
        float sizeRate = 0.9f;
        float rayLength = boxCollider.size.x * thisSize.x;

        Vector2 direction = transform.right * thisSize.x * sizeRate;
        Vector2 startPos = transform.position + new Vector3(((1 - sizeRate) * thisSize.x - rayLength) / 2, -boxCollider.size.y * thisSize.y / 2 - 0.1f, 0);

        // デバッグ表示
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, rayLength, groundLayer);
        Debug.DrawRay(startPos, direction, Color.orangeRed);

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
