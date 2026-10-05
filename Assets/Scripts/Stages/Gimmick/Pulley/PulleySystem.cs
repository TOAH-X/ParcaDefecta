using System.Collections.Generic;
using UnityEngine;
using R3;
using ParcaDefecta.System;

/// <summary>
/// 紐 1 本ぶんの滑車の系。経路リストと速さを持ち、計算を一手に引き受ける（仕様 E1〜E4、F1〜F4）。
/// 毎 Tick に「全部品を読む → 判定 → 反映」を行い、部品の位置の正解（初期位置からの変位）を持つ。
/// 端を別の紐の部品に結ぶ場合は、結ぶ先の紐の系を「親の系」として登録する。
/// 親を持たない系だけが時間を受け取り、各系は自分が動いた直後に子の系を更新する（仕様 F3、F4）。
/// 名前は Manager ではなく System。このプロジェクトでは XxxManager が Singleton を意味するため。
/// </summary>
public class PulleySystem : MonoBehaviour
{
    /// <summary>経路リストの 1 行。部品と、その部品に巻き付く向き</summary>
    [System.Serializable]
    public struct RouteEntry
    {
        public PulleyNode node;
        public PulleyWrapDirection wrap;
    }

    [Header("紐の経路(端 → 滑車 → … → 端 の順に登録)")]
    [SerializeField] private List<RouteEntry> route = new();

    [Header("紐が送られる速さ(単位/秒)")]
    [SerializeField, Min(0f)] private float ropeSpeed = 1f;

    [Header("余白(上がる部品を、滑車の手前どれだけで止めるか)")]
    [SerializeField, Min(0f)] private float margin = 1f;

    [Header("親の系(端を別の紐の部品に結ぶ場合だけ、その紐の系を登録)")]
    [SerializeField] private PulleySystem parentSystem;

