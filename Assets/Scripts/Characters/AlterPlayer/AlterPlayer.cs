using UnityEngine;
using Cysharp.Threading.Tasks;

public class AlterPlayer : MonoBehaviour
{
    [SerializeField] private PlayerMoverHistory playerMoverHistory;
    [SerializeField] private AlterPlayerMover alterPlayerMover;

    // 状態(分離されているか)
    bool isSeparated = false;

    // 何秒遅れて追従するか
    [SerializeField] float syncDelaySeconds = 1.0f;             // Player側を参照すること
    // 分離された後何秒後にリセットされるか
    [SerializeField] float separationDuration = 5.0f;           // Player側を参照すること

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // プレイヤーの動きと同期
        if (isSeparated == false) 
        {
            SynchronizePlayer();
        }
    }

    // 追従※AlterPlayerMoveに移動すること
    public void SynchronizePlayer()
    {
        // nフレーム前のデータを取得し状態を適用
        if (playerMoverHistory.TryGetPastFrameData((int)(60 * syncDelaySeconds), out var pastData)) 
        {
            transform.position = pastData.position;
            transform.rotation = pastData.rotation;
        }
    }

    // 解放？※壁抜け対策を行うこと
    public void Separation(Vector2 moveDirection)
    {
        // 分離
        isSeparated = true;
        // 重力・当たり判定の適用
        alterPlayerMover.SetState(isSeparated);

        alterPlayerMover.InertialMovement(moveDirection, 5f, 5f, this.GetCancellationTokenOnDestroy()).Forget();


        // UniTaskで待機 → 自動的に戻す
        ResetSeparationAsync(separationDuration).Forget();
    }

    // 解放状態を戻す
    private async UniTaskVoid ResetSeparationAsync(float delay)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: this.GetCancellationTokenOnDestroy());

        // 分離状態を戻す
        isSeparated = false;
        // 重力・当たり判定の適用を外す
        alterPlayerMover.SetState(isSeparated);
    }

    // ILaunchable 実装
    public void Launch(Vector2 force)
    {
        alterPlayerMover.Launch(force);
    }
}
