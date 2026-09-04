using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Scripting;

// 基底シングルトンを Singleton<T> と仮定して継承します
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class SceneLoader : Singleton<SceneLoader>
{
    /// <summary>
    /// 指定したシーンへ即座に遷移します
    /// </summary>
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 非同期でシーンをロードします（将来的にフェード演出などを追加しやすい）
    /// </summary>
    public void ChangeSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
