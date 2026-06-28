using System.Collections.Generic;
using UnityEngine;

public class SePlayer : MonoBehaviour
{
    [SerializeField] private int maxConcurrentSe = 8;

    private readonly List<AudioSource> sources = new();

    private void Awake()
    {
        for (int i = 0; i < maxConcurrentSe; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            sources.Add(source);
        }
    }

    public void Play(string key, AudioClip clip, bool loop = false)
    {
        if (clip == null)
        {
            Debug.LogWarning($"SePlayer: 再生対象のクリップが null です。キー='{key}'");
            return;
        }

        AudioSource availableSource = null;
        foreach (var source in sources)
        {
            if (!source.isPlaying)
            {
                availableSource = source;
                break;
            }
        }

        if (availableSource == null)
        {
            Debug.LogWarning("SePlayer: 同時再生上限に達しました。音を再生できませんでした。");
            return;
        }

        availableSource.loop = loop;
        availableSource.PlayOneShot(clip);
    }
}
