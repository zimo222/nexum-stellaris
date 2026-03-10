using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 任务静态数据
[CreateAssetMenu(fileName = "NewQuest", menuName = "GameData/QuestDefine")]
public class QuestDefineSO : ScriptableObject
{
    public string id;                     // 任务ID
    public string questName;
    public string nextQuestId;   // 完成后自动开始的下一个任务ID（可为空）                // 任务名称
    public QuestType questType;                 // 任务类型（主线/支线）
    public string description;                  // 任务描述
    public List<string> prerequisiteQuestIds;   // 前置任务ID列表
    public List<QuestObjectiveDefineSO> objectives; // 目标列表
    public bool isLinearObjectives;              // 目标是否线性
    public string chapterMap;                    // 关联地图场景
    public string bossId;                        // 关联BOSS ID
    public List<RewardItem> rewards;             // 奖励
    public bool autoStart;                        // 是否自动开始
    public bool hidden;                           // 是否隐藏（需要触发才显示）
}