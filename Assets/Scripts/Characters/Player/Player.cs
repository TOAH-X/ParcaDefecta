using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;


public class Player : MonoBehaviour
{
    [SerializeField] PlayerMover playerMover;
    [SerializeField] PlayerInputReader playerInputReader;
    [SerializeField] PlayerMoverHistory playerMoverHistory;

    [SerializeField] AlterPlayer alterPlayer;
    [SerializeField] Transform alterPlayerTransform;

    // 新生
    //public bool TeleportationPressed { get; private set; }
    // 解放
    //public bool SeparationPressed { get; private set; }

    // 新生クールタイム
    [SerializeField] float teleportationCoolTime = 1.0f;
    public float TeleportationCoolTime => teleportationCoolTime;
    // 
    public float UseTeleportationTimer { get; private set; }
    // 解放クールタイム
    [SerializeField] float separationCoolTime = 6.0f;
    public float SeparationCoolTime => separationCoolTime;
    // 
    public float UseSeparationTimer { get; private set; }


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

        // 新生
        if (playerInputReader.TeleportationPressed)
        {
            //playerMover.Teleportation();
            TeleportationAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
        // 解放
        if (playerInputReader.SeparationPressed)
        {
            //playerMover.Separation();
            SeparationAsync(moveInput, this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    // 新生？※壁抜け対策を行うこと
    private async UniTaskVoid TeleportationAsync(CancellationToken token)
    {
        Debug.Log("Teleportation");
        UseTeleportationTimer = teleportationCoolTime;

        // 処理
        // 移動
        playerMover.SetPositionAndRotation(alterPlayerTransform.position, alterPlayerTransform.rotation);
        // 履歴をリセット
        playerMoverHistory.ClearData();

        // カウントダウン
        while (UseTeleportationTimer > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            if (token.IsCancellationRequested) return;

            UseTeleportationTimer -= Time.deltaTime;
        }

        UseTeleportationTimer = 0;
    }

    // 解放？※壁抜け対策を行うこと
    private async UniTaskVoid SeparationAsync(Vector2 moveDirection, CancellationToken token)
    {
        Debug.Log("Separation");
        UseSeparationTimer = separationCoolTime;

        // 処理
        // 呼び出し
        alterPlayer.Separation(moveDirection);

        // カウントダウン
        while (UseSeparationTimer > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            if (token.IsCancellationRequested) return;

            UseSeparationTimer -= Time.deltaTime;
        }

        // alterPlayerの場所を自身の場所に移動させること
        // 履歴をリセット
        playerMoverHistory.ClearData();

        // 終了
        UseSeparationTimer = 0;
    }
}
