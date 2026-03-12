using UnityEngine;

public class MainMenuPanel : BasePanel
{
    // 可以在这里添加主菜单特有的逻辑，比如按钮点击事件
    private void Start()
    {
        // 示例：如果需要在打开时做些什么，可以重写 OnOpen
    }

    public void OnStartGame()
    {
        // 开始游戏按钮的逻辑
        Debug.Log("开始游戏");
        // 可以加载游戏场景等
        SceneDataManager.Instance.LoadScene("1_TheNestOfWarmLight_0");
    }

    public void OnClick(string name)
    {
        Debug.Log(name);
        UIManager.Instance.OpenPanel(name);
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}