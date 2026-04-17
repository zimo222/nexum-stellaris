using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    public GameObject categoryTitlePrefab;      // 分类标题预制体（需包含 TMP_Text 组件）

    [Header("右侧详情区域")]
    public TMP_Text taskNameText;                // 任务名称
    public TMP_Text PlaceText;
    public TMP_Text taskNumText;
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
                // 只取进行中的任务，优先主线第一个，其次世界第一个
                var mainQuest = activeQuests.FirstOrDefault(q =>
                    q.state == QuestProgressState.Available &&
                    GameDataManager.Instance.QuestDict.TryGetValue(q.questId, out var def) &&
                    def.category == QuestCategory.Main);
                if (mainQuest != null)
                    selectedQuestId = mainQuest.questId;
                else
                {
                    var worldQuest = activeQuests.FirstOrDefault(q => q.state == QuestProgressState.Available);
                    selectedQuestId = worldQuest?.questId;
                }
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
    /// 刷新左侧任务按钮列表（分组显示：主线标题 + 主线任务按钮，世界标题 + 世界任务按钮）
    /// </summary>
    private void RefreshLeftList()
    {
        // 清除所有子物体（包括之前生成的标题和按钮）
        foreach (Transform child in leftContent)
        {
            Destroy(child.gameObject);
        }

        // 获取玩家进行中的任务
        var activeQuests = PlayerDataManager.Instance?.CurrentPlayerData?.activeQuests;
        if (activeQuests == null || activeQuests.Count == 0) return;

        // 按任务类别分组
        var mainQuests = new List<PlayerQuestProgress>();
        var worldQuests = new List<PlayerQuestProgress>();

        foreach (var progress in activeQuests)
        {
            // 只显示进行中的任务（避免显示未开始或已完成）
            if (progress.state != QuestProgressState.Available) continue;

            // 获取任务静态数据
            if (!GameDataManager.Instance.QuestDict.TryGetValue(progress.questId, out var questData))
                continue;

            if (questData.category == QuestCategory.Main)
                mainQuests.Add(progress);
            else if (questData.category == QuestCategory.World)
                worldQuests.Add(progress);
        }

        // 生成主线分组
        if (mainQuests.Count > 0)
        {
            CreateCategoryTitle("主线任务");
            foreach (var progress in mainQuests)
            {
                CreateQuestButton(progress);
            }
        }

        // 生成世界分组
        if (worldQuests.Count > 0)
        {
            CreateCategoryTitle("世界任务");
            foreach (var progress in worldQuests)
            {
                CreateQuestButton(progress);
            }
        }
    }

    /// <summary>
    /// 创建一个分类标题
    /// </summary>
    private void CreateCategoryTitle(string title)
    {
        GameObject titleObj = Instantiate(categoryTitlePrefab, leftContent);
        TMP_Text titleText = titleObj.GetComponentInChildren<TMP_Text>();
        if (titleText != null)
            titleText.text = title;
    }

    /// <summary>
    /// 创建一个任务按钮并绑定点击事件
    /// </summary>
    private void CreateQuestButton(PlayerQuestProgress progress)
    {
        // 获取任务静态数据
        if (!GameDataManager.Instance.QuestDict.TryGetValue(progress.questId, out var questData))
            return;

        // 实例化按钮
        GameObject btnObj = Instantiate(taskButtonPrefab, leftContent);
        QuestItemView itemView = btnObj.GetComponent<QuestItemView>();
        if (itemView != null)
            itemView.UpdateUI(questData);

        // 绑定点击事件
        Button btn = btnObj.GetComponent<Button>();
        string capturedId = progress.questId;
        btn.onClick.AddListener(() => OnTaskButtonClicked(capturedId));
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
        PlaceText.text = questData.place;

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

        // 生成奖励图标
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

            // 获取物品图标
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
                string capturedId = itemId;
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

        // 可以继续添加其他字典，如材料等
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
        return objectiveId;
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
    /// 返回按钮点击事件（直接调用基类的 OnClickOut 以关闭面板）
    /// </summary>
    public void OnReturnButtonClick()
    {
        OnClickOut();
    }
}