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

    private BoxCollider2D boxCollider;
    private ContactFilter2D contactFilter;
    private readonly List<Collider2D> hitBuffer = new();
    private readonly List<Rider> riders = new();

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        contactFilter = new ContactFilter2D { useTriggers = false };
    }

    /// <summary>
    /// 乗っているものを調べ直し、重さの合計を更新する。
    /// </summary>
    public void Detect(float dt)
    {
        foreach (Rider rider in riders)
        {
            rider.isSeen = false;
        }

        Bounds bounds = boxCollider.bounds;
        Vector2 center = new Vector2(bounds.center.x, bounds.max.y + detectHeight / 2f);
        Vector2 size = new Vector2(Mathf.Max(0f, bounds.size.x - SideInset * 2f), detectHeight);

        Physics2D.OverlapBox(center, size, 0f, contactFilter, hitBuffer);
        foreach (Collider2D hit in hitBuffer)
        {
            if (hit == boxCollider) continue;

            IWeighted weighted = hit.GetComponentInParent<IWeighted>();
            if (weighted == null) continue;

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
