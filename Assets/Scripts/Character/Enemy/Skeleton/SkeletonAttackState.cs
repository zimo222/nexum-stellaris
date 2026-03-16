using UnityEngine;

public class SkeletonAttackState : EnemyState
{
    private Enemy_Skeleton enemy;
    private bool hasAttacked;

    public SkeletonAttackState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        hasAttacked = false;
        enemy.SetZeroVelocity(); // 攻击时停止移动
    }

    public override void Exit()
    {
        base.Exit();
        enemy.lastTimeAttacked = Time.time; // 记录攻击时间
    }

    public override void Update()
    {
        base.Update();

        // 当动画事件触发（triggerCalled 变为 true）时，执行一次攻击
        if (!hasAttacked && triggerCalled)
        {
            hasAttacked = true;

            // 使用攻击检测范围查找所有碰撞体
            Collider2D[] colliders = Physics2D.OverlapCircleAll(enemy.attackCheck.position, enemy.attackCheckRadius);
            foreach (var hit in colliders)
            {
                Player player = hit.GetComponent<Player>();
                if (player != null)
                {
                    // 通过战斗管理器造成伤害
                    CombatManager.Instance.ApplyDamage(enemy.gameObject, player.gameObject, enemy.attackDamage);
                }
            }
        }

        // 攻击动画播放完毕后，回到战斗状态
        if (triggerCalled && hasAttacked)
        {
            stateMachine.ChangeState(enemy.battleState);
        }
    }
}