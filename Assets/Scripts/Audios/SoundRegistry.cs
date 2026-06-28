using System.Collections.Generic;
using UnityEngine;

public class SoundRegistry : MonoBehaviour
{
    private readonly Dictionary<string, AudioClip> bgmClips = new();
    private readonly Dictionary<string, AudioClip> seClips = new();

    public void RegisterBgm(string key, AudioClip clip)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("SoundRegistry: BGMキーが空です。");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning($"SoundRegistry: BGM '{key}' のクリップが null です。");
            return;
        }

        bgmClips[key] = clip;
    }

    public void RegisterSe(string key, AudioClip clip)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("SoundRegistry: SEキーが空です。");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning($"SoundRegistry: SE '{key}' のクリップが null です。");
            return;
        }

        seClips[key] = clip;
    }

    public bool TryGetBgm(string key, out AudioClip clip)
    {
        return bgmClips.TryGetValue(key, out clip);
    }

    public bool TryGetSe(string key, out AudioClip clip)
    {
        return seClips.TryGetValue(key, out clip);
    }
}
