using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 紐が滑車に巻き付く向き。経路リストの順に紐をたどったとき、滑車の周りをどちら向きに回るか。
/// 端（PulleyEnd）では無視される。
/// </summary>
public enum PulleyWrapDirection
{
    Clockwise,
    CounterClockwise,
}

/// <summary>
/// 紐の経路の幾何計算。隣り合う部品どうしの接線、滑車に巻き付く弧、区間の長さ、紐の全長、描画用の折れ線を求める。
/// シーンや時間には依存しない純粋な計算（仕様 E5 と同じ方針）。
/// </summary>
public static class PulleyRouteGeometry
{
    /// <summary>経路上の 1 部品。端は半径 0 として扱う</summary>
    public struct Circle
    {
        public Vector2 center;
        public float radius;
        public PulleyWrapDirection wrap;

        public bool IsPoint => radius <= 0f;
    }

    /// <summary>計算結果。使い回せるようにリストは再利用する</summary>
    public class Shape
    {
        /// <summary>描画用の折れ線（ワールド座標）</summary>
        public readonly List<Vector2> Points = new();
        /// <summary>直線区間の長さ。部品数 - 1 個</summary>
        public readonly List<float> SegmentLengths = new();
        /// <summary>巻き付き部分の長さ。部品数と同じ個数で、端は 0</summary>
        public readonly List<float> ArcLengths = new();
        /// <summary>各部品に紐が入る接点。部品数と同じ個数で、先頭は部品の位置</summary>
        public readonly List<Vector2> EntryPoints = new();
        /// <summary>各部品から紐が出る接点。部品数と同じ個数で、末尾は部品の位置</summary>
        public readonly List<Vector2> ExitPoints = new();
        /// <summary>紐の全長</summary>
        public float TotalLength;
        /// <summary>計算できなかった理由。null なら有効</summary>
        public string Error;

        public bool IsValid => Error == null;

        public void Clear()
        {
            Points.Clear();
            SegmentLengths.Clear();
            ArcLengths.Clear();
            EntryPoints.Clear();
            ExitPoints.Clear();
            TotalLength = 0f;
            Error = null;
        }
    }

    // 弧を折れ線にするときの分割角度
    private const float ArcStepDegrees = 10f;
    private const float TwoPi = Mathf.PI * 2f;
    private const float Epsilon = 1e-4f;

    /// <summary>
    /// 経路の形を計算する。失敗した場合は shape.Error に理由が入る。
    /// </summary>
    public static bool TryBuild(IReadOnlyList<Circle> circles, Shape shape)
    {
        shape.Clear();

        int count = circles.Count;
        if (count < 2)
        {
            shape.Error = "経路の部品が 2 つ未満です";
            return false;
        }

        // 各部品から出る接点と、各部品に入る接点。両端は部品の位置で埋めておく
        List<Vector2> exitPoints = shape.ExitPoints;
        List<Vector2> entryPoints = shape.EntryPoints;
        for (int i = 0; i < count; i++)
        {
            exitPoints.Add(circles[i].center);
            entryPoints.Add(circles[i].center);
        }

        for (int i = 0; i < count - 1; i++)
        {
            if (!TryTangent(circles[i], circles[i + 1], out Vector2 from, out Vector2 to))
            {
                shape.Error = $"{i} 番目と {i + 1} 番目の部品が近すぎて接線が引けません";
                return false;
            }
            exitPoints[i] = from;
            entryPoints[i + 1] = to;
        }

        for (int i = 0; i < count; i++)
        {
            shape.ArcLengths.Add(0f);
        }

        // 先頭は端なので、出る点がそのまま端の位置
        shape.Points.Add(exitPoints[0]);

        for (int i = 0; i < count - 1; i++)
        {
            float segment = Vector2.Distance(exitPoints[i], entryPoints[i + 1]);
            shape.SegmentLengths.Add(segment);
            shape.TotalLength += segment;
            shape.Points.Add(entryPoints[i + 1]);

            // 途中の滑車では、入った点から出る点まで弧をたどる
            int next = i + 1;
            if (next < count - 1 && !circles[next].IsPoint)
            {
                float arc = AppendArc(circles[next], entryPoints[next], exitPoints[next], shape.Points);
                shape.ArcLengths[next] = arc;
                shape.TotalLength += arc;
            }
        }

        return true;
    }

    /// <summary>
    /// 部品 a から部品 b へ渡る紐の接点を求める。
    /// 巻き付く向きが同じなら外接線、異なれば内接線（交差する線）になる。
    /// </summary>
    private static bool TryTangent(Circle a, Circle b, out Vector2 from, out Vector2 to)
    {
        from = a.center;
        to = b.center;

        Vector2 d = b.center - a.center;
        float distance = d.magnitude;
        if (distance < Epsilon) return false;

        if (a.IsPoint && b.IsPoint) return true;

        Vector2 dir = d / distance;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        // 反時計回りを +1、時計回りを -1 とする
        int signA = WrapSign(a.wrap);
        int signB = WrapSign(b.wrap);

        // 端は向きを持たないので、相手に合わせる
        if (a.IsPoint) signA = signB;
        else if (b.IsPoint) signB = signA;

        bool sameSide = signA == signB;
        float along = sameSide
            ? (a.radius - b.radius) / distance
            : (a.radius + b.radius) / distance;

        if (Mathf.Abs(along) > 1f) return false;

        // 紐の進行方向が a から b へ向くように法線の向きを選ぶ
        float across = -signA * Mathf.Sqrt(1f - along * along);
        Vector2 normal = along * dir + across * perp;

        from = a.center + a.radius * normal;
        to = b.center + (sameSide ? b.radius : -b.radius) * normal;
        return true;
    }

    /// <summary>
    /// 滑車に巻き付く弧を折れ線に追加し、弧の長さを返す。
    /// </summary>
    private static float AppendArc(Circle circle, Vector2 entry, Vector2 exit, List<Vector2> points)
    {
        float startAngle = Mathf.Atan2(entry.y - circle.center.y, entry.x - circle.center.x);
        float endAngle = Mathf.Atan2(exit.y - circle.center.y, exit.x - circle.center.x);

        // 巻き付く向きに沿った符号付きの回転量（反時計回りが正）
        float sweep = endAngle - startAngle;
        if (circle.wrap == PulleyWrapDirection.CounterClockwise)
        {
            if (sweep < -Epsilon) sweep += TwoPi;
            if (sweep < 0f) sweep = 0f;
        }
        else
        {
            if (sweep > Epsilon) sweep -= TwoPi;
            if (sweep > 0f) sweep = 0f;
        }

        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) * Mathf.Rad2Deg / ArcStepDegrees));
        for (int k = 1; k <= steps; k++)
        {
            float angle = startAngle + sweep * k / steps;
            points.Add(circle.center + circle.radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }

        return circle.radius * Mathf.Abs(sweep);
    }

    private static int WrapSign(PulleyWrapDirection wrap)
    {
        return wrap == PulleyWrapDirection.CounterClockwise ? 1 : -1;
    }
}
