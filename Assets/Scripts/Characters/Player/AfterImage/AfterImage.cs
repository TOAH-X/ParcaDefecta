using UnityEngine;
using System.Collections.Generic;

public class AfterImage : MonoBehaviour
{
    [SerializeField] private PlayerMoverHistory playerMoverHistory;
    [SerializeField] private SpriteRenderer afterImagePrefab;
    [SerializeField] private Transform afterImageParent;
    [SerializeField] private int imageCount = 3;
    [SerializeField] private float syncDelaySeconds = 1.0f;
    [SerializeField] private float startAlpha = 0.5f;
    [SerializeField] private float endAlpha = 0.1f;

    private List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    void Start()
    {
        if (afterImagePrefab == null) return;

        // 指定された数だけ残像用のスプライトを生成
        for (int i = 0; i < imageCount; i++)
        {
            SpriteRenderer sr = Instantiate(afterImagePrefab, afterImageParent);
            renderers.Add(sr);
        }
    }

    void LateUpdate()
    {
        if (playerMoverHistory == null) return;

        // プレイヤー(現在)から分身(syncDelaySeconds前)までの間を等間隔で埋める
        float totalFrames = 60f * syncDelaySeconds;
        float step = totalFrames / (imageCount + 1);

        // 1つ前の描画位置（最初はプレイヤー自身の現在地）
        Vector3 lastPosition = transform.position;

        for (int i = 0; i < renderers.Count; i++)
        {
            int framesAgo = (int)(step * (i + 1));

            if (playerMoverHistory.TryGetPastFrameData(framesAgo, out var data))
            {
                // 前の描画位置（プレイヤー本体または1つ前の残像）との距離が近い場合は非表示
                if (Vector3.Distance(data.position, lastPosition) < 0.1f)
                {
                    renderers[i].gameObject.SetActive(false);
                    continue;
                }

                renderers[i].gameObject.SetActive(true);
                renderers[i].transform.SetPositionAndRotation(data.position, data.rotation);
                renderers[i].transform.localScale = data.scale;
                renderers[i].sprite = data.sprite;

                // プレイヤーに近いほど濃く、分身に近いほど薄くなるように透明度を調整
                Color color = renderers[i].color;
                color.a = Mathf.Lerp(startAlpha, endAlpha, (float)(i + 1) / (imageCount + 1));
                renderers[i].color = color;

                // 表示した残像の座標を「次の残像」の比較用として保存
                lastPosition = data.position;
            }
            else
            {
                renderers[i].gameObject.SetActive(false);
            }
        }
    }
}
