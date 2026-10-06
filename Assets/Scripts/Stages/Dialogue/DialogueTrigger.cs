using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// ステージに置く、会話の開始部品。きっかけがそろったら DialogueManager に再生を依頼する。
/// UI のことは知らない。StageTransitionPortal と同じ立ち位置。
/// 「範囲に入ったら」で使うときは、同じオブジェクトにトリガーの Collider2D を付ける。
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    /// <summary>きっかけの種類。「近づいてボタン」を足すときはここに増やす</summary>
    public enum StartMode
    {
        OnPlayerEnter, // Collider2D に Player が入ったら
        OnStageStart,  // ステージが生成されたら(Start)
    }

    [Header("再生する会話")]
    [SerializeField] private DialogueData dialogue;

    [Header("きっかけ")]
    [SerializeField] private StartMode startMode = StartMode.OnPlayerEnter;

    [Header("一度だけ(オンなら、このステージの間は 1 回しか再生しない。やり直せばまた再生する)")]
    [SerializeField] private bool playOnce = true;

    private bool hasPlayed;

    private void Start()
    {
        if (startMode == StartMode.OnStageStart)
        {
            TryPlay();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (startMode != StartMode.OnPlayerEnter) return;
        // 親オブジェクトも含めて Player スクリプトの存在を確認
        if (other.GetComponentInParent<Player>() == null) return;

        TryPlay();
    }

    private void TryPlay()
    {
        if (dialogue == null)
        {
            Debug.LogWarning($"[DialogueTrigger] {name}: 会話データが設定されていません", this);
            return;
        }
        if (playOnce && hasPlayed) return;

        // 別の会話が出ている間は始めない（DialogueManager 側でも無視されるが、再生済みの印は付けない）
        if (DialogueManager.Instance.IsPlaying) return;

        hasPlayed = true;
        DialogueManager.Instance.PlayAsync(dialogue).Forget();
    }
}
