using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    public TMP_Text taskRewardText;              // 任务奖励描述

    [Header("追踪按钮")]
    public Button trackButton;                    // 切换追踪的按钮

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
                var firstActive = activeQuests.FirstOrDefault(q => q.state == QuestState.InProgress);
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
            if (progress.state != QuestState.InProgress) continue;

            // 获取任务静态数据
            if (!GameDataManager.Instance.QuestDict.TryGetValue(progress.questId, out var questData))
                continue;

            // 实例化按钮
            GameObject btnObj = Instantiate(taskButtonPrefab, leftContent);
            questButtons.Add(btnObj);

            // 设置按钮上的文本（假设预制体结构：CategoryText / ChapterText / NameText）
            TMP_Text categoryText = btnObj.transform.Find("CategoryText")?.GetComponent<TMP_Text>();
            TMP_Text chapterText = btnObj.transform.Find("ChapterText")?.GetComponent<TMP_Text>();
            TMP_Text nameText = btnObj.transform.Find("NameText")?.GetComponent<TMP_Text>();

            if (categoryText != null)
                categoryText.text = questData.category == QuestCategory.Main ? "主线" : "世界";

            if (chapterText != null)
                chapterText.text = ExtractChapterFromId(questData.id);   // 从ID中提取章节

            if (nameText != null)
                nameText.text = questData.questName;

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
            taskRewardText.text = "";
            return;
        }

        // 获取任务静态数据
        if (!GameDataManager.Instance.QuestDict.TryGetValue(selectedQuestId, out var questData))
        {
            taskNameText.text = "任务数据缺失";
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

        // 奖励描述
        taskRewardText.text = "";
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
        return active.Exists(q => q.questId == questId && q.state == QuestState.InProgress);
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