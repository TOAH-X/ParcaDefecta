public interface ISaveable
{
    // オブジェクトを一意に識別するID（セーブデータのキーとして使用）
    string SaveDataId { get; }
    
    // セーブ時に現在の状態をGameSaveDataに書き込む
    void PopulateSaveData(GameSaveData saveData);
    
    // ロード時にGameSaveDataから状態を復元する
    void LoadFromSaveData(GameSaveData saveData);
}
