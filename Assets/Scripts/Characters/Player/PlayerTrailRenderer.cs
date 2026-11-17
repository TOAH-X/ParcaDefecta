using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本体から分身までをつなぐ紐（見た目の線と当たり判定）を描く部品。
/// 長さは分身の遅れ（AlterPlayer.SyncDelaySeconds）で決まり、紐の端は分身の位置と一致する。
/// </summary>
public class PlayerTrailRenderer : MonoBehaviour
{
    [SerializeField] private PlayerMoverHistory history;
    [SerializeField, Tooltip("紐の長さ（遅れの秒数）を決める分身")] private AlterPlayer alterPlayer;
    [SerializeField] private float edgeRadius = 0.1f;
    [SerializeField] private GameObject trailSegmentPrefab; // インスペクタからスクリプト付きのプレハブをアタッチ可能にする

    // セグメント表示用のオブジェクトプール
    private List<GameObject> segmentPool = new List<GameObject>();
    private List<TrailSegmentHandler> activeSegments = new List<TrailSegmentHandler>();

    // 区切りごとの紐の点（PlayerMoverHistory.CollectTrail が入れる。使い回して毎フレームの確保を避ける）
    private readonly List<List<Vector3>> pointBuffers = new List<List<Vector3>>();

    private class TrailSegmentHandler
    {
        public GameObject Root;
        public LineRenderer Line;
        public EdgeCollider2D Collider;
        public List<Vector2> ColliderPoints = new List<Vector2>();
        public Vector3[] LinePositions;
    }

    void LateUpdate()
    {
        if (history == null || alterPlayer == null) return;

        int segmentCount = history.CollectTrail(alterPlayer.SyncDelaySeconds, pointBuffers);

        // アクティブなセグメントの数を調整
        while (activeSegments.Count < segmentCount)
        {
            activeSegments.Add(CreateSegment());
        }
        while (activeSegments.Count > segmentCount)
        {
            ReleaseSegment(activeSegments.Count - 1);
        }

        // 各セグメントを更新
        for (int i = 0; i < segmentCount; i++)
        {
            UpdateSegment(activeSegments[i], pointBuffers[i]);
        }
    }

    private TrailSegmentHandler CreateSegment()
    {
        GameObject go;
        if (segmentPool.Count > 0)
        {
            go = segmentPool[segmentPool.Count - 1];
            segmentPool.RemoveAt(segmentPool.Count - 1);
        }
        else if (trailSegmentPrefab != null)
        {
            // プレハブが設定されている場合はそれを使用
            go = Instantiate(trailSegmentPrefab, transform);
        }
        else
        {
            // プレハブがない場合は空のオブジェクトを作成
            go = new GameObject("TrailSegment");
            go.transform.SetParent(transform);
        }

        go.SetActive(true);

        // コンポーネントの取得（なければ追加）
        var line = go.GetComponent<LineRenderer>();
        if (line == null) line = go.AddComponent<LineRenderer>();

        var collider = go.GetComponent<EdgeCollider2D>();
        if (collider == null) collider = go.AddComponent<EdgeCollider2D>();

        var handler = new TrailSegmentHandler
        {
            Root = go,
            Line = line,
            Collider = collider
        };

        // ロジック上必須な設定のみ行う
        // 位置計算がワールド座標ベースなので、ここだけは強制する
        handler.Line.useWorldSpace = true;

        // 物理判定の設定
        handler.Collider.edgeRadius = edgeRadius;
        handler.Collider.isTrigger = true;

        return handler;
    }

    private void ReleaseSegment(int index)
    {
        var handler = activeSegments[index];
        handler.Root.SetActive(false);
        segmentPool.Add(handler.Root);
        activeSegments.RemoveAt(index);
    }

    private void UpdateSegment(TrailSegmentHandler handler, List<Vector3> points)
    {
        int count = points.Count;
        if (count < 2)
        {
            handler.Line.positionCount = 0;
            handler.Collider.enabled = false;
            return;
        }

        handler.Collider.enabled = true;
        if (handler.LinePositions == null || handler.LinePositions.Length != count)
        {
            handler.LinePositions = new Vector3[count];
        }

        handler.ColliderPoints.Clear();
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = points[i];
            handler.LinePositions[i] = pos;
            handler.ColliderPoints.Add(new Vector2(pos.x, pos.y));
        }

        handler.Line.positionCount = count;
        handler.Line.SetPositions(handler.LinePositions);
        handler.Collider.SetPoints(handler.ColliderPoints);
    }
}