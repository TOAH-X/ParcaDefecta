using UnityEngine;

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