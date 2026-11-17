using UnityEngine;
using UnityEngine.Scripting;

[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class FPSManager : Singleton<FPSManager>
{
    [SerializeField] private int targetFrameRate = 60;

    protected override void Awake()
    {
        base.Awake();

        // VSync（垂直同期）をオンにする。オフだとビルドで画面の描き替えの途中に次の絵が届き、
        // カメラが動くと横線を境に絵がずれて歪んで見える（ティアリング）。
        // PC ではモニターのリフレッシュレートに合わせて描き、下の targetFrameRate は無視される
        QualitySettings.vSyncCount = 1;
        // VSync が効かない環境（スマホなど）では、この値でFPSを固定する
        Application.targetFrameRate = targetFrameRate;
    }
}
