using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 8 隣接の 47 状態を列挙し、素材 5 枚の象限を組み合わせてスプライトシートを合成します。
/// Unity のアセット操作には依存しません。
/// </summary>
public static class BlockTileComposer
{
    // 隣接ビット。上から時計回り。
    public const int T = 1 << 0;
    public const int TR = 1 << 1;
    public const int R = 1 << 2;
    public const int BR = 1 << 3;
    public const int B = 1 << 4;
    public const int BL = 1 << 5;
    public const int L = 1 << 6;
    public const int TL = 1 << 7;

    public const int StateCount = 47;
    public const int Columns = 8;
    public const int Rows = 6;

    public const int NeighborThis = 1;    // RuleTile.TilingRuleOutput.Neighbor.This
    public const int NeighborNotThis = 2; // RuleTile.TilingRuleOutput.Neighbor.NotThis

    public enum Source { Fill, Single, HorizontalBar, VerticalBar, InnerCorners }
    public enum Quadrant { TopLeft, TopRight, BottomLeft, BottomRight }

    private static readonly Quadrant[] AllQuadrants =
        { Quadrant.TopLeft, Quadrant.TopRight, Quadrant.BottomLeft, Quadrant.BottomRight };

    private static readonly int[] Diagonals = { TR, BR, BL, TL };

    private static bool Has(int mask, int bit) => (mask & bit) != 0;

    /// <summary>
    /// 斜めが判定に意味を持つのは、それに接する上下左右の 2 つが両方あるときだけです。
    /// </summary>
    public static bool IsDiagonalRelevant(int mask, int diagonal)
    {
        switch (diagonal)
        {
            case TR: return Has(mask, T) && Has(mask, R);
            case BR: return Has(mask, R) && Has(mask, B);
            case BL: return Has(mask, B) && Has(mask, L);
            case TL: return Has(mask, L) && Has(mask, T);
            default: throw new ArgumentException("diagonal bit expected", nameof(diagonal));
        }
    }

    /// <summary>意味を持たない斜めのビットを落として正規化します。</summary>
    public static int Canonicalize(int mask)
    {
        int result = mask & (T | R | B | L);
        foreach (int d in Diagonals)
        {
            if (Has(mask, d) && IsDiagonalRelevant(mask, d)) result |= d;
        }
        return result;
    }

    /// <summary>47 状態をマスク値の昇順で返します。番号(0〜46)はこの並びです。</summary>
    public static IReadOnlyList<int> EnumerateStates()
    {
        var set = new SortedSet<int>();
        for (int m = 0; m < 256; m++) set.Add(Canonicalize(m));
        if (set.Count != StateCount)
            throw new InvalidOperationException($"state count mismatch: {set.Count}");
        return new List<int>(set);
    }

    /// <summary>象限ごとに、どの素材のその象限を使うかを決めます。回転・反転はしません。</summary>
    public static Source SelectSource(int mask, Quadrant quadrant)
    {
        int v, h, d;
        switch (quadrant)
        {
            case Quadrant.TopLeft: v = T; h = L; d = TL; break;
            case Quadrant.TopRight: v = T; h = R; d = TR; break;
            case Quadrant.BottomLeft: v = B; h = L; d = BL; break;
            case Quadrant.BottomRight: v = B; h = R; d = BR; break;
            default: throw new ArgumentOutOfRangeException(nameof(quadrant));
        }

        bool hasV = Has(mask, v);
        bool hasH = Has(mask, h);
        if (!hasV && !hasH) return Source.Single;        // 外角
        if (hasV && !hasH) return Source.VerticalBar;    // 縦の縁
        if (!hasV && hasH) return Source.HorizontalBar;  // 横の縁
        return Has(mask, d) ? Source.Fill : Source.InnerCorners;
    }

    /// <summary>
    /// Rule Tile の 1 行に設定する隣接条件。上下左右は常に指定し、斜めは意味を持つときだけ指定します。
    /// これにより 47 行が互いに排他になり、並び順に依存しません。
    /// </summary>
    public static IEnumerable<(Vector3Int position, int value)> GetRuleNeighbors(int mask)
    {
        (int bit, Vector3Int pos)[] orthogonal =
        {
            (T, new Vector3Int(0, 1, 0)), (R, new Vector3Int(1, 0, 0)),
            (B, new Vector3Int(0, -1, 0)), (L, new Vector3Int(-1, 0, 0)),
        };
        (int bit, Vector3Int pos)[] diagonal =
        {
            (TR, new Vector3Int(1, 1, 0)), (BR, new Vector3Int(1, -1, 0)),
            (BL, new Vector3Int(-1, -1, 0)), (TL, new Vector3Int(-1, 1, 0)),
        };

        foreach (var (bit, pos) in orthogonal)
            yield return (pos, Has(mask, bit) ? NeighborThis : NeighborNotThis);
        foreach (var (bit, pos) in diagonal)
        {
            if (!IsDiagonalRelevant(mask, bit)) continue;
            yield return (pos, Has(mask, bit) ? NeighborThis : NeighborNotThis);
        }
    }

