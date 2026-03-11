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

// 对话条目（一句话）
[System.Serializable]
public class DialogueEntry
{
    public Sprite background;   // 背景图，可为空
    public string speakerId;    // 说话者ID（例如 "mother", "pure_white"）
    [TextArea(1, 3)]
    public string content;      // 说话内容
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "GameData/QuestDefine")]
public class QuestDefineSO : ScriptableObject
{
    public string id;                     // 任务ID（唯一）
    public string questName;               // 任务名称
    public string nextQuestId;              // 完成后自动开始的下一个任务ID（可为空）

    public QuestCategory category;          // 主线/世界
    public QuestContentType contentType;    // 对话/战斗

    // 如果 contentType == Dialogue，使用这个对话列表
    public List<DialogueEntry> dialogueEntries;

    // 如果 contentType == Combat，使用这个目标列表（沿用你已有的目标系统）
    public List<QuestObjectiveDefineSO> objectives;

    // 其他字段（前置任务、自动开始等）可以保留，这里只列出必要的
}