using UnityEngine;

/// <summary>
/// ステージ演出の「数字を出す部品」の親。今の数字（0〜1）を出すことだけが仕事で、その数字が何に使われるかは知らない。
/// 数字を読んで何かを変える部品（LightBlendEffect など）は、Inspector でこれを指定して毎フレーム読みに行く。
///
/// 決まり:
///   - 演出は StageSystem/Staging の下に 1 つずつ置く（演出 1 つ = オブジェクト 1 つ）
///   - 数字を出す部品は必ずこのクラスを継承する
/// </summary>
public abstract class StagingSource : MonoBehaviour
{
    /// <summary>今の数字（0〜1）</summary>
    public abstract float Value { get; }
}
