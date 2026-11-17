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
    [SerializeField] PlayerDeath death;

    [Header("Camera")]
    [SerializeField, Tooltip("カメラに見てほしい点。本体と分身をまとめた注目点(PlayerFocusPoint)を設定する。未設定なら Player 自身")]
    Transform cameraFocus;

    /// <summary>カメラの追従先。未設定なら Player 自身</summary>
    public Transform CameraFocus => cameraFocus != null ? cameraFocus : transform;

    // クールタイム
    public float TeleportationCoolTime => teleportation.CoolTime;
    public float UseTeleportationTimer => teleportation.CurrentTimer;
    public float SeparationCoolTime => separation.CoolTime;
    public float UseSeparationTimer => separation.CurrentTimer;

    // 操作可能か
    private bool isOperable = true;

    // 実際に操作を受け付けるか。操作可能で、かつ死亡していないとき
    private bool CanOperate => isOperable && !(death != null && death.IsDead);


    // シングルトンの初期化はAwakeで行うのが最も安全です
    void Awake()
    {
        // StageManager側で生成・破棄を管理しているため、
        // Awakeでの重複チェックによるDestroyは行わず、常に最新をInstanceに登録します。
        // これにより、Destroy(旧) と Instantiate(新) が同フレームで起きた際の消失を防ぎます。
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 再生時にスクリプトを読み込み直さない設定のため、前の再生の値を起動時に戻す
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        Instance = null;
    }

    // Update is called once per frame
    void Update()
    {
        // 先に入力を読み、このフレームの値で判断する
        playerInputReader.Read();

        // リトライ入力はポーズ中でも受け付ける
        if (playerInputReader.RetryPressed)
        {
            OnRetryPressed();
            return;
        }

        // ポーズ中は他の入力を受け付けない
        if (IsPaused()) return;

        if (CanOperate)
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
    // キー入力側（Update）と同様に、ポーズ中は受け付けない
    public void OnTeleportationButtonClick()
    {
        if (IsPaused()) return;
        if (!CanOperate) return;
        teleportation.Execute();
    }

    public void OnSeparationButtonClick()
    {
        if (IsPaused()) return;
        if (!CanOperate) return;
        // 現在の入力値を渡して実行
        separation.Execute(playerInputReader.MoveInput);
    }

    // 以下未確定
    public void OnJumpButtonClick()
    {
        if (IsPaused()) return;
        if (!CanOperate) return;
        playerMover.Jump();
    }

    private static bool IsPaused()
    {
        return TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value;
    }

    /// <summary>
    /// リトライ入力処理。ポーズ中でも実行可能。
    /// </summary>
    private void OnRetryPressed()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ReloadCurrentStage();
        }
    }

    /// <summary>
    /// UI ボタンからの左右入力を受け取る。実際の移動は Update 内の PlayerControl で一括して行う。
    /// </summary>
    public void SetUIHorizontalInput(float horizontal)
    {
        playerInputReader.SetUIHorizontal(horizontal);
    }
}
