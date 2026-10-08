using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// BlockTileSetDefinition から、47 枚入りのスプライトシート(PNG)と Rule Tile を生成・更新します。
/// </summary>
public static class BlockTileSetGenerator
{
    private const string SheetSuffix = "_Sheet";

    /// <summary>入力を検査し、問題があれば errors に追加して false を返します。</summary>
    public static bool Validate(BlockTileSetDefinition definition, List<string> errors)
    {
        int expectedSize = -1;
        foreach (var (source, texture) in definition.EnumerateSources())
        {
            if (texture == null)
            {
                errors.Add($"{source} の素材が未設定です。");
                continue;
            }

            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{source} は PNG ファイルのアセットを指定してください。");
                continue;
            }

            if (!TryReadPngSize(path, out int width, out int height))
            {
                errors.Add($"{source} の PNG ヘッダーを読めません: {path}");
                continue;
            }

            if (width != height)
            {
                errors.Add($"{source} が正方形ではありません({width}x{height})。");
                continue;
            }

            if (width % 2 != 0)
            {
                errors.Add($"{source} のサイズが偶数ではありません({width})。");
                continue;
            }

            if (expectedSize < 0) expectedSize = width;
            else if (expectedSize != width)
                errors.Add($"{source} のサイズ({width})が他の素材({expectedSize})と一致しません。");
        }

        if (string.IsNullOrWhiteSpace(definition.OutputName))
            errors.Add("Output Name が空です。");
        else if (definition.OutputName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            errors.Add("Output Name にファイル名に使えない文字が含まれています。");

        if (!IsAssetsFolderPath(definition.RuleTileFolder))
            errors.Add("Rule Tile Folder は Assets/ から始まるパスにしてください。");

        if (!string.IsNullOrEmpty(definition.SheetFolder) && !IsAssetsFolderPath(definition.SheetFolder))
            errors.Add("Sheet Folder は空か、Assets/ から始まるパスにしてください。");

        if (definition.PixelsPerUnit <= 0)
            errors.Add("Pixels Per Unit は 1 以上にしてください。");

        if (definition.Padding < 0)
            errors.Add("Padding は 0 以上にしてください。");

        return errors.Count == 0;
    }

    /// <summary>シートと Rule Tile を生成します。既存のアセットがあれば上書き更新します。</summary>
    public static void Generate(BlockTileSetDefinition definition)
    {
        var errors = new List<string>();
        if (!Validate(definition, errors))
        {
            Debug.LogError("BlockTileSetGenerator: 入力に問題があります。\n" + string.Join("\n", errors), definition);
            return;
        }

        try
        {
            var sources = LoadSources(definition, out int tileSize);
            var pixels = BlockTileComposer.ComposeSheet(
                sources, tileSize, definition.Padding, out int width, out int height);

            string sheetFolder = ResolveSheetFolder(definition);
            string ruleTileFolder = definition.RuleTileFolder.TrimEnd('/');
            EnsureFolder(sheetFolder);
            EnsureFolder(ruleTileFolder);

            string sheetPath = $"{sheetFolder}/{definition.OutputName}{SheetSuffix}.png";
            string ruleTilePath = $"{ruleTileFolder}/{definition.OutputName}.asset";

            WritePng(pixels, width, height, sheetPath);
            AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(sheetPath, definition, width, height);
            SliceSheet(sheetPath, definition.OutputName, tileSize, definition.Padding);

            var sprites = LoadSprites(sheetPath);
            var ruleTile = CreateOrUpdateRuleTile(ruleTilePath, definition, sprites);
            RefreshLoadedTilemaps();

            Debug.Log($"BlockTileSetGenerator: 生成完了\n  シート: {sheetPath} ({width}x{height})\n  Rule Tile: {ruleTilePath}", ruleTile);
        }
        catch (Exception e)
        {
            Debug.LogException(e, definition);
        }
    }

    // ---- 入力 ----

