using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 会話の 1 行。話者の名前、立ち絵、本文を持つ。
/// 行に持たせる情報を増やすとき(立ち絵の左右、吹き出し表示、他言語など)はこの型に欄を足す。
/// Model とトリガーは行の中身を解釈しないので、ここを変えても影響は View に閉じる。
/// </summary>
[System.Serializable]
public class DialogueLine
{
    [SerializeField, Tooltip("話者の名前。空なら名前欄を出さない")] private string name;
    [SerializeField, Tooltip("立ち絵。空なら立ち絵欄を出さない")] private Sprite portrait;
    [SerializeField, TextArea(2, 5)] private string text;

    public string Name => name;
    public Sprite Portrait => portrait;
    public string Text => text;
}

/// <summary>
/// 会話 1 つぶんのデータ。行の一覧を上から順に再生する。
/// 会話の塊(複数行)1 つにつき、このアセットを 1 つ作る。
/// DialogueTrigger はこのアセットを直接参照して再生を依頼する(文字列の ID では指定しない)。
/// </summary>
[CreateAssetMenu(fileName = "DialogueData", menuName = "DialogueData")]
public class DialogueData : ScriptableObject
{
    [SerializeField, Tooltip("セーブなどで会話を識別するための文字。再生の指定には使わない(今は未使用)")] private string id;
    [SerializeField] private List<DialogueLine> lines = new();

    public string Id => id;
    public IReadOnlyList<DialogueLine> Lines => lines;
}
