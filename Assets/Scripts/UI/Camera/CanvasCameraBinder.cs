using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 常駐（DontDestroyOnLoad）Canvas用のカメラ再バインド部品。
/// Screen Space - Camera のCanvasはシーン遷移でworldCameraへの参照が切れるため、
/// シーンロードのたびにCamera.main（MainCameraタグのカメラ）を差し直す。
/// カメラが見つからない間はOverlayモードに退避して表示を維持する。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasCameraBinder : MonoBehaviour
{
    [SerializeField] private float planeDistance = 1f;

    private Canvas targetCanvas;

    private void Awake()
    {
        targetCanvas = GetComponent<Canvas>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Bind();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Bind();
    }

    /// <summary>
    /// 現在のシーンのMain CameraをworldCameraに割り当てる。
    /// </summary>
    private void Bind()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            targetCanvas.worldCamera = mainCamera;
            targetCanvas.planeDistance = planeDistance;
        }
        else
        {
            // カメラ未発見時はOverlayに退避（次のsceneLoadedで再試行される）
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }
}
