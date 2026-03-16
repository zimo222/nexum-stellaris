using UnityEngine;

public class SkeletonMoveState : EnemyState
{
    private Enemy_Skeleton enemy;
    private Vector2 targetPatrolPoint;

    public SkeletonMoveState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        // 获取一个新的随机巡逻点
        targetPatrolPoint = enemy.GetRandomPatrolPoint();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        // 移动向目标点
        enemy.MoveToPosition(targetPatrolPoint);

        // 如果检测到玩家，进入战斗状态
        if (enemy.PlayerDetected())
        {
            stateMachine.ChangeState(enemy.battleState);
        }
        // 如果到达目标点附近（距离小于0.5），回到空闲状态
        else if (Vector2.Distance(enemy.transform.position, targetPatrolPoint) < 0.5f)
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}