using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 任务详情面板
/// 继承自 BPanel 以便使用返回按钮的默认行为
/// </summary>
public class TaskDetailPanel : BPanel
{
    [Header("左侧任务列表")]
    public Transform leftContent;               // 任务按钮的父物体（通常为 ScrollView 的 Content）
    public GameObject taskButtonPrefab;         // 任务按钮预制体（需包含 Button 组件及三个 TextMeshPro 文本）

    [Header("右侧详情区域")]
    public TMP_Text taskNameText;                // 任务名称
    public TMP_Text taskDetailText;              // 任务详情（描述 + 目标进度）

    [Header("奖励区域")]
    public Transform rewardContainer;            // 奖励图标的父物体（建议使用 HorizontalLayoutGroup 或 GridLayoutGroup）
    public GameObject rewardIconPrefab;          // 奖励图标预制体（需包含 Button、Image，可选数量文本）

    [Header("追踪按钮")]
    public Button trackButton;                   // 切换追踪的按钮

    // 奖励按钮点击事件（参数：物品ID）
    public UnityEvent<string> OnRewardClicked;

    // 当前选中的任务ID
    private string selectedQuestId;
    // 动态生成的按钮列表（用于清理）
    private List<GameObject> questButtons = new List<GameObject>();

    private void OnEnable()
    {
        // 刷新左侧任务列表
        RefreshLeftList();

        // 初始化选中的任务：优先使用当前追踪的任务，若无效则选择第一个进行中的任务
        string trackedId = QuestManager.Instance?.TrackedQuestId;
        if (!string.IsNullOrEmpty(trackedId) && IsQuestActive(trackedId))
        {
            selectedQuestId = trackedId;
        }
        else
        {
            var activeQuests = PlayerDataManager.Instance?.CurrentPlayerData?.activeQuests;
            if (activeQuests != null && activeQuests.Count > 0)
            {
                // 只取进行中的任务
                var firstActive = activeQuests.FirstOrDefault(q => q.state == QuestProgressState.Available);
                selectedQuestId = firstActive?.questId;
            }
        }

        // 更新右侧显示
        UpdateRightPanel();

        // 绑定追踪按钮事件
        trackButton.onClick.AddListener(OnTrackButtonClicked);
    }

    private void OnDisable()
    {
        trackButton.onClick.RemoveListener(OnTrackButtonClicked);
    }

    /// <summary>
    /// 刷新左侧任务按钮列表
    /// </summary>
    private void RefreshLeftList()
    {
        // 清除旧按钮
        foreach (var btn in questButtons)
        {
            Destroy(btn);
        }
        questButtons.Clear();

        // 获取玩家进行中的任务
        var activeQuests = PlayerDataManager.Instance?.CurrentPlayerData?.activeQuests;
        if (activeQuests == null || activeQuests.Count == 0) return;

        foreach (var progress in activeQuests)
        {
            // 只显示进行中的任务（避免显示未开始或已完成）
            if (progress.state != QuestProgressState.Available) continue;

            // 获取任务静态数据
            if (!GameDataManager.Instance.QuestDict.TryGetValue(progress.questId, out var questData))
                continue;

            // 实例化按钮
            GameObject btnObj = Instantiate(taskButtonPrefab, leftContent);
            questButtons.Add(btnObj);

            QuestItemView itemView = btnObj.gameObject.GetComponent<QuestItemView>();
            itemView.UpdateUI(questData);

            // 绑定点击事件
            Button btn = btnObj.GetComponent<Button>();
            string capturedId = progress.questId;   // 避免闭包问题
            btn.onClick.AddListener(() => OnTaskButtonClicked(capturedId));
        }
    }

    /// <summary>
    /// 左侧任务按钮点击回调
    /// </summary>
    private void OnTaskButtonClicked(string questId)
    {
        selectedQuestId = questId;
        UpdateRightPanel();
    }

    /// <summary>
    /// 追踪按钮点击回调
    /// </summary>
    private void OnTrackButtonClicked()
    {
        if (!string.IsNullOrEmpty(selectedQuestId))
        {
            QuestManager.Instance.SetTrackedQuest(selectedQuestId);
        }
    }

