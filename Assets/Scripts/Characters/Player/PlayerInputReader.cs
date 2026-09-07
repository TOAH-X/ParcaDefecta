using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    // 移動入力（キー入力を優先し、無ければ UI ボタンの入力を使う）
    public Vector2 MoveInput { get; private set; }
    // ジャンプ入力
    public bool JumpPressed { get; private set; }
    // 新生入力
    public bool TeleportationPressed { get; private set; }
    // 解放入力
    public bool SeparationPressed { get; private set; }
    // リトライ入力
    public bool RetryPressed { get; private set; }


    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction teleportationAction;
    private InputAction separationAction;
    private InputAction retryAction;

    // UI ボタンからの左右入力（-1 / 0 / 1）。PlayerUIBridge が毎フレーム書き込む
    private float uiHorizontal;

    /// <summary>
    /// UI ボタンからの左右入力を設定する。押していないときは 0 を渡す。
    /// </summary>
    public void SetUIHorizontal(float horizontal)
    {
        uiHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions["Move"];
        jumpAction = InputSystem.actions["Jump"];
        teleportationAction = InputSystem.actions["Teleportation"];
        separationAction = InputSystem.actions["Separation"];
        retryAction = InputSystem.actions["Retry"];
    }

    // Update is called once per frame
    void Update()
    {
        /*
        // 入力キーの取得
        // 移動
        float h = Input.GetAxisRaw("Horizontal");
        // float v = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(h, 0).normalized;
        // ジャンプ
        JumpPressed = Input.GetKeyDown(KeyCode.Space);
        // 新生？(影の場所と刷り替わる)
        TeleportationPressed = Input.GetKeyDown(KeyCode.E);
        // 解放？(影の場所と刷り替わる)
        SeparationPressed = Input.GetKeyDown(KeyCode.Q);
        */

        // 移動。キー入力が無いときだけ UI ボタンの入力を採用する
        float horizontal = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;
        if (horizontal == 0f)
        {
            horizontal = uiHorizontal;
        }
        MoveInput = new Vector2(horizontal, 0).normalized;
        // ジャンプ
        if (jumpAction != null)
        {
            JumpPressed = jumpAction.triggered;
        }
        else
        {
            JumpPressed = false;
        }

        // 新生
        if (teleportationAction != null)
        {
            TeleportationPressed = teleportationAction.triggered;
        }
        else
        {
            TeleportationPressed = false;
        }

        // 解放
        if (separationAction != null)
        {
            SeparationPressed = separationAction.triggered;
        }
        else
        {
            SeparationPressed = false;
        }

        // リトライ
        if (retryAction != null)
        {
            RetryPressed = retryAction.triggered;
        }
        else
        {
            RetryPressed = false;
        }
    }
}
