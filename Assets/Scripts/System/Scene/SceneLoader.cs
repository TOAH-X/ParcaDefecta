using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Cysharp.Threading.Tasks;

/// <summary>
/// シーン遷移の窓口。シーン遷移は必ずトランジション演出を通る。
/// 遷移中に来た要求は無視する（多重ロード防止）。
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class SceneLoader : Singleton<SceneLoader>
{
    /// <summary>遷移中なら true。演出の開始から終了までを含む</summary>
    public bool IsChanging { get; private set; }

    /// <summary>
    /// 演出で画面を隠してからシーンをロードし、完了後に画面を開けます。完了まで待てます。
    /// </summary>
    public async UniTask ChangeSceneAsync(string sceneName, TransitionManager.TransitionType type = TransitionManager.TransitionType.Fade)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoader] シーン名が空のため遷移しません");
            return;
        }

        if (IsChanging)
        {
            Debug.LogWarning($"[SceneLoader] 遷移中のため '{sceneName}' への要求を無視します");
            return;
        }

        IsChanging = true;
        try
        {
            await TransitionManager.Instance.RunWithTransitionAsync(
                () => SceneManager.LoadSceneAsync(sceneName).ToUniTask(),
                type);
        }
        finally
        {
            IsChanging = false;
        }
    }

    /// <summary>
    /// ChangeSceneAsync の完了を待たない版。Button の OnClick など戻り値を扱えない場所から使います。
    /// </summary>
    public void ChangeScene(string sceneName, TransitionManager.TransitionType type = TransitionManager.TransitionType.Fade)
    {
        ChangeSceneAsync(sceneName, type).Forget();
    }
}
