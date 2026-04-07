using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    public event System.Action<GameObject> OnEnemyDied; // 敌人死亡时触发，参数为敌人自身

    // 在血量归零的地方触发（例如在 ApplyDamage 中，或者敌人自己的死亡逻辑）
    // 由于 CombatManager 通过 ApplyDamage 减少血量，我们在 ApplyDamage 中检测敌人死亡并调用事件？不，ApplyDamage 在 CombatManager 中，所以 CombatManager 可以直接处理。
    // 更简单：在 CombatManager.ApplyDamage 中，当敌人血量归零时，直接调用 EnemyDefeated，并在此处处理波次逻辑。
    // 这样不需要额外事件。


    [Header("UI")]
    [SerializeField] private Slider healthSlider;   // 拖拽血条Slider到此处

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


        // 初始化血条
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;       // 设置为最大血量，显示具体数值
            healthSlider.value = currentHealth;
        }
        else
        {
            Debug.LogWarning("Enemy: 未绑定血条Slider", this);
        }
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
        dashDirection = direction.normalized;
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



    // 重写 Damage 方法（假设 Entity 中有虚方法 Damage）
    public override void Damage()
    {
        base.Damage();          // 调用父类方法（如果有特效、动画等）
        UpdateHealthBar();      // 更新血条
    }



    // 更新血条显示
    private void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }
}