    /// <summary>
    /// 更新右侧详情面板
    /// </summary>
    private void UpdateRightPanel()
    {
        if (string.IsNullOrEmpty(selectedQuestId))
        {
            taskNameText.text = "未选择任务";
            taskDetailText.text = "";
            ClearRewardIcons();
            return;
        }

        // 获取任务静态数据
        if (!GameDataManager.Instance.QuestDict.TryGetValue(selectedQuestId, out var questData))
        {
            taskNameText.text = "任务数据缺失";
            taskDetailText.text = "";
            ClearRewardIcons();
            return;
        }

        // 任务名称
        taskNameText.text = questData.questName;

        // 构建详情文本（描述 + 目标进度）
        string detail = string.IsNullOrEmpty(questData.description) ? "暂无描述" : questData.description;

        var progress = PlayerDataManager.Instance.GetQuestProgress(selectedQuestId);
        if (progress != null && progress.objectives.Count > 0)
        {
            detail += "\n\n目标：";
            foreach (var objProgress in progress.objectives)
            {
                // 获取目标定义（用于显示描述）
                string objDesc = GetObjectiveDescription(selectedQuestId, objProgress.objectiveId);
                detail += $"\n• {objDesc}: {objProgress.currentAmount}/{objProgress.requiredAmount}";
                if (objProgress.isCompleted)
                    detail += " (已完成)";
            }
        }
        taskDetailText.text = detail;

        // 生成奖励图标（替换原来的文本奖励描述）
        GenerateRewardIcons(questData);
    }

    /// <summary>
    /// 清除所有奖励图标
    /// </summary>
    private void ClearRewardIcons()
    {
        if (rewardContainer == null) return;
        foreach (Transform child in rewardContainer)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 根据任务数据生成奖励图标列表
    /// </summary>
    private void GenerateRewardIcons(QuestDefineSO questData)
    {
        ClearRewardIcons();

        if (questData.Reward == null || questData.Reward.Count == 0) return;
        if (rewardContainer == null || rewardIconPrefab == null)
        {
            Debug.LogWarning("奖励容器或奖励预制体未指定！");
            return;
        }

        foreach (string rewardStr in questData.Reward)
        {
            // 解析奖励字符串，格式支持 "itemId" 或 "itemId:amount"
            string itemId = rewardStr;
            int amount = 1;
            if (rewardStr.Contains(":"))
            {
                var parts = rewardStr.Split(':');
                if (parts.Length == 2)
                {
                    itemId = parts[0];
                    int.TryParse(parts[1], out amount);
                }
            }

            // 获取物品图标（从各字典中查找）
            Sprite itemIcon = GetItemIcon(itemId);
            if (itemIcon == null)
            {
                Debug.LogWarning($"未找到物品图标: {itemId}");
                //continue;
            }

            // 实例化奖励图标
            GameObject iconObj = Instantiate(rewardIconPrefab, rewardContainer);

            // 设置图标
            Image iconImage = iconObj.GetComponent<Image>();
            if (iconImage != null)
                iconImage.sprite = itemIcon;

            // 设置数量文本（如果预制体有 TMP_Text 组件）
            TMP_Text amountText = iconObj.GetComponentInChildren<TMP_Text>();
            if (amountText != null)
            {
                amountText.text = amount > 1 ? amount.ToString() : "";
            }

            // 绑定点击事件
            Button btn = iconObj.GetComponent<Button>();
            if (btn != null)
            {
                string capturedId = itemId; // 避免闭包问题
                btn.onClick.AddListener(() => OnRewardClicked?.Invoke(capturedId));
            }
        }
    }

    /// <summary>
    /// 根据物品 ID 获取对应的图标 Sprite
    /// </summary>
    private Sprite GetItemIcon(string itemId)
    {
        // 优先查找武器
        if (GameDataManager.Instance.ExotextDict.TryGetValue(itemId, out var exotext))
            return exotext.icon;

        // 查找圣痕
        if (GameDataManager.Instance.NexusVestureDict.TryGetValue(itemId, out var nexus))
            return nexus.icon;

        // 查找材料（假设 MaterialDefineSO 也有 icon 字段，需根据实际调整）
        // if (GameDataManager.Instance.MaterialDict.TryGetValue(itemId, out var material))
        //     return material.icon;

        return null;
    }

    /// <summary>
    /// 获取任务目标的描述文本
    /// </summary>
    private string GetObjectiveDescription(string questId, string objectiveId)
    {
        if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData) && questData.objectives != null)
        {
            var objDefine = questData.objectives.Find(o => o.objectiveId == objectiveId);
            if (objDefine != null && !string.IsNullOrEmpty(objDefine.description))
                return objDefine.description;
        }
        return objectiveId;   // 回退显示ID
    }

    /// <summary>
    /// 判断指定任务是否正在进行中
    /// </summary>
    private bool IsQuestActive(string questId)
    {
        var active = PlayerDataManager.Instance?.CurrentPlayerData?.activeQuests;
        if (active == null) return false;
        return active.Exists(q => q.questId == questId && q.state == QuestProgressState.Available);
    }

    /// <summary>
    /// 从任务ID中提取章节信息（例如 "MainQuest_001" -> "第一章"）
    /// </summary>
    private string ExtractChapterFromId(string id)
    {
        var match = System.Text.RegularExpressions.Regex.Match(id, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int chapterNum))
        {
            return $"第{chapterNum}章";
        }
        return "未知章节";
    }

    /// <summary>
    /// 返回按钮点击事件（直接调用基类的 OnClickOut 以关闭面板）
    /// </summary>
    public void OnReturnButtonClick()
    {
        OnClickOut();
    }
}