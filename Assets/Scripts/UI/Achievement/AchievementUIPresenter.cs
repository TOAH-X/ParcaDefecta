using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 実績 UI の Presenter 層
/// Model（AchievementManager）からのイベントをリッスンし、
/// キュー管理して View（プレハブ）を順番に表示する
/// Canvas にアタッチして使用
/// </summary>
public class AchievementUIPresenter : MonoBehaviour
{
    [SerializeField] private GameObject achievementNotificationPrefab;
    [SerializeField] private Transform notificationContainer;
    [SerializeField] private float displayDuration = 3f;

    private Queue<AchievementEntry> notificationQueue = new Queue<AchievementEntry>();
    private bool isDisplaying = false;

    private void OnEnable()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
        }
    }

    private void OnDisable()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        }
    }

    private void OnAchievementUnlocked(AchievementEntry achievement)
    {
        notificationQueue.Enqueue(achievement);
        _ = ProcessQueueAsync();
    }

    private async UniTaskVoid ProcessQueueAsync()
    {
        while (notificationQueue.Count > 0)
        {
            if (isDisplaying)
            {
                // 現在表示中なら次のフレームで再度チェック
                await UniTask.Delay(100);
                continue;
            }

            var achievement = notificationQueue.Dequeue();
            await ShowNotificationAsync(achievement);
        }
    }

    private async UniTask ShowNotificationAsync(AchievementEntry achievement)
    {
        isDisplaying = true;

        // プレハブを親なしでインスタンス化
        if (achievementNotificationPrefab == null)
        {
            Debug.LogWarning("[AchievementUIPresenter] achievementNotificationPrefab が設定されていません");
            isDisplaying = false;
            return;
        }

        GameObject notificationGameObject = Instantiate(achievementNotificationPrefab);

        // 親を設定
        var container = notificationContainer ?? transform;
        if (notificationGameObject != null && container != null)
        {
            notificationGameObject.transform.SetParent(container, false);
        }

        // View コンポーネントを取得
        var notificationView = notificationGameObject.GetComponent<AchievementNotificationView>();
        if (notificationView == null)
        {
            Debug.LogWarning("[AchievementUIPresenter] AchievementNotificationView コンポーネントが見つかりません");
            Destroy(notificationGameObject);
            isDisplaying = false;
            return;
        }

        // View に表示指示
        _ = notificationView.Show(achievement, displayDuration);

        // 表示期間 + フェード時間待機
        await UniTask.Delay((int)((displayDuration + 0.6f) * 1000));

        // クリーンアップ
        Destroy(notificationGameObject);

        isDisplaying = false;
    }
}
