using UnityEngine;

public class BossMoveState : EnemyState
{
    private Enemy_Boss boss;
    private Vector2 targetPatrolPoint;

    public BossMoveState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        boss = _enemyBase as Enemy_Boss;
    }

    public override void Enter()
    {
        base.Enter();
        targetPatrolPoint = boss.GetRandomPatrolPoint();
    }

    public override void Update()
    {
        base.Update();

        boss.MoveToPosition(targetPatrolPoint);

        if (boss.PlayerDetected())
        {
            stateMachine.ChangeState(boss.battleState);
        }
        else if (Vector2.Distance(boss.transform.position, targetPatrolPoint) < 0.5f)
        {
            stateMachine.ChangeState(boss.idleState);
        }
    }
}