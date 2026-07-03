using UnityEngine;

/// <summary>
/// プレイヤーの接触を検知し、別のステージへの遷移を行うポータル。
/// 演出（エフェクト・SEなど）を差し込みやすいように関数を分離しています。
/// </summary>
public class StageTransitionPortal : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("チェックを入れると、StageDatabase上の次のステージへ自動で遷移します。")]
    [SerializeField] private bool transitionToNextStage = true;

    [Tooltip("transitionToNextStageがfalseの場合、ここに遷移先のステージIDを指定します。")]
    [SerializeField] private string targetStageId;

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 親オブジェクトも含めてPlayerスクリプトの存在を確認
        if (other.GetComponentInParent<Player>() == null) return;

        ExecuteTransition();
    }

    /// <summary>
    /// ステージ遷移処理を実行します。
    /// </summary>
    private void ExecuteTransition()
    {
        isTriggered = true;

        if (transitionToNextStage)
        {
            Debug.Log("StageTransitionPortal: 次のステージへの遷移を開始します。");
            StageManager.Instance.AdvanceToNextStage();
        }
        else
        {
            if (string.IsNullOrEmpty(targetStageId))
            {
                Debug.LogError("StageTransitionPortal: 遷移先ステージIDが設定されていません。");
                isTriggered = false;
                return;
            }

            Debug.Log($"StageTransitionPortal: ステージ '{targetStageId}' への遷移を開始します。");
            StageManager.Instance.LoadStage(targetStageId);
        }
    }

    /// <summary>
    /// ポータルのトリガー状態をリセットします。リトライ時などに利用可能です。
    /// </summary>
    public void ResetPortal()
    {
        isTriggered = false;
    }
}
