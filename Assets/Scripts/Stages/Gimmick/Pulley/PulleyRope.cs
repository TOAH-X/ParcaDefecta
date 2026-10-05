using UnityEngine;

/// <summary>
/// 紐の描画。PulleySystem が求めた経路の折れ線を LineRenderer に渡す（仕様 F8）。
/// 編集中の確認は PulleySystem の Gizmo が担当する（仕様 F9）。
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PulleyRope : MonoBehaviour
{
    [Header("描画する系(未設定なら親から探す)")]
    [SerializeField] private PulleySystem system;

    private LineRenderer lineRenderer;
    private readonly PulleyRouteGeometry.Shape shapeBuffer = new();
    private Vector3[] positionBuffer = new Vector3[0];

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;

        if (system == null)
        {
            system = GetComponentInParent<PulleySystem>();
        }
        if (system == null)
        {
            Debug.LogWarning($"[PulleyRope] {name}: 描画する PulleySystem が見つかりません", this);
        }
    }

    private void LateUpdate()
    {
        Refresh();
    }

    /// <summary>
    /// 今の部品の位置に合わせて線を引き直す。
    /// </summary>
    public void Refresh()
    {
        if (system == null) return;

        if (!system.TryBuildShape(shapeBuffer))
        {
            lineRenderer.positionCount = 0;
            return;
        }

        var points = shapeBuffer.Points;
        // SetPositions は配列の長さをそのまま使うため、点の数と一致させる
        if (positionBuffer.Length != points.Count)
        {
            positionBuffer = new Vector3[points.Count];
        }
        for (int i = 0; i < points.Count; i++)
        {
            positionBuffer[i] = points[i];
        }

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(positionBuffer);
    }
}
