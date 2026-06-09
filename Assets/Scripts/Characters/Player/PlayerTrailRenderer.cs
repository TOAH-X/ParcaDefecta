using System.Collections.Generic;
using UnityEngine;

public class PlayerTrailRenderer : MonoBehaviour
{
    [SerializeField] private PlayerMoverHistory history;
    [SerializeField] private float edgeRadius = 0.1f;
    [SerializeField] private GameObject trailSegmentPrefab; // インスペクタからスクリプト付きのプレハブをアタッチ可能にする

    // セグメント表示用のオブジェクトプール
    private List<GameObject> segmentPool = new List<GameObject>();
    private List<TrailSegmentHandler> activeSegments = new List<TrailSegmentHandler>();

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
        if (history == null) return;

        var historySegments = history.Segments;

        // アクティブなセグメントの数を調整
        while (activeSegments.Count < historySegments.Count)
        {
            activeSegments.Add(CreateSegment());
        }
        while (activeSegments.Count > historySegments.Count)
        {
            ReleaseSegment(activeSegments.Count - 1);
        }

        // 各セグメントを更新
        for (int i = 0; i < historySegments.Count; i++)
        {
            UpdateSegment(activeSegments[i], historySegments[i]);
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

    private void UpdateSegment(TrailSegmentHandler handler, IReadOnlyList<PlayerMoverHistory.PlayerFrameData> data)
    {
        int count = data.Count;
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
            Vector3 pos = data[i].position;
            handler.LinePositions[i] = pos;
            handler.ColliderPoints.Add(new Vector2(pos.x, pos.y));
        }

        handler.Line.positionCount = count;
        handler.Line.SetPositions(handler.LinePositions);
        handler.Collider.SetPoints(handler.ColliderPoints);
    }
}