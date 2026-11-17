using UnityEngine;
using R3;

/// <summary>
/// 本体（Player）が始点から終点へどれだけ進んだかを 0〜1 で出す部品。
/// 始点 → 終点の向きに沿った進み具合だけを見るので、横・縦・斜めのどれでも使える（線から離れた分は無視する）。
/// 始点より手前は 0、終点より先は 1。位置だけで決まるので、止まれば変わらず、やり直しても自然に元へ戻る。
/// Player は StageManager から受け取る（CinemachineTargetUpdater と同じ形）。分身は対象にしない。
/// </summary>
public class PlayerPathProgressSource : StagingSource
{
    [SerializeField, Tooltip("始点。ここで 0")] private Transform start;
    [SerializeField, Tooltip("終点。ここで 1")] private Transform end;

    private Transform player;
    private float lastValue;

    /// <summary>今の進み具合（0〜1）。Player がいない間は最後の値を保つ</summary>
    public override float Value
    {
        get
        {
            if (player != null && start != null && end != null)
            {
                lastValue = CalculateProgress(player.position);
            }
            return lastValue;
        }
    }

    private void Start()
    {
        // すでに Player がいれば先に覚えておく（通知を受け取り損ねた場合のため）
        var currentPlayer = StageManager.Instance.CurrentPlayer;
        if (currentPlayer != null) player = currentPlayer;

        StageManager.Instance.OnPlayerSpawned
            .Subscribe(p => player = p)
            .AddTo(this);
    }

    /// <summary>
    /// 位置を、始点 → 終点の線に投影した割合（0〜1）にする。
    /// </summary>
    private float CalculateProgress(Vector2 position)
    {
        Vector2 a = start.position;
        Vector2 ab = (Vector2)end.position - a;
        float lengthSquared = ab.sqrMagnitude;
        if (lengthSquared < 0.0001f) return 0f;

        return Mathf.Clamp01(Vector2.Dot(position - a, ab) / lengthSquared);
    }

    private void OnDrawGizmos()
    {
        if (start == null || end == null) return;

        // シーン画面だけに出る目印（始点は暗い色、終点は明るい色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start.position, end.position);
        Gizmos.color = new Color(0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(start.position, 0.3f);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(end.position, 0.3f);
    }
}
