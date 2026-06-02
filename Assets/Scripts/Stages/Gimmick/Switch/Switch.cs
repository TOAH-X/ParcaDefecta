using UnityEngine;
using System.Collections.Generic;

public class Switch : MonoBehaviour
{
    // 現在スイッチ内にいる「Player」タグを持つCollider2Dのリスト
    private List<Collider2D> occupants = new List<Collider2D>();
    private bool isSwitchOn = false; // 現在のスイッチの状態

    // プレイヤーがトリガーに入った時に呼ばれる
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 接触したオブジェクトがプレイヤーかどうかをタグで判定
        if (other.CompareTag("Player"))
        {
            if (!occupants.Contains(other))
            {
                occupants.Add(other);
                CheckSwitchState();
            }
        }
    }

    // プレイヤーがトリガー内にいる間、毎フレーム呼ばれる
    private void OnTriggerStay2D(Collider2D other)
    {
        // 接触したオブジェクトがプレイヤーかどうかをタグで判定
        if (other.CompareTag("Player"))
        {
            // リストにない場合は追加（OnTriggerEnter2Dが呼ばれなかった場合への対策）
            if (!occupants.Contains(other))
            {
                occupants.Add(other);
                CheckSwitchState();
            }
        }
    }

    // プレイヤーがトリガーから離れた時に呼ばれる
    private void OnTriggerExit2D(Collider2D other)
    {
        // 接触したオブジェクトがプレイヤーかどうかをタグで判定
        if (other.CompareTag("Player"))
        {
            if (occupants.Contains(other))
            {
                occupants.Remove(other);
                CheckSwitchState();
            }
        }
    }

    // 毎フレーム、スイッチの状態をチェックする（ワープや消滅対策）
    void Update()
    {
        // 無効になったColliderをリストから削除
        occupants.RemoveAll(collider => collider == null || !collider.gameObject.activeInHierarchy);
        CheckSwitchState();
    }

    /// <summary>
    /// スイッチの状態を評価し、必要であればON/OFFの処理を実行する
    /// </summary>
    private void CheckSwitchState()
    {
        bool newSwitchState = occupants.Count > 0;

        if (newSwitchState != isSwitchOn)
        {
            isSwitchOn = newSwitchState;
            if (isSwitchOn)
            {
                Debug.Log("Switch ON: プレイヤーがスイッチを起動しました。");
                // ここにON時のロジックを記述
            }
            else
            {
                Debug.Log("Switch OFF: スイッチから誰もいなくなりました。");
                // ここにOFF時のロジックを記述
            }
        }
    }
}
