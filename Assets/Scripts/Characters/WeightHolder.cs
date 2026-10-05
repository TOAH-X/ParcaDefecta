using UnityEngine;

/// <summary>
/// 重さを持たせる部品。Player、AlterPlayer、物に付ける。
/// 当たり判定が無効なものは足場に検知されないので、影状態の AlterPlayer は重さなしとして扱われる。
/// </summary>
public class WeightHolder : MonoBehaviour, IWeighted
{
    [Header("重さ(整数・0以上)")]
    [SerializeField, Min(0)] private int weight = 1;

    public int Weight => weight;
}
