/// <summary>
/// シーンの種類。シーンへ移るときや、今いるシーンが何かを知るときは、シーン名ではなくこの種類で扱う。
/// 種類ごとの実際のシーンは SceneDatabase に登録する。
/// </summary>
public enum SceneType
{
    Title,  // タイトル
    Game,   // ゲーム本編（ステージを置くシーン）
    Debug,  // 確認用（ステージを置くシーン）
}
