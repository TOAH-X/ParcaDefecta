using UnityEngine;

/// <summary>
/// ボタンから GameFlowManager（ゲーム進行）を呼ぶための仲介クラス。
/// GameFlowManager はシーンの外にいるシングルトンで、ボタンの OnClick から直接選べないため、この部品をシーンに置いて呼ぶ。
/// ゲーム進行の操作が増えたら、ここに関数を足す（ボタンごとにスクリプトを増やさない）。
/// シーンを移るだけのボタンは SceneLoadButton を使う。
/// </summary>
public class GameFlowUIBridge : MonoBehaviour
{
    /// <summary>
    /// ゲームを始める。ゲームのシーンにいなければ、ゲームのシーンへ移ってからステージを置く。
    /// </summary>
    public void StartGame()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartGame();
        }
    }

    /// <summary>
    /// 現在のステージを最初からやり直す。
    /// </summary>
    public void ReloadCurrentStage()
    {
        // 必要に応じてここでポーズ解除やSE再生などの演出を挟むことも可能です
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ReloadCurrentStage();
        }
    }
}
