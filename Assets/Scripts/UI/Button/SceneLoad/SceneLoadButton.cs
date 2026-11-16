using UnityEngine;

namespace ParcaDefecta.System.Scene
{
    /// <summary>
    /// UIのボタンからシーン遷移機能を呼び出すためのブリッジクラス。
    /// 遷移先（シーンの種類）と演出の種類は Inspector で設定する。実際のシーンは SceneDatabase で決まる。
    /// ゲームを始めるボタンは、シーンを移るだけではないので GameFlowUIBridge を使う。
    /// </summary>
    public class SceneLoadButton : MonoBehaviour
    {
        [SerializeField, Tooltip("遷移先のシーンの種類")]
        private SceneType sceneType = SceneType.Title;

        [SerializeField, Tooltip("遷移時の演出の種類")]
        private TransitionManager.TransitionType transitionType = TransitionManager.TransitionType.Fade;

        /// <summary>
        /// 設定した演出付きでシーンへ遷移します。ButtonのOnClickイベントに登録して使用します。
        /// </summary>
        public void LoadScene()
        {
            SceneLoader.Instance.ChangeScene(sceneType, transitionType);
        }
    }
}
