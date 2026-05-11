using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;


public class Player : MonoBehaviour
{
    [SerializeField] PlayerMover playerMover;
    [SerializeField] PlayerInputReader playerInputReader;
    [SerializeField] PlayerMoverHistory playerMoverHistory;

    [Header("Abilities")]
    [SerializeField] PlayerTeleportation teleportation;
    [SerializeField] PlayerSeparation separation;

    public float TeleportationCoolTime => teleportation.CoolTime;
    public float UseTeleportationTimer => teleportation.CurrentTimer;
    public float SeparationCoolTime => separation.CoolTime;
    public float UseSeparationTimer => separation.CurrentTimer;

    // 操作可能か
    private bool isOperable = true;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
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
}
