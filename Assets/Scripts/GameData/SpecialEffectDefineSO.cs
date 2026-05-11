using UnityEngine;

// 特效类型枚举
public enum SpecialEffectType
{
    None,
    WeaveMagic,   // 织法：减少本次技能能量消耗
    Echo,         // 回响：减少技能冷却
    Warmth,       // 余温：获得护盾
    Memory,       // 追忆：叠层爆炸（暂不实现）
    Bond,         // 羁绊：召唤虚影
}

[CreateAssetMenu(fileName = "NewSpecialEffect", menuName = "GameData/SpecialEffectDefine")]
public class SpecialEffectDefineSO : ScriptableObject
{
    public string id;                 // 唯一标识
    public string effectName;         // 显示名称，如“织法”
    public SpecialEffectType effectType;

    [Tooltip("基础强度值，例如 0.15 代表减少15%消耗")]
    public float baseStrength;

    [Tooltip("UI短描述模板，如“魔法消耗减少{0}%”")]
    public string shortDesc;

    [TextArea] public string description;   // 详细描述
    public Sprite icon;                     // 可选图标
}