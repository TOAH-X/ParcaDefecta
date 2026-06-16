using UnityEngine;

/// <summary>
/// ステージプレハブのルートにアタッチし、スポーン地点などの参照を保持します。
/// </summary>
public class StageData : MonoBehaviour
{
    [SerializeField, Tooltip("プレイヤーが生成される位置")] private Transform spawnPoint;
    public Transform SpawnPoint => spawnPoint;
}