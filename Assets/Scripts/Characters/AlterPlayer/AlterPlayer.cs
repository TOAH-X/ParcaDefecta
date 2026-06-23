using UnityEngine;
using Cysharp.Threading.Tasks;
using ParcaDefecta.System;

public class AlterPlayer : MonoBehaviour
{
    [SerializeField] private PlayerMoverHistory playerMoverHistory;
    [SerializeField] private AlterPlayerMover alterPlayerMover;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // 状態(分離されているか)
    bool isSeparated = false;

    // 通常状態のカラーを保持
    private Color normalColor;

    // 何秒遅れて追従するか
    [SerializeField] float syncDelaySeconds = 1.0f;             // Player側を参照すること
    // 分離された後何秒後にリセットされるか
    [SerializeField] float separationDuration = 5.0f;           // Player側を参照すること

    // Start is called before the first frame update
    void Start()
    {
        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // ポーズ中は同期処理を停止
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        // プレイヤーの動きと同期
        if (isSeparated == false)
        {
            SynchronizePlayer();
        }

        UpdateVisibility();
    }

    // 追従※AlterPlayerMoveに移動すること
    public void SynchronizePlayer()
    {
        // nフレーム前のデータを取得し状態を適用
        if (playerMoverHistory.TryGetPastFrameData((int)(60 * syncDelaySeconds), out var pastData))
        {
            transform.position = pastData.position;
            transform.rotation = pastData.rotation;
            transform.localScale = pastData.scale;
            spriteRenderer.sprite = pastData.sprite;
        }
    }

    /// <summary>
    /// プレイヤーと重なっているか判定して表示/非表示を切り替える
    /// </summary>
    private void UpdateVisibility()
    {
        if (Player.Instance == null) return;

        // プレイヤーとの距離を計算
        float distance = Vector3.Distance(transform.position, Player.Instance.transform.position);

        // 重なっているとみなすしきい値
        // 同期中は少しのズレでも見えるように小さめに、分離中はテレポート直後の重なりを消す程度に判定
        spriteRenderer.enabled = distance > 0.05f;
    }

    // 解放？※壁抜け対策を行うこと
    public void Separation(Vector2 moveDirection)
    {
        // 分離
        isSeparated = true;
        // 重力・当たり判定の適用
        alterPlayerMover.SetState(isSeparated);

        // 分離中は不透明（255）にする
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(normalColor.r, normalColor.g, normalColor.b, 1.0f);
        }

        alterPlayerMover.InertialMovement(moveDirection, 5f, 5f, this.GetCancellationTokenOnDestroy()).Forget();


        // UniTaskで待機 → 自動的に戻す
        ResetSeparationAsync(separationDuration).Forget();
    }

    // 解放状態を戻す
    private async UniTaskVoid ResetSeparationAsync(float delay)
    {
        if (TimeManager.Instance != null)
            await TimeManager.Instance.WaitSecondsAsync(delay, this.GetCancellationTokenOnDestroy());
        else
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: this.GetCancellationTokenOnDestroy());

        // 分離状態を戻す
        isSeparated = false;
        // 重力・当たり判定の適用を外す
        alterPlayerMover.SetState(isSeparated);

        // 透明度を通常状態に戻す
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }
}
