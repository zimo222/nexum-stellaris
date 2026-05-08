using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂载在玩家对象上，每30秒自动将当前位置和场景名保存到 PlayerDataManager
/// </summary>
public class PlayerPositionAutoSaver : MonoBehaviour
{
    [Tooltip("保存间隔（秒）")]
    public float saveInterval = 10f;

    private void Start()
    {
        // 第一次保存可以立即执行一次（可选）
        InvokeRepeating(nameof(SaveCurrentPosition), saveInterval, saveInterval);
    }

    private void SaveCurrentPosition()
    {
        // 确保 PlayerDataManager 存在且当前有登录玩家
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.CurrentPlayerData == null)
            return;

        // 获取当前场景名称
        string currentScene = SceneManager.GetActiveScene().name;

        // 获取玩家位置（Transform 的 x, y）
        double posX = transform.position.x;
        double posY = transform.position.y;

        // 调用数据管理器保存
        PlayerDataManager.Instance.UpdatePlayerPosition(currentScene, posX, posY);
    }

    private void OnDestroy()
    {
        // 停止延迟调用（避免组件销毁后仍触发调用）
        CancelInvoke();
    }
}