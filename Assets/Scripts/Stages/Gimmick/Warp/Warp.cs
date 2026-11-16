using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ワープの入口。触れたものを出口（warpTarget）へ移す。
/// ・移したものの勢いは 0 にする（落下やジャンプの速さを持ち越さない）
/// ・着いた場所に重なっている入口は、そのものが一度出るまで反応しない（双方向や、出口が別の入口の上にある場合の往復を防ぐ）
/// </summary>
public class Warp : MonoBehaviour
{
    [SerializeField] private Transform warpTarget; // 転送先のターゲット（出口）。双方向なら相手の入口

    // 着いた場所の入口を調べるとき、当たり判定を少し小さくして測る。
    // ふちが触れているだけのものまで記録すると、物理の側では入っていない扱いになり、出たときの記録消しが来ないため
    private const float ArrivalCheckShrink = 0.05f;

    // ワープで着いたばかりで、まだこの入口から出ていないもの。出るまで反応しない
    private readonly HashSet<Collider2D> justArrived = new();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 接触したオブジェクトのタグがプレイヤーかどうかを確認
        if (!collision.CompareTag("Player")) return;

        // 着いたばかりなら、一度出るまで反応しない
        justArrived.RemoveWhere(c => c == null);
        if (justArrived.Contains(collision)) return;

        PerformWarp(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 出たら、次に入ったときはまた反応する（分身の当たり判定が消えたときもここに来る）
        justArrived.Remove(collision);
    }

    /// <summary>
    /// ワープ（転送）の実行処理
    /// </summary>
    /// <param name="traveler">ワープさせるものの当たり判定</param>
    private void PerformWarp(Collider2D traveler)
    {
        if (warpTarget == null)
        {
            Debug.LogWarning($"{gameObject.name}: 転送先(warpTarget)が設定されていません。");
            return;
        }

        GameObject target = traveler.gameObject;
        Vector3 destination = warpTarget.position;

        // 当たり判定の中心が、オブジェクトの位置からどれだけずれているか（着いた場所を調べるのに使う）
        Bounds bounds = traveler.bounds;
        Vector2 centerOffset = (Vector2)bounds.center - (Vector2)target.transform.position;

        // 軌跡を分断し、新しいセグメントを開始する
        if (target.TryGetComponent<PlayerMoverHistory>(out var history))
        {
            history.StartNewSegment();
        }

        // 位置を出口へ移し、勢いを 0 にする。
        // 物理の位置も一緒に移さないと、次の物理の計算で元の位置に引き戻される
        Rigidbody2D rb = traveler.attachedRigidbody;
        if (rb != null)
        {
            rb.position = destination;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        target.transform.position = destination;

        MarkArrivalAt((Vector2)destination + centerOffset, bounds.size, traveler);

        // TODO: ここにパーティクルの再生や、フェード演出、SEの再生などを追加できます
        Debug.Log($"{target.name} が {warpTarget.name} にワープしました。");
    }

    /// <summary>
    /// 着いた場所に重なっている入口に、着いたばかりのものを記録する
    /// </summary>
    private static void MarkArrivalAt(Vector2 center, Vector2 size, Collider2D traveler)
    {
        Vector2 checkSize = new Vector2(
            Mathf.Max(0.01f, size.x - ArrivalCheckShrink * 2f),
            Mathf.Max(0.01f, size.y - ArrivalCheckShrink * 2f));

        var filter = new ContactFilter2D();
        filter.useTriggers = true;
        var hits = new List<Collider2D>();
        Physics2D.OverlapBox(center, checkSize, 0f, filter, hits);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Warp>(out var warp))
            {
                warp.justArrived.Add(traveler);
            }
        }
    }
}
