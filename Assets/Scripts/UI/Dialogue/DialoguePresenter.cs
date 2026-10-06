using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// 会話 UI の Presenter 層。
/// Model（DialogueManager）からの依頼イベントを受けて View を動かし、行を出し終えたら Model に報告する。
/// 送りの入力（Advance アクションと、ウィンドウのタップ）もここで受けて Model に渡す。
/// DialogueCanvas プレハブのルートにアタッチして使用。
/// </summary>
public class DialoguePresenter : MonoBehaviour
{
    [SerializeField] private DialogueView view;

    [Header("タップで送るためのボタン(ウィンドウ全体を覆う)")]
    [SerializeField] private Button advanceButton;

    // 送りの入力アクション（InputSystem_Actions の Dialogue マップ）
    private const string AdvanceActionName = "Dialogue/Advance";

    // OnDisable 時に Instance を叩くと終了処理中に再生成される恐れがあるため、購読時の参照を保持する
    private DialogueManager manager;
    private InputAction advanceAction;

    // 表示中の行の打ち切り用
    private CancellationTokenSource lineCts;

    private void Awake()
    {
        if (view == null) view = GetComponentInChildren<DialogueView>(true);
        if (advanceButton != null) advanceButton.onClick.AddListener(HandleAdvanceInput);
    }

    private void Start()
    {
        advanceAction = InputSystem.actions != null ? InputSystem.actions.FindAction(AdvanceActionName) : null;
        if (advanceAction == null)
        {
            Debug.LogWarning($"[DialoguePresenter] 入力アクション '{AdvanceActionName}' が見つかりません。タップでのみ送れます");
        }
    }

    private void OnEnable()
    {
        manager = DialogueManager.Instance;
        if (manager == null) return;

        manager.OnStarted += HandleStarted;
        manager.OnLineRequested += HandleLineRequested;
        manager.OnCompleteRequested += HandleCompleteRequested;
        manager.OnFinished += HandleFinished;

        // 途中参加時の同期。既に再生中なら、今の行を出した状態にしておく
        if (manager.IsPlaying && manager.CurrentData != null)
        {
            HandleStarted(manager.CurrentData);
            HandleLineRequested(manager.CurrentData.Lines[manager.CurrentIndex]);
            HandleCompleteRequested();
        }
    }

    private void OnDisable()
    {
        CancelLine();
        if (manager == null) return;

        manager.OnStarted -= HandleStarted;
        manager.OnLineRequested -= HandleLineRequested;
        manager.OnCompleteRequested -= HandleCompleteRequested;
        manager.OnFinished -= HandleFinished;
        manager = null;
    }

    private void OnDestroy()
    {
        if (advanceButton != null) advanceButton.onClick.RemoveListener(HandleAdvanceInput);
    }

    private void Update()
    {
        if (manager == null || !manager.IsPlaying) return;
        if (advanceAction != null && advanceAction.triggered)
        {
            HandleAdvanceInput();
        }
    }

    private void HandleAdvanceInput()
    {
        if (manager == null || !manager.IsPlaying) return;
        manager.Advance();
    }

    private void HandleStarted(DialogueData data)
    {
        if (view != null) view.Open();
    }

    private void HandleLineRequested(DialogueLine line)
    {
        ShowLineAsync(line).Forget();
    }

    private void HandleCompleteRequested()
    {
        if (view != null) view.CompleteLine();
    }

    private void HandleFinished()
    {
        CancelLine();
        if (view != null) view.Close();
    }

    /// <summary>
    /// View に行を出させ、出し終えたら Model に報告する。
    /// 新しい行や終了で打ち切られた場合は報告しない。
    /// </summary>
    private async UniTaskVoid ShowLineAsync(DialogueLine line)
    {
        CancelLine();
        lineCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken token = lineCts.Token;

        if (view == null)
        {
            Debug.LogWarning("[DialoguePresenter] View が設定されていません。表示なしで完了報告します");
            manager?.NotifyLineShown();
            return;
        }

        try
        {
            await view.ShowLineAsync(line, token);
        }
        catch (System.OperationCanceledException)
        {
            return;
        }

        manager?.NotifyLineShown();
    }

    private void CancelLine()
    {
        lineCts?.Cancel();
        lineCts?.Dispose();
        lineCts = null;
    }
}
