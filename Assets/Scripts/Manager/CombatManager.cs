using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // 添加 UI 命名空间

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
    [SerializeField] private Slider healthSliderA; // 血条 Slider，可在 Inspector 中拖拽，或自动从子物体获取
    [SerializeField] private Slider healthSliderB; // 血条 Slider，可在 Inspector 中拖拽，或自动从子物体获取
    [SerializeField] private Slider energySlider; // 血条 Slider，可在 Inspector 中拖拽，或自动从子物体获取
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text energyText;

    // 战斗任务状态
    private QuestDefineSO currentCombatQuest;   // 当前进行的战斗任务数据
    private int currentWaveIndex = -1;           // 当前波次索引（-1表示未开始）
    public List<GameObject> activeEnemies = new List<GameObject>(); // 当前存活的敌人列表
    private Vector2 combatSpawnCenter;

    private bool isAwakeCalled = false;

    // 在 class CombatManager 顶部添加
    [Header("Memory Protection")]
    [SerializeField] private MemoryProtectionUI memoryUI;
    private bool isMemoryProtectionEnabled = false;
    private int remainingMemoryProtection = 0;

    public string CurrentCombatQuestId { get; private set; }

    // 在类中添加一个标志位
    private bool isProcessingMemoryProtection = false;

    void Awake()
    {
        DeadlockDetector.Log("[CombatManager] Awake start");

        if (isAwakeCalled)
        {
            Debug.LogError("CombatManager.Awake 重复调用！");
            return;
        }
        isAwakeCalled = true;

        DeadlockDetector.Log("[CombatManager] isAwakeCalled set");

        // 非单例分支
        if (GetComponent<NonSingletonMark>())
        {
            DeadlockDetector.Log("[CombatManager] Non-singleton detected");
            InitSliderRefs();
            DeadlockDetector.Log("[CombatManager] Non-singleton init done, exit");
            return;
        }

        DeadlockDetector.Log("[CombatManager] Checking singleton instance");

        if (Instance != null && Instance != this)
        {
            DeadlockDetector.Log("[CombatManager] Duplicate instance, destroying self");
            Destroy(gameObject);
            return;
        }

        DeadlockDetector.Log("[CombatManager] Setting singleton");
        Instance = this;
        DontDestroyOnLoad(gameObject);
        DeadlockDetector.Log("[CombatManager] Singleton set, calling InitSliderRefs");
        InitSliderRefs();
        DeadlockDetector.Log("[CombatManager] Awake finished successfully");
    }

    void InitSliderRefs()
    {
        DeadlockDetector.Log("[CombatManager] InitSliderRefs start");
        if (healthSliderA == null)
        {
            DeadlockDetector.Log("[CombatManager] healthSliderA is null, searching in children");
            healthSliderA = GetComponentInChildren<Slider>();
            if (healthSliderA == null)
            {
                Debug.LogWarning("CombatManager: 未找到子物体 Slider，血条将无法显示。");
                DeadlockDetector.Log("[CombatManager] healthSliderA not found");
            }
        }
        DeadlockDetector.Log("[CombatManager] InitSliderRefs end");
    }

    /// <summary>
    /// 注册玩家（通常在 Player.Start 中调用）
    /// </summary>
    public void RegisterPlayer(GameObject playerObj)
    {
        Player = playerObj;
        if (GetComponent<NonSingletonMark>()) return;
        UpdateHealthSlider(); // 注册玩家时同步血量显示
        UpdateEnergySlider(); // 注册玩家时同步血量显示
        PlayerDataManager.Instance.OnPlayerDataChanged += UpdateHealthSlider;
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

            // 血量变化后立即更新血条
            UpdateHealthSlider();

            if (playerData.CurrentHealth <= 0)
                HandleFatalDamage();/*PlayerDefeated();*/
            return;
        }

        // 处理敌人受伤
        Enemy enemyComp = target.GetComponent<Enemy>();
        if (enemyComp != null)
        {
            enemyComp.currentHealth -= amount;
            enemyComp.Damage();

            if (enemyComp.currentHealth <= 0)
            {
                // 敌人死亡
                activeEnemies.Remove(target); // 从当前活动列表中移除
                EnemyDefeated(target); // 调用原有的 EnemyDefeated 方法（销毁敌人等）
                CheckWaveCompletion(); // 检查当前波次是否结束
            }
            return;
        }
    }

    public void CostEnergy(GameObject target, int amount)
    {
        // 处理玩家受伤
        Player playerComp = target.GetComponent<Player>();
        if (playerComp != null)
        {
            PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
            playerData.CurrentEnergy -= amount;
            UpdateEnergySlider();
            return;
        }
    }

    /// <summary>
    /// 更新血条显示（根据玩家当前血量）
    /// </summary>
    private void UpdateHealthSlider(PlayerData player = null)
    {
        if (healthSliderA == null) return;

        if(player != null)
        {
            healthSliderA.maxValue = player.BaseStats.Health;
            healthSliderA.value = player.CurrentHealth;
            healthSliderB.maxValue = player.BaseStats.Health;
            healthSliderB.value = player.CurrentHealth;
            healthText.text = player.CurrentHealth.ToString() + "/" + player.BaseStats.Health.ToString();
            return;
        }

        if (Player == null) return;

        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        if (playerData == null) return;

        // 设置 Slider 的最大值和当前值
        healthSliderA.maxValue = playerData.BaseStats.Health;
        healthSliderA.value = playerData.CurrentHealth;
        healthSliderB.maxValue = playerData.BaseStats.Health;
        healthSliderB.value = playerData.CurrentHealth;
        healthText.text = playerData.CurrentHealth.ToString() + "/" + playerData.BaseStats.Health.ToString();
    }

    /// <summary>
    /// 更新能条显示（根据玩家当前能量）
    /// </summary>
    private void UpdateEnergySlider(PlayerData player = null)
    {
        if (energySlider == null) return;

        if (player != null)
        {
            energySlider.maxValue = player.BaseStats.Energy;
            energySlider.value = player.CurrentEnergy;
            energyText.text = player.CurrentEnergy.ToString() + "/" + player.BaseStats.Energy.ToString();
            return;
        }

        if (Player == null) return;

        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        if (playerData == null) return;

        // 设置 Slider 的最大值和当前值
        energySlider.maxValue = playerData.BaseStats.Energy;
        energySlider.value = playerData.CurrentEnergy;
        energyText.text = playerData.CurrentEnergy.ToString() + "/" + playerData.BaseStats.Energy.ToString();
    }

    private void PlayerDefeated()
    {
        Debug.Log("Player defeated!");
        OnPlayerDefeat?.Invoke();

        // 如果正在进行战斗任务，则战斗失败
        if (currentCombatQuest != null)
        {
            CombatFailed();
        }
        // 否则，可能只是普通死亡，可以单独处理复活逻辑
        else
        {
            // 普通死亡重置血量（如果需要）
            PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
            playerData.CurrentHealth = playerData.BaseStats.Health;
        }
    }

    public void EnemyDefeated(GameObject enemy)
    {
        Debug.Log("Enemy defeated!");
        OnPlayerVictory?.Invoke(); // 这个事件可能不合适，需要区分玩家胜利还是敌人死亡？OnPlayerVictory 应是整个战斗胜利。建议改为 OnEnemyDefeated 事件。
        Destroy(enemy);
        // currentEnemy = null; // 这是单个敌人，不应清空，因为我们可能有多个敌人
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

    /// <summary>
    /// 开始一场战斗（由任务管理器调用）
    /// </summary>
    public void StartCombat(QuestDefineSO questData, Vector2 spawnCenter)
    {
        if (currentCombatQuest != null)
        {
            Debug.LogWarning("已有战斗正在进行，无法开始新战斗");
            return;
        }

        CurrentCombatQuestId = questData.id;
        currentCombatQuest = questData;
        currentWaveIndex = 0;
        activeEnemies.Clear();

        // 生成第一波敌人
        //SpawnWave(currentWaveIndex, spawnCenter);
        if(questData.id == "MainQuest_005008")
        {
            // 例如在 StartCombat 之前
            int memoryCount = 5; // 自定义次数，也可以从 LongTermMemory 实例获取记忆条数作为初始值
            CombatManager.Instance.EnableMemoryProtection(memoryCount);
        }

        currentCombatQuest = questData;
        combatSpawnCenter = spawnCenter;
        currentWaveIndex = 0;
        activeEnemies.Clear();
        SpawnWave(currentWaveIndex, combatSpawnCenter);
    }

    private void SpawnWave(int waveIndex, Vector2 spawnCenter)
    {
        if (currentCombatQuest == null || waveIndex >= currentCombatQuest.waves.Count)
        {
            Debug.LogError("波次索引无效");
            return;
        }

        WaveDefine wave = currentCombatQuest.waves[waveIndex];
        foreach (var spawnInfo in wave.enemies)
        {
            for (int i = 0; i < spawnInfo.count; i++)
            {
                // 生成位置：围绕中心点随机偏移（例如半径2米内的随机点）
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 2f;
                Vector2 spawnPos = spawnCenter + randomOffset;

                GameObject enemyObj = Instantiate(spawnInfo.enemyPrefab, spawnPos, Quaternion.identity);
                activeEnemies.Add(enemyObj);

                // 订阅敌人死亡事件（通过 Enemy 脚本的死亡通知，暂时用简单方法：在 Enemy 死亡时调用 CombatManager 的方法）
                // 我们将在敌人脚本中添加死亡回调
            }
        }

        Debug.Log($"生成第 {waveIndex + 1} 波敌人，共 {activeEnemies.Count} 个");
    }

    private void CheckWaveCompletion()
    {
        if (currentCombatQuest == null) return;

        // 如果当前波次没有存活的敌人了
        if (activeEnemies.Count == 0)
        {
            currentWaveIndex++;
            if (currentWaveIndex < currentCombatQuest.waves.Count)
            {
                // 还有下一波，生成下一波
                // 需要知道生成中心，可以在 StartCombat 时保存 spawnCenter
                // 我们在 StartCombat 中添加一个字段 Vector2 combatSpawnCenter
                SpawnWave(currentWaveIndex, combatSpawnCenter);
            }
            else
            {
                // 所有波次完成，战斗胜利
                CombatVictory();
            }
        }
    }

    public void CombatVictory()
    {
        CurrentCombatQuestId = null;
        Debug.Log("战斗胜利！");
        // 通知任务管理器任务完成
        if (currentCombatQuest != null)
        {
            QuestManager.Instance.CompleteQuest(currentCombatQuest.id);
        }
        // 清理战斗状态
        currentCombatQuest = null;
        currentWaveIndex = -1;
        activeEnemies.Clear();
    }

    public void CombatFailed()
    {
        if (currentCombatQuest == null) return;

        CurrentCombatQuestId = null;
        Debug.Log("战斗失败，重置任务");

        // 销毁所有生成的敌人
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();

        // 重置玩家血量
        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        playerData.CurrentHealth = playerData.BaseStats.Health;
        // 可以触发玩家复活动画等，这里简单处理

        // 通知任务管理器战斗失败，回退任务
        QuestManager.Instance.OnCombatFailed(currentCombatQuest.id);
        UpdateHealthSlider();

        // 清理战斗状态
        currentCombatQuest = null;
        currentWaveIndex = -1;
    }

    // 新增方法：由任务管理器在最终战开始时调用
    public void EnableMemoryProtection(int initialCount)
    {
        isMemoryProtectionEnabled = true;
        remainingMemoryProtection = initialCount;
    }

    // 修改 ForceFatalDamageToPlayer 方法（如果还没有则添加）
    public void ForceFatalDamageToPlayer()
    {
        if (Player == null) return;
        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        playerData.CurrentHealth = 0;
        UpdateHealthSlider();
        HandleFatalDamage();
    }

    // 新的核心处理方法
    // 在 CombatManager 类中添加这个协程（放在任何位置）
    private IEnumerator ChangeTimeScaleGradually(float targetScale, float duration, System.Action onComplete = null)
    {
        float startScale = Time.timeScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            Time.timeScale = Mathf.Lerp(startScale, targetScale, t);
            yield return null;
        }
        Time.timeScale = targetScale;
        onComplete?.Invoke();
    }

    // 修改你的 HandleFatalDamage 方法（之前应该是调用 PlayerDefeated 的地方改为这个）
    private void HandleFatalDamage()
    {
        if (isMemoryProtectionEnabled && remainingMemoryProtection > 0)
        {
            // 防止多个协程同时运行
            if (isProcessingMemoryProtection) return;
            isProcessingMemoryProtection = true;

            // 1. 时间渐变到0
            StartCoroutine(ChangeTimeScaleGradually(0f, 2.0f, () =>
            {
                // 2. 时间归零后，显示纯白记忆UI
                memoryUI.Show(() =>
                {
                    // 3. 玩家按空格后，先隐藏UI
                    memoryUI.Hide();

                    // 4. 恢复玩家血量 + 加经验
                    PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
                    playerData.CurrentHealth = playerData.BaseStats.Health;
                    UpdateHealthSlider();
                    int expToGive = ExperienceCurve.RequiredExp(playerData.Level) * 10;
                    PlayerDataManager.Instance.AddExperience(expToGive);
                    remainingMemoryProtection--;

                    // 5. 时间渐变回1
                    StartCoroutine(ChangeTimeScaleGradually(1f, 2.0f, () =>
                    {
                        isProcessingMemoryProtection = false;
                        // 战斗继续（自然恢复）
                    }));
                });
            }));
        }
        else
        {
            PlayerDefeated();
        }
    }


    // 协程：将 timeScale 从当前值渐变到 target（duration 秒）
    private IEnumerator GraduallyChangeTimeScale(float target, float duration)
    {
        float start = Time.timeScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 不受 timeScale 影响
            float t = elapsed / duration;
            Time.timeScale = Mathf.Lerp(start, target, t);
            yield return null;
        }
        Time.timeScale = target;
    }

    // 调用此方法实现时间减缓到0（常配合记忆保护）
    public void PauseGameGradually(float duration = 0.3f)
    {
        StartCoroutine(GraduallyChangeTimeScale(0f, duration));
    }

    // 恢复时间流速
    public void ResumeGameGradually(float duration = 0.3f)
    {
        StartCoroutine(GraduallyChangeTimeScale(1f, duration));
    }
}