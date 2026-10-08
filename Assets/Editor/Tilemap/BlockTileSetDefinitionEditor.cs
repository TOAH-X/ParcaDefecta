using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// BlockTileSetDefinition の Inspector。入力チェックの結果と Generate ボタンを表示します。
/// </summary>
[CustomEditor(typeof(BlockTileSetDefinition))]
public class BlockTileSetDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var definition = (BlockTileSetDefinition)target;
        var errors = new List<string>();
        bool valid = BlockTileSetGenerator.Validate(definition, errors);

        EditorGUILayout.Space();
        foreach (var error in errors)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        using (new EditorGUI.DisabledScope(!valid))
        {
            if (GUILayout.Button("Generate", GUILayout.Height(32)))
            {
                BlockTileSetGenerator.Generate(definition);
            }
        }

        EditorGUILayout.HelpBox(
            "47 枚入りのシートと Rule Tile を生成します。再実行時は同じアセットを上書き更新するので、パレットや Tilemap の参照は維持されます。",
            MessageType.Info);
    }
}
