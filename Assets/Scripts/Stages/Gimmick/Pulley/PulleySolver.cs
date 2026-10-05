using System.Collections.Generic;

/// <summary>
/// 滑車の判定。動く部品の重さと動ける余地から、どの部品が下がり、どの部品が上がるかを決める（仕様 B5〜B8）。
/// シーンや時間には依存しない純粋な計算で、前回の結果を持たない（仕様 E4、E5）。
/// </summary>
public static class PulleySolver
{
    /// <summary>動く部品 1 つぶんの入力</summary>
    public struct Part
    {
        /// <summary>重さ（整数）。weightDivisor が 2 以上なら、重さは weight ÷ weightDivisor</summary>
        public int weight;
        /// <summary>
        /// 重さの分母。別の紐の端が結ばれた部品は、その紐から伝わる重さが分数になるので持つ（例: 4 ÷ 2）。
        /// 設定しなければ 0 のままで、1 として扱う。
        /// </summary>
        public int weightDivisor;
        /// <summary>紐の係数。部品が 1 上がったときに余る紐の長さ。端は 1、動滑車は 2</summary>
        public int coefficient;
        /// <summary>あとどれだけ上がれるか（部品自身の移動距離）</summary>
        public float roomUp;
        /// <summary>あとどれだけ下がれるか（部品自身の移動距離）</summary>
        public float roomDown;

        public int Divisor => weightDivisor > 0 ? weightDivisor : 1;
    }

    /// <summary>
    /// 紐の端を引いたときの手応え。別の紐の部品に結ばれた端が、結ぶ先の紐へ伝える内容（仕様 B12、B13）。
    /// </summary>
    public struct Pull
    {
        /// <summary>次に持ち上がる部品の、紐から見た重さ。weight ÷ weightDivisor</summary>
        public int weight;
        public int weightDivisor;
        /// <summary>端を引き上げる向きに、あとどれだけ紐を送れるか。持ち上がる部品が無ければ 0</summary>
        public float ropeRoomUp;
        /// <summary>端を下ろす向きに、あとどれだけ紐を送れるか。下がれる部品が無ければ 0</summary>
        public float ropeRoomDown;
    }

    public enum Outcome
    {
        /// <summary>下がる部品と上がる部品が決まり、動く</summary>
        Moving,
        /// <summary>下がる側が重くないので、その場で止まる</summary>
        Balanced,
        /// <summary>下がれる部品か上がれる部品が無く、限界で止まっている</summary>
        Blocked,
    }

    public struct Result
    {
        public Outcome outcome;
        /// <summary>動いている部品のどれかが限界に達するまでに送れる紐の長さ</summary>
        public float ropeRoom;
    }

    /// <summary>余地がこれ以下なら、もう動けないものとして扱う</summary>
    public const float RoomEpsilon = 1e-4f;

    /// <summary>
    /// 判定する。moves には部品ごとに「紐が 1 送られたときの移動距離」が入る（上が正、動かない部品は 0）。
    /// 紐から見た重さが同じ部品は組になり、同じ距離ずつ動く（仕様 B7）。
    /// 限界に達した部品は次の判定で候補から外れるので、交代や、組の残りだけが動き続ける動きは自然に起きる（仕様 B6）。
    /// </summary>
    public static Result Solve(IReadOnlyList<Part> parts, List<float> moves)
    {
        var result = new Result { outcome = Outcome.Blocked };

        moves.Clear();
        for (int i = 0; i < parts.Count; i++)
        {
            moves.Add(0f);
        }

        // まだ下がれる部品のうち、紐から見て最も重いもの
        int heaviestDown = -1;
        for (int i = 0; i < parts.Count; i++)
        {
            if (!CanMoveDown(parts[i])) continue;
            if (heaviestDown < 0 || IsHeavier(parts[i], parts[heaviestDown]))
            {
                heaviestDown = i;
            }
        }
        if (heaviestDown < 0) return result;

        // まだ上がれる部品のうち、紐から見て最も軽いもの。下がる組に入る部品は除く
        int lightestUp = -1;
        bool hasSameWeightUp = false;
        for (int i = 0; i < parts.Count; i++)
        {
            if (!CanMoveUp(parts[i])) continue;
            if (IsInDownGroup(parts[i], parts[heaviestDown]))
            {
                hasSameWeightUp = true;
                continue;
            }
            if (lightestUp < 0 || IsHeavier(parts[lightestUp], parts[i]))
            {
                lightestUp = i;
            }
        }
        if (lightestUp < 0)
        {
            // 上がれる部品が、下がる組と同じ重さのものしか無いなら、限界ではなく釣り合い
            if (hasSameWeightUp) result.outcome = Outcome.Balanced;
            return result;
        }

        if (!IsHeavier(parts[heaviestDown], parts[lightestUp]))
        {
            result.outcome = Outcome.Balanced;
            return result;
        }

        // 組の係数の合計。紐が 1 送られると、組の部品はそれぞれ「1 ÷ 合計」だけ動く
        int downSum = 0;
        int upSum = 0;
        for (int i = 0; i < parts.Count; i++)
        {
            if (IsInDownGroup(parts[i], parts[heaviestDown])) downSum += parts[i].coefficient;
            else if (IsInUpGroup(parts[i], parts[lightestUp])) upSum += parts[i].coefficient;
        }

        float ropeRoom = float.PositiveInfinity;
        for (int i = 0; i < parts.Count; i++)
        {
            if (IsInDownGroup(parts[i], parts[heaviestDown]))
            {
                moves[i] = -1f / downSum;
                ropeRoom = System.Math.Min(ropeRoom, parts[i].roomDown * downSum);
            }
            else if (IsInUpGroup(parts[i], parts[lightestUp]))
            {
                moves[i] = 1f / upSum;
                ropeRoom = System.Math.Min(ropeRoom, parts[i].roomUp * upSum);
            }
        }

        result.outcome = Outcome.Moving;
        result.ropeRoom = ropeRoom;
        return result;
    }

