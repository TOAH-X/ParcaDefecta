using UnityEngine;
using UnityEngine.InputSystem;

// 指定のキーを入力するとゲームを終了します
// Unityのプロジェクトフォルダ内に入れるだけで機能します
// このプロジェクトは新しい Input System だけを使う設定なので、古い Input.GetKeyDown は使わない（呼ぶと毎フレームエラーになる）
public class ApplicationQuitter : MonoBehaviour
{
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            QuitGame();
        }
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
        Application.Quit();
#endif
    }

    // ゲームの開始時にインスタンスを作成します
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateInstanceOnLoad()
    {
        var instance = new GameObject(nameof(ApplicationQuitter), typeof(ApplicationQuitter));
        DontDestroyOnLoad(instance);
    }
}