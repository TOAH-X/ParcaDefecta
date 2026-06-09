using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // TimeManager側でリセットされるため、ここではUIの非表示処理など
            // 必要最低限の処理に留める（現在はResume内でTimeManagerを呼んでいるので、そのままでも可）
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
            TimeManager.Instance.SetMasterPause(false);

            Time.timeScale = 1f;
        }

        public void Pause()
        {
            Debug.Log("ゲーム一時停止");
            TimeManager.Instance.SetMasterPause(true);

            Time.timeScale = 0f;

            // ポーズ中はカーソルを表示して操作可能にする
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}