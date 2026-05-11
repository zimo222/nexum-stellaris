using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[System.Serializable]
public class PlayerData
{
    // ====================      基础账户信息      ====================
    public string PlayerID;                         // 唯一标识
    public string PlayerName;
    public DateTime CreateTime;                     // 账号创建时间
    public DateTime LastLoginTime;                  // 上次登录时间

    // ====================     游戏进度与资源     ====================
    public int Level = 1;
    public int Experience;
    public int Crystals;                            // 水晶
    public int Coins;                               // 金币
    public string CurrentScene = "1_TheNestOfWarmLight";      // 当前所在场景名
    public double PosX = -86;              // X坐标
    public double PosY = 105;              // Y坐标

    // ====================     角色与装备系统     ====================
    public CharacterStats BaseStats;                 // 基础属性
    public int CurrentHealth;   // 当前生命值
    public int CurrentEnergy;   // 当前能量值
    public int skillNum;
    public SkillData[] Skills;
    // 装备索引（指向EquipmentBag的下标）
    // 每个武器类别当前装备的武器 ID（null 表示未装备）
    public string[] EquippedExotextIds = new string[7];
    public string[] EquippedNexusVestureIds = new string[5];
    /*
    public int EquippedCogniThreadIndex = -1;         // 思缕索引
    public int EquippedTangibleNexusIndex = -1;       // 触络索引
    public int EquippedAbyssalHeartIndex = -1;        // 装心索引
    public int EquippedVolitionVeinIndex = -1;        // 志脉索引
    public int EquippedImprintStepIndex = -1;         // 迹印索引
    */
    public List<ExotextData> ExotextBag = new List<ExotextData>();
    public List<NexusVestureData> NexusVestureBag = new List<NexusVestureData>();
    public List<MaterialData> MaterialBag = new List<MaterialData>();

    // ========== 修改点：将单个模块列表扩展为7个武器的模块列表 ==========

    // 替换原来的定义
    public WeaponModuleList[] equippedModuleIdsForWeapons = new WeaponModuleList[7];
    public List<string> equippedModuleIds; // 当前装备的模块ID列表
    //public List<string>[] equippedModuleIds = new List<string>[7];

    // ====================   任务角色与装备系统   ====================
    public List<PlayerQuestProgress> activeQuests;       // 进行中的任务列表
    public List<string> completedQuestIds;               // 已完成的任务ID列表

    // ====================       设置与其他       ====================
    public float MusicVolume = 0.8f;
    public float SFXVolume = 0.8f;
    public string LastLoginIP = "";


    // ====================        构造函数        ====================
    #region 构造方法
    // 空构造函数为JSON反序列化所需
    public PlayerData() { }

    public PlayerData(string playerName)
    {
        // 基础属性先根据初始等级计算
        UpdateBaseStatsByLevel();
        CurrentHealth = (int)BaseStats.Health;   // 初始满血
        PlayerID = System.Guid.NewGuid().ToString();
        PlayerName = playerName;
        CreateTime = DateTime.Now;
        LastLoginTime = DateTime.Now;

        Crystals = 50000;
        Coins = 3000000;

        // 任务系统初始化为空，第一个任务通常由剧情触发
        activeQuests = new List<PlayerQuestProgress>();
        completedQuestIds = new List<string>();

        // 初始化7个武器的模块列表
        for (int i = 0; i < 7; i++)
        {
            equippedModuleIdsForWeapons[i] = new WeaponModuleList();
        }

        InitializeDefaultNexumIdem();
        InitializeDefaultMaterial();
        InitializeDefaultQuest();
        SortedBag();

        // 初始化装备数组为空
        for (int i = 0; i < EquippedExotextIds.Length; i++)
            EquippedExotextIds[i] = null;
        for (int i = 0; i < EquippedNexusVestureIds.Length; i++)
            EquippedNexusVestureIds[i] = null;
        
    }

