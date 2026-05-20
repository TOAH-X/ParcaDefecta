using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;
    [SerializeField] private AudioSource seSource;

    [Header("SE Settings")]
    [SerializeField] private int maxSameSECount = 3;
    private Dictionary<AudioClip, int> sePlayCountDict = new Dictionary<AudioClip, int>();

    [Header("Volume Settings")]
    [Range(0, 1)] public float masterVolume = 1.0f;
    [Range(0, 1)] public float bgmVolume = 0.5f;
    [Range(0, 1)] public float seVolume = 0.5f;

    private AudioSource currentBgmSource;
    private SoundClipData currentBgmClipData; // 現在再生中のBGMのSoundClipDataを保持
    private CancellationTokenSource fadeCts;

    protected override void Awake()
    {
        base.Awake();
        currentBgmSource = bgmSourceA;
    }


    #region BGM Methods

    /// <summary>
    /// BGMを再生します。fadeDurationが0より大きい場合、クロスフェードします。
    /// </summary>
    public void PlayBGM(AudioClip clip, float fadeDuration = 1.0f)
    {
        if (currentBgmSource.clip == clip) return;

        fadeCts?.Cancel();
        fadeCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        CrossFadeBGMAsync(clip, fadeDuration, fadeCts.Token).Forget();
    }

    private async UniTaskVoid CrossFadeBGMAsync(AudioClip nextClip, float duration, CancellationToken token)
    {
        AudioSource nextSource = (currentBgmSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
        AudioSource prevSource = currentBgmSource;

        nextSource.clip = nextClip;
        nextSource.volume = 0;
        nextSource.Play();

        float expiredTime = 0;
        float startPrevVolume = prevSource.volume;

        while (expiredTime < duration)
        {
            // ポーズ（timeScale=0）の影響を受けずにフェードを完了させる
            expiredTime += Time.unscaledDeltaTime;
            float t = expiredTime / duration;

            prevSource.volume = Mathf.Lerp(startPrevVolume, 0, t) * masterVolume * bgmVolume;
            nextSource.volume = Mathf.Lerp(0, 1.0f, t) * masterVolume * bgmVolume;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            if (token.IsCancellationRequested) return;
        }

        prevSource.Stop();
        prevSource.volume = 0;
        nextSource.volume = 1.0f * masterVolume * bgmVolume;
        currentBgmSource = nextSource;
    }

    #endregion

    #region SE Methods

    /// <summary>
    /// SEを再生します。同一クリップの同時再生数を制限します。
    /// </summary>
    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;

        if (!sePlayCountDict.ContainsKey(clip))
        {
            sePlayCountDict[clip] = 0;
        }

        if (sePlayCountDict[clip] < maxSameSECount)
        {
            sePlayCountDict[clip]++;
            PlaySEAndCountDown(clip).Forget();
        }
    }

    private async UniTaskVoid PlaySEAndCountDown(AudioClip clip)
    {
        seSource.PlayOneShot(clip, masterVolume * seVolume);

        // 再生が終わるまで待機（クリップの長さ分）
        await UniTask.Delay((int)(clip.length * 1000));

        if (sePlayCountDict.ContainsKey(clip))
        {
            sePlayCountDict[clip]--;
        }
    }

    #endregion

    /// <summary>
    /// 外部（設定画面など）から音量を更新した際に呼び出します
    /// </summary>
    public void UpdateVolume()
    {
        if (currentBgmSource != null)
        {
            // 現在再生中のBGMデータがあれば、そのvolumeMultiplierも考慮して音量を更新
            if (currentBgmClipData != null)
            {
                currentBgmSource.volume = currentBgmClipData.volumeMultiplier * masterVolume * bgmVolume;
            }
        }
    }
}