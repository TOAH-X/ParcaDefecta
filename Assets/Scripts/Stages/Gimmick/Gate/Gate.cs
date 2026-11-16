using UnityEngine;
using R3;
using ParcaDefecta.System;

/// <summary>
/// 開閉する扉。OnToggleGate で「開けて(true)」「閉めて(false)」の指示を受けて動く。
/// 呼ぶ側はスイッチに限らない（Inspector の UnityEvent などでつなぐ）。
///
/// 指示は数で管理する：開けての指示が 1 つ以上残っていれば開く。
/// そのため呼ぶ側は true と false を必ず対で送ること（true を続けて送ると数がずれて閉まらなくなる）。
/// </summary>
public class Gate : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private Vector2 openOffset = new Vector2(0, 2f); // 上にどれくらい動くか
    [SerializeField] private float moveDuration = 0.5f;             // 開閉にかかる時間

    private Vector3 closedPosition;
    private Vector3 openPosition;

    // 内部状態
    private float currentProgress = 0f; // 0 (閉) ～ 1 (開)
    private bool isTargetOpen = false;
    private int activeSourceCount = 0;  // 現在「開けて」の指示を出している数

    void Start()
    {
        // 初期位置を保存
        closedPosition = transform.position;
        openPosition = closedPosition + (Vector3)openOffset;

        if (TimeManager.Instance == null) return;

        // PlayerMoverと同様にTimeManagerのOnTickを購読して更新
        TimeManager.Instance.OnTick
            .Subscribe(dt => UpdateMovement(dt))
            .AddTo(this);
    }

    /// <summary>
    /// 開閉の指示を受ける。スイッチなどの UnityEvent から呼ばれる
    /// </summary>
    /// <param name="isOpen">true で開けて、false で閉めて</param>
    public void OnToggleGate(bool isOpen)
    {
        if (isOpen) activeSourceCount++;
        else activeSourceCount--;

        // 1つでも「開けて」の指示があれば開ける
        isTargetOpen = activeSourceCount > 0;
    }

    /// <summary>
    /// ゲートの移動処理（TimeManagerから呼ばれる）
    /// </summary>
    private void UpdateMovement(float dt)
    {
        // 目標値（開なら1, 閉なら0）
        float targetValue = isTargetOpen ? 1f : 0f;

        // 現在の進捗を目標値に近づける（ポーズ中ならdtは0なので動かない）
        if (!Mathf.Approximately(currentProgress, targetValue))
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetValue, dt / moveDuration);

            // 線形補間（Lerp）で座標を更新
            transform.position = Vector3.Lerp(closedPosition, openPosition, currentProgress);
        }
    }
}
