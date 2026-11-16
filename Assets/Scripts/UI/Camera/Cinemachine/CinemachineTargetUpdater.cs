using UnityEngine;
using Unity.Cinemachine;
using R3;

/// <summary>
/// CinemachineCamera の追従対象とカメラの範囲を、ステージ・プレイヤーの生成に合わせて更新するクラス。
/// 範囲は同じオブジェクトの CinemachineConfiner2D に、ステージの StageData.CameraArea を入れて決める。
/// </summary>
public class CinemachineTargetUpdater : MonoBehaviour
{
    void Start()
    {
        var vcam = GetComponent<CinemachineCamera>();
        var confiner = GetComponent<CinemachineConfiner2D>();

        if (confiner == null)
        {
            Debug.LogWarning("CinemachineTargetUpdater: CinemachineConfiner2D が付いていないため、カメラの範囲は制限されません。");
        }

        // StageManagerのステージ生成通知を購読し、カメラの範囲を差し替える
        StageManager.Instance.OnStageSpawned
            .Subscribe(stage =>
            {
                if (confiner == null) return;

                if (stage.CameraArea == null)
                {
                    Debug.LogWarning($"CinemachineTargetUpdater: ステージ '{stage.name}' に CameraArea が設定されていないため、カメラの範囲は制限されません。");
                }

                confiner.BoundingShape2D = stage.CameraArea;
                // 範囲の形は計算して覚えておく仕組みなので、差し替えたら作り直させる
                confiner.InvalidateBoundingShapeCache();
            })
            .AddTo(this); // このコンポーネントが破棄されたら自動で購読解除

        // StageManagerのプレイヤー生成通知を購読
        StageManager.Instance.OnPlayerSpawned
            .Subscribe(targetTransform =>
            {
                vcam.Follow = targetTransform;
                vcam.LookAt = targetTransform;
                // 前の位置を忘れさせ、次の更新で新しい Player の位置へ瞬時に移す。
                // これが無いと Damping で前の場所からゆっくり動き、暗幕が開いたあとに移動が見えてしまう
                vcam.PreviousStateIsValid = false;
                Debug.Log($"CinemachineTargetUpdater: カメラの追従対象を {targetTransform.name} に更新しました。");
            })
            .AddTo(this); // このコンポーネントが破棄されたら自動で購読解除
    }
}