    /// <summary>
    /// 根据当前 Level 重新计算基础属性（生命、攻击、防御等）
    /// 调用时机：角色等级发生变化时
    /// </summary>
    public void UpdateBaseStatsByLevel()
    {
        int lv = Level;
        // 生命：100 + 9*(lv-1)
        BaseStats.Health = 100 + 9 * (lv - 1);
        // 攻击：15 + 3*(lv-1)
        BaseStats.Attack = 15 + 3 * (lv - 1);
        // 防御：10 + 0.9*(lv-1)
        BaseStats.Defence = Mathf.RoundToInt(10f + 0.9f * (lv - 1));
        // 能量固定100
        BaseStats.Energy = 100;
        // 元素加成：每级+0.2%，初始1% → 100级 1% + 19.8% = 20.8%
        BaseStats.ElementBonus = 0.01f + 0.002f * (lv - 1);

        // 心弦率（CritRate）：每级+0.3%，初始5% → 100级 5% + 29.7% = 34.7%
        BaseStats.CritRate = 0.05f + 0.003f * (lv - 1);
        // 绎动值（CritDamage）：每级+0.002，初始0.5 → 100级 0.5 + 0.002*99 = 0.698
        BaseStats.CritDamage = 0.5f + 0.002f * (lv - 1);
        // 限制绎动值不超过1.0（安全）
        if (BaseStats.CritDamage > 1.0f) BaseStats.CritDamage = 1.0f;
    }

    // 获取角色在指定等级下的基础属性（不含装备）
    public CharacterStats GetBaseStatsAtLevel(int level)
    {
        CharacterStats stats = new CharacterStats();
        stats.Health = 100 + 9 * (level - 1);
        stats.Attack = 15 + 3 * (level - 1);
        stats.Defence = Mathf.RoundToInt(10f + 0.9f * (level - 1));
        stats.Energy = 100;
        stats.ElementBonus = 0.01f + 0.002f * (level - 1);
        stats.CritRate = 0.05f + 0.003f * (level - 1);
        stats.CritDamage = 0.5f + 0.002f * (level - 1);
        if (stats.CritDamage > 1.0f) stats.CritDamage = 1.0f;
        return stats;
    }

    // 获取某武器在指定等级下的属性（从静态数据动态计算）
    public CharacterStats GetExotextStatsAtLevel(ExotextDefineSO def, int level)
    {
        CharacterStats stats = new CharacterStats();
        stats.Health = (int)(def.baseHealth + def.healthPerLevel * (level - 1));
        stats.Attack = (int)(def.baseAttack + def.attackPerLevel * (level - 1));
        stats.Defence = (int)(def.baseDefence + def.defencePerLevel * (level - 1));
        stats.Energy = (int)(def.baseEnergy + def.energyPerLevel * (level - 1));
        stats.ElementBonus = def.baseElementBonus + def.elementBonusPerLevel * (level - 1);
        stats.CritRate = def.baseCritRate + def.critRatePerLevel * (level - 1);
        stats.CritDamage = def.baseCritDamage + def.critDamagePerLevel * (level - 1);
        return stats;
    }

    // 获取某防具在指定等级下的属性（类似）
    public CharacterStats GetNexusVestureStatsAtLevel(NexusVestureDefineSO def, int level)
    {
        CharacterStats stats = new CharacterStats();
        stats.Health = (int)(def.baseHealth + def.healthPerLevel * (level - 1));
        stats.Attack = (int)(def.baseAttack + def.attackPerLevel * (level - 1));
        stats.Defence = (int)(def.baseDefence + def.defencePerLevel * (level - 1));
        stats.Energy = (int)(def.baseEnergy + def.energyPerLevel * (level - 1));
        stats.ElementBonus = def.baseElementBonus + def.elementBonusPerLevel * (level - 1);
        stats.CritRate = def.baseCritRate + def.critRatePerLevel * (level - 1);
        stats.CritDamage = def.baseCritDamage + def.critDamagePerLevel * (level - 1);
        return stats;
    }

