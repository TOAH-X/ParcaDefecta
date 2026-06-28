using System;
using UnityEngine;

[Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
    public bool loop;
}

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    [SerializeField] private SoundEntry[] bgmEntries = Array.Empty<SoundEntry>();
    [SerializeField] private SoundEntry[] seEntries = Array.Empty<SoundEntry>();

    public bool TryGetBgm(string key, out AudioClip clip, out bool loop)
    {
        return TryGetClip(bgmEntries, key, out clip, out loop);
    }

    public bool TryGetSe(string key, out AudioClip clip, out bool loop)
    {
        return TryGetClip(seEntries, key, out clip, out loop);
    }

    private static bool TryGetClip(SoundEntry[] entries, string key, out AudioClip clip, out bool loop)
    {
        clip = null;
        loop = false;

        foreach (var entry in entries)
        {
            if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
            {
                clip = entry.clip;
                loop = entry.loop;
                return clip != null;
            }
        }

        return false;
    }
}