    private static Dictionary<BlockTileComposer.Source, Color32[]> LoadSources(
        BlockTileSetDefinition definition, out int tileSize)
    {
        var result = new Dictionary<BlockTileComposer.Source, Color32[]>();
        tileSize = -1;
        foreach (var (source, texture) in definition.EnumerateSources())
        {
            string path = AssetDatabase.GetAssetPath(texture);
            var temp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!temp.LoadImage(File.ReadAllBytes(path)))
                    throw new InvalidDataException($"PNG を読み込めません: {path}");
                if (tileSize < 0) tileSize = temp.width;
                if (temp.width != tileSize || temp.height != tileSize)
                    throw new InvalidDataException($"素材のサイズが一致しません: {path}");
                result[source] = temp.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temp);
            }
        }
        return result;
    }

    private static bool TryReadPngSize(string assetPath, out int width, out int height)
    {
        width = height = 0;
        try
        {
            using var stream = File.OpenRead(assetPath);
            var header = new byte[24];
            if (stream.Read(header, 0, header.Length) != header.Length) return false;
            // PNG signature + IHDR
            if (header[0] != 0x89 || header[1] != (byte)'P' || header[2] != (byte)'N' || header[3] != (byte)'G') return false;
            width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return width > 0 && height > 0;
        }
        catch (IOException)
        {
            return false;
        }
    }

    // ---- 出力先 ----

    private static string ResolveSheetFolder(BlockTileSetDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.SheetFolder))
            return definition.SheetFolder.TrimEnd('/');
        string fillPath = AssetDatabase.GetAssetPath(definition.Fill);
        return Path.GetDirectoryName(fillPath)?.Replace('\\', '/') ?? "Assets";
    }

    private static bool IsAssetsFolderPath(string path)
    {
        return !string.IsNullOrEmpty(path)
            && (path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal))
            && !path.Contains("..");
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;
        Directory.CreateDirectory(assetFolder);
        AssetDatabase.Refresh();
    }

    // ---- シート ----

    private static void WritePng(Color32[] pixels, int width, int height, string assetPath)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void ConfigureImporter(string sheetPath, BlockTileSetDefinition definition, int width, int height)
    {
        var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter
            ?? throw new InvalidOperationException($"TextureImporter を取得できません: {sheetPath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = definition.PixelsPerUnit;
        importer.filterMode = definition.FilterMode;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(width, height)), 32, 16384);
        // 圧縮由来の端の色ズレを抑える(容量は Normal と同じ)
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        // 隙間なく並んだシートでは Extrude Edges が隣のタイルを描いてしまうため 0 にし、
        // メッシュはタイルの矩形をそのまま使う
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteExtrude = 0;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    private static void SliceSheet(string sheetPath, string outputName, int tileSize, int padding)
    {
        var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter
            ?? throw new InvalidOperationException($"TextureImporter を取得できません: {sheetPath}");

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer)
            ?? throw new InvalidOperationException("ISpriteEditorDataProvider を取得できません。");
        provider.InitSpriteEditorDataProvider();

        // 既存のスプライトは名前で引き当てて ID を維持し、Rule Tile からの参照が切れないようにする
        var existingById = new Dictionary<string, GUID>();
        foreach (var rect in provider.GetSpriteRects())
        {
            if (!existingById.ContainsKey(rect.name)) existingById[rect.name] = rect.spriteID;
        }

        var rects = new SpriteRect[BlockTileComposer.StateCount];
        for (int i = 0; i < rects.Length; i++)
        {
            string name = SpriteName(outputName, i);
            var cell = BlockTileComposer.GetCellRect(i, tileSize, padding);
            rects[i] = new SpriteRect
            {
                name = name,
                rect = new Rect(cell.x, cell.y, cell.width, cell.height),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
                spriteID = existingById.TryGetValue(name, out var id) ? id : GUID.Generate(),
            };
        }

        provider.SetSpriteRects(rects);
        var nameFileIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameFileIdProvider?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static string SpriteName(string outputName, int index) => $"{outputName}_{index:00}";

    private static Dictionary<string, Sprite> LoadSprites(string sheetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .GroupBy(s => s.name)
            .ToDictionary(g => g.Key, g => g.First());
    }

    // ---- Rule Tile ----

    private static RuleTile CreateOrUpdateRuleTile(
        string ruleTilePath, BlockTileSetDefinition definition, Dictionary<string, Sprite> sprites)
    {
        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ruleTilePath);
        RuleTile tile;
        if (existing == null)
        {
            tile = ScriptableObject.CreateInstance<RuleTile>();
            AssetDatabase.CreateAsset(tile, ruleTilePath);
        }
        else if (existing is RuleTile existingTile)
        {
            tile = existingTile;
        }
        else
        {
            throw new InvalidOperationException($"出力先に Rule Tile 以外のアセットがあります: {ruleTilePath}");
        }

        var states = BlockTileComposer.EnumerateStates();
        tile.m_DefaultSprite = GetSprite(sprites, definition.OutputName, 0);
        tile.m_DefaultColliderType = definition.ColliderType;
        tile.m_TilingRules.Clear();

        for (int i = 0; i < states.Count; i++)
        {
            var rule = new RuleTile.TilingRule
            {
                m_Id = i,
                m_Sprites = new[] { GetSprite(sprites, definition.OutputName, i) },
                m_ColliderType = definition.ColliderType,
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
            };
            rule.m_Neighbors.Clear();
            rule.m_NeighborPositions.Clear();
            foreach (var (position, value) in BlockTileComposer.GetRuleNeighbors(states[i]))
            {
                rule.m_NeighborPositions.Add(position);
                rule.m_Neighbors.Add(value);
            }
            tile.m_TilingRules.Add(rule);
        }

        tile.UpdateNeighborPositions();
        EditorUtility.SetDirty(tile);
        AssetDatabase.SaveAssets();
        return tile;
    }

    private static Sprite GetSprite(Dictionary<string, Sprite> sprites, string outputName, int index)
    {
        string name = SpriteName(outputName, index);
        if (!sprites.TryGetValue(name, out var sprite))
            throw new InvalidOperationException($"シートにスプライト {name} が見つかりません。");
        return sprite;
    }

    private static void RefreshLoadedTilemaps()
    {
        foreach (var tilemap in Resources.FindObjectsOfTypeAll<Tilemap>())
        {
            if (tilemap.gameObject.scene.IsValid()) tilemap.RefreshAllTiles();
        }
    }
}
