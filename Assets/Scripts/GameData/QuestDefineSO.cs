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

    [Header("对话框架模式")]
    public YesNo useCGMode = YesNo.Yes;      // Yes = 传统CG对话, No = 使用 dialogueFrame
 }

[CreateAssetMenu(fileName = "NewQuest", menuName = "GameData/QuestDefine")]
public class QuestDefineSO : ScriptableObject
{
    public string id;                           // 任务ID（唯一）
    public string questName;                     // 任务名称
    public string questNum;                     // 任务名称

    public string chapterName;
    public string chapterNum;

    public string place;

    public string lastQuestId;                 // 前置任务名称
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

    [Header("战斗型任务配置")]
    public List<WaveDefine> waves;   // 战斗波次

    [Header("任务奖励")]
    public List<string> Reward;
    public int exp;


    public YesNo isSceneTrans = YesNo.No;

    public string targetSceneName;
    public int targetX, targetY;


    // 新增：是否自动开始下一个任务
    [Header("自动开始下一幕")]
    public YesNo autoStartNextQuest = YesNo.No;   // 默认 No

    [Header("任务所在地图")]
    public string questSceneName;       // 例如 "Scene1", "Scene2"

}
public enum YesNo
{
    No = 0,   // 默认值
    Yes = 1
}