    // 获取当前角色最终总属性（基于当前等级、已装备的武器和防具）
    public CharacterStats GetTotalStatsAtLevel(int level)
    {
        CharacterStats total = GetBaseStatsAtLevel(level);

        /*
        // 遍历已装备的武器（EquippedExotextIds）
        for (int i = 0; i < EquippedExotextIds.Length; i++)
        {
            string id = EquippedExotextIds[i];
            if (string.IsNullOrEmpty(id)) continue;
            var data = ExotextBag.Find(e => e.Id == id);
            if (data == null) continue;
            var def = GameDataManager.Instance.ExotextDict[data.Id];
            total += GetExotextStatsAtLevel(def, level);
        }
        */

        // 遍历已装备的防具
        for (int i = 0; i < EquippedNexusVestureIds.Length; i++)
        {
            string id = EquippedNexusVestureIds[i];
            if (string.IsNullOrEmpty(id)) continue;
            var data = NexusVestureBag.Find(v => v.Id == id);
            if (data == null) continue;
            var def = GameDataManager.Instance.NexusVestureDict[data.Id];
            total += GetNexusVestureStatsAtLevel(def, level);
        }

        // 限制心弦率最大0.5（50%）
        total.CritRate = Mathf.Clamp01(total.CritRate);
        // 限制绎动值最大1.0（100%）
        if (total.CritDamage > 1.0f) total.CritDamage = 1.0f;
        return total;
    }


    public void SortedBag()
    {

        ExotextBag.Sort((a, b) =>
        {
            int statusOrderA = a.Stats.Stars;
            int statusOrderB = b.Stats.Stars;

            if (statusOrderA != statusOrderB)
                return statusOrderB.CompareTo(statusOrderA); // 降序排列，优先级高的在前
            return b.Stats.Level.CompareTo(a.Stats.Level);
        });
        NexusVestureBag.Sort((a, b) =>
        {
            int statusOrderA = a.Stats.Stars;
            int statusOrderB = b.Stats.Stars;

            if (statusOrderA != statusOrderB)
                return statusOrderB.CompareTo(statusOrderA); // 降序排列，优先级高的在前
            return 0;
        });
        MaterialBag.Sort((a, b) =>
        {
            int statusOrderA = a.Stars;
            int statusOrderB = b.Stars;

            if (statusOrderA != statusOrderB)
                return statusOrderB.CompareTo(statusOrderA); // 降序排列，优先级高的在前
            return 0;
        });

    }
    #endregion


    // ====================      装备相关方法      ====================
    #region 装备方法
    // 初始化默认装备
    private void InitializeDefaultNexumIdem()
    {
        
        for (int i = 1; i <= 5; i++)
        {
            AddDefaultNExotext("Exotext_00" + i + "_VotiveEmber");
            AddDefaultNExotext("Exotext_00" + i + "_ThoughtChime");
            AddDefaultNExotext("Exotext_00" + i + "_EdgeText");
            AddDefaultNExotext("Exotext_00" + i + "_ThreadShot");
            AddDefaultNExotext("Exotext_00" + i + "_StellarScribe");
            AddDefaultNExotext("Exotext_00" + i + "_DuoVoice");
            AddDefaultNExotext("Exotext_00" + i + "_MnemonicTool");
        }
        
        for (int i = 1; i <= 5; i++)
        {
            AddDefaultNexusvesture("NexusVesture_00" + i + "_CogniThread");
            AddDefaultNexusvesture("NexusVesture_00" + i + "_TangibleNexus");
            AddDefaultNexusvesture("NexusVesture_00" + i + "_AbyssalHeart");
            AddDefaultNexusvesture("NexusVesture_00" + i + "_VolitionVein");
            AddDefaultNexusvesture("NexusVesture_00" + i + "_ImprintStep");
        }
        
    }

