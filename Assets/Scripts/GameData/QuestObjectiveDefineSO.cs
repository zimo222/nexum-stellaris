using System;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;

// 任务目标静态数据
[System.Serializable]
public class QuestObjectiveDefineSO
{
    public string objectiveId;              // 唯一ID
    public QuestObjectiveType type;          // 类型
    public string targetId;                  // 目标ID（敌人ID、物品ID、地点ID等）
    public int requiredAmount;                // 所需数量
    public string description;                // 目标描述（用于UI）
    public string nextObjectiveId;            // 线性任务中，完成后自动激活的下一个目标ID
}

// 奖励物品
[System.Serializable]
public class RewardItem
{
    public string itemId;    // 物品ID（装备ID或材料ID）
    public int amount;       // 数量
}

// 任务类型枚举
public enum QuestType
{
    MainStory,
    SideQuest,
    DailyQuest
}