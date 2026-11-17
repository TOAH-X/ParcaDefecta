using UnityEngine;

/// <summary>
/// カメラの範囲（CameraArea）を、左右・上下の端の数字（ワールド座標）で決めるための部品。
/// 入れた値から、同じオブジェクトの BoxCollider2D の中心(Offset)と大きさ(Size)を計算して書き込む。
/// カメラ（CinemachineConfiner2D）は今までどおり BoxCollider2D を見るので、カメラ側は変えていない。
///
/// 決まり: 範囲はこの部品の 4 つの数字で決める。BoxCollider2D の Size・Offset を直接変えても、
/// 次にこの部品の値を変えたとき（またはオブジェクトを動かしたとき）に上書きされる。
/// オブジェクトを動かしても、端の位置は入れた数字のまま保たれる（回転・拡大はしない前提）。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class CameraAreaBounds : MonoBehaviour
{
    [SerializeField, Tooltip("左端(ワールド座標)")] private float minX = -10f;
    [SerializeField, Tooltip("右端(ワールド座標)")] private float maxX = 10f;
    [SerializeField, Tooltip("下端(ワールド座標)")] private float minY = -5f;
    [SerializeField, Tooltip("上端(ワールド座標)")] private float maxY = 5f;

    public float MinX => minX;
    public float MaxX => maxX;
    public float MinY => minY;
    public float MaxY => maxY;

    /// <summary>
    /// 端の値をまとめて入れる（エディタのスクリプトから、今の BoxCollider2D の範囲を写すときなどに使う）。
    /// </summary>
    public void SetBounds(float newMinX, float newMaxX, float newMinY, float newMaxY)
    {
        minX = newMinX;
        maxX = newMaxX;
        minY = newMinY;
        maxY = newMaxY;
        Apply();
    }

    /// <summary>
    /// 部品を付けたとき（と Inspector の Reset）に呼ばれる。
    /// 初めの値で BoxCollider2D を上書きしないよう、今の BoxCollider2D の範囲を写して始める。
    /// </summary>
    private void Reset()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Vector2 center = (Vector2)transform.position + box.offset;
        Vector2 half = box.size * 0.5f;
        minX = center.x - half.x;
        maxX = center.x + half.x;
        minY = center.y - half.y;
        maxY = center.y + half.y;
    }

    private void OnValidate()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // エディタでオブジェクトを動かしたときも、端の位置を保つ
        if (!Application.isPlaying && transform.hasChanged)
        {
            transform.hasChanged = false;
            Apply();
        }
    }
#endif

    /// <summary>
    /// 端の値から BoxCollider2D の中心と大きさを計算して書き込む。
    /// </summary>
    private void Apply()
    {
        if (minX > maxX)
        {
            Debug.LogWarning($"[CameraAreaBounds] {name}: 左端(Min X)が右端(Max X)より大きいため入れ替えます。", this);
            (minX, maxX) = (maxX, minX);
        }
        if (minY > maxY)
        {
            Debug.LogWarning($"[CameraAreaBounds] {name}: 下端(Min Y)が上端(Max Y)より大きいため入れ替えます。", this);
            (minY, maxY) = (maxY, minY);
        }

        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector2 size = new Vector2(maxX - minX, maxY - minY);
        Vector2 offset = center - (Vector2)transform.position;

        if (box.offset != offset) box.offset = offset;
        if (box.size != size) box.size = size;
    }
}
