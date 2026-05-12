using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 类型枚举（可扩展）
/// </summary>
public enum BuffType
{
    HealthRegen,        // 持续回血
    MoveSpeedUp,        // 增加移速
    AttackSpeedUp,      // 增加攻速
    DefenseUp,          // 增加防御
    DamageReduction,    // 减伤
    EnergyRegen,        // 持续回能
}

/// <summary>
/// 单个 Buff 数据
/// </summary>
[System.Serializable]
public class Buff
{
    public BuffType type;
    public float duration;          // 总持续时间（秒）
    public float intensity;         // 强度（例如回血每次回复量，或移速增加百分比 0.2 表示+20%）
    public float tickInterval = 1f; // 间隔时间（仅对持续回血/回能有效）
    private float timer;
    private float tickTimer;

    public Buff(BuffType type, float duration, float intensity, float tickInterval = 1f)
    {
        this.type = type;
        this.duration = duration;
        this.intensity = intensity;
        this.tickInterval = tickInterval;
        this.timer = duration;
        this.tickTimer = 0f;
    }

    public bool Update(float deltaTime)
    {
        timer -= deltaTime;
        if (type == BuffType.HealthRegen || type == BuffType.EnergyRegen)
        {
            tickTimer += deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                return true; // 触发一次 tick
            }
        }
        return false;
    }

    public bool IsExpired => timer <= 0f;
}

