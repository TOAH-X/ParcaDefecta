using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// シーンの種類ごとの、実際のシーンの一覧表。
/// 行き先を変えるときはここだけを書き換える（例: 試験版と本番の切り替え）。
/// </summary>
[CreateAssetMenu(fileName = "SceneDatabase", menuName = "SceneDatabase")]
public class SceneDatabase : ScriptableObject
{
    /// <summary>
    /// 1 行分。シーンのファイルを選ぶと、シーン名が自動で写される。
    /// ビルドではシーンのファイル自体を参照できないので、実行時はシーン名を使う。
    /// </summary>
    [System.Serializable]
    public class SceneEntry
    {
        [SerializeField] private SceneType type;

        [SerializeField, Tooltip("シーンのファイルをドラッグして選ぶ（エディタ専用。名前を写すために使う）")]
        private Object sceneAsset;

        [SerializeField, Tooltip("自動で写されるシーン名。手で書き換えない")]
        private string sceneName;

        [SerializeField, Tooltip("このシーンで遊ぶステージ一覧。ステージを置くシーン（Game・Debug）だけ指定する")]
        private StageDatabase stageDatabase;

        public SceneType Type => type;
        public string SceneName => sceneName;
        public StageDatabase StageDatabase => stageDatabase;

#if UNITY_EDITOR
        public Object SceneAsset => sceneAsset;

        /// <summary>選んだシーンのファイルから名前を写す。名前が変わっていれば true</summary>
        public bool SyncSceneName()
        {
            string name = sceneAsset != null ? sceneAsset.name : string.Empty;
            if (sceneName == name) return false;
            sceneName = name;
            return true;
        }
#endif
    }

    [SerializeField] private List<SceneEntry> scenes = new List<SceneEntry>();

    /// <summary>
    /// 種類に対応するシーン名を取得する。登録が無いか、シーンが未設定なら false。
    /// </summary>
    public bool TryGetSceneName(SceneType type, out string sceneName)
    {
        var entry = scenes.FirstOrDefault(s => s.Type == type);
        sceneName = entry?.SceneName;
        return !string.IsNullOrEmpty(sceneName);
    }

    /// <summary>
    /// シーン名に対応する種類を取得する。登録が無ければ false。
    /// </summary>
    public bool TryGetSceneType(string sceneName, out SceneType type)
    {
        var entry = scenes.FirstOrDefault(s => !string.IsNullOrEmpty(s.SceneName) && s.SceneName == sceneName);
        type = entry != null ? entry.Type : default;
        return entry != null;
    }

    /// <summary>
    /// シーン名に対応するステージ一覧を取得する。登録が無いか、一覧が未指定なら false。
    /// </summary>
    public bool TryGetStageDatabase(string sceneName, out StageDatabase stageDatabase)
    {
        var entry = scenes.FirstOrDefault(s => !string.IsNullOrEmpty(s.SceneName) && s.SceneName == sceneName);
        stageDatabase = entry?.StageDatabase;
        return stageDatabase != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // シーンのファイルから名前を写す（シーンの名前を変えても追いかける）
        foreach (var entry in scenes)
        {
            if (entry.SyncSceneName()) UnityEditor.EditorUtility.SetDirty(this);
        }

        // 同じ種類の重複
        foreach (var dup in scenes.GroupBy(s => s.Type).Where(g => g.Count() > 1))
        {
            Debug.LogWarning($"SceneDatabase: 種類 '{dup.Key}' が重複しています。1 つだけにしてください。", this);
        }

        var buildScenes = UnityEditor.EditorBuildSettings.scenes;
        foreach (var entry in scenes)
        {
            // ステージ一覧の指定（ステージを置くシーンだけに必要）
            bool isStageScene = entry.Type == SceneType.Game || entry.Type == SceneType.Debug;
            if (isStageScene && entry.StageDatabase == null)
            {
                Debug.LogWarning($"SceneDatabase: 種類 '{entry.Type}' にステージ一覧(StageDatabase)が指定されていません。", this);
            }
            else if (!isStageScene && entry.StageDatabase != null)
            {
                Debug.LogWarning($"SceneDatabase: 種類 '{entry.Type}' はステージを置かないシーンなので、ステージ一覧は使われません。", this);
            }

            // シーンの未設定
            if (entry.SceneAsset == null)
            {
                Debug.LogWarning($"SceneDatabase: 種類 '{entry.Type}' のシーンが設定されていません。", this);
                continue;
            }

            // シーン以外のファイル
            if (!(entry.SceneAsset is UnityEditor.SceneAsset))
            {
                Debug.LogWarning($"SceneDatabase: 種類 '{entry.Type}' にシーン以外のファイル '{entry.SceneAsset.name}' が設定されています。", this);
                continue;
            }

            // ビルド設定に入っていない（入っていないシーンは実行時に読み込めない）
            string path = UnityEditor.AssetDatabase.GetAssetPath(entry.SceneAsset);
            bool inBuild = buildScenes.Any(s => s.enabled && s.path == path);
            if (!inBuild)
            {
                Debug.LogWarning($"SceneDatabase: シーン '{entry.SceneName}'（種類 '{entry.Type}'）がビルド設定に入っていないか、無効になっています。", this);
            }
        }
    }
#endif
}
