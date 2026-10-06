using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;

/// <summary>
/// 会話ウィンドウの View 層。立ち絵・名前・本文を表示し、本文は 1 文字ずつ送る。
/// Presenter からの指示に従って表示するだけで、会話の進行は判断しない。
/// 会話中はポーズで Time.timeScale が 0 になるので、文字送りは timeScale の影響を受けない時間で進める。
/// </summary>
public class DialogueView : MonoBehaviour
{
    [Header("ウィンドウ全体(会話中だけ有効にする)")]
    [SerializeField] private GameObject window;

    [Header("立ち絵(無い行では隠す)")]
    [SerializeField] private Image portrait;

    [Header("名前(空の行では枠ごと隠す)")]
    [SerializeField] private GameObject nameRoot;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("本文")]
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("送れる合図(本文を全部出したときだけ表示)")]
    [SerializeField] private GameObject advanceMark;

    [Header("文字送りの速さ(1 秒あたりの文字数。0 なら一度に全部出す)")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 30f;

    // 文字送りを飛ばして全部出す要求。ShowLineAsync が見て即座に終える
    private bool completeRequested;

    // Awake では閉じない。ウィンドウは非表示で保存してあり、このコンポーネントはウィンドウ自身に付いているので、
    // Awake は Open で表示した瞬間に走る。そこで閉じると開いた直後に消えてしまう

    /// <summary>
    /// ウィンドウを開く。本文は空にしておく。
    /// </summary>
    public void Open()
    {
        if (bodyText != null) bodyText.text = string.Empty;
        SetAdvanceMarkVisible(false);
        SetWindowVisible(true);
    }

    /// <summary>
    /// ウィンドウを閉じる。
    /// </summary>
    public void Close()
    {
        SetWindowVisible(false);
    }

    /// <summary>
    /// 1 行を表示する。本文を 1 文字ずつ出し、全部出たら戻る。
    /// 途中で CompleteLine が呼ばれたら、残りを一度に出して戻る。
    /// </summary>
    public async UniTask ShowLineAsync(DialogueLine line, CancellationToken token)
    {
        completeRequested = false;
        SetAdvanceMarkVisible(false);

        // 名前。空なら枠ごと隠す
        bool hasName = !string.IsNullOrEmpty(line.Name);
        if (nameRoot != null) nameRoot.SetActive(hasName);
        if (nameText != null) nameText.text = hasName ? line.Name : string.Empty;

        // 立ち絵。無ければ隠す
        if (portrait != null)
        {
            portrait.sprite = line.Portrait;
            portrait.gameObject.SetActive(line.Portrait != null);
        }

        await TypeAsync(line.Text, token);

        SetAdvanceMarkVisible(true);
    }

    /// <summary>
    /// 文字送りを飛ばして、本文を全部出す。
    /// </summary>
    public void CompleteLine()
    {
        completeRequested = true;
    }

    /// <summary>
    /// 本文を 1 文字ずつ出す。文字数は TextMeshPro が解釈したあとの数（タグなどは数えない）を使う。
    /// </summary>
    private async UniTask TypeAsync(string text, CancellationToken token)
    {
        if (bodyText == null) return;

        bodyText.text = text;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;

        if (charactersPerSecond <= 0f)
        {
            bodyText.maxVisibleCharacters = total;
            return;
        }

        bodyText.maxVisibleCharacters = 0;
        float shown = 0f;
        while (bodyText.maxVisibleCharacters < total)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            if (completeRequested)
            {
                bodyText.maxVisibleCharacters = total;
                return;
            }

            shown += Time.unscaledDeltaTime * charactersPerSecond;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
        }
    }

    private void SetWindowVisible(bool visible)
    {
        if (window != null) window.SetActive(visible);
    }

    private void SetAdvanceMarkVisible(bool visible)
    {
        if (advanceMark != null) advanceMark.SetActive(visible);
    }
}
