using UnityEngine;
using ParcaDefecta.System;

/// <summary>
/// ワープの見た目（入口・出口の子の Visual）を、一定の速さで回し続ける部品。
/// 当たり判定や物理の部品がある根元は回さず、見た目だけを回すために Visual に付ける。
/// ポーズ中は止まる。
/// </summary>
public class WarpRotator : MonoBehaviour
{
    [SerializeField, Tooltip("1 秒に回る角度。プラスで反時計回り、マイナスで時計回り")]
    private float degreesPerSecond = 90f;

    private void Update()
    {
        float dt = TimeManager.Instance != null ? TimeManager.Instance.DeltaTime : Time.deltaTime;
        transform.Rotate(0f, 0f, degreesPerSecond * dt);
    }
}
