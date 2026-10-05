using UnityEngine;

/// <summary>
/// 滑車。固定なら定滑車、固定でなければ動滑車として振る舞う。
/// 円周ではなく半径を持ち、巻き付き分の紐の長さと回転表示はここから求める。
/// </summary>
public class PulleyWheel : PulleyNode
{
    [Header("半径")]
    [SerializeField, Min(0.01f)] private float radius = 0.5f;

    // 滑車本体を回すと、子に置いた部品（結ばれた端など）まで回ってしまうので、絵だけを持つ子を回す
    [Header("回す見た目(絵だけを持つ子。未設定なら回さない)")]
    [SerializeField] private Transform visual;

    private float initialVisualAngle;

    public override float Radius => radius;

    private void Awake()
    {
        if (visual != null) initialVisualAngle = visual.localEulerAngles.z;
    }

    /// <summary>
    /// この滑車の上を通り過ぎた紐の長さを受け取り、見た目を回す。PulleySystem が動かしたあとに呼ぶ。
    /// flow は起動時からの合計で、経路リストの順に紐が進む向きが正。
    /// 滑車が動いたかどうかではなく紐が通った長さで回すので、動いていても紐が通らなければ回らない。
    /// </summary>
    public void ApplyRopeFlow(float flow, PulleyWrapDirection wrap)
    {
        if (visual == null) return;

        // 反時計回りに巻き付く滑車は、紐が進むと反時計回り（角度が正）に回る
        float sign = wrap == PulleyWrapDirection.CounterClockwise ? 1f : -1f;
        float degrees = sign * flow / radius * Mathf.Rad2Deg;
        visual.localRotation = Quaternion.Euler(0f, 0f, initialVisualAngle + degrees);
    }

    private void OnDrawGizmos()
    {
        // 経路に登録されていなくても、滑車の大きさが分かるように円を描く
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
