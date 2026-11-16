using UnityEngine;

/// <summary>
/// キャラクターの接地判定(Player と AlterPlayer に付ける)
/// </summary>
public class GroundChecker : MonoBehaviour
{
    [Header("LayerのGround")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] BoxCollider2D boxCollider;

    // 地面との接地判定
    public bool Check()
    {
        Vector2 thisSize = new Vector2(Mathf.Abs(this.transform.localScale.x), Mathf.Abs(this.transform.localScale.y));

        // レイの幅は当たり判定の幅の 90%。左右均等に内側へ詰め、横の壁に届かないようにする
        float sizeRate = 0.9f;
        float colliderWidth = boxCollider.size.x * thisSize.x;
        float rayLength = colliderWidth * sizeRate;

        // 当たり判定の中心（offset を含む）を基準に、足元の 0.1 下へ横向きのレイを引く。
        // 向きの反転は localScale.x の符号で行うため、offset.x にも符号を掛ける
        Vector2 center = (Vector2)transform.position + new Vector2(boxCollider.offset.x * transform.localScale.x, boxCollider.offset.y * thisSize.y);
        Vector2 startPos = center + new Vector2(-rayLength / 2, -boxCollider.size.y * thisSize.y / 2 - 0.1f);
        Vector2 direction = Vector2.right;

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, rayLength, groundLayer);

        // デバッグ表示（実際に判定しているレイと同じ長さ）
        Debug.DrawRay(startPos, direction * rayLength, Color.orangeRed);

        return hit.collider != null;
    }
}
