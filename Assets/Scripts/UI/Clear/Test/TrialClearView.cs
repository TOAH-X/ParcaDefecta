using UnityEngine;
using R3;
using ParcaDefecta.System;

/// <summary>
/// 試験版のクリア画面。本番では置かない（本番にクリアという考え方があるかは未定）。
/// GameFlowManager が「次のステージが無い」(OnStagesEnded) を知らせたら、ゲームを止めて画面を出す。
/// 止め方はポーズメニューと同じ PauseManager.Pause()（印を立て、timeScale を 0 にして物理も止める）。
/// 「タイトルへ」ボタンは同じ Canvas の GameFlowUIBridge.ReturnToTitle を呼ぶ。
/// ポーズはシーンが変わったときに TimeManager が解く。
/// </summary>
public class TrialClearView : MonoBehaviour
{
    [SerializeField, Tooltip("表示・非表示を切り替える画面のまとまり")]
    private GameObject panel;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        GameFlowManager.Instance.OnStagesEnded
            .Subscribe(_ => Show())
            .AddTo(this);
    }

    private void Show()
    {
        // 後ろのゲームを止める（操作も物理も止まる）
        if (PauseManager.Instance != null) PauseManager.Instance.Pause();
        if (panel != null) panel.SetActive(true);
    }
}
