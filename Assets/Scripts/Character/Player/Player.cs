using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class Player : Entity
{
    public static Player Instance { get; private set; }

    [Header("Attack details")]
    public Vector2[] attackMovement;
    public float counterAttackDuration = .2f;

    public bool isBusy { get; private set; }
    public bool isIdle;

    [Header("Move info")]
    public float moveSpeed = 12f;
    public float jumpForce;
    public float gravityScale = 20f;

    [Header("Dash info")]
    public float dashSpeed;
    public float dashDuration;
    public int attackType;

    [Header("Energy Regen")]
    public int energyRegenRate = 2;
    private float energyRegenTimer = 0f;

    public float jumpStartY { get; set; }
    public bool isJumping { get; set; }

    #region States
    public PlayerStateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerAirState airState { get; private set; }
    public PlayerDashState dashState { get; private set; }
    public PlayerPrimaryAttackState primaryAttack { get; private set; }
    public PlayerCounterAttackState counterAttack { get; private set; }
    #endregion

    private PlayerData playerData;

    [Header("Spell selection")]
    public int selectedSpellIndex = 0;

    public WeaponSlotsUI weaponSlotsUI;

    private bool isAwakeCalled = false;

    [Header("实时战斗数值")]
    private int magicCost;

    protected override void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        if (isAwakeCalled)
        {
            Debug.LogError("Player.Awake 递归调用被阻止！调用堆栈：\n" + System.Environment.StackTrace);
            return;
        }
        isAwakeCalled = true;

        if (GetComponent<NonSingletonMark>())
        {
            base.Awake();
            InitStates();
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        base.Awake();
        InitStates();
    }

    protected override void Start()
    {
        DeadlockDetector.Log("[Player] Start begin");
        base.Start();

        playerData = PlayerDataManager.Instance.CurrentPlayerData;
        if (playerData == null)
        {
            Debug.LogError("PlayerData is null!");
            return;
        }

        if (GetComponent<NonSingletonMark>() == null)
        {
            // 使用协程延迟注册，直到 CombatManager 实例就绪
            StartCoroutine(RegisterToCombatManagerWhenReady());
        }

        stateMachine.Initialize(idleState);
        DeadlockDetector.Log("[Player] Start end");
    }

    private System.Collections.IEnumerator RegisterToCombatManagerWhenReady()
    {
        // 等待最多 5 秒，直到 CombatManager.Instance 不为 null
        float timeout = 5f;
        float elapsed = 0f;
        while (CombatManager.Instance == null && elapsed < timeout)
        {
            yield return null; // 等待一帧
            elapsed += Time.deltaTime;
        }

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.RegisterPlayer(gameObject);
            DeadlockDetector.Log("[Player] Registered to CombatManager");
        }
        else
        {
            Debug.LogError("[Player] Failed to register to CombatManager: Instance still null after timeout");
        }
    }

    protected override void Update()
    {
        base.Update();
        stateMachine.currentState.Update();
        if (GetComponent<NonSingletonMark>()) return;
        CheckForDashInput();
        HandleEnergyRegen();
    }
    void OnEnable()
    {
        DeadlockDetector.Log("[Player] OnEnable");
    }

    private void InitStates()
    {
        stateMachine = new PlayerStateMachine();
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        primaryAttack = new PlayerPrimaryAttackState(this, stateMachine, "Attack");
        counterAttack = new PlayerCounterAttackState(this, stateMachine, "CounterAttack");
    }

    public IEnumerator BusyFor(float _seconds)
    {
        isBusy = true;
        yield return new WaitForSeconds(_seconds);
        isBusy = false;
    }

    public void AnimationTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    protected void CheckForDashInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal == 0 && vertical == 0) { }
        else
        {
            dashDirection = new Vector2(horizontal, vertical).normalized;
        }

        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K)) && !isJumping)
        {
            stateMachine.ChangeState(dashState);
        }
    }

    public void SpawnBullet()
    {


        List<string> moduleIds = PlayerDataManager.Instance.GetWeaponModuleList(selectedSpellIndex);

        magicCost = 5 * moduleIds.Count(id => !string.IsNullOrEmpty(id));

        // 先尝试触发特效（可能会设置 currentSpellManaReduction）
        TryTriggerSpecialEffect();

        if (playerData.CurrentEnergy < magicCost) return;

        CombatManager.Instance.CostEnergy(this.gameObject, magicCost);

        SpellSequence sequence = SpellSequenceBuilder.BuildSequence(moduleIds);

        SpellExecutor executor = GetComponent<SpellExecutor>();
        if (executor != null)
        {
            executor.ExecuteSequence(sequence);
        }
        else
        {
            Debug.LogError("玩家身上没有 SpellExecutor 组件");
        }
    }

    private void HandleEnergyRegen()
    {
        if (playerData == null) return;

        if (playerData.CurrentEnergy < playerData.BaseStats.Energy)
        {
            energyRegenTimer += Time.deltaTime;
            if (energyRegenTimer >= 0.1f)
            {
                int newEnergy = Mathf.Min(playerData.CurrentEnergy + energyRegenRate, playerData.BaseStats.Energy);
                energyRegenTimer -= 0.1f;
                CombatManager.Instance.CostEnergy(this.gameObject, playerData.CurrentEnergy - newEnergy);
            }
        }
        else
        {
            energyRegenTimer = 0f;
        }
    }

    protected void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ================= 心弦/绎动特效逻辑 =================
    private void TryTriggerSpecialEffect()
    {
        PlayerData pData = PlayerDataManager.Instance.CurrentPlayerData;
        if (pData == null) return;

        float heartStringRate = pData.TotalCritRate;    // 心弦率
        float yiDongValue = pData.TotalCritDamage;      // 绎动值

        //if (Random.value > heartStringRate) return;

        // 获取当前装备的武器数据
        ExotextData weapon = PlayerDataManager.Instance.GetEquippedExotext((ExotextType)selectedSpellIndex);
        if (weapon == null) return;

        // 获取武器定义
        if (!GameDataManager.Instance.ExotextDict.TryGetValue(weapon.Id, out var def)) return;
        if (def.possibleEffects == null || def.possibleEffects.Count == 0) return;

        // 随机选择一个特效
        foreach(var sf in def.possibleEffects)
        {
            if (Random.value > heartStringRate * sf.baseRateStrength) continue;
            //var effectDef = def.possibleEffects[Random.Range(0, def.possibleEffects.Count)];
            var effectDef = sf;
            float strength = effectDef.baseDamageStrength * yiDongValue;

            // 应用效果
            ApplySpecialEffect(effectDef, strength);

            // 显示 UI 消息（不再使用 PromptTextManager）
            //string message = effectDef.shortDesc;
            string template = effectDef.shortDesc;   // 从配置读取："<color=#0000FF>魔法消耗-{0:F0}%</color>"
            string message = string.Format(template, strength * 100f);
            if (MessagePopupController.Instance != null)
                MessagePopupController.Instance.ShowMessage(message);
        }

    }

    private void ApplySpecialEffect(SpecialEffectDefineSO effectDef, float strength)
    {
        PlayerData pData = PlayerDataManager.Instance.CurrentPlayerData;
        switch (effectDef.effectType)
        {
            case SpecialEffectType.WeaveMagic:
                magicCost = (int)((1 - strength) * magicCost);
                break;
            case SpecialEffectType.Recover:
                //pData.currentSpellManaReduction = Mathf.Clamp01(strength);
                CombatManager.Instance.ApplyDamage(null, this.gameObject, (int)(-this.playerData.TotalHealth * strength));
                break;
            case SpecialEffectType.Regenerate:
                //pData.currentSpellManaReduction = Mathf.Clamp01(strength);
                this.GetComponent<BuffController>().AddBuff(BuffType.HealthRegen, 5f, this.playerData.TotalHealth * strength, 1f);
                break;
            case SpecialEffectType.Echo:
                // 这里调用你的冷却减少系统，如果没有可以留空或实现简单逻辑
                Debug.Log($"回响触发：技能冷却减少 {strength * 100}%");
                // 例如: SkillManager.Instance.ReduceAllCooldowns(strength);
                break;
            case SpecialEffectType.Warmth:
                // 护盾系统（你需要自己实现）
                int maxHealth = pData.TotalHealth;
                int shield = Mathf.RoundToInt(maxHealth * strength);
                Debug.Log($"余温触发：获得 {shield} 点护盾");
                // 例如: ShieldManager.Instance.AddShield(shield);
                break;
            default:
                Debug.LogWarning($"未处理特效类型：{effectDef.effectType}");
                break;
        }
    }
}