    /// <summary>
    /// 紐の過不足を、動ける部品に割り振る。別の紐の部品に結ばれた端が動いたあと、紐の全長を保つために使う。
    /// rope が正なら紐が余っているので、まだ下がれる部品のうち最も重い組が下がる。
    /// 負なら足りないので、まだ上がれる部品のうち最も軽い組が上がる（仕様 B12 の「次に持ち上がる部品」）。
    /// moves には部品ごとの移動距離が入る（上が正）。戻り値は割り振れた紐の長さで、限界に当たると rope の大きさより小さくなる。
    /// </summary>
    public static float Absorb(IReadOnlyList<Part> parts, float rope, List<float> moves)
    {
        moves.Clear();
        for (int i = 0; i < parts.Count; i++)
        {
            moves.Add(0f);
        }
        if (rope == 0f) return 0f;

        bool isDown = rope > 0f;
        int lead = FindGroup(parts, isDown, out int sum, out float ropeRoom);
        if (lead < 0) return 0f;

        float amount = System.Math.Min(System.Math.Abs(rope), ropeRoom);
        for (int i = 0; i < parts.Count; i++)
        {
            if (!IsInGroup(parts[i], parts[lead], isDown)) continue;
            moves[i] = (isDown ? -amount : amount) / sum;
        }
        return amount;
    }

    /// <summary>
    /// 紐の端を引いたときの手応えを求める。
    /// 重さは、次に持ち上がる部品（まだ上がれる部品のうち最も軽いもの）の紐から見た重さ。
    /// 上がれる部品が無いときは、まだ下がれる部品のうち最も重いものの重さを使う。
    /// </summary>
    public static Pull Probe(IReadOnlyList<Part> parts)
    {
        var pull = new Pull { weight = 0, weightDivisor = 1 };

        int up = FindGroup(parts, false, out _, out float ropeRoomUp);
        int down = FindGroup(parts, true, out _, out float ropeRoomDown);
        if (up >= 0) pull.ropeRoomUp = ropeRoomUp;
        if (down >= 0) pull.ropeRoomDown = ropeRoomDown;

        int lead = up >= 0 ? up : down;
        if (lead >= 0)
        {
            pull.weight = parts[lead].weight;
            pull.weightDivisor = parts[lead].Divisor * parts[lead].coefficient;
        }
        return pull;
    }

    /// <summary>
    /// 動かす組を選ぶ。isDown なら、まだ下がれる部品のうち最も重い組。そうでなければ、まだ上がれる部品のうち最も軽い組。
    /// 戻り値は組の代表の番号（無ければ -1）。sum は組の係数の合計、ropeRoom は組のどれかが限界に達するまでに送れる紐の長さ。
    /// </summary>
    private static int FindGroup(IReadOnlyList<Part> parts, bool isDown, out int sum, out float ropeRoom)
    {
        sum = 0;
        ropeRoom = 0f;

        int lead = -1;
        for (int i = 0; i < parts.Count; i++)
        {
            if (isDown ? !CanMoveDown(parts[i]) : !CanMoveUp(parts[i])) continue;
            if (lead < 0 || (isDown ? IsHeavier(parts[i], parts[lead]) : IsHeavier(parts[lead], parts[i])))
            {
                lead = i;
            }
        }
        if (lead < 0) return -1;

        for (int i = 0; i < parts.Count; i++)
        {
            if (IsInGroup(parts[i], parts[lead], isDown)) sum += parts[i].coefficient;
        }

        ropeRoom = float.PositiveInfinity;
        for (int i = 0; i < parts.Count; i++)
        {
            if (!IsInGroup(parts[i], parts[lead], isDown)) continue;
            float room = isDown ? parts[i].roomDown : parts[i].roomUp;
            ropeRoom = System.Math.Min(ropeRoom, room * sum);
        }
        return lead;
    }

    private static bool IsInGroup(Part part, Part lead, bool isDown)
    {
        return isDown ? IsInDownGroup(part, lead) : IsInUpGroup(part, lead);
    }

    private static bool CanMoveDown(Part part) => part.roomDown > RoomEpsilon;

    private static bool CanMoveUp(Part part) => part.roomUp > RoomEpsilon;

    private static bool IsInDownGroup(Part part, Part heaviestDown)
    {
        return CanMoveDown(part) && IsSame(part, heaviestDown);
    }

    private static bool IsInUpGroup(Part part, Part lightestUp)
    {
        return CanMoveUp(part) && IsSame(part, lightestUp);
    }

    /// <summary>
    /// 紐から見た重さ（重さ ÷ 分母 ÷ 係数）で a が b より重いか。割り算を避けて整数のまま比べる。
    /// </summary>
    private static bool IsHeavier(Part a, Part b)
    {
        return (long)a.weight * b.Divisor * b.coefficient > (long)b.weight * a.Divisor * a.coefficient;
    }

    /// <summary>紐から見た重さが a と b で同じか</summary>
    private static bool IsSame(Part a, Part b)
    {
        return (long)a.weight * b.Divisor * b.coefficient == (long)b.weight * a.Divisor * a.coefficient;
    }
}
