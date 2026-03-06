[System.Serializable]
public class GameData
{
    public string currentScene;       // 当前场景名
    public float playerPosX, playerPosY; // 玩家位置
    public bool[] collectedItems;     // 收集物标志（示例）

    // 默认构造函数
    public GameData()
    {
        currentScene = "MainMenu";
        playerPosX = playerPosY = 0;
        collectedItems = new bool[10]; // 假设最多10个收集物
    }
}