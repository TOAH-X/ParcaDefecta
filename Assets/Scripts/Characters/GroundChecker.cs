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
        float sizeRate = 0.9f;
        float rayLength = boxCollider.size.x * thisSize.x;

        Vector2 direction = transform.right * thisSize.x * sizeRate;
        Vector2 startPos = transform.position + new Vector3(((1 - sizeRate) * thisSize.x - rayLength) / 2, -boxCollider.size.y * thisSize.y / 2 - 0.1f, 0);

        // デバッグ表示
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, rayLength, groundLayer);
        Debug.DrawRay(startPos, direction, Color.orangeRed);

        return hit.collider != null;
    }
}
