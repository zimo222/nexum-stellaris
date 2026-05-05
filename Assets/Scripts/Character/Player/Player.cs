using System.Collections;
using System.Collections.Generic;
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
            CombatManager.Instance.RegisterPlayer(gameObject);
        }

        stateMachine.Initialize(idleState);
        DeadlockDetector.Log("[Player] Start end");
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
        if (playerData.CurrentEnergy < 10) return;

        CombatManager.Instance.CostEnergy(this.gameObject, 10);

        List<string> moduleIds = PlayerDataManager.Instance.GetWeaponModuleList(selectedSpellIndex);
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
}