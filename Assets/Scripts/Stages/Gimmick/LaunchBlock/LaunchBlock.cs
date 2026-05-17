using UnityEngine;

public class LaunchBlock : MonoBehaviour
{
    [SerializeField] private float launchForce = 20f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ILaunchable インターフェースを実装しているコンポーネントを取得
        ILaunchable launchable = collision.gameObject.GetComponent<ILaunchable>();

        if (launchable != null)
        {
            // インターフェース経由で打ち上げを実行
            // これにより PlayerMover 側の状態管理（isJumping等）も一括で行われる
            launchable.Launch(launchForce);
        }
    }
}
