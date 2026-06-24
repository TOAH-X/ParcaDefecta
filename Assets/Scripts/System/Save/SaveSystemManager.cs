using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystemManager : Singleton<SaveSystemManager>
{
    private readonly HashSet<ISaveable> saveables = new HashSet<ISaveable>();
    private GameSaveData currentSaveData;
    private string saveFilePath;

    protected override void Awake()
    {
        base.Awake();
        saveFilePath = Path.Combine(Application.persistentDataPath, "save_data.json");
        currentSaveData = new GameSaveData();
    }

    public void Register(ISaveable saveable)
    {
        saveables.Add(saveable);
    }

    public void Unregister(ISaveable saveable)
    {
        saveables.Remove(saveable);
    }

    [ContextMenu("Save Game")]
    public void Save()
    {
        if (currentSaveData == null)
        {
            currentSaveData = new GameSaveData();
        }

        // 各オブジェクトから最新の状態を回収
        foreach (var saveable in saveables)
        {
            saveable.PopulateSaveData(currentSaveData);
        }

        // JSONに変換して書き込み
        try
        {
            string json = JsonUtility.ToJson(currentSaveData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"[SaveSystemManager] Game saved successfully to: {saveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystemManager] Failed to save game: {e.Message}");
        }
    }

    [ContextMenu("Load Game")]
    public void Load()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("[SaveSystemManager] Save file does not exist. Creating new save data.");
            currentSaveData = new GameSaveData();
            return;
        }

        // ファイルの読み込みとJSONからの復元
        try
        {
            string json = File.ReadAllText(saveFilePath);
            currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log($"[SaveSystemManager] Game loaded successfully from: {saveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystemManager] Failed to load game: {e.Message}");
            currentSaveData = new GameSaveData();
        }

        // 各オブジェクトに状態を配信して復元
        foreach (var saveable in saveables)
        {
            saveable.LoadFromSaveData(currentSaveData);
        }
    }

    // デバッグ用：セーブデータの削除
    [ContextMenu("Delete Save Data")]
    public void DeleteSaveData()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("[SaveSystemManager] Save data deleted.");
        }
        currentSaveData = new GameSaveData();
    }
}
