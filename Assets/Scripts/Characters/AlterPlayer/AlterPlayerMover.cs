using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class AlterPlayerMover : MonoBehaviour
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
        /*
        if (direction.x > 0)
        {
            rb2D.velocity = Vector2.right * speed;
        }
        else if (direction.x < 0)
        {
            rb2D.velocity = Vector2.left * speed;
        }
        // rb2D.velocity = direction * speed;

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            // キャンセル時はここに来る（エラーにはならない）
        }

        rb2D.velocity = Vector2.zero;
        */
        // 慣性移動開始
        float timer = duration;
        while (timer > 0f && !token.IsCancellationRequested)
        {
            //rb2D.AddForce(direction.normalized * speed, ForceMode2D.Force);
            if (direction.x > 0)
            {
                rb2D.linearVelocity = new Vector2(Vector2.right.x * speed, rb2D.linearVelocity.y);
            }
            else if (direction.x < 0)
            {
                rb2D.linearVelocity = new Vector2(Vector2.left.x * speed, rb2D.linearVelocity.y);
            }

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

    // 打ち上げ
    public void Launch(Vector2 force)
    {
        // 一旦速度リセット（不要なら削除可）
        rb2D.linearVelocity = Vector2.zero;

        // 力を加える
        rb2D.AddForce(force, ForceMode2D.Impulse);
    }
}
