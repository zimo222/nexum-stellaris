using UnityEngine;

public class BossStunnedState : EnemyState
{
    private Enemy_Boss boss;

    public BossStunnedState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        boss = _enemyBase as Enemy_Boss;
    }

    public override void Enter()
    {
        base.Enter();
        boss.fx.InvokeRepeating("RedColorBlink", 0, 0.1f);
        stateTimer = boss.stunDuration;
        rb.velocity = new Vector2(-boss.facingxDir * boss.stunDirection.x, boss.stunDirection.y);
    }

    public override void Exit()
    {
        base.Exit();
        boss.fx.Invoke("CancelRedBlink", 0);
    }

    public override void Update()
    {
        base.Update();
        if (stateTimer <= 0)
        {
            stateMachine.ChangeState(boss.idleState);
        }
    }
}