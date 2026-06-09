using UnityEngine;

namespace ParcaDefecta.System.Scene
{
    /// <summary>
    /// UIのボタンからシーン遷移機能を呼び出すためのブリッジクラス。
    /// </summary>
    public class SceneLoadButton : MonoBehaviour
    {
        [SerializeField, Tooltip("遷移先のシーン名")]
        private string sceneName;

        /// <summary>
        /// 指定したシーンへ即座に遷移します。ButtonのOnClickイベントに登録して使用します。
        /// </summary>
        public void LoadScene()
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            SceneLoader.Instance.ChangeScene(sceneName);
        }

        /// <summary>
        /// 非同期でシーンをロードします。
        /// </summary>
        public void LoadSceneAsync() => SceneLoader.Instance.ChangeSceneAsync(sceneName);
    }
}