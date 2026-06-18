using UnityEngine;
using Unity.Cinemachine;
using R3;

/// <summary>
/// CinemachineVirtualCameraのターゲットを、プレイヤー生成イベントに合わせて自動更新するクラス。
/// </summary>
public class CinemachineTargetUpdater : MonoBehaviour
{
    void Start()
    {
        var vcam = GetComponent<CinemachineCamera>();

        // StageManagerのプレイヤー生成通知を購読
        StageManager.Instance.OnPlayerSpawned
            .Subscribe(targetTransform =>
            {
                vcam.Follow = targetTransform;
                vcam.LookAt = targetTransform;
                Debug.Log($"CinemachineTargetUpdater: カメラの追従対象を {targetTransform.name} に更新しました。");
            })
            .AddTo(this); // このコンポーネントが破棄されたら自動で購読解除
    }
}