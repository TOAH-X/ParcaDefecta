using System.Collections.Generic;
using UnityEngine;
using ParcaDefecta.System;

public class PlayerMoverHistory : MonoBehaviour
{
    // 履歴用のフレーム数（60fpsなら60フレーム）
    [SerializeField] private int frameDelay = 60;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // セグメント化された履歴データ
    private List<List<PlayerFrameData>> segments = new List<List<PlayerFrameData>>();
    public IReadOnlyList<IReadOnlyList<PlayerFrameData>> Segments => segments;

    // Start is called before the first frame update
    void Start()
    {
        segments.Add(new List<PlayerFrameData>());
    }

    // Update is called once per frame
    void Update()
    {

    }

    void LateUpdate()
    {
        // ポーズ中は履歴を記録しない
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value) return;

        // 今フレームのデータを記録
        PlayerFrameData frameData = new PlayerFrameData
        {
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale,
            sprite = spriteRenderer.sprite
        };

        // 最新のセグメントに追加
        segments[segments.Count - 1].Add(frameData);

        // 全セグメントの合計点数を計算し、制限を超えていたら古いものから削除
        int totalCount = GetTotalPointCount();
        while (totalCount > frameDelay + 1)
        {
            if (segments[0].Count > 0)
            {
                segments[0].RemoveAt(0);
                totalCount--;
            }

            // セグメントが空になったら（かつ最新ではない場合）削除
            if (segments[0].Count == 0 && segments.Count > 1)
            {
                segments.RemoveAt(0);
            }
            else if (segments[0].Count == 0 && segments.Count == 1)
            {
                break;
            }
        }
    }

    /// <summary>
    /// nフレーム前のデータの取得
    /// </summary>
    public bool TryGetPastFrameData(int framesAgo, out PlayerFrameData frameData)
    {
        frameData = default;

        int totalCount = GetTotalPointCount();
        if (framesAgo < 0 || framesAgo >= totalCount)
            return false;

        // 後ろから（最新から）カウントして該当データを探す
        int targetIndexFromLast = totalCount - 1 - framesAgo;
        int currentCounter = 0;

        foreach (var segment in segments)
        {
            if (targetIndexFromLast < currentCounter + segment.Count)
            {
                frameData = segment[targetIndexFromLast - currentCounter];
                return true;
            }
            currentCounter += segment.Count;
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
    }

    /// <summary>
    /// 履歴データをすべてクリア
    /// </summary>
    public void ClearData()
    {
        segments.Clear();
        segments.Add(new List<PlayerFrameData>());
    }

    private int GetTotalPointCount()
    {
        int count = 0;
        foreach (var seg in segments) count += seg.Count;
        return count;
    }

    // 過去フレームのデータ構造
    [System.Serializable]
    public struct PlayerFrameData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Sprite sprite;
    }
}