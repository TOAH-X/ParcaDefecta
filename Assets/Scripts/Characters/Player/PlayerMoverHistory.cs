using System.Collections.Generic;
using UnityEngine;
using ParcaDefecta.System;

/// <summary>
/// 本体の位置・向き・大きさ・絵を、1/60 秒ごとに記録する部品。分身・残像・紐が読む。
///
/// - 記録は描画のフレームとは関係なく 1/60 秒ごとに 1 つ（1 フレームで 0 個や複数個のこともある）。
///   そのため「60 個 = 1 秒」がどの環境でも成り立ち、紐の点の数（当たり判定の形）も環境で変わらない。
/// - 取り出しは「何秒前」で行い、前後 2 つの記録の間を補間する（描画のタイミングがばらついてもなめらか）。
/// - ワープや新生では区切り（セグメント）を作り、区切りをまたいでは補間しない（遅れてパッと飛ぶ）。
/// - 読む部品が記録より先に動いても同じ結果になるよう、読まれたときにそのフレームの記録がまだなら先に済ませる。
/// </summary>
public class PlayerMoverHistory : MonoBehaviour
{
    /// <summary>1 秒あたりの記録の数</summary>
    public const int SamplesPerSecond = 60;
    private const float SampleInterval = 1f / SamplesPerSecond;

    // 何秒前まで記録を残すか。取り出す秒数（分身の遅れなど）より長くしておくこと
    [SerializeField, Min(0.1f)] private float keepSeconds = 1.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // セグメント化された履歴データ。
    // 最初の空セグメントはフィールド初期化で用意する(Start だと、生成直後の LateUpdate が
    // Start より先に走るケースや、Play 中の再コンパイル後に初期化が消えるケースで空のままになる)。
    private List<List<PlayerFrameData>> segments = new List<List<PlayerFrameData>> { new List<PlayerFrameData>() };

    // 最新の記録から今までに経った時間（次の記録までの端数）
    private float sinceLastSample;
    // 前のフレームの状態（記録の瞬間の位置を、前のフレームと今のフレームの間で補間して出すため）
    private bool hasPrevious;
    private Vector3 prevPosition;
    private Quaternion prevRotation;
    private Vector3 prevScale;
    // 区切りを作った直後。次の記録では前のフレームから補間しない（ワープの前後を線でつながないため）
    private bool breakPending;
    // 最後に記録処理をしたフレーム番号
    private int lastProcessedFrame = -1;

    /// <summary>残す秒数（警告用に分身などが読む）</summary>
    public float KeepSeconds => keepSeconds;

    void LateUpdate()
    {
        ProcessFrame();
    }

    /// <summary>
    /// このフレームの記録を済ませる。1 フレームに 1 回だけ動く（LateUpdate と、先に読まれたときの両方から呼ぶ）。
    /// </summary>
    private void ProcessFrame()
    {
        if (lastProcessedFrame == Time.frameCount) return;

        // ポーズ中は記録しない（時間も進めない）
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        lastProcessedFrame = Time.frameCount;

        // 念のための保険。セグメントが無ければ 1 つ用意する
        if (segments.Count == 0) segments.Add(new List<PlayerFrameData>());

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        Vector3 scale = transform.localScale;
        Sprite sprite = spriteRenderer.sprite;

        // 最初の記録、または区切りの直後は、今の状態をそのまま 1 つ記録して始める
        if (!hasPrevious || breakPending)
        {
            AddSample(position, rotation, scale, sprite);
            sinceLastSample = 0f;
            SetPrevious(position, rotation, scale);
            hasPrevious = true;
            breakPending = false;
            RemoveOldData();
            return;
        }

        float dt = TimeManager.Instance != null ? TimeManager.Instance.DeltaTime : Time.deltaTime;
        sinceLastSample += dt;

        // 1/60 秒たまるごとに 1 つ記録する。記録の瞬間の状態は、前のフレームと今のフレームの間で補間する
        while (sinceLastSample >= SampleInterval)
        {
            sinceLastSample -= SampleInterval;
            float t = dt > 0f ? Mathf.Clamp01((dt - sinceLastSample) / dt) : 1f;
            AddSample(
                Vector3.Lerp(prevPosition, position, t),
                Quaternion.Slerp(prevRotation, rotation, t),
                Vector3.Lerp(prevScale, scale, t),
                sprite);
        }

        SetPrevious(position, rotation, scale);
        RemoveOldData();
    }

    private void SetPrevious(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        prevPosition = position;
        prevRotation = rotation;
        prevScale = scale;
    }

    private void AddSample(Vector3 position, Quaternion rotation, Vector3 scale, Sprite sprite)
    {
        segments[segments.Count - 1].Add(new PlayerFrameData
        {
            position = position,
            rotation = rotation,
            scale = scale,
            sprite = sprite
        });
    }

    /// <summary>
    /// 残す数（keepSeconds 分 + 補間用の予備）を超えた古い記録を消す。
    /// </summary>
    private void RemoveOldData()
    {
        int maxCount = Mathf.CeilToInt(keepSeconds * SamplesPerSecond) + 2;
        int totalCount = GetTotalCount();

        while (totalCount > maxCount && segments.Count > 0)
        {
            if (segments[0].Count > 0)
            {
                segments[0].RemoveAt(0);
                totalCount--;
            }

            // セグメントが空になったら（かつ最新ではない場合）削除
            if (segments[0].Count == 0)
            {
                if (segments.Count > 1) segments.RemoveAt(0);
                else break;
            }
        }
    }

