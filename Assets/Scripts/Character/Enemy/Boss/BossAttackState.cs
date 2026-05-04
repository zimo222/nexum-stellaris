using UnityEngine;

public class BossAttackState : EnemyState
{
    private Enemy_Boss boss;
    private bool hasAttacked;

    public BossAttackState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        boss = _enemyBase as Enemy_Boss;
    }

    public override void Enter()
    {
        base.Enter();
        hasAttacked = false;
        boss.SetZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();
        boss.lastTimeAttacked = Time.time;
    }

    public override void Update()
    {
        base.Update();

        // 假设动画事件触发时调用 AnimationFinishTrigger()，triggerCalled 变为 true
        if (!hasAttacked && triggerCalled)
        {
            hasAttacked = true;
            // 攻击判定（使用 enemy.attackCheck 区域）
            Collider2D[] colliders = Physics2D.OverlapCircleAll(boss.attackCheck.position, boss.attackCheckRadius);
            foreach (var hit in colliders)
            {
                Player player = hit.GetComponent<Player>();
                if (player != null)
                {
                    CombatManager.Instance.ApplyDamage(boss.gameObject, player.gameObject, boss.attackDamage);
                }
            }
        }

        if (triggerCalled)
        {
            stateMachine.ChangeState(boss.battleState);
        }
    }
}