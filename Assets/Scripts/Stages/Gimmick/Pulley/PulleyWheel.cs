using UnityEngine;

/// <summary>
/// 滑車。固定なら定滑車、固定でなければ動滑車として振る舞う。
/// 円周ではなく半径を持ち、巻き付き分の紐の長さと回転表示はここから求める。
/// 半径は見た目（visual の絵）の実際の大きさから求める。見た目が無いときだけ手打ちの半径を使う。
/// </summary>
public class PulleyWheel : PulleyNode
{
    [Header("半径の予備(見た目が無いときだけ使う)")]
    [SerializeField, Min(0.01f)] private float radius = 0.5f;

    [Header("紐が通る位置の割合(1 = 絵の外周。溝が内側にある絵なら小さくする)")]
    [SerializeField, Range(0.1f, 1f)] private float ropeRadiusRate = 1f;

    // 滑車本体を回すと、子に置いた部品（結ばれた端など）まで回ってしまうので、絵だけを持つ子を回す
    // 大きさもこの子で持つ（本体を拡大すると子に置いた部品まで大きくなるため）
    [Header("回す見た目(絵だけを持つ子。未設定なら回さず、半径は手打ちの値)")]
    [SerializeField] private Transform visual;

    [Header("使っている半径(観察用)")]
    [SerializeField] private float currentRadius;

    private float initialVisualAngle;

    // 再生中は最初に求めた半径を使い続ける（大きさは再生中に変えない前提）
    private bool hasCachedRadius;
    private float cachedRadius;

    public override float Radius
    {
        get
        {
            if (!Application.isPlaying) return CalculateRadius();

            if (!hasCachedRadius)
            {
                cachedRadius = CalculateRadius();
                hasCachedRadius = true;
                currentRadius = cachedRadius;
            }
            return cachedRadius;
        }
    }

    private void Awake()
    {
        if (visual != null) initialVisualAngle = visual.localEulerAngles.z;
    }

    /// <summary>
    /// 見た目の絵の実際の大きさ（親まで含めたスケール）から半径を求める。
    /// 描画範囲の箱（Renderer.bounds）は回転で大きくなるので使わず、絵の元の幅 × lossyScale で測る。
    /// </summary>
    private float CalculateRadius()
    {
        if (visual != null && visual.TryGetComponent(out SpriteRenderer spriteRenderer) && spriteRenderer.sprite != null)
        {
            float width = spriteRenderer.sprite.bounds.size.x * Mathf.Abs(visual.lossyScale.x);
            if (width > 0f) return width / 2f * ropeRadiusRate;
        }
        return radius;
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
        float degrees = sign * flow / Radius * Mathf.Rad2Deg;
        visual.localRotation = Quaternion.Euler(0f, 0f, initialVisualAngle + degrees);
    }

    private void OnValidate()
    {
        currentRadius = CalculateRadius();
    }

    private void OnDrawGizmos()
    {
        // 経路に登録されていなくても、滑車の大きさが分かるように円を描く（紐が通る位置）
        float r = Radius;
        if (!Application.isPlaying) currentRadius = r;
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
}
