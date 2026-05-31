using UnityEngine;

public class Warp : MonoBehaviour
{
    [SerializeField] private Transform warpTarget; // 転送先のターゲット（出口）

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 接触したオブジェクトのタグがプレイヤーかどうかを確認
        if (collision.CompareTag("Player"))
        {
            PerformWarp(collision.gameObject);
        }
    }

    /// <summary>
    /// ワープ（転送）の実行処理
    /// </summary>
    /// <param name="player">ワープさせるプレイヤーのGameObject</param>
    private void PerformWarp(GameObject player)
    {
        if (warpTarget == null)
        {
            Debug.LogWarning($"{gameObject.name}: 転送先(warpTarget)が設定されていません。");
            return;
        }

        // 軌跡を分断し、新しいセグメントを開始する
        if (player.TryGetComponent<PlayerMoverHistory>(out var history))
        {
            history.StartNewSegment();
        }

        // プレイヤーの位置を目的地の位置に変更
        player.transform.position = warpTarget.position;

        // TODO: ここにパーティクルの再生や、フェード演出、SEの再生などを追加できます
        Debug.Log($"{player.name} が {warpTarget.name} にワープしました。");
    }
}
