using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] PlayerMover playerMover;
    [SerializeField] PlayerInputReader playerInputReader;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 移動関連の呼び出し
        // 左右移動
        Vector2 moveInput = playerInputReader.MoveInput;
        playerMover.Move(moveInput);
        // ジャンプ
        if (playerInputReader.JumpPressed)
        {
            playerMover.Jump();
        }
    }
}