    [Header("Scene ビュー表示")]
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.3f);

    // 実行中に変化する状態。Inspector での観察用（仕様 E8）
    [Header("判定の結果(観察用)")]
    [SerializeField] private PulleySolver.Outcome outcome = PulleySolver.Outcome.Blocked;
    [SerializeField] private List<PulleyNode> movingDownNodes = new();
    [SerializeField] private List<PulleyNode> movingUpNodes = new();
    [SerializeField] private float currentRopeLength;

    /// <summary>初期配置から求めた紐の全長（仕様 B2）</summary>
    public float RopeLength { get; private set; }

    public IReadOnlyList<RouteEntry> Route => route;
    public float RopeSpeed => ropeSpeed;
    public PulleySolver.Outcome Outcome => outcome;
    public IReadOnlyList<PulleyNode> MovingDownNodes => movingDownNodes;
    public IReadOnlyList<PulleyNode> MovingUpNodes => movingUpNodes;
    public float CurrentRopeLength => currentRopeLength;

    // 経路の各部品の、初期位置からの変位（上が正）。位置の正解（仕様 E2）
    private float[] displacements;
    // 経路の各部品の紐の係数。部品が 1 上がったときに余る紐の長さ（端は 1、動滑車は 2）
    private int[] coefficients;
    // 初期配置での、各区間（隣り合う部品の間の直線部分）の長さ。滑車の上を通り過ぎた紐の長さを求めるのに使う
    private float[] initialSegmentLengths;
    private bool isReady;

    // この系を親として登録している系。自分が動いた直後に、登録された順で更新する
    private readonly List<PulleySystem> childSystems = new();
    // 親の系の部品に結ばれた端の、経路での番号。無ければ -1
    private int attachedIndex = -1;
    private PulleyNode attachedTo;

    // 紐の過不足とみなす下限
    private const float SlackEpsilon = 1e-5f;

    // 紐の向きを垂直とみなす許容誤差
    private const float VerticalTolerance = 0.01f;
    // ストッパーとの重なりを判定するときの許容誤差
    private const float StopperTolerance = 0.001f;

    // 計算結果の使い回し用
    private readonly List<PulleyRouteGeometry.Circle> circleBuffer = new();
    private readonly PulleyRouteGeometry.Shape shapeBuffer = new();
    private readonly List<string> problemBuffer = new();
    private readonly HashSet<PulleyNode> seenBuffer = new();
    private readonly List<PulleySolver.Part> partBuffer = new();
    private readonly List<int> partRouteIndexBuffer = new();
    private readonly List<float> moveBuffer = new();

    private void Awake()
    {
        // 親の系には、検査の結果にかかわらず登録する。Awake の順番に依存しないよう、相手の一覧へ足すだけにする
        if (parentSystem != null && parentSystem != this)
        {
            parentSystem.childSystems.Add(this);
        }

        // 起動時の検査（仕様 F4）
        Validate(problemBuffer);

        // 初期配置を記録し、紐の全長を確定する
        foreach (RouteEntry entry in route)
        {
            if (entry.node != null) entry.node.CaptureInitialPosition();
        }

        bool built = TryBuildShape(shapeBuffer);
        if (built)
        {
            RopeLength = shapeBuffer.TotalLength;
            currentRopeLength = RopeLength;
            ValidateGeometry(shapeBuffer, problemBuffer);
            Debug.Log($"[PulleySystem] {name}: 紐の全長 = {RopeLength:F3}", this);
        }
        else
        {
            problemBuffer.Add($"経路を計算できません。{shapeBuffer.Error}");
        }

        foreach (string problem in problemBuffer)
        {
            Debug.LogWarning($"[PulleySystem] {name}: {problem}", this);
        }

        // 問題が 1 つでもあれば動かさない。誤った配置のまま動いて原因が分かりにくくなるのを防ぐ
        if (problemBuffer.Count > 0)
        {
            Debug.LogWarning($"[PulleySystem] {name}: 問題があるため、この系は動かしません", this);
            return;
        }

        displacements = new float[route.Count];
        coefficients = new int[route.Count];
        for (int i = 0; i < route.Count; i++)
        {
            coefficients[i] = GetCoefficient(shapeBuffer, i, out _);
        }
        initialSegmentLengths = shapeBuffer.SegmentLengths.ToArray();
        attachedIndex = FindAttachedEnd(out attachedTo);
        isReady = true;
    }

    private void Start()
    {
        // ストッパーは有効になった順に一覧へ入るので、全部がそろう Start で検査する
        if (isReady)
        {
            problemBuffer.Clear();
            ValidateStoppers(problemBuffer);
            foreach (string problem in problemBuffer)
            {
                Debug.LogWarning($"[PulleySystem] {name}: {problem}", this);
            }
            if (problemBuffer.Count > 0)
            {
                Debug.LogWarning($"[PulleySystem] {name}: 問題があるため、この系は動かしません", this);
                isReady = false;
            }
        }

        // 親を持つ系は時間を受け取らず、親の系から更新される（仕様 F4）
        if (parentSystem != null) return;
        if (TimeManager.Instance == null) return;

        // ポーズ中は dt が 0 で流れてくるので、判定は行われても移動は進まない（仕様 E9）
        TimeManager.Instance.OnTick
            .Subscribe(dt => Tick(dt))
            .AddTo(this);
    }

    /// <summary>
    /// 1 Tick の処理。「全部品を読む → 判定 → 反映」の順を固定し、途中で読み直さない（仕様 E6）。
    /// </summary>
    private void Tick(float dt)
    {
        if (isReady) UpdateSelf(dt);

        // 自分の計算を終えてから、子の系を更新する（仕様 F4）
        foreach (PulleySystem child in childSystems)
        {
            if (child != null && child.isActiveAndEnabled) child.Tick(dt);
        }
    }

    private void UpdateSelf(float dt)
    {
        // 読む
        foreach (RouteEntry entry in route)
        {
            entry.node.ReadInputs(dt);
        }
        if (attachedIndex >= 0) FollowAttachedEnd();
        if (!TryBuildShape(shapeBuffer)) return;
        currentRopeLength = shapeBuffer.TotalLength;
        ReadParts(shapeBuffer);

        // 判定
        PulleySolver.Result result = PulleySolver.Solve(partBuffer, moveBuffer);
        outcome = result.outcome;
        movingDownNodes.Clear();
        movingUpNodes.Clear();
        for (int i = 0; i < moveBuffer.Count; i++)
        {
            PulleyNode node = route[partRouteIndexBuffer[i]].node;
            if (moveBuffer[i] < 0f) movingDownNodes.Add(node);
            else if (moveBuffer[i] > 0f) movingUpNodes.Add(node);
        }

        // 反映
        if (result.outcome == PulleySolver.Outcome.Moving) Move(result, dt);
        RotateWheels();
    }

    /// <summary>
    /// 各滑車に、その上を通り過ぎた紐の長さを渡して見た目を回す。
    /// 滑車より手前の区間が初期配置からどれだけ短くなったかの合計が、通り過ぎた紐の長さになる。
    /// いまの形だけから求めるので、前回の状態は持たない。
    /// </summary>
    private void RotateWheels()
    {
        if (!TryBuildShape(shapeBuffer)) return;

        float flow = 0f;
        for (int i = 1; i < route.Count; i++)
        {
            flow += initialSegmentLengths[i - 1] - shapeBuffer.SegmentLengths[i - 1];
            if (route[i].node is PulleyWheel wheel)
            {
                wheel.ApplyRopeFlow(flow, route[i].wrap);
            }
        }
    }

    /// <summary>
    /// 結ばれた端を結ぶ先の部品と同じだけ動かし、そのぶん生じた紐の過不足を、この紐の動く部品に割り振る。
    /// 「係数 × 変位」の合計が 0 なら紐の全長が保たれているので、合計のずれがそのまま過不足になる。
    /// </summary>
    private void FollowAttachedEnd()
    {
        displacements[attachedIndex] = attachedTo.Displacement;
        route[attachedIndex].node.ApplyDisplacement(displacements[attachedIndex]);

        float slack = 0f;
        for (int i = 0; i < route.Count; i++)
        {
            if (!route[i].node.IsFixed) slack += coefficients[i] * displacements[i];
        }
        if (Mathf.Abs(slack) <= SlackEpsilon) return;

        if (!TryBuildShape(shapeBuffer)) return;
        ReadParts(shapeBuffer);
        PulleySolver.Absorb(partBuffer, slack, moveBuffer);
        for (int i = 0; i < moveBuffer.Count; i++)
        {
            if (moveBuffer[i] == 0f) continue;

            int index = partRouteIndexBuffer[i];
            displacements[index] += moveBuffer[i];
            route[index].node.ApplyDisplacement(displacements[index]);
        }
    }

    /// <summary>
    /// 経路の中から、別の紐の部品に結ばれた端を探す。無ければ -1。
    /// </summary>
    private int FindAttachedEnd(out PulleyNode target)
    {
        for (int i = 0; i < route.Count; i++)
        {
            if (route[i].node is PulleyEnd end && end.AttachedTo != null)
            {
                target = end.AttachedTo;
                return i;
            }
        }
        target = null;
        return -1;
    }

    /// <summary>
    /// 動く部品の重さ・係数・動ける余地を集める。固定の部品と、吊られていない部品（結ばれた端を含む）は対象外。
    /// </summary>
    private void ReadParts(PulleyRouteGeometry.Shape shape)
    {
        partBuffer.Clear();
        partRouteIndexBuffer.Clear();

        for (int i = 0; i < route.Count; i++)
        {
            PulleyNode node = route[i].node;
            if (node.IsFixed || coefficients[i] <= 0) continue;

            GetRoom(shape, i, out float roomUp, out float roomDown);
            int weight = node.Weight;
            int weightDivisor = 1;
            AddAttachedPull(node, ref weight, ref weightDivisor, ref roomUp, ref roomDown);

            partBuffer.Add(new PulleySolver.Part
            {
                weight = weight,
                weightDivisor = weightDivisor,
                coefficient = coefficients[i],
                roomUp = roomUp,
                roomDown = roomDown,
            });
            partRouteIndexBuffer.Add(i);
        }
    }

    /// <summary>
    /// 部品に別の紐の端が結ばれていれば、その紐から伝わる重さを足し、動ける余地を狭める（仕様 B12、B13）。
    /// 結ばれた端の重さは、その紐で次に持ち上がる部品の、紐から見た重さ。
    /// その紐の部品がこれ以上動けなければ、結ぶ先の部品も動けない。
    /// </summary>
    private void AddAttachedPull(PulleyNode node, ref int weight, ref int weightDivisor, ref float roomUp, ref float roomDown)
    {
        foreach (PulleySystem child in childSystems)
        {
            if (child == null || child.attachedTo != node) continue;

            if (!child.isReady || !child.TryProbeAttachedEnd(out PulleySolver.Pull pull, out float endRoomUp, out float endRoomDown))
            {
                // 結ばれた紐を計算できないときは、結ぶ先も動かさない
                roomUp = 0f;
                roomDown = 0f;
                continue;
            }

            // 分数どうしの足し算。weight ÷ weightDivisor ＋ pull.weight ÷ pull.weightDivisor
            weight = weight * pull.weightDivisor + pull.weight * weightDivisor;
            weightDivisor *= pull.weightDivisor;
            roomUp = Mathf.Min(roomUp, endRoomUp);
            roomDown = Mathf.Min(roomDown, endRoomDown);
        }
    }

    /// <summary>
    /// 結ばれた端を引いたときの手応えを、結ぶ先の紐の系へ答える。親の系が「読む」の中で呼ぶ。
    /// endRoomUp / endRoomDown は、端があとどれだけ上がれるか・下がれるか。
    /// </summary>
    private bool TryProbeAttachedEnd(out PulleySolver.Pull pull, out float endRoomUp, out float endRoomDown)
    {
        pull = default;
        endRoomUp = 0f;
        endRoomDown = 0f;

        // 時間は進めずに入力だけ読み直す。足場の猶予などは、自分の更新のときに進む
        foreach (RouteEntry entry in route)
        {
            entry.node.ReadInputs(0f);
        }
        if (!TryBuildShape(shapeBuffer)) return false;
        ReadParts(shapeBuffer);

        pull = PulleySolver.Probe(partBuffer);

        // 端が 1 動くと、紐は「端の係数の大きさ」だけ過不足になる。重さもその倍率で伝わる
        int scale = Mathf.Abs(coefficients[attachedIndex]);
        pull.weight *= scale;
        endRoomUp = pull.ropeRoomUp / scale;
        endRoomDown = pull.ropeRoomDown / scale;
        return true;
    }

    /// <summary>
    /// 移動の層。判定の結果に従って紐を送り、部品へ変位を渡す（仕様 E7）。
    /// 限界を超える分は送らないので、端ではぴたりと止まる（仕様 B9）。
    /// </summary>
    private void Move(PulleySolver.Result result, float dt)
    {
        float rope = Mathf.Min(ropeSpeed * dt, result.ropeRoom);
        if (rope <= 0f) return;

        // moveBuffer は、紐が 1 送られたときの部品ごとの移動距離（仕様 D1、B7）
        for (int i = 0; i < moveBuffer.Count; i++)
        {
            if (moveBuffer[i] == 0f) continue;

            int index = partRouteIndexBuffer[i];
            displacements[index] += rope * moveBuffer[i];
            route[index].node.ApplyDisplacement(displacements[index]);
        }
    }

    /// <summary>
    /// 経路の index 番目の部品から、隣の部品へ向かう紐の向きを返す。
    /// toNext が false なら 1 つ前、true なら 1 つ後ろの部品へ向かう区間。
    /// </summary>
    private static Vector2 GetRopeDirection(PulleyRouteGeometry.Shape shape, int index, bool toNext)
    {
        Vector2 from = toNext ? shape.ExitPoints[index] : shape.EntryPoints[index];
        Vector2 to = toNext ? shape.EntryPoints[index + 1] : shape.ExitPoints[index - 1];
        return (to - from).normalized;
    }

    /// <summary>
    /// 紐の係数を求める。部品から出る紐が上向きなら +1、下向きなら -1 として、つながる区間ぶんを足す。
    /// 吊られた端は 1、吊られた動滑車は 2 になる。
    /// </summary>
    private static int GetCoefficient(PulleyRouteGeometry.Shape shape, int index, out bool isVertical)
    {
        float sum = 0f;
        isVertical = true;

        if (index > 0)
        {
            Vector2 direction = GetRopeDirection(shape, index, false);
            sum += direction.y;
            if (Mathf.Abs(direction.x) > VerticalTolerance) isVertical = false;
        }
        if (index < shape.EntryPoints.Count - 1)
        {
            Vector2 direction = GetRopeDirection(shape, index, true);
            sum += direction.y;
            if (Mathf.Abs(direction.x) > VerticalTolerance) isVertical = false;
        }

        return Mathf.RoundToInt(sum);
    }

    /// <summary>
    /// 紐の長さとストッパーから決まる、部品の動ける余地を求める（仕様 B9）。
    /// 上へ伸びる紐は上がるほど縮むので、余白を残した長さまでが上がれる余地になる。下へ伸びる紐は逆。
    /// </summary>
    private void GetRoom(PulleyRouteGeometry.Shape shape, int index, out float roomUp, out float roomDown)
    {
        roomUp = float.PositiveInfinity;
        roomDown = float.PositiveInfinity;

        if (index > 0)
        {
            float room = Mathf.Max(0f, shape.SegmentLengths[index - 1] - margin);
            float y = GetRopeDirection(shape, index, false).y;
            if (y > 0.5f) roomUp = Mathf.Min(roomUp, room);
            else if (y < -0.5f) roomDown = Mathf.Min(roomDown, room);
        }
        if (index < route.Count - 1)
        {
            float room = Mathf.Max(0f, shape.SegmentLengths[index] - margin);
            float y = GetRopeDirection(shape, index, true).y;
            if (y > 0.5f) roomUp = Mathf.Min(roomUp, room);
            else if (y < -0.5f) roomDown = Mathf.Min(roomDown, room);
        }

        // 足場の上に、足場以外に支えられたものがあれば、その足元までしか上がれない（仕様 J49）
        PulleyNode node = route[index].node;
        roomUp = Mathf.Min(roomUp, node.BlockedRoomUp);

        // ストッパー。左右の位置が重なるストッパーの箱までの距離が余地になる
        node.GetBox(out Vector2 boxMin, out Vector2 boxMax);
        foreach (PulleyStopper stopper in PulleyStopper.ActiveStoppers)
        {
            if (!IsUnderOrOver(stopper, node, boxMin, boxMax, out Bounds bounds)) continue;

            // 部品より下にあれば床、上にあれば天井として働く
            if (bounds.center.y <= (boxMin.y + boxMax.y) / 2f)
            {
                roomDown = Mathf.Min(roomDown, Mathf.Max(0f, boxMin.y - bounds.max.y));
            }
            else
            {
                roomUp = Mathf.Min(roomUp, Mathf.Max(0f, bounds.min.y - boxMax.y));
            }
        }
    }

    /// <summary>
    /// ストッパーが部品の真下か真上にあるか（左右の位置が重なるか）を返す。端が接しているだけなら重なりとしない。
    /// </summary>
    private static bool IsUnderOrOver(PulleyStopper stopper, PulleyNode node, Vector2 boxMin, Vector2 boxMax, out Bounds bounds)
    {
        bounds = stopper.Bounds;
        if (stopper.gameObject == node.gameObject) return false;

        return boxMax.x > bounds.min.x + StopperTolerance && boxMin.x < bounds.max.x - StopperTolerance;
    }

    /// <summary>
    /// 動く部品とストッパーの位置関係を検査し、問題を problems に追加する。
    /// 止まっている部品はストッパーに接しているだけなので、実行中に呼んでも問題にはならない。
    /// </summary>
    public void ValidateStoppers(List<string> problems)
    {
        foreach (RouteEntry entry in route)
        {
            PulleyNode node = entry.node;
            if (node == null || node.IsFixed) continue;

            node.GetBox(out Vector2 boxMin, out Vector2 boxMax);
            foreach (PulleyStopper stopper in PulleyStopper.ActiveStoppers)
            {
                if (!IsUnderOrOver(stopper, node, boxMin, boxMax, out Bounds bounds)) continue;

                bool isInside = boxMin.y < bounds.max.y - StopperTolerance && boxMax.y > bounds.min.y + StopperTolerance;
                if (isInside)
                {
                    problems.Add($"部品 {node.name} が、ストッパー {stopper.name} にめり込んだ位置にあります");
                }
            }
        }
    }

    /// <summary>
    /// 今の部品の位置から経路の形を計算する。描画や長さの確認に使う。
    /// </summary>
    public bool TryBuildShape(PulleyRouteGeometry.Shape shape)
    {
        circleBuffer.Clear();
        foreach (RouteEntry entry in route)
        {
            if (entry.node == null)
            {
                shape.Clear();
                shape.Error = "経路に未設定の欄があります";
                return false;
            }

            circleBuffer.Add(new PulleyRouteGeometry.Circle
            {
                center = entry.node.AnchorPosition,
                radius = entry.node.Radius,
                wrap = entry.wrap,
            });
        }

        return PulleyRouteGeometry.TryBuild(circleBuffer, shape);
    }

    /// <summary>
    /// 経路の登録内容を検査し、問題を文章で返す。紐の向きの検査は ValidateGeometry で行う。
    /// </summary>
    public void Validate(List<string> problems)
    {
        problems.Clear();

        if (route.Count < 2)
        {
            problems.Add("経路の部品が 2 つ未満です");
            return;
        }

        for (int i = 0; i < route.Count; i++)
        {
            if (route[i].node == null)
            {
                problems.Add($"経路の {i} 番目が未設定です");
            }
        }

        PulleyNode first = route[0].node;
        PulleyNode last = route[route.Count - 1].node;
        if (first != null && first is not PulleyEnd)
        {
            problems.Add($"経路の先頭 {first.name} が PulleyEnd ではありません");
        }
        if (last != null && last is not PulleyEnd)
        {
            problems.Add($"経路の末尾 {last.name} が PulleyEnd ではありません");
        }

        seenBuffer.Clear();
        foreach (RouteEntry entry in route)
        {
            if (entry.node == null) continue;

            if (!seenBuffer.Add(entry.node))
            {
                problems.Add($"{entry.node.name} が二重に登録されています");
            }
            if (entry.node.IsFixed && entry.node.Weight != 0)
            {
                problems.Add($"固定の部品 {entry.node.name} に重さ {entry.node.Weight} が設定されています");
            }
        }

        ValidateParent(problems);
    }

    /// <summary>
    /// 親の系の登録と、端を置いた場所が食い違っていないかを検査する（仕様 F4）。
    /// </summary>
    private void ValidateParent(List<string> problems)
    {
        if (parentSystem == this)
        {
            problems.Add("親の系に自分自身が登録されています");
            return;
        }

        int attachedCount = 0;
        foreach (RouteEntry entry in route)
        {
            if (entry.node is not PulleyEnd end) continue;
            PulleyNode target = end.AttachedTo;
            if (target == null) continue;

            attachedCount++;
            if (end.IsFixed)
            {
                problems.Add($"結ばれた端 {end.name} が固定になっています。結ぶ先と一緒に動くので、固定はオフにします");
            }
            if (parentSystem == null)
            {
                problems.Add($"端 {end.name} は {target.name} に結ばれていますが、親の系が登録されていません");
            }
            else if (!parentSystem.HasNode(target))
            {
                problems.Add($"端 {end.name} の結ぶ先 {target.name} が、親の系 {parentSystem.name} の経路にありません");
            }
        }

        if (parentSystem != null && attachedCount == 0)
        {
            problems.Add($"親の系 {parentSystem.name} が登録されていますが、その部品に結ばれた端(部品の子に置いた PulleyEnd)がありません");
        }
        if (attachedCount > 1)
        {
            problems.Add("結ばれた端が 2 つ以上あります。1 本の紐につき 1 つまでです");
        }
    }

    private bool HasNode(PulleyNode node)
    {
        foreach (RouteEntry entry in route)
        {
            if (entry.node == node) return true;
        }
        return false;
    }

    /// <summary>
    /// 動く部品につながる紐の向きを検査し、問題を problems に追加する。
    /// 計算できた経路の形に対して呼ぶ。
    /// </summary>
    public void ValidateGeometry(PulleyRouteGeometry.Shape shape, List<string> problems)
    {
        for (int i = 0; i < route.Count; i++)
        {
            PulleyNode node = route[i].node;
            if (node.IsFixed) continue;

            int coefficient = GetCoefficient(shape, i, out bool isVertical);
            if (!isVertical)
            {
                // 動く部品につながる斜めの紐は対応しない（仕様 B14）
                problems.Add($"動く部品 {node.name} につながる紐が垂直ではありません");
            }
            else if (node is PulleyEnd end && end.AttachedTo != null)
            {
                // 結ばれた端は、その下にこの紐の部品がぶら下がる形だけに対応する
                if (coefficient >= 0)
                {
                    problems.Add($"結ばれた端 {node.name} から、紐が下へ伸びていません(この紐の部品が端の下にぶら下がる形だけに対応しています)");
                }
            }
            else if (coefficient <= 0)
            {
                problems.Add($"動く部品 {node.name} が紐に吊られていません(紐が上へ伸びていません)");
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        Validate(problemBuffer);
        ValidateStoppers(problemBuffer);
        bool built = TryBuildShape(shapeBuffer);
        if (built && problemBuffer.Count == 0)
        {
            ValidateGeometry(shapeBuffer, problemBuffer);
        }
        bool ok = built && problemBuffer.Count == 0;

        Gizmos.color = ok ? gizmoColor : Color.red;

        if (!built)
        {
            // 形が作れないときは、登録されている部品どうしを直線で結んで位置だけ示す
            for (int i = 0; i < route.Count - 1; i++)
            {
                if (route[i].node == null || route[i + 1].node == null) continue;
                Gizmos.DrawLine(route[i].node.AnchorPosition, route[i + 1].node.AnchorPosition);
            }
            return;
        }

        List<Vector2> points = shapeBuffer.Points;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
        }

        // 端の位置を小さな球で示す
        Gizmos.DrawSphere(points[0], 0.08f);
        Gizmos.DrawSphere(points[points.Count - 1], 0.08f);
    }
}
