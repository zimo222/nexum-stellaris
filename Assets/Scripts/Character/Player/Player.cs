using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity
{
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
    public Vector2 dashDirection { get; private set; } = new Vector2 ( 0, -1 ); // 新增：八向冲刺方向

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

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        primaryAttack = new PlayerPrimaryAttackState(this, stateMachine, "Attack");
        counterAttack = new PlayerCounterAttackState(this, stateMachine, "CounterAttack");
        // 不再初始化 wallSlide, wallJump
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
        // 从GameDataManager获取子弹定义，假设ID为"PlayerBullet"
        BulletDefineSO bulletDef = GameDataManager.Instance.BulletDict["PlayerBullet"];
        if (bulletDef == null || bulletDef.prefab == null)
        {
            Debug.LogError("Bullet prefab not found!");
            return;
        }

        // 计算生成位置：玩家位置 + 面向方向 * 偏移量（例如0.5米）
        Vector2 spawnPos = (Vector2)transform.position + dashDirection * 0.5f;
        /*
        // 实例化子弹
        GameObject bulletObj = Instantiate(bulletDef.prefab, spawnPos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            // 伤害值 = 玩家攻击力（从PlayerData获取）
            int damage = playerData.BaseStats.Attack;
            bullet.Initialize(dashDirection, gameObject, bulletDef.speed, damage * bulletDef.damage);
        }
        */

        List<string> moduleIds = playerData.equippedModuleIds;
        List<SpellModuleSO> modules = new List<SpellModuleSO>();
        foreach (string id in moduleIds)
        {
            if (!string.IsNullOrEmpty(id) && GameDataManager.Instance.SpellModuleDict.ContainsKey(id))
                modules.Add(GameDataManager.Instance.SpellModuleDict[id]);
        }

        // 生成子弹
        GameObject bulletObj = Instantiate(bulletDef.prefab, spawnPos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            int damage = playerData.BaseStats.Attack;
            bullet.Initialize(dashDirection, gameObject, bulletDef.speed, (int)(damage * bulletDef.damage), modules);
        }
    }
}