using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Scripting;
using Cysharp.Threading.Tasks;

/// <summary>
/// シーンに依存しない常駐Canvasの設置係。
/// 起動時に対象のCanvasプレハブをAddressablesからロードし、自分の子として配置する。
/// 親のGameManagerSystemsがDontDestroyOnLoadのため、配置したCanvasはシーンをまたいで生存する。
/// Canvasの中身(Presenter等)には関知しない。
/// </summary>
[Preserve] // リフレクション経由で生成されるためストリッピング対象から除外
public class CanvasManager : Singleton<CanvasManager>
{
    // 常駐させるCanvasプレハブのAddressablesアドレス一覧
    // 追加したいCanvasができたらここにアドレスを追記する
    private static readonly string[] CanvasAddresses =
    {
        AddressableKeys.NotificationCanvas,
    };

    protected override void Awake()
    {
        base.Awake();
        _ = InitializeAsync(this.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// 全ての常駐Canvasプレハブをロードして子として生成する。
    /// </summary>
    private async UniTaskVoid InitializeAsync(CancellationToken token)
    {
        foreach (var address in CanvasAddresses)
        {
            try
            {
                var prefab = await Addressables
                    .LoadAssetAsync<GameObject>(address)
                    .ToUniTask(cancellationToken: token);

                if (prefab == null)
                {
                    Debug.LogError($"[CanvasManager] Canvasプレハブ '{address}' が見つかりません。Addressables設定を確認してください。");
                    continue;
                }

                Instantiate(prefab, transform);
                Debug.Log($"[CanvasManager] '{address}' を設置しました");
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("[CanvasManager] 初期化がキャンセルされました");
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CanvasManager] '{address}' のロードエラー: {ex.Message}");
            }
        }
    }
}
