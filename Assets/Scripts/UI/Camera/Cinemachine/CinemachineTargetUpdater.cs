using UnityEngine;
using Unity.Cinemachine;
using R3;

/// <summary>
/// CinemachineCamera の追従対象とカメラの範囲を、ステージ・プレイヤーの生成に合わせて更新するクラス。
/// 範囲は同じオブジェクトの CinemachineConfiner2D に、ステージの StageData.CameraArea を入れて決める。
/// 別のシーンから移ってきた場合は、このシーンの Start より先にステージと Player が作られて通知が終わっていることがあるので、
/// 受け取りを始める前に、今のステージと Player を読んで設定する。
/// </summary>
public class CinemachineTargetUpdater : MonoBehaviour
{
    private CinemachineCamera vcam;
    private CinemachineConfiner2D confiner;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>();

        if (confiner == null)
        {
            Debug.LogWarning("CinemachineTargetUpdater: CinemachineConfiner2D が付いていないため、カメラの範囲は制限されません。");
        }

        // すでにステージと Player があれば、先に設定しておく（通知を受け取り損ねた場合のため）
        var currentStage = StageManager.Instance.CurrentStage;
        if (currentStage != null) ApplyStage(currentStage);

        var currentPlayer = StageManager.Instance.CurrentPlayer;
        if (currentPlayer != null) ApplyPlayer(currentPlayer);

        // StageManagerのステージ生成通知を購読し、カメラの範囲を差し替える
        StageManager.Instance.OnStageSpawned
            .Subscribe(ApplyStage)
            .AddTo(this); // このコンポーネントが破棄されたら自動で購読解除

        // StageManagerのプレイヤー生成通知を購読
        StageManager.Instance.OnPlayerSpawned
            .Subscribe(ApplyPlayer)
            .AddTo(this); // このコンポーネントが破棄されたら自動で購読解除
    }

    /// <summary>
    /// ステージのカメラ範囲を Confiner に入れる。
    /// </summary>
    private void ApplyStage(StageData stage)
    {
        if (confiner == null) return;

        if (stage.CameraArea == null)
        {
            Debug.LogWarning($"CinemachineTargetUpdater: ステージ '{stage.name}' に CameraArea が設定されていないため、カメラの範囲は制限されません。");
        }

        confiner.BoundingShape2D = stage.CameraArea;
        // 範囲の形は計算して覚えておく仕組みなので、差し替えたら作り直させる
        confiner.InvalidateBoundingShapeCache();
    }

    /// <summary>
    /// 追従対象を、Player が指定するカメラの注目点（Player.CameraFocus）にする。
    /// 何を見るかは Player 側が決める（StageData がカメラの範囲を持つのと同じ考え方）。
    /// </summary>
    private void ApplyPlayer(Transform playerTransform)
    {
        Transform targetTransform = playerTransform.TryGetComponent<Player>(out var player) ? player.CameraFocus : playerTransform;

        vcam.Follow = targetTransform;
        vcam.LookAt = targetTransform;
        // 前の位置を忘れさせ、次の更新で新しい Player の位置へ瞬時に移す。
        // これが無いと Damping で前の場所からゆっくり動き、暗幕が開いたあとに移動が見えてしまう
        vcam.PreviousStateIsValid = false;
        Debug.Log($"CinemachineTargetUpdater: カメラの追従対象を {targetTransform.name} に更新しました。");
    }
}
