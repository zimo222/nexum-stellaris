using TMPro;
using UnityEngine;

/// <summary>
/// 挂载在 TMP_Text 上，显示当前玩家的等级（Lv.X）
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class PlayerLevelText : MonoBehaviour
{
    private TMP_Text levelText;

    private void Awake()
    {
        levelText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        // 确保 PlayerDataManager 单例已初始化
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerLevelText: 未找到 PlayerDataManager 实例！");
            return;
        }

        // 初始化显示
        UpdateLevelDisplay(PlayerDataManager.Instance.CurrentPlayerData);

        // 订阅数据变更事件
        PlayerDataManager.Instance.OnPlayerDataChanged += UpdateLevelDisplay;
    }

    private void OnDestroy()
    {
        // 取消订阅，避免内存泄漏
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerDataChanged -= UpdateLevelDisplay;
        }
    }

    private void UpdateLevelDisplay(PlayerData data)
    {
        if (levelText != null && data != null)
        {
            levelText.text = $"Lv.{data.Level}";
        }
    }
}