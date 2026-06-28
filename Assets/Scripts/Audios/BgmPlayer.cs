using System.Collections.Generic;
using UnityEngine;

public class BgmPlayer : MonoBehaviour
{
    [SerializeField] private int maxBgmSources = 2;

    private readonly List<AudioSource> sources = new();
    private AudioSource currentSource;
    private AudioSource nextSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        for (int i = 0; i < maxBgmSources; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0f;
            sources.Add(source);
        }

        currentSource = sources[0];
        nextSource = sources[1];
    }

    public void Play(string key, AudioClip clip, float fadeDuration = 0f, bool loop = true)
    {
        if (clip == null)
        {
            Debug.LogWarning($"BgmPlayer: 再生対象のクリップが null です。キー='{key}'");
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        foreach (var source in sources)
        {
            source.loop = loop;
        }

        fadeRoutine = StartCoroutine(FadeToClipRoutine(clip, fadeDuration));
    }

    public void Stop(float fadeDuration = 0f)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeOutRoutine(fadeDuration));
    }

    private System.Collections.IEnumerator FadeToClipRoutine(AudioClip clip, float fadeDuration)
    {
        nextSource.Stop();
        nextSource.clip = clip;
        nextSource.Play();

        float elapsed = 0f;
        float startVolume = currentSource.volume;
        float targetVolume = 1f;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / Mathf.Max(fadeDuration, 0.0001f);
            currentSource.volume = Mathf.Lerp(startVolume, 0f, t);
            nextSource.volume = Mathf.Lerp(0f, targetVolume, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentSource.Stop();
        currentSource.volume = 0f;
        currentSource.clip = null;

        nextSource.volume = 1f;

        var temp = currentSource;
        currentSource = nextSource;
        nextSource = temp;
        fadeRoutine = null;
    }

    private System.Collections.IEnumerator FadeOutRoutine(float fadeDuration)
    {
        float elapsed = 0f;
        float startVolume = currentSource.volume;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / Mathf.Max(fadeDuration, 0.0001f);
            currentSource.volume = Mathf.Lerp(startVolume, 0f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentSource.Stop();
        currentSource.volume = 0f;
        currentSource.clip = null;
        fadeRoutine = null;
    }
}
