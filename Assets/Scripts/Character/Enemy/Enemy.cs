using System.Collections;
using UnityEngine;

public class Enemy : Entity
{
    [Header("Combat")]
    [SerializeField] protected LayerMask whatIsPlayer;
    public int maxHealth = 50;          // 最大生命值（可在Inspector调整）
    public int currentHealth;            // 当前生命值
    public int attackDamage = 10;        // 触碰伤害

    [Header("Stunned info")]
    public float stunDuration;
    public Vector2 stunDirection;
    protected bool canBeStunned;
    [SerializeField] protected GameObject counterImage;

    [Header("Move info")]
    public float moveSpeed = 3f;
    public float idleTime = 2f;         // 空闲等待时间
    public float battleTime = 5f;       // 战斗状态持续时间（例如追逐超时后返回巡逻）

    [Header("Attack info")]
    public float attackDistance = 2f;    // 攻击触发距离
    public float attackCooldown = 1f;
    [HideInInspector] public float lastTimeAttacked;

    [Header("Patrol info")]
    public float patrolRange = 5f;       // 以出生点为中心的巡逻半径
    private Vector2 startPosition;       // 初始位置（作为巡逻中心）

    public EnemyStateMachine stateMachine { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new EnemyStateMachine();
    }

    protected override void Start()
    {
        base.Start();
        startPosition = transform.position;
        currentHealth = maxHealth;        // 初始化生命值
    }

    protected override void Update()
    {
        base.Update();
        stateMachine.currentState.Update();
    }

    public virtual void OpenCounterAttackWindow()
    {
        canBeStunned = true;
        counterImage.SetActive(true);
    }

    public virtual void CloseCounterAttackWindow()
    {
        canBeStunned = false;
        counterImage.SetActive(false);
    }

    public virtual bool CanBeStunned()
    {
        if (canBeStunned)
        {
            CloseCounterAttackWindow();
            return true;
        }
        return false;
    }

    public virtual void AnimationFinishTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    // 检测玩家是否在攻击范围内（用于决定是否攻击）
    public virtual bool PlayerInAttackRange()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackDistance, whatIsPlayer);
        return playerCollider != null;
    }

    // 检测玩家是否在视线/感知范围内（用于进入战斗状态）
    public virtual bool PlayerDetected()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, battleTime * moveSpeed, whatIsPlayer); // 简单的半径检测
        return playerCollider != null;
    }

    // 获取玩家位置（需要场景中有一个标签为"Player"的对象）
    protected virtual Transform GetPlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    // 移动到目标位置（追逐或巡逻）
    public void MoveToPosition(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        SetVelocity(direction.x * moveSpeed, direction.y * moveSpeed);
    }

    // 巡逻逻辑：在起始点周围随机移动（可被状态调用）
    public Vector2 GetRandomPatrolPoint()
    {
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomY = Random.Range(-patrolRange, patrolRange);
        return startPosition + new Vector2(randomX, randomY);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (startPosition != Vector2.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPosition, patrolRange);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}