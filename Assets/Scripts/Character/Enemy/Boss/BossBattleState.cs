using UnityEngine;

public class BossBattleState : EnemyState
{
    private Enemy_Boss boss;
    private Transform playerTransform;
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

        timeSinceLastRangedAttack += Time.deltaTime;

        // 远程攻击判定（按间隔切换）
        if (timeSinceLastRangedAttack >= boss.rangedAttackInterval)
        {
            stateMachine.ChangeState(boss.rangedAttackState);
            return;
        }

        // 近战攻击判定
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

        // ★ 关键改动：不再有 battleTimer 倒计时，仅当玩家彻底脱离检测范围才脱战
        if (!boss.PlayerDetected())
        {
            stateMachine.ChangeState(boss.idleState);
        }
    }
}