    /// <summary>
    /// シート上のタイル矩形(左下原点、Unity のスプライト矩形と同じ向き)。余白は含みません。
    /// 番号 0 がシートの左上に来るよう並べます。
    /// </summary>
    public static RectInt GetCellRect(int index, int tileSize, int padding)
    {
        int pitch = tileSize + padding * 2;
        int col = index % Columns;
        int rowFromTop = index / Columns;
        int x = col * pitch + padding;
        int y = (Rows - 1 - rowFromTop) * pitch + padding;
        return new RectInt(x, y, tileSize, tileSize);
    }

    /// <summary>
    /// 素材 5 枚のピクセル(Texture2D.GetPixels32 と同じ左下原点の並び)から
    /// 47 枚入りのシートを合成します。最後の 1 マスは空のままです。
    /// 各タイルの周囲 padding ピクセルには、そのタイル自身の端を引き伸ばして埋めます
    /// (Bilinear サンプリングで隣のタイルの色が混ざるのを防ぐため)。
    /// </summary>
    public static Color32[] ComposeSheet(
        IReadOnlyDictionary<Source, Color32[]> sources, int tileSize, int padding, out int width, out int height)
    {
        if (tileSize <= 0 || tileSize % 2 != 0)
            throw new ArgumentException("tileSize must be a positive even number", nameof(tileSize));
        if (padding < 0)
            throw new ArgumentException("padding must be 0 or greater", nameof(padding));
        foreach (Source s in Enum.GetValues(typeof(Source)))
        {
            if (!sources.TryGetValue(s, out var px) || px == null || px.Length != tileSize * tileSize)
                throw new ArgumentException($"source {s} is missing or has wrong size", nameof(sources));
        }

        int pitch = tileSize + padding * 2;
        width = Columns * pitch;
        height = Rows * pitch;
        var sheet = new Color32[width * height];
        int half = tileSize / 2;

        var states = EnumerateStates();
        for (int i = 0; i < states.Count; i++)
        {
            var cell = GetCellRect(i, tileSize, padding);
            foreach (var q in AllQuadrants)
            {
                var src = sources[SelectSource(states[i], q)];
                GetQuadrantOrigin(q, half, out int qx, out int qy);
                for (int y = 0; y < half; y++)
                {
                    int srcIndex = (qy + y) * tileSize + qx;
                    int dstIndex = (cell.y + qy + y) * width + cell.x + qx;
                    Array.Copy(src, srcIndex, sheet, dstIndex, half);
                }
            }
            ExtrudeEdges(sheet, width, cell, padding);
        }
        return sheet;
    }

    /// <summary>タイルの端のピクセルを周囲の余白へ引き伸ばします。四隅は角のピクセルで埋まります。</summary>
    private static void ExtrudeEdges(Color32[] sheet, int width, RectInt cell, int padding)
    {
        if (padding == 0) return;

        // 左右へ引き伸ばす
        for (int y = cell.y; y < cell.yMax; y++)
        {
            int row = y * width;
            var left = sheet[row + cell.x];
            var right = sheet[row + cell.xMax - 1];
            for (int p = 1; p <= padding; p++)
            {
                sheet[row + cell.x - p] = left;
                sheet[row + cell.xMax - 1 + p] = right;
            }
        }

        // 上下へ引き伸ばす。左右の余白も含めた幅で行うので四隅も埋まる
        int x0 = cell.x - padding;
        int x1 = cell.xMax + padding;
        int bottomRow = cell.y * width;
        int topRow = (cell.yMax - 1) * width;
        for (int x = x0; x < x1; x++)
        {
            var bottom = sheet[bottomRow + x];
            var top = sheet[topRow + x];
            for (int p = 1; p <= padding; p++)
            {
                sheet[bottomRow - p * width + x] = bottom;
                sheet[topRow + p * width + x] = top;
            }
        }
    }

    private static void GetQuadrantOrigin(Quadrant q, int half, out int x, out int y)
    {
        // 左下原点のピクセル座標。上側の象限は y が大きい側。
        x = (q == Quadrant.TopLeft || q == Quadrant.BottomLeft) ? 0 : half;
        y = (q == Quadrant.TopLeft || q == Quadrant.TopRight) ? half : 0;
    }
}
