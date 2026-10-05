/// <summary>
/// 重さを持つもの。滑車の足場は、乗っているもののうちこれを持つものだけを数える（滑車の仕様 C2）。
/// </summary>
public interface IWeighted
{
    /// <summary>今の重さ（整数）</summary>
    int Weight { get; }
}
