using UnityEngine;

public class SkeletonIdleState : EnemyState
{
    private Enemy_Skeleton enemy;
    private float idleTimer;

    public SkeletonIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        idleTimer = enemy.idleTime;  // 空闲等待时间
        enemy.SetZeroVelocity();     // 停止移动
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        idleTimer -= Time.deltaTime;

        // 如果检测到玩家，进入战斗状态
        if (enemy.PlayerDetected())
        {
            stateMachine.ChangeState(enemy.battleState);
        }
        // 空闲时间结束，进入移动状态（巡逻）
        else if (idleTimer <= 0)
        {
            stateMachine.ChangeState(enemy.moveState);
        }
    }
}