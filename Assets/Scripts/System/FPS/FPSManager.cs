using UnityEngine;

public class FPSManager : Singleton<FPSManager>
{
    [SerializeField] private int targetFrameRate = 60;

    protected override void Awake()
    {
        base.Awake();

        // VSync（垂直同期）をオフにしないとtargetFrameRateが適用されない場合があるため設定
        QualitySettings.vSyncCount = 0;
        // FPSを固定
        Application.targetFrameRate = targetFrameRate;
    }
}