    /// <summary>
    /// 指定秒数前の状態を取り出す。前後 2 つの記録の間を補間する（絵は古い方）。
    /// 記録が足りなければ false。前後 2 つが別の区切りなら、補間せず古い方を返す。
    /// </summary>
    public bool TryGetPastData(float secondsAgo, out PlayerFrameData frameData)
    {
        ProcessFrame();
        frameData = default;

        // 最新の記録から数えて何個前か（端数あり）
        float samplesAgo = Mathf.Max(0f, (secondsAgo - sinceLastSample) * SamplesPerSecond);
        int newerIndex = Mathf.FloorToInt(samplesAgo);
        int olderIndex = newerIndex + 1;
        float t = samplesAgo - newerIndex; // 0 = 新しい方、1 = 古い方

        if (!TryGetFromNewest(newerIndex, out var newer, out int newerSegment)) return false;
        if (!TryGetFromNewest(olderIndex, out var older, out int olderSegment))
        {
            // ちょうど一番古い記録に当たったときだけ、それを返す
            if (t <= 0f) { frameData = newer; return true; }
            return false;
        }

        if (newerSegment != olderSegment)
        {
            frameData = older;
            return true;
        }

        frameData = Interpolate(older, newer, 1f - t);
        return true;
    }

    /// <summary>
    /// 紐の点を取り出す。今の位置から指定秒数前までの点を、区切りごとに buffers へ入れる（古い順）。
    /// 一番古い端は TryGetPastData と同じ補間の点にするので、紐の端は分身の位置と一致する。
    /// 戻り値は使った区切りの数。
    /// </summary>
    public int CollectTrail(float secondsAgo, List<List<Vector3>> buffers)
    {
        ProcessFrame();

        int segmentCount = 0;
        int totalCount = GetTotalCount();
        if (totalCount == 0) return 0;

        float samplesAgo = Mathf.Max(0f, (secondsAgo - sinceLastSample) * SamplesPerSecond);
        // 紐に含める、最も古い記録の位置（新しい方から数えて）
        int newestIncluded = Mathf.Min(Mathf.FloorToInt(samplesAgo), totalCount - 1);

        // 端の点（分身の位置）。取れない（記録が足りない）ときは、残っている一番古い記録まで
        bool hasEnd = TryGetPastData(secondsAgo, out var end);

        int indexFromOldest = 0;
        int firstIncludedFromOldest = totalCount - 1 - newestIncluded;
        bool endPlaced = false;

        for (int s = 0; s < segments.Count; s++)
        {
            var segment = segments[s];
            List<Vector3> points = null;

            for (int i = 0; i < segment.Count; i++, indexFromOldest++)
            {
                if (indexFromOldest < firstIncludedFromOldest) continue;

                if (points == null)
                {
                    points = GetBuffer(buffers, segmentCount);
                    // この区切りの最初の点の直前に分身の位置があれば、それを端にする
                    if (hasEnd && !endPlaced && i > 0)
                    {
                        points.Add(end.position);
                        endPlaced = true;
                    }
                }
                points.Add(segment[i].position);
            }

            if (points == null) continue;

            // 最新の区切りの先は、今の本体の位置まで伸ばす
            if (s == segments.Count - 1) points.Add(transform.position);

            segmentCount++;
        }

        return segmentCount;
    }

    private static List<Vector3> GetBuffer(List<List<Vector3>> buffers, int index)
    {
        while (buffers.Count <= index) buffers.Add(new List<Vector3>());
        var buffer = buffers[index];
        buffer.Clear();
        return buffer;
    }

    private static PlayerFrameData Interpolate(PlayerFrameData from, PlayerFrameData to, float t)
    {
        return new PlayerFrameData
        {
            position = Vector3.Lerp(from.position, to.position, t),
            rotation = Quaternion.Slerp(from.rotation, to.rotation, t),
            scale = Vector3.Lerp(from.scale, to.scale, t),
            sprite = from.sprite
        };
    }

    /// <summary>
    /// 最新の記録から数えて index 個前の記録と、その区切りの番号を取得する。
    /// </summary>
    private bool TryGetFromNewest(int index, out PlayerFrameData data, out int segmentIndex)
    {
        data = default;
        segmentIndex = -1;
        if (index < 0) return false;

        int remaining = index;
        for (int s = segments.Count - 1; s >= 0; s--)
        {
            var segment = segments[s];
            if (remaining < segment.Count)
            {
                data = segment[segment.Count - 1 - remaining];
                segmentIndex = s;
                return true;
            }
            remaining -= segment.Count;
        }
        return false;
    }

    /// <summary>
    /// 新しい軌跡セグメントを開始（ワープ時などに呼び出す）
    /// </summary>
    public void StartNewSegment()
    {
        // 現在の最新セグメントが空でない場合のみ新しいセグメントを追加
        if (segments[segments.Count - 1].Count > 0)
        {
            segments.Add(new List<PlayerFrameData>());
        }
        // 次の記録では、移る前の位置から補間しない
        breakPending = true;
    }

    /// <summary>
    /// 履歴データをすべてクリア
    /// </summary>
    public void ClearData()
    {
        segments.Clear();
        segments.Add(new List<PlayerFrameData>());
        hasPrevious = false;
        breakPending = false;
        sinceLastSample = 0f;
    }

    private int GetTotalCount()
    {
        int count = 0;
        foreach (var seg in segments) count += seg.Count;
        return count;
    }

    // 1 回分の記録
    [System.Serializable]
    public struct PlayerFrameData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Sprite sprite;
    }
}
