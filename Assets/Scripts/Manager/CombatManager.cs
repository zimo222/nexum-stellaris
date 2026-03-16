using UnityEngine;
using System;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    // 事件
    public event Action OnBattleStart;
    public event Action OnPlayerVictory;
    public event Action OnPlayerDefeat;

    private GameObject player;
    private GameObject currentEnemy;

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
    }

    /// <summary>
    /// 注册玩家（通常在Player.Start中调用）
    /// </summary>
    public void RegisterPlayer(GameObject playerObj)
    {
        player = playerObj;
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
            playerComp.Damage(); // 触发受伤特效等

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

    private void PlayerDefeated()
    {
        Debug.Log("Player defeated!");
        OnPlayerDefeat?.Invoke();
        Time.timeScale = 0f; // 暂停游戏，可替换为显示失败UI
        // 可以添加更多逻辑，如重新开始等
    }

    private void EnemyDefeated(GameObject enemy)
    {
        Debug.Log("Enemy defeated!");
        OnPlayerVictory?.Invoke();
        Destroy(enemy);
        currentEnemy = null;
        Time.timeScale = 0f; // 胜利后暂停，或恢复时间
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
    }
}