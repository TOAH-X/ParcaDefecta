using UnityEngine;
using UnityEngine.InputSystem;

namespace ParcaDefecta.System
{
    /// <summary>
    /// ポーズメニューの表示や入力を管理するクラス。
    /// UIボタンからのアクセスを容易にするため、シングルトンとして実装します。
    /// </summary>
    public class PauseManager : Singleton<PauseManager>
    {
        private void Start()
        {
            // ゲーム開始時はレジューム（解除）状態にする
            Resume();
        }

        void Update()
        {
            // TimeManagerのインスタンスが存在しない場合は処理をスキップ
            if (TimeManager.Instance == null) return;

            /*
            // ポーズの切り替え（Escキー） 
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // TimeManager側のReactivePropertyのValueを参照
                if (TimeManager.Instance.IsPaused.Value)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
            */
        }

        public void Resume()
        {
            Debug.Log("ゲーム再開");
            if (TimeManager.Instance != null)
                TimeManager.Instance.SetMasterPause(false);

            Time.timeScale = 1f;
        }

        public void Pause()
        {
            Debug.Log("ゲーム一時停止");
            if (TimeManager.Instance != null)
                TimeManager.Instance.SetMasterPause(true);

            Time.timeScale = 0f;

            // ポーズ中はカーソルを表示して操作可能にする
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}