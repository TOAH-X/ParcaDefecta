using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private string soundDatabaseAddress = "SoundDatabase";
    [SerializeField] private SoundDatabase soundDatabase;
    [SerializeField] private BgmPlayer bgmPlayer;
    [SerializeField] private SePlayer sePlayer;

    protected override void Awake()
    {
        base.Awake();

        if (bgmPlayer == null)
        {
            bgmPlayer = GetComponent<BgmPlayer>();
        }

        if (sePlayer == null)
        {
            sePlayer = GetComponent<SePlayer>();
        }

        if (bgmPlayer == null)
        {
            bgmPlayer = gameObject.AddComponent<BgmPlayer>();
        }

        if (sePlayer == null)
        {
            sePlayer = gameObject.AddComponent<SePlayer>();
        }

        _ = InitializeAsync(this.GetCancellationTokenOnDestroy());
    }

    public void SetSoundDatabase(SoundDatabase database)
    {
        soundDatabase = database;
    }

    private async UniTaskVoid InitializeAsync(CancellationToken token)
    {
        if (soundDatabase != null)
        {
            return;
        }

        try
        {
            var database = await Addressables
                .LoadAssetAsync<SoundDatabase>(soundDatabaseAddress)
                .ToUniTask(cancellationToken: token);

            if (database == null)
            {
                Debug.LogError($"[SoundManager] SoundDatabase '{soundDatabaseAddress}' が見つかりません。Addressables設定を確認してください。");
                return;
            }

            soundDatabase = database;
            Debug.Log($"[SoundManager] SoundDatabase '{soundDatabaseAddress}' を読み込みました。");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[SoundManager] 初期化がキャンセルされました。");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SoundManager] SoundDatabase の読み込みでエラーが発生しました: {ex.Message}");
        }
    }

    public void PlayBgm(string key, float fadeDuration = 0f)
    {
        if (soundDatabase != null && soundDatabase.TryGetBgm(key, out var clip, out var loop))
        {
            bgmPlayer.Play(key, clip, fadeDuration, loop);
        }
        else
        {
            Debug.LogWarning($"SoundManager: BGM '{key}' が登録されていません。");
        }
    }

    public void StopBgm(float fadeDuration = 0f)
    {
        bgmPlayer.Stop(fadeDuration);
    }

    public void PlaySe(string key)
    {
        if (soundDatabase != null && soundDatabase.TryGetSe(key, out var clip, out var loop))
        {
            sePlayer.Play(key, clip, loop);
        }
        else
        {
            Debug.LogWarning($"SoundManager: SE '{key}' が登録されていません。");
        }
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            bgmPlayer.enabled = false;
            sePlayer.enabled = false;
        }
        else
        {
            bgmPlayer.enabled = true;
            sePlayer.enabled = true;
        }
    }
}