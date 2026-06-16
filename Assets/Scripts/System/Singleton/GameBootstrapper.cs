using UnityEngine;

namespace ParcaDefecta.System
{
    /// <summary>
    /// ゲーム起動時に一度だけ実行される初期化クラス。
    /// </summary>
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // 各マネージャーの Instance を参照することで、Singleton.cs 側のロジックで
            // 「個別のGameObject作成」と「共通の親への紐付け」を自動実行させます。
            _ = TimeManager.Instance;
            _ = PauseManager.Instance;
            _ = FPSManager.Instance;
            _ = SceneLoader.Instance;
            _ = SoundManager.Instance;
            _ = StageManager.Instance;
        }
    }
}