/// <summary>
/// Buff 管理器，挂载在 Player 上
/// </summary>
public class BuffController : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private bool showDebugLog = false;

    private List<Buff> activeBuffs = new List<Buff>();
    private Player player;
    private PlayerData playerData;

    // 用于存储原始数值（以便恢复）
    private float originalMoveSpeed;
    private float originalAttackSpeed; // 如果你有攻速字段，需要定义
    private int originalDefence;

    private void Awake()
    {
        player = GetComponent<Player>();
        if (player == null)
            Debug.LogError("BuffController 需要挂载在 Player 物体上");

        // 记录原始值（假设 Player 类中有 moveSpeed，攻速可能在状态机中）
        originalMoveSpeed = player.moveSpeed;
        // 如果你有攻击速度字段，请在这里获取，例如 player.attackSpeed = 1f;
        // originalAttackSpeed = player.attackSpeed;
        originalDefence = PlayerDataManager.Instance.CurrentPlayerData.TotalDefence;
    }

    private void OnEnable()
    {
        // 订阅玩家数据变化事件，以便在属性变化时刷新原始防御（如果有装备变化）
        PlayerDataManager.Instance.OnPlayerDataChanged += OnPlayerDataChanged;
    }

    private void OnDisable()
    {
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnPlayerDataChanged -= OnPlayerDataChanged;
    }

    private void OnPlayerDataChanged(PlayerData data)
    {
        // 当玩家装备变化导致基础防御改变时，重新计算原始防御（用于恢复）
        originalDefence = data.TotalDefence;
        // 重新应用当前所有 Buff（因为防御加成需要基于当前原始值重新计算）
        ApplyAllBuffs();
    }

    private void Update()
    {
        if (activeBuffs.Count == 0) return;

        float dt = Time.deltaTime;
        bool needRefresh = false;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            Buff buff = activeBuffs[i];
            bool tickTrigger = buff.Update(dt);
            if (tickTrigger)
            {
                ApplyTickEffect(buff);
            }
            if (buff.IsExpired)
            {
                RemoveBuffAt(i);
                needRefresh = true;
            }
        }

        if (needRefresh)
            ApplyAllBuffs();
    }

    /// <summary>
    /// 添加一个 Buff
    /// </summary>
    public void AddBuff(BuffType type, float duration, float intensity, float tickInterval = 1f)
    {
        // 检查是否已存在同类型且未过期的 Buff，可以选择覆盖或叠加（这里简单实现：移除旧的同类型，再添加新的）
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].type == type)
            {
                RemoveBuffAt(i);
                i--;
            }
        }
        Buff newBuff = new Buff(type, duration, intensity, tickInterval);
        activeBuffs.Add(newBuff);
        ApplyAllBuffs();
        if (showDebugLog) Debug.Log($"添加 Buff: {type}, 强度: {intensity}, 持续: {duration}s");
    }

    /// <summary>
    /// 移除指定索引的 Buff
    /// </summary>
    private void RemoveBuffAt(int index)
    {
        if (index < 0 || index >= activeBuffs.Count) return;
        Buff buff = activeBuffs[index];
        activeBuffs.RemoveAt(index);
        if (showDebugLog) Debug.Log($"移除 Buff: {buff.type}");
    }

    /// <summary>
    /// 立即移除指定类型的所有 Buff
    /// </summary>
    public void RemoveBuffByType(BuffType type)
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].type == type)
                RemoveBuffAt(i);
        }
        ApplyAllBuffs();
    }

    /// <summary>
    /// 清空所有 Buff
    /// </summary>
    public void ClearAllBuffs()
    {
        activeBuffs.Clear();
        ApplyAllBuffs();
    }

    /// <summary>
    /// 应用所有持续型 Buff 的效果（移速、防御、攻速等）
    /// </summary>
    private void ApplyAllBuffs()
    {
        // 重置到原始值
        player.moveSpeed = originalMoveSpeed;
        // player.attackSpeed = originalAttackSpeed;  // 如果有攻速字段
        // 防御需要实时从 PlayerData 获取原始值（因为装备可能改变）
        originalDefence = PlayerDataManager.Instance.CurrentPlayerData.TotalDefence;
        int finalDefence = originalDefence;

        float moveSpeedBonus = 0f;
        float attackSpeedBonus = 0f;
        float defenceBonus = 0f;

        foreach (var buff in activeBuffs)
        {
            switch (buff.type)
            {
                case BuffType.MoveSpeedUp:
                    moveSpeedBonus += buff.intensity;
                    break;
                case BuffType.AttackSpeedUp:
                    attackSpeedBonus += buff.intensity;
                    break;
                case BuffType.DefenseUp:
                    defenceBonus += buff.intensity;
                    break;
                    // DamageReduction 通常在受伤时计算，不在属性中体现，单独处理
            }
        }

        player.moveSpeed = originalMoveSpeed * (1f + moveSpeedBonus);
        // player.attackSpeed = originalAttackSpeed * (1f + attackSpeedBonus);
        finalDefence = originalDefence + Mathf.RoundToInt(originalDefence * defenceBonus);
        // 需要修改玩家实时防御？你现在的防御是通过 TotalDefence 计算的，可能需要在 CombatManager 中动态获取加成
        // 为了简单，我们可以临时修改 PlayerData 的当前防御缓存？不推荐。更好的做法是在伤害计算时考虑防御加成。
        // 这里提供一种方案：在 BuffController 中存储一个防御加成值，然后在 CombatManager 计算伤害时调用 GetDefenseBonus()
    }

    /// <summary>
    /// 获取防御加成值（用于伤害计算）
    /// </summary>
    public float GetDefenseBonusPercent()
    {
        float bonus = 0f;
        foreach (var buff in activeBuffs)
        {
            if (buff.type == BuffType.DefenseUp)
                bonus += buff.intensity;
        }
        return bonus;
    }

    /// <summary>
    /// 获取减伤百分比（用于伤害计算）
    /// </summary>
    public float GetDamageReductionPercent()
    {
        float reduction = 0f;
        foreach (var buff in activeBuffs)
        {
            if (buff.type == BuffType.DamageReduction)
                reduction += buff.intensity;
        }
        return Mathf.Clamp01(reduction);
    }

    /// <summary>
    /// 应用周期性效果（回血/回能）
    /// </summary>
    private void ApplyTickEffect(Buff buff)
    {
        PlayerData pData = PlayerDataManager.Instance.CurrentPlayerData;
        if (pData == null) return;

        switch (buff.type)
        {
            case BuffType.HealthRegen:
                CombatManager.Instance.ApplyDamage(null, this.gameObject, -Mathf.RoundToInt(buff.intensity));
                if (showDebugLog) Debug.Log($"Buff 回血: +{buff.intensity}, 当前生命 {pData.CurrentHealth}");
                // 刷新 UI
                //CombatManager.Instance?.UpdateHealthSlider();
                break;
            case BuffType.EnergyRegen:
                int newEnergy = pData.CurrentEnergy + Mathf.RoundToInt(buff.intensity);
                pData.CurrentEnergy = Mathf.Min(newEnergy, pData.TotalEnergy);
                //CombatManager.Instance?.UpdateEnergySlider();
                break;
        }
    }

    /// <summary>
    /// 外部调用：增加防御 Buff（示例）
    /// </summary>
    public void AddDefenseBuff(float duration, float percentIncrease)
    {
        AddBuff(BuffType.DefenseUp, duration, percentIncrease);
    }

    /// <summary>
    /// 增加移速 Buff
    /// </summary>
    public void AddMoveSpeedBuff(float duration, float percentIncrease)
    {
        AddBuff(BuffType.MoveSpeedUp, duration, percentIncrease);
    }

    // 其他添加 Buff 的便捷方法...
}