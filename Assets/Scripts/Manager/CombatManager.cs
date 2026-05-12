using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // 添加 UI 命名空间
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

    public string CurrentCombatQuestId { get; private set; }

    // 在类中添加一个标志位
    private bool isProcessingMemoryProtection = false; 
    
    [Header("Memory Protection")]
    public bool enableTimeStopForMemory = true;   // 勾选则时停，不勾选则任何情况下都不改变 Time.timeScale

    [Header("Memory Protection (No Time Stop)")]
    [SerializeField] private PromptTextManager promptTextManager;  // 拖拽赋值
    [SerializeField] private string[] memoryLines;                 // 纯白的台词数组（Inspector中填入15句）
    private bool isMemoryProtectionEnabled = false;
    private int remainingMemoryProtection = 0;
    private bool isProcessingFatal = false;  // 防止连续触发


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

        // 自动查找 PromptTextManager（不依赖手动拖拽）
        if (promptTextManager == null)
        {
            promptTextManager = FindObjectOfType<PromptTextManager>();
            if (promptTextManager == null)
            {
                Debug.LogWarning("CombatManager: 场景中未找到 PromptTextManager，纯白记忆台词将无法显示。");
            }
        }
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
            playerData.CurrentHealth = Math.Min(playerData.TotalHealth, playerData.CurrentHealth);
            if(amount > 0) playerComp.Damage(); // 触发受伤特效等

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
            healthSliderA.maxValue = player.TotalHealth;
            healthSliderA.value = player.CurrentHealth;
            healthSliderB.maxValue = player.TotalHealth;
            healthSliderB.value = player.CurrentHealth;
            healthText.text = player.CurrentHealth.ToString() + "/" + player.TotalHealth.ToString();
            return;
        }

        if (Player == null) return;

        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        if (playerData == null) return;

        // 设置 Slider 的最大值和当前值
        healthSliderA.maxValue = playerData.TotalHealth;
        healthSliderA.value = playerData.CurrentHealth;
        healthSliderB.maxValue = playerData.TotalHealth;
        healthSliderB.value = playerData.CurrentHealth;
        healthText.text = playerData.CurrentHealth.ToString() + "/" + playerData.TotalHealth.ToString();
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
            playerData.CurrentHealth = (int)playerData.TotalHealth;
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
        playerData.CurrentHealth = (int)playerData.TotalHealth;

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
        playerData.CurrentHealth = (int)playerData.TotalHealth;
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
        if (!isMemoryProtectionEnabled || remainingMemoryProtection <= 0 || isProcessingFatal)
        {
            PlayerDefeated();
            return;
        }

        isProcessingFatal = true;

        // 血条已经在 ApplyDamage 中归零（因为玩家血量变为0）
        // 先停顿一下，让玩家看清血条空了
        StartCoroutine(DelayedRegenWithMessage());

        // 减少保护次数
        remainingMemoryProtection--;
        // 注意：isProcessingFatal 的恢复要放到回血完成之后，避免多次触发
    }

    private System.Collections.IEnumerator DelayedRegenWithMessage()
    {
        // 停顿 0.3~0.5 秒，让玩家看到“死亡”状态
        yield return new WaitForSeconds(1.0f);

        // 随机选一句纯白台词
        string line = memoryLines[UnityEngine.Random.Range(0, memoryLines.Length)];
        if (promptTextManager != null)
            promptTextManager.ShowMessage(line, 2f);

        // 开始金色回血（1秒）
        if (healthRegenCoroutine != null)
            StopCoroutine(healthRegenCoroutine);
        healthRegenCoroutine = StartCoroutine(Co_RegenHealthGold(1f));

        // 等待回血完成再解锁（防止回血过程中再次触发致命伤害）
        while (healthRegenCoroutine != null)
            yield return null;

        isProcessingFatal = false;
    }

    private System.Collections.IEnumerator DelayedGoldRegen()
    {
        // 停顿0.5秒，期间血条保持为0
        yield return new WaitForSeconds(0.5f);

        // 开始金色回血（1秒）
        if (healthRegenCoroutine != null)
            StopCoroutine(healthRegenCoroutine);
        healthRegenCoroutine = StartCoroutine(Co_RegenHealthGold(1f));
    }


    private System.Collections.IEnumerator DelayedRecover()
    {
        yield return new WaitForSeconds(0.1f);  // 短暂延迟，让玩家看到血条空了

        // 回血
        RecoverPlayer();

        // 减少保护次数
        remainingMemoryProtection--;
        isProcessingFatal = false;
    }

    private void RecoverPlayer()
    {
        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        playerData.CurrentHealth = (int)playerData.TotalHealth;
        UpdateHealthSlider();
        // 可选：给予经验
        int expToGive = ExperienceCurve.RequiredExp(playerData.Level) / 2;
        PlayerDataManager.Instance.AddExperience(expToGive);
    }

    private System.Collections.IEnumerator ReleaseFatalLock()
    {
        yield return null;  // 等待一帧
        isProcessingFatal = false;
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

    private Coroutine healthRegenCoroutine;

    /// <summary>
    /// 金色回血协程：1秒内将血条从当前值平滑增加到满值，期间血条显示金色
    /// </summary>
    private IEnumerator Co_RegenHealthGold(float duration)
    {
        PlayerData playerData = PlayerDataManager.Instance.CurrentPlayerData;
        int targetHealth = (int)playerData.TotalHealth;
        int startHealth = playerData.CurrentHealth;  // 通常是0

        float elapsed = 0f;

        // 记录原始颜色，并改为金色
        Color originalColorA = healthSliderA.fillRect.GetComponentInChildren<UnityEngine.UI.Image>().color;
        Color originalColorB = healthSliderB.fillRect.GetComponentInChildren<UnityEngine.UI.Image>().color;

        SetSliderColor(Color.yellow);  // 金色

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            int newValue = Mathf.RoundToInt(Mathf.Lerp(startHealth, targetHealth, t));

            playerData.CurrentHealth = newValue;

            // 更新两个血条
            healthSliderA.value = newValue;
            healthSliderB.value = newValue;
            if (healthText != null)
                healthText.text = $"{newValue}/{targetHealth}";

            yield return null;
        }

        // 确保最终值为满血
        playerData.CurrentHealth = targetHealth;
        healthSliderA.value = targetHealth;
        healthSliderB.value = targetHealth;
        if (healthText != null)
            healthText.text = $"{targetHealth}/{targetHealth}";

        // 恢复原始颜色
        SetSliderColor(originalColorA, originalColorB);
        healthRegenCoroutine = null;
    }

    /// <summary> 设置两个血条滑块的颜色 </summary>
    private void SetSliderColor(Color color)
    {
        if (healthSliderA != null && healthSliderA.fillRect != null)
        {
            var img = healthSliderA.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = color;
        }
        if (healthSliderB != null && healthSliderB.fillRect != null)
        {
            var img = healthSliderB.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = color;
        }
    }

    /// <summary> 恢复原始颜色（需要你预先保存原始颜色）</summary>
    private void SetSliderColor(Color colorA, Color colorB)
    {
        if (healthSliderA != null && healthSliderA.fillRect != null)
        {
            var img = healthSliderA.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = colorA;
        }
        if (healthSliderB != null && healthSliderB.fillRect != null)
        {
            var img = healthSliderB.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = colorB;
        }
    }
}