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

    // 状态实例
    public BossIdleState idleState { get; private set; }
    public BossMoveState moveState { get; private set; }
    public BossBattleState battleState { get; private set; }
    public BossAttackState attackState { get; private set; }
    public BossStunnedState stunnedState { get; private set; }
    public BossRangedAttackState rangedAttackState { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        idleState = new BossIdleState(this, stateMachine, "Idle");
        moveState = new BossMoveState(this, stateMachine, "Move");
        battleState = new BossBattleState(this, stateMachine, "Battle");
        attackState = new BossAttackState(this, stateMachine, "Attack");
        stunnedState = new BossStunnedState(this, stateMachine, "Stunned");
        rangedAttackState = new BossRangedAttackState(this, stateMachine, "RangedAttack", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
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
}