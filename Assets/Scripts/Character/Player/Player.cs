using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity
{

    // 单例实例
    public static Player Instance { get; private set; }

    [Header("Attack details")]
    public Vector2[] attackMovement;
    public float counterAttackDuration = .2f;

    public bool isBusy { get; private set; }
    public bool isIdle;

    [Header("Move info")]
    public float moveSpeed = 12f;
    public float jumpForce;          // 向上的初速度
    public float gravityScale = 20f; // 自定义重力加速度

    [Header("Dash info")]
    public float dashSpeed;
    public float dashDuration;
    public int attackType;

    [Header("Energy Regen")]
    public int energyRegenRate = 2;          // 每秒恢复量
    private float energyRegenTimer = 0f;


    public float jumpStartY { get; set; } // 记录起跳时的 y 坐标
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
    // 移除墙相关状态：wallSlide, wallJump
    #endregion

    private PlayerData playerData;


    [Header("Spell selection")]
    public int selectedSpellIndex = 0;

    public WeaponSlotsUI weaponSlotsUI;

    protected override void Awake()
    {
        // 单例检查：如果已存在实例且不是自己，则销毁当前对象
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // 确保单例在场景切换时不被销毁
        base.Awake();
        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        primaryAttack = new PlayerPrimaryAttackState(this, stateMachine, "Attack");
        counterAttack = new PlayerCounterAttackState(this, stateMachine, "CounterAttack");
    }

    protected override void Start()
    {
        base.Start();
        playerData = PlayerDataManager.Instance.CurrentPlayerData;
        // 确保玩家数据存在
        if (playerData == null)
            Debug.LogError("PlayerData is null!");

        // 注册到战斗管理器（阶段五）
        CombatManager.Instance.RegisterPlayer(gameObject);
        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();
        stateMachine.currentState.Update();
        CheckForDashInput();
        HandleEnergyRegen();   // 新增能量回复
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

        if (horizontal == 0 && vertical == 0)
        {
            // 无输入时默认朝当前面向方向
            //dashDirection = new Vector2(facingxDir, facingyDir);
        }
        else
        {
            dashDirection = new Vector2(horizontal, vertical).normalized;
        }

        // 冲刺键检测（注意：攻击状态下也能冲刺，会打断攻击）
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K)) && !isJumping)
        {
            stateMachine.ChangeState(dashState);
        }
    }

    /// <summary>
    /// 动画事件调用的生成子弹方法
    /// </summary>
    public void SpawnBullet()
    {
        if (playerData.CurrentEnergy < 10) return;

        CombatManager.Instance.CostEnergy(this.gameObject, 10);

        // 获取玩家装备的法术模块ID列表（从 PlayerData 中）
        List<string> moduleIds = PlayerDataManager.Instance.GetWeaponModuleList(selectedSpellIndex); // 假设有此字段
                                                               // 构建法术序列
        SpellSequence sequence = SpellSequenceBuilder.BuildSequence(moduleIds);

        // 获取 SpellExecutor 组件并执行
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


    // 新增方法：处理能量回复
    private void HandleEnergyRegen()
    {
        if (playerData == null) return;

        // 如果当前能量未满，则计时回复
        if (playerData.CurrentEnergy < playerData.BaseStats.Energy)
        {
            energyRegenTimer += Time.deltaTime;
            if (energyRegenTimer >= 0.1f)
            {
                // 每秒恢复固定值，但不超过上限
                int newEnergy = Mathf.Min(playerData.CurrentEnergy + energyRegenRate, playerData.BaseStats.Energy);
                //playerData.CurrentEnergy = newEnergy;
                energyRegenTimer -= 0.1f; // 保留多余时间，避免累积误差
                CombatManager.Instance.CostEnergy(this.gameObject, playerData.CurrentEnergy - newEnergy);
            }
        }
        else
        {
            // 能量已满时重置计时器
            energyRegenTimer = 0f;
        }
    }
}