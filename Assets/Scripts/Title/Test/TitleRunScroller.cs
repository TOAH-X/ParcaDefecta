using UnityEngine;

/// <summary>
/// 体験版のタイトル演出の係。自分（と子の PlayerRunAnimation）を一定の速さで右へ動かし、
/// 指定したカメラを同じだけ動かす。決めた距離ごとに、自分もカメラも左へ戻す。
/// カメラは写すだけで、カメラを動かすのはこの係だけにする（ほかの部品が動かすと奪い合いになる）。
///
/// 置き方の決まり: この係とカメラは同じ位置（縦横）に置く。子のキャラは「カメラから見た位置」に置けばよい。
/// 背景の奥行き（DepthLayerController）はカメラの位置を見て動くので、そのまま流れて見える。
/// 継ぎ目なく見えるには、ステージの全部の層を、プレハブ上で Loop Distance の周期で繰り返す絵にする
/// （奥行きの計算上、どの層も同じ周期でよい。奥の層ほど画面に入る範囲が広いので、その分まで絵を置く）。
/// </summary>
public class TitleRunScroller : MonoBehaviour
{
    [SerializeField, Tooltip("一緒に動かすカメラ。未設定なら Camera.main")]
    private Camera targetCamera;

    [SerializeField, Tooltip("右へ動く速さ。Player の走る速さ(PlayerMover.moveSpeed)と同じにしておく")]
    private float speed = 7.5f;

    [SerializeField, Tooltip("開始位置からこの距離だけ進むたびに、同じ距離だけ左へ戻す。0 以下なら戻さない")]
    private float loopDistance = 0f;

    private float startX;
    private Vector3 cameraOffset;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            Debug.LogWarning($"[TitleRunScroller] {name}: カメラが指定されていないため Camera.main を使います。", this);
        }

        startX = transform.position.x;

        if (targetCamera != null)
        {
            cameraOffset = targetCamera.transform.position - transform.position;
            if (Mathf.Abs(cameraOffset.x) > 0.01f || Mathf.Abs(cameraOffset.y) > 0.01f)
            {
                Debug.LogWarning($"[TitleRunScroller] {name}: 係とカメラの位置(縦横)がずれています。同じ位置に置くと、子のキャラを「カメラから見た位置」で置けます。", this);
            }
        }
    }

    private void Update()
    {
        Vector3 position = transform.position;
        position.x += speed * Time.deltaTime;

        if (loopDistance > 0f)
        {
            while (position.x - startX >= loopDistance)
            {
                position.x -= loopDistance;
            }
        }

        transform.position = position;

        // カメラは係と同じだけ動かす（奥行きの部品が LateUpdate で読む前にそろえる）
        if (targetCamera != null)
        {
            targetCamera.transform.position = position + cameraOffset;
        }
    }
}
