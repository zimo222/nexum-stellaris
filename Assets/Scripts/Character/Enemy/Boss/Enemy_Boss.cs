using UnityEngine;
using System.Collections;

public class Enemy_Boss : Enemy
{
    [Header("Ranged Attack Settings")]
    public GameObject[] bulletPrefabs;
    public float rangedAttackInterval = 5f;
    public float rangedAttackDuration = 3f;
    [Tooltip("每秒发射的子弹数量（密度）")]
    public float bulletDensity = 5f;
    public float bulletSpeed = 5f;
    public int bulletDamage = 10;

    [Header("Ranged Attack Variation")]
    // Burst模式参数
    public int burstCount = 3;
    public float burstInterval = 0.2f;
    public enum FireMode { RandomDirection, AimAtPlayer, Spiral, Burst, Circle360, SweepCircle }
    public FireMode fireMode = FireMode.RandomDirection;


    // Circle360模式参数
    [Tooltip("360°圆圈一次发射的子弹数量")]
    public int circleBulletCount = 12;

    // SweepCircle模式参数
    [Tooltip("每次发射的子弹数量")]
    public int sweepBulletCount = 12;
    [Tooltip("偏移角度变化范围（度）")]
    public float sweepOffsetRange = 180f;
    [Tooltip("每次发射偏移角度的变化步长（度）")]
    public float sweepStep = 10f;

    // 在 Enemy_Boss 类中添加
    private int currentPhase = 0;
    private float[] phaseHealthThresholds = { 0.8f, 0.6f, 0.4f, 0.2f, 0.1f, 0.05f }; // 依次是75%、50%、25%血量

    private int lastHealth;          // 上一帧的血量
    private Coroutine phaseCheckCoroutine;

    // 状态实例
    public BossIdleState idleState { get; private set; }
    public BossMoveState moveState { get; private set; }
    public BossBattleState battleState { get; private set; }
    public BossAttackState attackState { get; private set; }
    public BossStunnedState stunnedState { get; private set; }
    public BossRangedAttackState rangedAttackState { get; private set; }

    public BossPhaseAttackState phaseAttackState { get; private set; }
    // 需要在 Awake 或 Start 中初始化

    protected override void Awake()
    {
        base.Awake();

        idleState = new BossIdleState(this, stateMachine, "Idle");
        moveState = new BossMoveState(this, stateMachine, "Move");
        battleState = new BossBattleState(this, stateMachine, "Battle");
        attackState = new BossAttackState(this, stateMachine, "Attack");
        stunnedState = new BossStunnedState(this, stateMachine, "Stunned");
        rangedAttackState = new BossRangedAttackState(this, stateMachine, "RangedAttack", this);

        phaseAttackState = new BossPhaseAttackState(this, stateMachine, "PhaseAttack", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
        lastHealth = currentHealth;
        phaseCheckCoroutine = StartCoroutine(CheckPhaseTransition());
    }

    public Coroutine StartBossCoroutine(IEnumerator coroutine)
    {
        return StartCoroutine(coroutine);
    }

    public void StopBossCoroutine(Coroutine coroutine)
    {
        if (coroutine != null) StopCoroutine(coroutine);
    }

    public override bool CanBeStunned()
    {
        if (base.CanBeStunned())
        {
            stateMachine.ChangeState(stunnedState);
            return true;
        }
        return false;
    }

    private IEnumerator CheckPhaseTransition()
    {
        while (true)
        {
            yield return null; // 每帧检查一次
            if (currentHealth < lastHealth)
            {
                // 血量减少，检查阶段
                float healthPercent = (float)currentHealth / maxHealth;
                // 假设你在 Inspector 或代码中定义了阶段阈值数组
                for (int i = currentPhase; i < phaseHealthThresholds.Length; i++)
                {
                    if (healthPercent <= phaseHealthThresholds[i])
                    {
                        currentPhase = i + 1;
                        // 切换到阶段攻击状态
                        stateMachine.ChangeState(phaseAttackState);
                        break;
                    }
                }
                lastHealth = currentHealth;
            }
        }
    }
}