using UnityEngine;

/// <summary>
/// マネージャー用のシングルトンの土台。Instance を読むと、無ければ探すか作る（GameBootstrapper が起動時に全部作る）。
///
/// 再生時にスクリプトを読み込み直さない設定（Enter Play Mode Options で Reload Domain がオフ）を使うため、
/// static な値は前の再生の値が残る。決まり: static な値を足したら、起動時に空に戻す処理も一緒に書く
/// （ここでは GameBootstrapper が instance を戻し、終了中の印は SingletonLifecycle が戻す）。
///
/// 終了中（アプリの終了・エディタで再生を止めるとき）は、Instance を読んでも新しく作らない。
/// 消えていく途中の物の OnDisable / OnDestroy が Instance を読むと、マネージャーが作り直されて残ってしまうため。
/// そのため終了中は null が返ることがある。OnDisable / OnDestroy で読むときは null を確かめること。
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private const string ContainerName = "GameManagerSystems";

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                // 終了中は作らない（探しもしない）
                if (SingletonLifecycle.IsQuitting) return null;

                instance = (T)Object.FindFirstObjectByType(typeof(T));
                if (instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();

                    // 生成時に自動で親に紐付ける
                    SetupParent(obj);
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// 前の再生の instance を忘れる。GameBootstrapper が起動時に呼ぶ（Reload Domain オフのため）。
    /// </summary>
    internal static void ResetStatic()
    {
        instance = null;
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;

            // シーンに最初から置いてある場合も親に紐付ける
            SetupParent(gameObject);

            // 親が DontDestroyOnLoad なら子も自動的に維持されるが、
            // 念のためルートオブジェクトに対してのみ実行する
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private static void SetupParent(GameObject child)
    {
        GameObject container = GameObject.Find(ContainerName);
        if (container == null)
        {
            container = new GameObject(ContainerName);
            DontDestroyOnLoad(container);
        }
        child.transform.SetParent(container.transform);
    }
}

/// <summary>
/// シングルトン全体で共有する「終了中か」の印。Singleton&lt;T&gt; は型ごとに static が分かれるので、ここにまとめる。
/// </summary>
public static class SingletonLifecycle
{
    /// <summary>アプリの終了中、またはエディタで再生を止めている途中なら true</summary>
    public static bool IsQuitting { get; private set; }

    // 再生のたびに最初に呼ばれる（Reload Domain オフでも呼ばれる）。前の再生の印を戻し、登録し直す
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        IsQuitting = false;

        Application.quitting -= HandleQuitting;
        Application.quitting += HandleQuitting;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        UnityEditor.EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#endif
    }

    private static void HandleQuitting()
    {
        IsQuitting = true;
    }

#if UNITY_EDITOR
    private static void HandlePlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) IsQuitting = true;
    }
#endif
}
