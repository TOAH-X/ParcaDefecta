using UnityEngine;
using R3;
using ParcaDefecta.System;

public class SwitchGate : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private Vector2 openOffset = new Vector2(0, 2f); // 上にどれくらい動くか
    [SerializeField] private float moveDuration = 0.5f;             // 開閉にかかる時間

    private Vector3 closedPosition;
    private Vector3 openPosition;

    // 内部状態
    private float currentProgress = 0f; // 0 (閉) ～ 1 (開)
    private bool isTargetOpen = false;
    private int activeSwitchCount = 0;  // 現在このゲートを起動しているスイッチの数

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
    /// スイッチの状態が変化した時にUnityEventから呼び出される
    /// </summary>
    /// <param name="isOn">スイッチがONかOFFか</param>
    public void OnToggleGate(bool isOn)
    {
        if (isOn) activeSwitchCount++;
        else activeSwitchCount--;

        // 1つでもスイッチがONならゲートを開けるターゲットにする
        isTargetOpen = activeSwitchCount > 0;
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
