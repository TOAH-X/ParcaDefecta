using UnityEngine;
using R3;
using ParcaDefecta.System;

public class PlayerMover : MonoBehaviour, ILaunchable
{
    [Header("移動速度")]
    [SerializeField] float moveSpeed = 7.5f;
    [Header("ジャンプ力")]
    [SerializeField] float jumpForce = 12.0f;
    [Header("LayerのGround")]
    [SerializeField] LayerMask groundLayer;
    [Header("コヨーテタイム (秒)")]
    [SerializeField] float coyoteTime = 0.1f;

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

    // コヨーテタイム用のカウンター
    private float coyoteCounter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (TimeManager.Instance == null) return;

        // TimeManagerのOnTickを購読して、ポーズ状態を反映した更新を行う
        TimeManager.Instance.OnTick
            .Subscribe(dt => PerformUpdate(dt))
            .AddTo(this);
    }

    /// <summary>
    /// 毎フレームの内部状態更新（接地判定やタイマーなど）
    /// </summary>
    private void PerformUpdate(float dt)
    {
        bool grounded = IsGrounding();

        if (grounded)
        {
            coyoteCounter = coyoteTime; // 接地中は常にタイマーを満タンにする
            // ジャンプ中かつ、落下中または静止しており、地面に接地した瞬間にフラグを解除
            if (isJumping && rb2D.linearVelocity.y <= 0.01f)
            {
                isJumping = false;
            }
        }
        else
        {
            coyoteCounter -= dt; // 空中にいる間はカウントダウン（ポーズ中はdt=0なので停止する）
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    /// <param name="入力"></param>
    public void Move(Vector2 input)
    {
        // ポーズ中は何もしない
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        moveDirection = input * moveSpeed;
        float moveX = moveDirection.x;


        //isMoving = moveDirection.magnitude > 0.1f; // 移動中かどうかを更新
        isMoving = input.magnitude > 0f;

        /*
        if (isMoving) // 入力が一定以上の場合のみ更新
        {
            moveDirection.Normalize(); // 斜め移動を一定速度にするために正規化
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90; // 回転角度を計算
            transform.Translate(moveSpeed * Time.deltaTime * moveDirection, Space.World); // 移動方向に沿って移動
        }
        */
        // 物理演算(Rigidbody)に基づいた水平移動
        // transform.Translate ではなく速度を上書きすることで、ポーズ時の停止を確実にする
        rb2D.linearVelocity = new Vector2(input.x * moveSpeed, rb2D.linearVelocity.y);

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
        // ポーズ中は何もしない
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        // コヨーテタイム内（coyoteCounter > 0）かつ、すでにジャンプ中でない場合
        if (coyoteCounter > 0f && !isJumping)
        {
            // 崖から歩いて落ちている最中のコヨーテジャンプを安定させるため、垂直速度をリセット
            if (rb2D.linearVelocity.y < 0)
            {
                rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, 0);
            }

            // 通常ジャンプ
            rb2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isJumping = true;
            coyoteCounter = 0f; // 二重ジャンプ防止のためにカウンターをゼロにする
        }
    }

    // 地面との接地判定
    private bool IsGrounding()
    {
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

    /// <summary>
    /// 外部ギミックによる打ち上げ処理
    /// </summary>
    public void Launch(float verticalForce)
    {
        // ポーズ中は何もしない
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        // 垂直速度を直接上書き（ジャンプ力との重複を防ぐ）
        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, verticalForce);

        // ジャンプ状態としてマークし、コヨーテタイムを終了させることで
        // 同じフレーム内でのジャンプ入力の重複適用を防ぐ
        isJumping = true;
        coyoteCounter = 0f;
    }
}
