using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownDisplay : MonoBehaviour
{
    private enum SkillType
    {
        Teleportation,
        Separation
    }

    [Header("設定")]
    [SerializeField] private SkillType skillType;
    [SerializeField] private Image cooldownImage; // Fill Method が設定されたImage

    void Update()
    {
        // PlayerインスタンスまたはUI参照がない場合は処理しない
        if (Player.Instance == null || cooldownImage == null) return;

        float currentTimer;
        float coolTime;

        // 選択されたスキルの種類に応じて、Playerから値を取得
        switch (skillType)
        {
            case SkillType.Teleportation:
                currentTimer = Player.Instance.UseTeleportationTimer;
                coolTime = Player.Instance.TeleportationCoolTime;
                break;
            case SkillType.Separation:
                currentTimer = Player.Instance.UseSeparationTimer;
                coolTime = Player.Instance.SeparationCoolTime;
                break;
            default:
                return;
        }

        // クールタイムの進捗を計算 (残り時間 / 最大時間)
        // Timerが0に向かって減るため、fillAmountも1から0へ減少します
        if (coolTime > 0)
        {
            cooldownImage.fillAmount = 1 - currentTimer / coolTime;
        }
        else
        {
            cooldownImage.fillAmount = 1f;
        }
    }
}
