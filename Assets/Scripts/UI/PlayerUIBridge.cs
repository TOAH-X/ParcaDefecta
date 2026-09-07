using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// シーン上のUI（ボタンなど）から、プレハブとして生成されたPlayerインスタンスへ命令を届ける仲介クラス。
/// </summary>
public class PlayerUIBridge : MonoBehaviour
{
    // 左右移動の状態（UIボタンの押し続けを管理）
    // 本クラスはボタンごとに複数存在しうるため、0 は「離した瞬間」だけ Player に渡し、
    // 押している間だけ毎フレーム自分の値を渡す。0 を毎フレーム渡すと他の Bridge の入力を上書きしてしまう
    private float _uiHorizontalInput = 0f;

    // --- UIボタンの OnClick イベントから呼び出す ---

    public void OnJumpClick() => Player.Instance?.OnJumpButtonClick();

    public void OnTeleportationClick() => Player.Instance?.OnTeleportationButtonClick();

    public void OnSeparationClick() => Player.Instance?.OnSeparationButtonClick();

    // --- 移動ボタン用（EventTriggerのPointerDown / PointerUpから呼び出す） ---

    public void OnMoveLeftDown() => _uiHorizontalInput = -1f;
    public void OnMoveRightDown() => _uiHorizontalInput = 1f;
    public void OnMoveStop() => ClearMoveInput();

    private void Update()
    {
        // PointerUp を取りこぼしても押しっぱなしにならないよう、
        // ポインタが 1 つも押されていなければ移動入力を解除する
        if (_uiHorizontalInput != 0f && !IsAnyPointerPressed())
        {
            ClearMoveInput();
        }

        // 押していないときは何もしない（他の Bridge の入力を上書きしないため）
        if (_uiHorizontalInput == 0f) return;

        // Playerが存在しない場合はスキップ
        if (Player.Instance == null) return;

        // 押している間は毎フレーム渡す。Player が再生成された直後でも押しっぱなしの状態を引き継げる
        Player.Instance.SetUIHorizontalInput(_uiHorizontalInput);
    }

    // ウィンドウのフォーカスが外れると PointerUp が届かないため、ここで解除する
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ClearMoveInput();
    }

    private void OnDisable()
    {
        ClearMoveInput();
    }

    // 0 にして、その場で Player にも渡す（毎フレームは渡さない）
    private void ClearMoveInput()
    {
        _uiHorizontalInput = 0f;
        Player.Instance?.SetUIHorizontalInput(0f);
    }

    /// <summary>
    /// マウス・タッチ・ペンのいずれかが押されているか。
    /// </summary>
    private static bool IsAnyPointerPressed()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed) return true;

        var pen = Pen.current;
        if (pen != null && pen.tip.isPressed) return true;

        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
            {
                if (touch.isInProgress) return true;
            }
        }

        return false;
    }
}