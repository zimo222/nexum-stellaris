using UnityEngine;
using UnityEngine.UI; // 添加 UI 命名空间
using System;
using TMPro;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    // 事件
    public event Action OnBattleStart;
    public event Action OnPlayerVictory;
    public event Action OnPlayerDefeat;

    // 公开玩家对象，供其他脚本（如敌人）访问
    public GameObject Player { get; private set; }

    private GameObject currentEnemy;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider; // 血条 Slider，可在 Inspector 中拖拽，或自动从子物体获取
    [SerializeField] private TMP_Text healthText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 如果没有手动赋值，则尝试从子物体获取 Slider
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
            if (healthSlider == null)
            {
                Debug.LogWarning("CombatManager: 未找到子物体 Slider，血条将无法显示。");
            }
        }
    }

    /// <summary>
    /// 注册玩家（通常在 Player.Start 中调用）
    /// </summary>
    public void RegisterPlayer(GameObject playerObj)
    {
        Player = playerObj;
        UpdateHealthSlider(); // 注册玩家时同步血量显示
    }

    /// <summary>
    /// 注册敌人（开始战斗时调用）
    /// </summary>
    public void RegisterEnemy(GameObject enemyObj)
    {
        currentEnemy = enemyObj;
        OnBattleStart?.Invoke();
    }

    /// <summary>
    /// 造成伤害
    /// </summary>
    /// <param name="source">伤害来源</param>
    /// <param name="target">伤害目标</param>
    /// <param name="amount">伤害量</param>
    public void ApplyDamage(GameObject source, GameObject target, int amount)
    {
        if (target == null) return;

        // 处理玩家受伤
        Player playerComp = target.GetComponent<Player>();
        if (playerComp != null)
        {
            PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
            playerData.CurrentHealth -= amount;
            Debug.Log(amount);
            playerComp.Damage(); // 触发受伤特效等

            // 血量变化后立即更新血条
            UpdateHealthSlider();

            if (playerData.CurrentHealth <= 0)
                PlayerDefeated();
            return;
        }

        // 处理敌人受伤
        Enemy enemyComp = target.GetComponent<Enemy>();
        if (enemyComp != null)
        {
            enemyComp.currentHealth -= amount;
            enemyComp.Damage();

            if (enemyComp.currentHealth <= 0)
                EnemyDefeated(enemyComp.gameObject);
        }
    }

    /// <summary>
    /// 更新血条显示（根据玩家当前血量）
    /// </summary>
    private void UpdateHealthSlider()
    {
        if (healthSlider == null) return;
        if (Player == null) return;

        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        if (playerData == null) return;

        // 设置 Slider 的最大值和当前值
        healthSlider.maxValue = playerData.BaseStats.Health;
        healthSlider.value = playerData.CurrentHealth;
        healthText.text = playerData.CurrentHealth.ToString() + "/" + playerData.BaseStats.Health.ToString();
    }

    private void PlayerDefeated()
    {
        Debug.Log("Player defeated!");
        OnPlayerDefeat?.Invoke();
        //Time.timeScale = 0f; // 暂停游戏，可替换为显示失败UI
        // 可以添加更多逻辑，如重新开始等
    }

    private void EnemyDefeated(GameObject enemy)
    {
        Debug.Log("Enemy defeated!");
        OnPlayerVictory?.Invoke();
        Destroy(enemy);
        currentEnemy = null;
        //Time.timeScale = 0f; // 胜利后暂停，或恢复时间
    }

    /// <summary>
    /// 开始战斗（可由触发战斗的脚本调用）
    /// </summary>
    public void StartBattle(GameObject enemy)
    {
        RegisterEnemy(enemy);
        // 重置玩家血量到满血（从最大血量同步）
        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        playerData.CurrentHealth = playerData.BaseStats.Health;

        // 血量重置后更新血条
        UpdateHealthSlider();
    }
}