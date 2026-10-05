using UnityEngine;

/// <summary>
/// 紐の端。固定なら固定端、固定でなければおもりや足場の端として振る舞う。
/// 経路リストの両端は必ずこの部品になる。
/// 別の紐の部品の子に置くと、その部品に結ばれた端になり、自分では動かず結ぶ先と同じだけ動く（仕様 B12、F5）。
/// </summary>
public class PulleyEnd : PulleyNode
{
    /// <summary>
    /// 結ぶ先の部品。Transform の親が滑車の部品ならそれを返し、そうでなければ null。
    /// 別の項目として持たず、ヒエラルキーの親子から読み取る。置き場所と登録の食い違いを作らないため。
    /// </summary>
    public PulleyNode AttachedTo
    {
        get
        {
            Transform parent = transform.parent;
            return parent != null ? parent.GetComponent<PulleyNode>() : null;
        }
    }
}
