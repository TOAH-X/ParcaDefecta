using UnityEngine;
using Unity.Cinemachine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 奥行き(Depth)を持つ背景グループの見え方を制御します。
/// エディタ上の Transform は「地形と同じ平面に置いたときの設置値」で、エディタでは何も書き換えません。
/// 実行中だけ、Play 開始時の設置値とカメラ位置から見かけの位置と縮小を計算して自身の Transform に書きます。
/// 更新は Cinemachine がカメラを動かした直後に行い、カメラや Cinemachine には書き込みません。
/// </summary>
[DisallowMultipleComponent]
public class DepthLayerController : MonoBehaviour
{
    private const float MinDepth = 0.01f;

    [SerializeField, Min(MinDepth), Tooltip("奥行き。1 = 地形の平面。大きいほど奥(遠景 5、中景 2.5 など)、1 未満で手前。横の視差と縮小はこの値で決まる")]
    private float depth = 1f;

    [SerializeField, Range(0f, 1f), Tooltip("縦方向に画面上で流れる速さの倍率。1 = 横と同じ(奥行き通り)、0 = カメラに固定して縦には流れない。縮小には影響しない")]
    private float verticalParallax = 1f;

    // Play 開始時の設置値
    private Vector3 _placedPosition;
    private Vector3 _placedScale;

    public float Depth => depth;

    private void Awake()
    {
        _placedPosition = transform.position;
        _placedScale = transform.localScale;
    }

    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    /// <summary>Cinemachine がカメラを動かした直後に呼ばれます。</summary>
    private void OnCameraUpdated(CinemachineBrain brain)
    {
        var camera = brain != null ? brain.OutputCamera : null;
        if (camera == null) return;
        Apply(camera.transform.position);
    }

    private void LateUpdate()
    {
        // Brain が無いシーンでの保険。Brain があれば CameraUpdatedEvent 側で更新する
        if (CinemachineBrain.ActiveBrainCount > 0) return;
        var camera = Camera.main;
        if (camera != null) Apply(camera.transform.position);
    }

    /// <summary>
    /// 横: 見かけ = カメラ + (設置位置 - カメラ) / depth。画面上は地形の 1/depth の速さで流れる。
    /// 縦: 画面上の速さを (1/depth) × verticalParallax にする。0 ならカメラに固定され縦には流れない。
    /// Scale = 設置 Scale / depth(縦横とも)。
    /// </summary>
    private void Apply(Vector3 cameraPosition)
    {
        float d = Mathf.Max(depth, MinDepth);
        float rateX = 1f / d;                   // 画面上の横の速さ(地形比)
        float rateY = rateX * verticalParallax; // 画面上の縦の速さ(地形比)
        Vector3 apparent = _placedPosition;
        apparent.x += (cameraPosition.x - _placedPosition.x) * (1f - rateX);
        apparent.y += (cameraPosition.y - _placedPosition.y) * (1f - rateY);

        transform.position = apparent;
        transform.localScale = new Vector3(_placedScale.x / d, _placedScale.y / d, _placedScale.z);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        depth = Mathf.Max(depth, MinDepth);
    }

    /// <summary>
    /// エディタ上の目安として、Main Camera(無ければ Scene ビューのカメラ)から見たときの
    /// 見かけの範囲を枠線で描きます。Transform は変更しません。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;

        Camera reference = Camera.main;
        if (reference == null && SceneView.lastActiveSceneView != null) reference = SceneView.lastActiveSceneView.camera;
        if (reference == null) return;

        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        bool hasBounds = false;
        Bounds placed = default;
        foreach (var r in renderers)
        {
            if (!hasBounds) { placed = r.bounds; hasBounds = true; }
            else placed.Encapsulate(r.bounds);
        }

        float d = Mathf.Max(depth, MinDepth);
        float rateX = 1f / d;
        float rateY = rateX * verticalParallax;
        Vector3 cam = reference.transform.position;
        Vector3 center = placed.center;
        center.x += (cam.x - placed.center.x) * (1f - rateX);
        center.y += (cam.y - placed.center.y) * (1f - rateY);
        Vector3 size = new Vector3(placed.size.x / d, placed.size.y / d, placed.size.z);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(center, size);
        Gizmos.DrawLine(placed.center, center);
    }
#endif
}