    private void AddDefaultNExotext(string defineId)
    {
        var def = GameDataManager.Instance.ExotextDict[defineId];
        var weapon = new ExotextData(
            id: def.id,
            type: def.type,
            element: def.element,
            stars: def.baseStars, maxstars: def.maxStars,
            health: def.baseHealth, attack: def.baseAttack, defence: def.baseDefence,
            energy: def.baseEnergy, critRate: def.baseCritRate, critDamage: def.baseCritDamage, elementBonus: def.baseElementBonus
        );
        ExotextBag.Add(weapon);
    }

    private void AddDefaultNexusvesture(string defineId)
    {
        var def = GameDataManager.Instance.NexusVestureDict[defineId];
        var stigmata = new NexusVestureData(
            id: def.id,
            position: def.Position,
            element: def.element,
            stars: def.baseStars, maxstars: def.maxStars,
            health: def.baseHealth, attack: def.baseAttack, defence: def.baseDefence,
            energy: def.baseEnergy, critRate: def.baseCritRate, critDamage: def.baseCritDamage, elementBonus: def.baseElementBonus
        );
        NexusVestureBag.Add(stigmata);
    }
    #endregion


    // ==================== 材料相关方法 ====================
    #region  材料方法
    // 初始化默认材料
    private void InitializeDefaultMaterial()
    {
        /*
        for (int i = 1; i <= 9; i++)
        {
            AddDefaultMaterial("MATE_0" + (i >= 10 ? "" : "0") + i.ToString(), 3333);
        }
        */
    }

    public void AddDefaultMaterial(string defineId, int Count)
    {
        var def = GameDataManager.Instance.MaterialDict[defineId];
        var material = new MaterialData(
            id: def.id, name: def.materialName,
            stars: def.baseStars, count: Count, num: def.num,
            introduction: def.introduction, description: def.description
        );
        MaterialBag.Add(material);
    }
    #endregion


    // ==================== 任务相关方法 ====================
    #region  任务方法
    // 初始化默认材料
    private void InitializeDefaultQuest()
    {
        AddDefaultQuest("MainQuest_001001");
    }

    public void AddDefaultQuest(string defineId)
    {
        var def = GameDataManager.Instance.QuestDict[defineId];
        var quest = new PlayerQuestProgress(
            questId: def.id
        );
        activeQuests.Add(quest);
    }
    #endregion
    


    public int TotalHealth => (int)GetTotalStatsAtLevel(Level).Health;
    public int TotalAttack => (int)GetTotalStatsAtLevel(Level).Attack;
    public int TotalDefence => (int)GetTotalStatsAtLevel(Level).Defence;
    public int TotalEnergy => GetTotalStatsAtLevel(Level).Energy;
    public float TotalCritRate => GetTotalStatsAtLevel(Level).CritRate;
    public float TotalCritDamage => GetTotalStatsAtLevel(Level).CritDamage;
    public float TotalElementBonus => GetTotalStatsAtLevel(Level).ElementBonus;
}

// ==================== 角色数据类 ====================
[System.Serializable]
public class SkillBranchData
{
    public string branchName;       // 分支名称
    public int level;               // 分支当前等级（动态，可升级）
    public TextStats textStats;      // 分支介绍和描述（静态）
}

// 技能大类数据
[System.Serializable]
public class SkillData
{
    public string skillName;        // 技能大类名称
    public int level;               // 分支当前等级（动态，可升级）
    public TextStats textStats;      // 技能大类介绍和描述（静态）
    public SkillBranchData[] branches; // 分支数组，长度可变（1或3）
}

// ==================== 装备数据类 ====================
[System.Serializable]// 绎络本我
public class NexumIdemData
{
    public string Id;               // ID
    public CharacterStats Stats;    // 属性

    // 装备状态
    public int EquippedToCharacterIndex = -1;        // 被哪个角色装备（-1表示未装备）

    public NexumIdemData() { }

