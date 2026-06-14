using UnityEngine;

/// <summary>
/// シーン上のUI（ボタンなど）から、プレハブとして生成されたPlayerインスタンスへ命令を届ける仲介クラス。
/// </summary>
public class PlayerUIBridge : MonoBehaviour
{
    // 左右移動の状態（UIボタンの押し続けを管理）
    private float _uiHorizontalInput = 0f;

    // --- UIボタンの OnClick イベントから呼び出す ---

    public void OnJumpClick() => Player.Instance?.OnJumpButtonClick();

    public void OnTeleportationClick() => Player.Instance?.OnTeleportationButtonClick();

    public void OnSeparationClick() => Player.Instance?.OnSeparationButtonClick();

    // --- 移動ボタン用（EventTriggerのPointerDown / PointerUpから呼び出す） ---

    public void OnMoveLeftDown() => _uiHorizontalInput = -1f;
    public void OnMoveRightDown() => _uiHorizontalInput = 1f;
    public void OnMoveStop() => _uiHorizontalInput = 0f;

    private void Update()
    {
        // Playerが存在しない、またはUIからの移動入力がない場合はスキップ
        if (Player.Instance == null || _uiHorizontalInput == 0) return;

        // UIの入力状態をPlayerに反映
        if (_uiHorizontalInput < 0)
        {
            Player.Instance.OnLeftMoveButtonDown();
        }
        else if (_uiHorizontalInput > 0)
        {
            Player.Instance.OnRightMoveButtonDown();
        }
    }
}