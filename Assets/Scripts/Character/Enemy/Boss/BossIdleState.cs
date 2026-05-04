using UnityEngine;

public class BossIdleState : EnemyState
{
    private Enemy_Boss boss;
    private float idleTimer;

    public BossIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        boss = _enemyBase as Enemy_Boss;
    }

    public override void Enter()
    {
        base.Enter();
        idleTimer = boss.idleTime;
        boss.SetZeroVelocity();
    }

    public override void Update()
    {
        base.Update();
        idleTimer -= Time.deltaTime;

        // ¼ì²âµ½Íæ¼Ò ¡ú ½øÈëÕ½¶·×´Ì¬
        if (boss.PlayerDetected())
        {
            stateMachine.ChangeState(boss.battleState);
        }
        else if (idleTimer <= 0)
        {
            stateMachine.ChangeState(boss.moveState); // ¿ÕÏÐ½áÊø ¡ú Ñ²ÂßÒÆ¶¯
        }
    }
}