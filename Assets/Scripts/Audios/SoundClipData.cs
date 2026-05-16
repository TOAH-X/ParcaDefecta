using UnityEngine;

// サウンドのタイプを区別するためのEnum
public enum SoundType
{
    BGM,
    SE
}

/// <summary>
/// 個々のオーディオクリップとその再生設定を保持するScriptableObject。
/// Unityエディタ上で新しいサウンドアセットとして作成できます。
/// </summary>
[CreateAssetMenu(fileName = "NewSoundClipData", menuName = "Audio/Sound Clip Data")]
public class SoundClipData : ScriptableObject
{
    [Tooltip("再生するオーディオクリップ")]
    public AudioClip clip;

    [Tooltip("サウンドのタイプ (BGMまたはSE)")]
    public SoundType soundType;

    [Range(0, 1)]
    [Tooltip("このサウンド個別の音量倍率。マスター音量とカテゴリ音量に加えて適用されます。")]
    public float volumeMultiplier = 1.0f;

    [Header("SE Specific Settings")]
    [Range(0.5f, 1.5f)]
    [Tooltip("SEのピッチの最小値 (ランダム再生用)。SEタイプの場合のみ適用されます。")]
    public float pitchMin = 1.0f;

    [Range(0.5f, 1.5f)]
    [Tooltip("SEのピッチの最大値 (ランダム再生用)。SEタイプの場合のみ適用されます。")]
    public float pitchMax = 1.0f;

    [Header("BGM Specific Settings")]
    [Tooltip("BGMの場合、ループ再生するかどうか。BGMタイプの場合のみ適用されます。")]
    public bool loop = true; // BGMは通常ループするのでデフォルトtrue
}