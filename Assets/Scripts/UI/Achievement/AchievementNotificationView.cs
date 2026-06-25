using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;

/// <summary>
/// 実績通知パネルの表示を担当する View 層
/// Presenter からの指示に従い、UI を表示・非表示にするのみ
/// </summary>
public class AchievementNotificationView : MonoBehaviour
{
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image achievementIcon;
    [SerializeField] private TextMeshProUGUI achievementTitle;
    [SerializeField] private TextMeshProUGUI achievementDescription;
    [SerializeField] private float fadeDuration = 0.3f;

    private CancellationTokenSource cts;

    /// <summary>
    /// 実績を表示する
    /// </summary>
    public async UniTaskVoid Show(AchievementEntry achievement, float displayDuration)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        // UI 要素にデータをセット
        if (achievementIcon != null)
        {
            achievementIcon.sprite = achievement.icon;
        }

        if (achievementTitle != null)
        {
            achievementTitle.text = achievement.title;
        }

        if (achievementDescription != null)
        {
            achievementDescription.text = achievement.description;
        }

        // フェードイン
        await FadeAsync(0, 1, fadeDuration, cts.Token);

        // 表示を保つ
        await UniTask.Delay((int)(displayDuration * 1000), cancellationToken: cts.Token);

        // フェードアウト
        await FadeAsync(1, 0, fadeDuration, cts.Token);
    }

    public void Hide()
    {
        cts?.Cancel();
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0;
        }
    }

    private async UniTask FadeAsync(float from, float to, float duration, CancellationToken token)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            }
            await UniTask.Yield(cancellationToken: token);
        }
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = to;
        }
    }

    private void OnDestroy()
    {
        cts?.Cancel();
    }
}
