using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using ParcaDefecta.System;

public class AlterPlayerMover : MonoBehaviour, ILaunchable
{
    [SerializeField] private Rigidbody2D rb2D;
    [SerializeField] private BoxCollider2D boxCollider2D;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // true=分離されたのでDynamicに、false=同期しているのでtransformに
    public void SetState(bool isSeparated)
    {
        if (isSeparated == true)
        {
            UnFreeze();
        }
        else
        {
            Freeze();
        }
    }

    // 惰性で移動
    public async UniTaskVoid InertialMovement(Vector2 direction, float speed, float duration, CancellationToken token)
    {
        // 慣性移動開始
        float timer = duration;
        while (timer > 0f && !token.IsCancellationRequested)
        {
            // ポーズ中は待機してループの先頭に戻る
            if (TimeManager.Instance != null && TimeManager.Instance.IsPaused.Value)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                continue;
            }

            // 方向ベクトルを考慮した一貫性のある速度適用
            rb2D.linearVelocity = new Vector2(direction.normalized.x * speed, rb2D.linearVelocity.y);

            timer -= Time.fixedDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
        }
    }

    // RigidBodyをStaticに
    public void Freeze()
    {
        rb2D.bodyType = RigidbodyType2D.Static;
        boxCollider2D.enabled = false;
    }

    // RigidBodyをDynamicに
    public void UnFreeze()
    {
        rb2D.bodyType = RigidbodyType2D.Dynamic;
        boxCollider2D.enabled = true;
    }

    /// <summary>
    /// 外部ギミックによる打ち上げ処理
    /// </summary>
    public void Launch(float force)
    {
        // 垂直速度を直接上書きして打ち上げる
        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, force);
    }
}
