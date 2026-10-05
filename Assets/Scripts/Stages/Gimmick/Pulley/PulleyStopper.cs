using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ストッパー。当たり判定を持つオブジェクトに付ける目印で、滑車の動く部品はこの当たり判定の箱で止まる（仕様 B9〜B11、F7）。
/// 地面などシーンのどこに置いてもよく、左右の位置が重なる部品すべてに効く。
/// 自分では何もせず、PulleySystem が動ける余地を求めるときに読みに来る（仕様 E3）。
/// </summary>
[ExecuteAlways] // 編集中の検査でも有効なストッパーの一覧を使うため
public class PulleyStopper : MonoBehaviour
{
    // 有効なストッパーの一覧。無効にしたり破棄したりすると外れる
    private static readonly List<PulleyStopper> activeStoppers = new();
    public static IReadOnlyList<PulleyStopper> ActiveStoppers => activeStoppers;

    private Collider2D stopperCollider;

    /// <summary>部品を止める箱（当たり判定を囲む四角形）</summary>
    public Bounds Bounds => stopperCollider.bounds;

    private void OnEnable()
    {
        stopperCollider = GetComponent<Collider2D>();
        if (stopperCollider == null)
        {
            Debug.LogWarning($"[PulleyStopper] {name}: 当たり判定が無いため、ストッパーとして働きません", this);
            return;
        }
        activeStoppers.Add(this);
    }

    private void OnDisable()
    {
        activeStoppers.Remove(this);
    }

    private void OnDrawGizmos()
    {
        if (stopperCollider == null) return;

        // 部品を止める箱を枠で示す
        Bounds bounds = stopperCollider.bounds;
        Gizmos.color = new Color(1f, 0.4f, 0.3f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
