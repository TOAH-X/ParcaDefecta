using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine.SceneManagement;

namespace ParcaDefecta.System
{
    /// <summary>
    /// ゲーム内の時間を一元管理するシングルトン。
    /// R3を使用して、ポーズ状態や毎フレームのTickを通知します。
    /// </summary>
    public class TimeManager : Singleton<TimeManager>
    {
        // ポーズ状態を保持するリアクティブプロパティ
        private readonly ReactiveProperty<bool> _isMasterPaused = new(false);
        private readonly ReactiveProperty<bool> _isGameLogicPaused = new(false);

        public ReactiveProperty<bool> isMasterPaused => _isMasterPaused;
        public ReactiveProperty<bool> IsPaused { get; private set; } = new(false);

        // 毎フレームの経過時間(deltaTime)を通知するストリーム
        // ポーズ中はこのストリームから流れる値が 0 になります
        private readonly Subject<float> onTick = new();
        public Observable<float> OnTick => onTick;

        /// <summary>
        /// ゲーム開始からの累積プレイ時間（ポーズ中を除く）。
        /// </summary>
        public float TotalTime { get; private set; }

        public float DeltaTime => IsPaused.Value ? 0f : Time.deltaTime;

        protected override void Awake()
        {
            base.Awake();
            // マスターポーズとゲームロジックポーズのいずれかが true ならポーズ状態とする
            _isMasterPaused.CombineLatest(_isGameLogicPaused, (m, g) => m || g)
                .Subscribe(x => IsPaused.Value = x)
                .AddTo(this);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // シーン遷移時にすべてのポーズ状態を強制解除する
            // PauseManagerに依存せず、ここで確実に時間を動かす
            Time.timeScale = 1f;

            SetMasterPause(false);
            SetGameLogicPause(false);
        }

        private void Update()
        {
            // ポーズ中は 0、動作中は実時間をストリームに流す
            float dt = DeltaTime;
            TotalTime += dt;
            onTick.OnNext(dt);
        }

        /// <summary>
        /// ポーズ状態を考慮した非同期待機を行います。
        /// </summary>
        /// <param name="seconds">待機時間（秒）</param>
        /// <param name="cancellationToken">キャンセル用トークン</param>
        /// <returns>待機完了後の余剰時間</returns>
        public async UniTask<float> WaitSecondsAsync(float seconds, CancellationToken cancellationToken = default)
        {
            if (seconds <= 0) return 0f;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                elapsed += DeltaTime;
            }

            return elapsed - seconds; // 待機時間を超えて経過した分を返す
        }

        /// <summary>マスターポーズ（UI等による停止）の設定</summary>
        public void SetMasterPause(bool pause) => _isMasterPaused.Value = pause;

        /// <summary>ゲームロジックポーズ（ギミック等による停止）の設定</summary>
        public void SetGameLogicPause(bool pause) => _isGameLogicPaused.Value = pause;

        // 以前の互換性のために残す（必要に応じて削除）
        public void SetPause(bool pause) => SetMasterPause(pause);

        private void OnDestroy()
        {
            onTick.OnCompleted();
            _isMasterPaused.Dispose();
            _isGameLogicPaused.Dispose();
            IsPaused.Dispose();
            onTick.Dispose();
        }
    }
}