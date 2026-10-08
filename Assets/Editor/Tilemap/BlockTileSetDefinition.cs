using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ブロックタイル生成の定義。素材 5 枚と出力設定を持ち、Inspector の Generate で
/// 47 枚入りのスプライトシートと Rule Tile を生成します(Editor 専用)。
/// </summary>
[CreateAssetMenu(fileName = "BlockTileSet", menuName = "Tilemap/Block Tile Set Definition")]
public class BlockTileSetDefinition : ScriptableObject
{
    [Header("Source Textures (PNG, 正方形, 全て同じサイズ)")]
    [SerializeField, Tooltip("塗りつぶし。縁なし(四方と斜めすべてに隣がある状態)")]
    private Texture2D fill;
    [SerializeField, Tooltip("単独。四辺に縁、四隅が外角(隣がない状態)")]
    private Texture2D single;
    [SerializeField, Tooltip("横棒の中央。上辺と下辺に縁(左右に隣がある状態)")]
    private Texture2D horizontalBar;
    [SerializeField, Tooltip("縦棒の中央。左辺と右辺に縁(上下に隣がある状態)")]
    private Texture2D verticalBar;
    [SerializeField, Tooltip("内角 4 つ。縁なしで四隅に内角(四方に隣があり、斜め 4 つが空いている状態)")]
    private Texture2D innerCorners;

    [Header("Output")]
    [SerializeField, Tooltip("シート名とスプライト名、Rule Tile 名の元になる名前")]
    private string outputName = "Block";
    [SerializeField, Tooltip("シートの出力フォルダ。空なら塗りつぶし素材と同じフォルダ")]
    private string sheetFolder = "";
    [SerializeField, Tooltip("Rule Tile の出力フォルダ")]
    private string ruleTileFolder = "Assets/Tiles/Rule";

    [Header("Import Settings")]
    [SerializeField, Tooltip("1 ユニットあたりのピクセル数。素材のサイズと同じにすると 1 タイル = 1 ユニット")]
    private int pixelsPerUnit = 256;
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
    [SerializeField, Tooltip("Rule Tile の当たり判定の形。真四角のブロックは Grid 推奨")]
    private Tile.ColliderType colliderType = Tile.ColliderType.Grid;
    [SerializeField, Min(0), Tooltip("シート上で各タイルの周囲に設ける余白(px)。タイル自身の端を引き伸ばして埋め、隣のタイルの色がにじむのを防ぎます。Mipmap を使う場合は 16 以上を推奨")]
    private int padding = 8;

    public Texture2D Fill => fill;
    public Texture2D Single => single;
    public Texture2D HorizontalBar => horizontalBar;
    public Texture2D VerticalBar => verticalBar;
    public Texture2D InnerCorners => innerCorners;
    public string OutputName => outputName;
    public string SheetFolder => sheetFolder;
    public string RuleTileFolder => ruleTileFolder;
    public int PixelsPerUnit => pixelsPerUnit;
    public FilterMode FilterMode => filterMode;
    public Tile.ColliderType ColliderType => colliderType;
    public int Padding => padding;

    /// <summary>素材を役割付きで列挙します。</summary>
    public IEnumerable<(BlockTileComposer.Source source, Texture2D texture)> EnumerateSources()
    {
        yield return (BlockTileComposer.Source.Fill, fill);
        yield return (BlockTileComposer.Source.Single, single);
        yield return (BlockTileComposer.Source.HorizontalBar, horizontalBar);
        yield return (BlockTileComposer.Source.VerticalBar, verticalBar);
        yield return (BlockTileComposer.Source.InnerCorners, innerCorners);
    }
}
