using UnityEngine;
using R3;

namespace ParcaDefecta.System
{
    /// <summary>
    /// UI側にアタッチし、TimeManagerの状態を監視して自分自身の表示・非表示を切り替えます。
    /// </summary>
    public class PauseUIObserver : MonoBehaviour
    {
        /// <summary>
        /// ポーズ状態を切り替えます。
        /// ポーズ中なら再開、通常時ならポーズ処理を実行します。
        /// </summary>
        public void TogglePause()
        {
            if (TimeManager.Instance == null) return;

            if (TimeManager.Instance.IsPaused.Value)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        /// <summary>
        /// UIのボタンなどから呼び出して、ゲームを再開（ポーズ解除）します。
        /// </summary>
        public void ResumeGame()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.Resume();
            }
        }

        /// <summary>
        /// UIのボタンなどから呼び出して、ゲームをポーズします。
        /// </summary>
        public void PauseGame()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.Pause();
            }
        }
    }
}