    public NexumIdemData(string id,
                        string element = "", int stars = 0,
                        float health = 0, float attack = 0, float defence = 0,
                        int energy = 0, float critRate = 0f, float critDamage = 0f, float elementBonus = 0f)
    {
        Id = id;
        Stats = new CharacterStats()
        {
            Element = element,
            Level = 1, Exp = 0,
            Stars = stars, SStars = 0, Fragments = 0,
            Health = health, Attack = attack, Defence = defence,
            Energy = energy, CritRate = critRate, CritDamage = critDamage, ElementBonus = elementBonus
        };
    }

    public float Health => Stats.Health;
    public float Attack => Stats.Attack;
    public float CritRate => Stats.CritRate;
    public float CritDamage => Stats.CritDamage;
    public float ElementBonus => Stats.ElementBonus;
}
[System.Serializable]// 绎语
public class ExotextData : NexumIdemData
{
    public ExotextType Type;

    public ExotextData() { }

    public ExotextData(string id, ExotextType type,
                        string element = "", int stars = 0, int maxstars = 0,
                        float health = 0, float attack = 0, float defence = 0,
                        int energy = 0, float critRate = 0f, float critDamage = 0f, float elementBonus = 0f,
                        string introduction = "", string description = "")
        : base(id, element, stars, health, attack, defence, energy, critRate, critDamage, elementBonus)
    {
        Type = type;
    }
}
[System.Serializable]//络身
public class NexusVestureData: NexumIdemData
{
    public NexusVesturePosition Position;

    public NexusVestureData() { }

    public NexusVestureData(string id, NexusVesturePosition position,
                        string element = "", int stars = 0, int maxstars = 0,
                        float health = 0, float attack = 0, float defence = 0,
                        int energy = 0, float critRate = 0f, float critDamage = 0f, float elementBonus = 0f,
                        string introduction = "", string description = "")
        : base(id, element, stars, health, attack, defence, energy, critRate, critDamage, elementBonus)
    {
        Position = position;
    }
}
[System.Serializable]
public class WeaponModuleList
{
    public List<string> moduleIds = new List<string>();
}
// ==================== 材料数据类 ====================
[System.Serializable]
public class MaterialData
{
    public string Id;                                // 材料ID
    public string Name;                              // 材料名称
    public int Stars;                             // 星级
    public int Count;                                // 材料数量
    public int Num;                                  // 数值

    public TextStats textStats;

    public MaterialData() { }

    public MaterialData(string id, string name, int stars, int count = 0, int num = 0, string introduction = null, string description = null)
    {
        Id = id;
        Name = name;
        Stars = stars;
        Count = count;
        Num = num;
        textStats = new TextStats
        {
            Introduction = introduction,
            Description = description
        };
    }
}
// ==================== 任务数据类 ====================
// 单个目标进度数据（可序列化）
[System.Serializable]
public class ObjectiveProgress
{
    public string objectiveId;
    public int currentAmount;
    public int requiredAmount;
    public bool isCompleted;

    public ObjectiveProgress(string id, int current, int required, bool completed)
    {
        objectiveId = id;
        currentAmount = current;
        requiredAmount = required;
        isCompleted = completed;
    }
}

// 单个任务的玩家进度数据（可序列化）
[System.Serializable]
public class PlayerQuestProgress
{
    public string questId;
    public QuestProgressState state;          // 持久化状态
    public List<ObjectiveProgress> objectives; // 战斗任务目标进度

    public PlayerQuestProgress(string questId)
    {
        this.questId = questId;
        this.state = QuestProgressState.Locked;
        this.objectives = new List<ObjectiveProgress>();
    }
}

// ==================== 属性结构体 ====================
[System.Serializable]
public struct CharacterStats
{
    public string Element;                           // 元素

    public int Level;                                // 等级
    public int Exp;                                  // 经验

    public int Stars;                                // 星级
    public int SStars;                               // 小星级
    public int Fragments;                            // 碎片

    public float Health;                               // 生命值
    public float Attack;                               // 攻击力
    public float Defence;                              // 防御力

