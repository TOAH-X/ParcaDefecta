using UnityEngine;

/// <summary>
/// 滑車の経路に登録できる部品の共通部分。
/// 属性（固定・重さ・動く向き）を自分の項目として持ち、PulleySystem から位置を受け取って見た目を更新する。
/// 自分では計算しない（仕様 E1、F6）。
/// </summary>
public abstract class PulleyNode : MonoBehaviour
{
    /// <summary>動く向き。今回は上下のみ。横は後から追加する（仕様 B15）</summary>
    public enum MoveAxis
    {
        Vertical,
    }

    [Header("固定(オンなら PulleySystem は位置を変えない)")]
    [SerializeField] private bool isFixed;
    [Header("重さ(整数・0以上)")]
    [SerializeField, Min(0)] private int weight;
    [Header("動く向き")]
    [SerializeField] private MoveAxis moveAxis = MoveAxis.Vertical;

    // 同じオブジェクトにあれば使うもの。起動時に PulleySystem から CaptureInitialPosition が呼ばれた時点で探す
    private PulleyPlatform platform;
    private Rigidbody2D body;

    // PulleySystem から受け取った変位の写し。計算上の位置を返すために持つ
    private float displacement;
    private bool isCaptured;

    // 部品の箱の、部品の位置からのずれ。起動時の当たり判定から求める
    private Vector2 boxMinOffset;
    private Vector2 boxMaxOffset;

    public bool IsFixed => isFixed;
    public MoveAxis Axis => moveAxis;

    /// <summary>PulleySystem から受け取った、初期位置からの変位（上が正）。結ばれた端が結ぶ先の動きを読むために使う</summary>
    public float Displacement => displacement;

    /// <summary>
    /// 現在の重さ。自分の重さに、足場に乗っているものの重さを加える。
    /// </summary>
    public virtual int Weight => weight + (platform != null ? platform.CurrentWeight : 0);

    /// <summary>
    /// 紐が取り付く位置。端は自身の位置、滑車は中心。
    /// 動く部品は、実行中は PulleySystem が決めた計算上の位置を返す。
    /// Rigidbody で動かす部品は Transform への反映が物理更新まで遅れるため、Transform を読むと紐の長さが合わなくなる。
    /// </summary>
    public Vector2 AnchorPosition
    {
        get
        {
            if (isCaptured && !isFixed) return InitialPosition + Vector2.up * displacement;
            return transform.position;
        }
    }

    /// <summary>滑車の半径。端は 0</summary>
    public virtual float Radius => 0f;

    /// <summary>初期配置の位置。PulleySystem が起動時に記録する（仕様 B2）</summary>
    public Vector2 InitialPosition { get; private set; }

    public void CaptureInitialPosition()
    {
        InitialPosition = transform.position;
        platform = GetComponent<PulleyPlatform>();
        body = GetComponent<Rigidbody2D>();
        ReadBoxOffsets();
        displacement = 0f;
        isCaptured = true;
    }

    /// <summary>
    /// ストッパーとの距離を測るための、部品の箱（当たり判定を囲む四角形）を返す。
    /// 当たり判定が無い部品は、中心の点として扱う。
    /// </summary>
    public void GetBox(out Vector2 min, out Vector2 max)
    {
        // 編集中は当たり判定の大きさが変わりうるので、その都度読み直す
        if (!isCaptured) ReadBoxOffsets();

        Vector2 position = AnchorPosition;
        min = position + boxMinOffset;
        max = position + boxMaxOffset;
    }

    private void ReadBoxOffsets()
    {
        Collider2D box = GetComponent<Collider2D>();
        if (box == null)
        {
            boxMinOffset = Vector2.zero;
            boxMaxOffset = Vector2.zero;
            return;
        }

        Bounds bounds = box.bounds;
        Vector2 position = transform.position;
        boxMinOffset = (Vector2)bounds.min - position;
        boxMaxOffset = (Vector2)bounds.max - position;
    }

    /// <summary>
    /// 判定の前に、外から入ってくる情報を読み直す。PulleySystem が「読む」の最初に呼ぶ（仕様 E6）。
    /// </summary>
    public void ReadInputs(float dt)
    {
        if (platform != null) platform.Detect(dt);
    }

    /// <summary>
    /// PulleySystem から変位を受け取って位置を更新する。
    /// 変位は初期位置からの距離で、上方向が正。
    /// </summary>
    public virtual void ApplyDisplacement(float displacement)
    {
        if (isFixed) return;

        this.displacement = displacement;

        // 今回は上下のみ。横移動を足すときはここで向きを切り替える
        Vector2 direction = Vector2.up;
        Vector2 target = InitialPosition + direction * displacement;

        if (body != null)
        {
            // 乗っているものを物理で運ぶため、Rigidbody 経由で動かす（仕様 E2）
            body.MovePosition(target);
        }
        else
        {
            transform.position = new Vector3(target.x, target.y, transform.position.z);
        }
    }
}
