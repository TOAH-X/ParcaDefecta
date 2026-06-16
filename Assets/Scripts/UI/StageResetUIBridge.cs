using UnityEngine;

/// <summary>
/// シーン上のUI（リトライボタンなど）からStageManagerへステージ再試行の命令を送る仲介クラス。
/// </summary>
public class StageResetUIBridge : MonoBehaviour
{
    /// <summary>
    /// UIボタンのOnClickイベントから呼び出すためのメソッド。
    /// </summary>
    public void OnResetButtonClick()
    {
        // 必要に応じてここでポーズ解除やSE再生などの演出を挟むことも可能です
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ReloadCurrentStage();
        }
    }
}