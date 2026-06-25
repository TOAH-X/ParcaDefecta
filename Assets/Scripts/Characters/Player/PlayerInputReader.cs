using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    // 移動入力
    public Vector2 MoveInput { get; private set; }
    // ジャンプ入力
    public bool JumpPressed { get; private set; }
    // 新生入力
    public bool TeleportationPressed { get; private set; }
    // 解放入力
    public bool SeparationPressed { get; private set; }


    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction teleportationAction;
    private InputAction separationAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions["Move"];
        jumpAction = InputSystem.actions["Jump"];
        teleportationAction = InputSystem.actions["Teleportation"];
        separationAction = InputSystem.actions["Separation"];
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

        // 移動
        if (moveAction != null)
        {
            Vector2 moveDirection = moveAction.ReadValue<Vector2>();
            MoveInput = new Vector2(moveDirection.x, 0).normalized;
        }
        else
        {
            MoveInput = Vector2.zero;
        }
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
    }
}
