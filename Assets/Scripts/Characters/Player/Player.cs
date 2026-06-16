using UnityEngine;
using R3;
using ParcaDefecta.System;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [SerializeField] PlayerMover playerMover;
    [SerializeField] PlayerInputReader playerInputReader;
    [SerializeField] PlayerMoverHistory playerMoverHistory;

    [Header("Abilities")]
    [SerializeField] PlayerTeleportation teleportation;
    [SerializeField] PlayerSeparation separation;

    // クールタイム
    public float TeleportationCoolTime => teleportation.CoolTime;
    public float UseTeleportationTimer => teleportation.CurrentTimer;
    public float SeparationCoolTime => separation.CoolTime;
    public float UseSeparationTimer => separation.CurrentTimer;

    // 操作可能か
    private bool isOperable = true;


    // シングルトンの初期化はAwakeで行うのが最も安全です
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        //デバッグ用
        StageManager.Instance?.StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        // ポーズ中は入力を受け付けない
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        if (isOperable == true)
        {
            PlayerControl();
        }
    }

    // 移動関連の呼び出し
    public void PlayerControl()
    {
        // 左右移動
        Vector2 moveInput = playerInputReader.MoveInput;
        playerMover.Move(moveInput);

        // ジャンプ
        if (playerInputReader.JumpPressed)
        {
            playerMover.Jump();
        }

        // 新生
        if (playerInputReader.TeleportationPressed)
        {
            teleportation.Execute();
        }
        // 解放
        if (playerInputReader.SeparationPressed)
        {
            separation.Execute(moveInput);
        }
    }

    // UIボタンの OnClick 等から呼び出すためのメソッド
    public void OnTeleportationButtonClick()
    {
        if (!isOperable) return;
        teleportation.Execute();
    }

    public void OnSeparationButtonClick()
    {
        if (!isOperable) return;
        // 現在の入力値を渡して実行
        separation.Execute(playerInputReader.MoveInput);
    }

    // 以下未確定
    public void OnJumpButtonClick()
    {
        if (!isOperable) return;
        playerMover.Jump();
    }

    public void OnLeftMoveButtonDown()
    {
        if (!isOperable) return;
        playerMover.Move(Vector2.left);

    }

    public void OnRightMoveButtonDown()
    {
        if (!isOperable) return;
        playerMover.Move(Vector2.right);
    }
}
