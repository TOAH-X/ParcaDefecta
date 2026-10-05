using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 滑車の足場。上面のすぐ上を幅全体の箱で調べ、乗っているものの重さを合計する（仕様 C3〜C5）。
/// 自分からは動かず、PulleySystem が「読む」の最初に Detect を呼ぶ（仕様 E3、E6）。
/// 合計は同じオブジェクトの PulleyNode が自分の重さに加える。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PulleyPlatform : MonoBehaviour
{
    [Header("検知する高さ(足場の上面から)")]
    [SerializeField, Min(0.01f)] private float detectHeight = 0.1f;
    [Header("離れてから重さが外れるまでの猶予(秒)")]
    [SerializeField, Min(0f)] private float graceSeconds = 0f;

    // 実行中に変化する状態。Inspector での観察用
    [Header("乗っている重さの合計(観察用)")]
    [SerializeField] private int currentWeight;

    public int CurrentWeight => currentWeight;

    /// <summary>
    /// 足場以外に支えられたものが上にあるとき、その足元まで、あとどれだけ上がれるか。無ければ無限大（仕様 J49）。
    /// </summary>
    public float BlockedRoomUp { get; private set; } = float.PositiveInfinity;

    /// <summary>乗っているもの 1 つぶんの記録</summary>
    private class Rider
    {
        public IWeighted weighted;
        public bool isSeen;
        // 離れてから重さが外れるまでの残り秒数
        public float remaining;
    }

    // 箱の左右を足場の幅よりわずかに狭め、横に接しているだけのものを数えない
    private const float SideInset = 0.01f;
    // 足場以外の支えを探すとき、乗っているものを下へ動かしてみる距離。物理で接しているものどうしのすき間より大きくする
    private const float SupportProbeDistance = 0.05f;
    // 足場以外に支えられたものの足元から、これだけ手前で足場を止める。
    // 触れるまで上げると、物理が相手を押し上げて支えから浮かせ、乗っているものとして数えてしまう
    private const float BlockGap = 0.02f;

    private BoxCollider2D boxCollider;
    private ContactFilter2D contactFilter;
    private readonly List<Collider2D> hitBuffer = new();
    private readonly List<RaycastHit2D> supportBuffer = new();
    private readonly List<Rider> riders = new();

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        contactFilter = new ContactFilter2D { useTriggers = false };
    }

    /// <summary>
    /// 乗っているものを調べ直し、重さの合計を更新する。
    /// position は PulleySystem が決めた計算上の位置。当たり判定への反映は物理更新まで遅れるので、そのぶん箱をずらして調べる（仕様 J47）。
    /// </summary>
    public void Detect(float dt, Vector2 position)
    {
        foreach (Rider rider in riders)
        {
            rider.isSeen = false;
        }
        BlockedRoomUp = float.PositiveInfinity;

        Bounds bounds = boxCollider.bounds;
        Rigidbody2D body = boxCollider.attachedRigidbody;
        Vector2 physicsPosition = body != null ? body.position : (Vector2)transform.position;
        bounds.center += (Vector3)(position - physicsPosition);

        Vector2 center = new Vector2(bounds.center.x, bounds.max.y + detectHeight / 2f);
        Vector2 size = new Vector2(Mathf.Max(0f, bounds.size.x - SideInset * 2f), detectHeight);

        Physics2D.OverlapBox(center, size, 0f, contactFilter, hitBuffer);
        foreach (Collider2D hit in hitBuffer)
        {
            if (hit == boxCollider) continue;

            IWeighted weighted = hit.GetComponentInParent<IWeighted>();
            if (weighted == null) continue;

            // 足場以外に支えられているものは、足場が下がっても付いてこないので重さに数えない（仕様 J48）。
            // 数えると「重さが加わって下がる → 箱から出て上がる」を繰り返して揺れる。代わりに、その足元で足場を止める（仕様 J49）
            if (IsSupportedElsewhere(hit))
            {
                float room = Mathf.Max(0f, hit.bounds.min.y - bounds.max.y - BlockGap);
                BlockedRoomUp = Mathf.Min(BlockedRoomUp, room);
                continue;
            }

            // 当たり判定を複数持つものも 1 つとして数える
            Rider rider = FindRider(weighted);
            if (rider == null)
            {
                rider = new Rider { weighted = weighted };
                riders.Add(rider);
            }
            rider.isSeen = true;
            rider.remaining = graceSeconds;
        }

        int total = 0;
        for (int i = riders.Count - 1; i >= 0; i--)
        {
            Rider rider = riders[i];

            // 破棄されたものは猶予を待たずに外す
            bool isDestroyed = rider.weighted is Object owner && owner == null;
            if (!rider.isSeen) rider.remaining -= dt;

            if (isDestroyed || (!rider.isSeen && rider.remaining <= 0f))
            {
                riders.RemoveAt(i);
                continue;
            }

            total += rider.weighted.Weight;
        }

        // 途中で変わったら、次の判定からすぐ反映される（仕様 C5）
        currentWeight = total;
    }

    /// <summary>
    /// 滑車の足場が無くても、ほかの何か（床や壁の上面、別の物）に支えられているかを返す。
    /// 足場を無視して少し下へ動かしてみて、何かに当たれば支えられている。
    /// </summary>
    private bool IsSupportedElsewhere(Collider2D target)
    {
        // 相手と衝突する設定のものだけが支えになる
        ContactFilter2D filter = contactFilter;
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(target.gameObject.layer));

        // 当たり判定を複数持つものは、Rigidbody 全体で調べる。自分自身の当たり判定は対象に入らない
        Rigidbody2D targetBody = target.attachedRigidbody;
        if (targetBody != null)
        {
            targetBody.Cast(Vector2.down, filter, supportBuffer, SupportProbeDistance);
        }
        else
        {
            target.Cast(Vector2.down, filter, supportBuffer, SupportProbeDistance);
        }

        foreach (RaycastHit2D support in supportBuffer)
        {
            // 滑車の足場は支えに数えない。床や壁と違い、重さがかかれば下がるので、別の足場にも乗っているものは両方の重さに数える（仕様 J51）
            if (support.collider.TryGetComponent(out PulleyPlatform _)) continue;
            // 上を向いた面だけが支えになる。横に接しているだけの壁は数えない
            if (support.normal.y < 0.5f) continue;

            return true;
        }
        return false;
    }

    private Rider FindRider(IWeighted weighted)
    {
        foreach (Rider rider in riders)
        {
            if (rider.weighted == weighted) return rider;
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        // 検知する箱を表示する
        BoxCollider2D box = boxCollider != null ? boxCollider : GetComponent<BoxCollider2D>();
        if (box == null) return;

        Bounds bounds = box.bounds;
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(
            new Vector3(bounds.center.x, bounds.max.y + detectHeight / 2f, 0f),
            new Vector3(Mathf.Max(0f, bounds.size.x - SideInset * 2f), detectHeight, 0f));
    }
}
