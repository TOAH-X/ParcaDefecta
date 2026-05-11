using System.Collections.Generic;
using UnityEngine;

public class PlayerMoverHistory : MonoBehaviour
{
    // 履歴用のフレーム数（60fpsなら60フレーム）
    [SerializeField] private int frameDelay = 60;
    [SerializeField] private SpriteRenderer spriteRenderer;
    // 過去フレームの履歴
    private Queue<PlayerFrameData> FrameHistory;

    // Start is called before the first frame update
    void Start()
    {
        FrameHistory = new Queue<PlayerFrameData>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void LateUpdate()
    {
        // 今フレームのデータを記録
        PlayerFrameData frameData = new PlayerFrameData
        {
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale,
            sprite = spriteRenderer.sprite
        };

        FrameHistory.Enqueue(frameData);

        // 余分な履歴は削除（常にframeDelay+αくらいに保つ）
        if (FrameHistory.Count > frameDelay + 1)
        {
            FrameHistory.Dequeue();
        }
    }

    /// <summary>
    /// nフレーム前のデータの取得
    /// </summary>
    public bool TryGetPastFrameData(int framesAgo, out PlayerFrameData frameData)
    {
        frameData = default;

        if (framesAgo < 0 || framesAgo >= FrameHistory.Count)
            return false;

        // Queueを配列にしてインデックスで取り出す
        // (0 が一番古いデータ)
        PlayerFrameData[] arr = FrameHistory.ToArray();
        int index = arr.Length - 1 - framesAgo; // 最新からframesAgo分戻る
        frameData = arr[index];
        return true;
    }

    /// <summary>
    /// 履歴データをすべてクリア
    /// </summary>
    public void ClearData()
    {
        FrameHistory.Clear();
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