using System.Collections.Generic;
using UnityEngine;

// 步骤类型
public enum QuestStepType
{
    Dialogue,   // 对话步骤
    Command     // 指令步骤
}

// 指令类型
public enum CommandType
{
    MoveCharacters,     // 移动角色
    Wait,               // 等待
    SpawnCharacter,     // 生成角色
    DestroyCharacter    // 销毁角色
}

// 单个角色移动指令
[System.Serializable]
public class CharacterMovement
{
    public string characterId;          // NPC的speakerId或"Player"
    public List<Vector2> waypoints;     // 路径点（世界坐标）
    public float speed = 2f;            // 移动速度
    public bool waitForCompletion = true; // 是否等待该角色移动完成
}

// 生成角色数据
[System.Serializable]
public class SpawnCharacterData
{
    public string characterId;          // 唯一标识（用于后续移动/销毁）
    public GameObject prefab;           // 角色预制体
    public Vector2 spawnPosition;       // 生成位置
    public string startState = "Idle";  // 初始动画状态名（可选）
}

// 销毁角色数据
[System.Serializable]
public class DestroyCharacterData
{
    public string characterId;
}

// 指令数据
[System.Serializable]
public class QuestCommand
{
    public CommandType commandType;
    public List<CharacterMovement> movements; // 用于 MoveCharacters
    public float waitTime;                    // 用于 Wait
    public SpawnCharacterData spawnData;      // 用于 SpawnCharacter
    public DestroyCharacterData destroyData;  // 用于 DestroyCharacter
}

// 混合步骤
[System.Serializable]
public class QuestStep
{
    public QuestStepType type;
    public DialogueEntry dialogueEntry;   // 对话步骤使用
    public QuestCommand commandEntry;     // 指令步骤使用
}