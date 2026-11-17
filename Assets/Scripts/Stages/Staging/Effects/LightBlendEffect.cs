using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 数字（0〜1）を読んで、指定したライトの明るさと色を変える部品。
/// 数字の出どころ（StagingSource）は知らない。Inspector でつなぐ。
/// 描画の直前（LateUpdate）に毎フレーム読むので、処理の順番の設定は要らない。
///
/// 決まり: 1 つのライトを変える部品は 1 つだけにする（2 つあると毎フレーム奪い合う）。
/// このライトの Inspector に入れた明るさと色は、この部品の値で上書きされる。
/// </summary>
public class LightBlendEffect : MonoBehaviour
{
    [SerializeField, Tooltip("数字を出す部品")] private StagingSource source;
    [SerializeField, Tooltip("変えるライト")] private Light2D target;

    [Header("数字が 0 のとき")]
    [SerializeField, Min(0f)] private float fromIntensity = 0f;
    [SerializeField] private Color fromColor = Color.white;

    [Header("数字が 1 のとき")]
    [SerializeField, Min(0f)] private float toIntensity = 1f;
    [SerializeField] private Color toColor = Color.white;

    [Header("変化の形(横 = 数字、縦 = 0 のときの値から 1 のときの値への割合)")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private void LateUpdate()
    {
        if (source == null || target == null) return;

        float t = curve.Evaluate(source.Value);
        target.intensity = Mathf.Max(0f, Mathf.LerpUnclamped(fromIntensity, toIntensity, t));
        target.color = Color.LerpUnclamped(fromColor, toColor, t);
    }
}
