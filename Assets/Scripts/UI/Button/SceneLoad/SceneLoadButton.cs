using UnityEngine;

namespace ParcaDefecta.System.Scene
{
    /// <summary>
    /// UIのボタンからシーン遷移機能を呼び出すためのブリッジクラス。
    /// 遷移先と演出の種類は Inspector で設定する。
    /// </summary>
    public class SceneLoadButton : MonoBehaviour
    {
        [SerializeField, Tooltip("遷移先のシーン名")]
        private string sceneName;

        [SerializeField, Tooltip("遷移時の演出の種類")]
        private TransitionManager.TransitionType transitionType = TransitionManager.TransitionType.Fade;

        /// <summary>
        /// 設定した演出付きでシーンへ遷移します。ButtonのOnClickイベントに登録して使用します。
        /// </summary>
        public void LoadScene()
        {
            SceneLoader.Instance.ChangeScene(sceneName, transitionType);
        }
    }
}