    public int Energy;                               // 能量
    public float CritRate;                           // 暴击率（0-1）
    public float CritDamage;                         // 暴击伤害（倍率，如1.5表示150%）
    public float ElementBonus;                       // 元素伤害加成（百分比，如0.3表示30%）

    public override string ToString()
    {
        return $"生命: {Health}, 攻击: {Attack}, 暴击: {CritRate:P0}, 爆伤: {CritDamage:P0}, 元素: {ElementBonus:P0}";
    }

    public static CharacterStats operator +(CharacterStats a, CharacterStats b)
    {
        a.Health += b.Health;
        a.Attack += b.Attack;
        a.Defence += b.Defence;
        a.Energy += b.Energy;
        a.CritRate += b.CritRate;
        a.CritDamage += b.CritDamage;
        a.ElementBonus += b.ElementBonus;
        return a;
    }

    public static CharacterStats operator -(CharacterStats a, CharacterStats b)
    {
        a.Health -= b.Health;
        a.Attack -= b.Attack;
        a.Defence -= b.Defence;
        a.Energy -= b.Energy;
        a.CritRate -= b.CritRate;
        a.CritDamage -= b.CritDamage;
        a.ElementBonus -= b.ElementBonus;
        return a;
    }

    public static CharacterStats operator *(CharacterStats a, double b)
    {
        a.Health = (int)(a.Health * b);
        a.Attack = (int)(a.Attack * b);
        a.Defence = (int)(a.Defence * b);
        a.Energy = (int)(a.Energy * b);
        a.CritRate = a.CritRate * ((float)b);
        a.CritDamage = a.CritDamage * ((float)b);
        a.ElementBonus = a.ElementBonus * ((float)b);
        return a;
    }
}
[System.Serializable]
public struct TextStats
{
    public string Introduction;                          // 介绍
    public string Description;                           // 描述
}

// ==================== 枚举定义 ====================
//绎语类型枚举
public enum ExotextType
{
    VotiveEmber,    // 愿烬,   第一章 · 暖光之巢,     愿烬 · 暖光余温, 愿烬 · 万家灯火
    ThoughtChime,   // 鸣思,   第二章 · 纯白回廊,     鸣思 · 纯白回响
    EdgeText,       // 锋语,   第三章 · 纯白回廊,     锋语 · 纯白之誓
    ThreadShot,     // 射缕,   第四章 · 初萌原野,     双声 · 初萌对话
    StellarScribe,  // 诠星杖, 第五章 · 痴迷工坊,     诠星杖 · 痴迷之笔
    DuoVoice,       // 双声,   第六章 · 痴迷工坊, 	   射缕 · 未寄的信
    MnemonicTool,   // 刻忆器, 第七章 · 万物共鸣之厅, 刻忆器 · 纯白之书
}
//络身类型枚举
public enum NexusVesturePosition
{
    CogniThread,    // 思缕
    TangibleNexus,  // 触络
    AbyssalHeart,   // 渊心
    VolitionVein,   // 志脉
    ImprintStep     // 迹印
}
// 任务状态枚举
// 任务运行时状态（持久化到 activeQuests 中的状态）
public enum QuestProgressState
{
    Locked,     // 未激活（前置任务未完成）
    Available,  // 激活但未开始（前置任务已完成，可交互）
    Completed   // 已完成
}
// 任务目标类型枚举（可根据游戏需要扩展）
public enum QuestObjectiveType
{
    KillEnemy,      // 击败特定敌人
    ReachLocation,  // 到达指定地点
    CollectItem,    // 收集物品
    TalkToNPC,      // 与NPC对话
    CompletePuzzle, // 完成解谜
    UseSkill,       // 使用特定技能
    ProtectTarget,  // 保护目标
    SurviveTime,    // 存活一段时间
    BossFight,      // 击败BOSS
    Any             // 无条件（自动完成）
}
public static class ExperienceCurve
{
    public static int RequiredExp(int level)
    {
        return 50 * level * level + 150 * level + 200;
    }
}