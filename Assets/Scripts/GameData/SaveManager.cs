using System.IO;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    private string savePath;

    protected override void Awake()
    {
        base.Awake();
        savePath = Application.persistentDataPath + "/save.json";
    }

    public void SaveGame(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("游戏已保存至 " + savePath);
    }

    public GameData LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            GameData data = JsonUtility.FromJson<GameData>(json);
            Debug.Log("游戏加载成功");
            return data;
        }
        else
        {
            Debug.Log("无存档，返回新数据");
            return new GameData(); // 返回默认数据
        }
    }
}