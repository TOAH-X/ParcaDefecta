using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 仮置き：Playerタグを持つオブジェクトに当たったか判定
        if (collision.CompareTag("Player"))
        {
            // ここでプレイヤー側の被弾処理を呼び出す（現在はログのみ）
            Debug.Log("Playerに弾が命中しました。");

            // 弾自体を消滅させる
            Destroy(gameObject);
        }
    }
}
