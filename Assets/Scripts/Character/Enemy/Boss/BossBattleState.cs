using UnityEngine;

public class BossBattleState : EnemyState
{
    private Enemy_Boss boss;
    private Transform playerTransform;
    private float battleTimer;
    private float timeSinceLastRangedAttack;

    public BossBattleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        boss = _enemyBase as Enemy_Boss;
    }

    public override void Enter()
    {
        base.Enter();
        playerTransform = CombatManager.Instance?.Player?.transform;
        battleTimer = boss.battleTime;
        timeSinceLastRangedAttack = 0f;
    }

    public override void Update()
    {
        base.Update();

        if (playerTransform == null)
        {
            stateMachine.ChangeState(boss.idleState);
            return;
        }

        battleTimer -= Time.deltaTime;
        timeSinceLastRangedAttack += Time.deltaTime;

        // 1. 远程攻击判定（间隔到了就切换）
        if (timeSinceLastRangedAttack >= boss.rangedAttackInterval)
        {
            stateMachine.ChangeState(boss.rangedAttackState);
            return;
        }

        // 2. 近战攻击判定
        if (boss.PlayerInAttackRange())
        {
            if (Time.time >= boss.lastTimeAttacked + boss.attackCooldown)
            {
                stateMachine.ChangeState(boss.attackState);
            }
            else
            {
                boss.SetZeroVelocity(); // 冷却中停止移动
            }
        }
        else
        {
            boss.MoveToPosition(playerTransform.position);
        }

        // 3. 脱离战斗（超时或丢失玩家）
        if (battleTimer <= 0 || !boss.PlayerDetected())
        {
            stateMachine.ChangeState(boss.idleState);
        }
    }
}