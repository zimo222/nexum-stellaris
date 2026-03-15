using System.Collections.Generic;
using UnityEngine;

// 任务类别（主线/世界）
public enum QuestCategory
{
    Main,   // 主线
    World   // 世界任务
}

// 任务内容类型（对话/战斗）
public enum QuestContentType
{
    Dialogue,   // 对话型
    Combat      // 战斗型
}

// 对话条目
[System.Serializable]
public class DialogueEntry
{
    public Sprite background;
    public string speakerId;
    [TextArea(1, 3)]
    public string content;
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "GameData/QuestDefine")]
public class QuestDefineSO : ScriptableObject
{
    public string id;                           // 任务ID（唯一）
    public string questName;                     // 任务名称
    public QuestCategory category;                // 主线/世界
    public QuestContentType contentType;          // 对话/战斗

    [Header("后续任务")]
    public List<string> nextQuestIds;            // 完成后解锁的任务ID列表（支持多个）

    [Header("任务介绍")]
    public string description;   // 对话内容

    [Header("对话型任务配置")]
    public List<DialogueEntry> dialogueEntries;   // 对话内容

    [Header("战斗/目标型任务配置")]
    public List<QuestObjectiveDefineSO> objectives; // 目标列表
}