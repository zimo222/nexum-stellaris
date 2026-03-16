using UnityEngine;

public class SkeletonBattleState : EnemyState
{
    private Enemy_Skeleton enemy;
    private Transform playerTransform;
    private float battleTimer;

    public SkeletonBattleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        playerTransform = CombatManager.Instance.Player.transform;
        battleTimer = enemy.battleTime;  // 战斗状态持续时间，超时后可能返回巡逻
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (playerTransform == null)
        {
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        battleTimer -= Time.deltaTime;

        // 检查攻击范围
        if (enemy.PlayerInAttackRange())
        {
            // 如果攻击冷却结束，进入攻击状态
            if (Time.time >= enemy.lastTimeAttacked + enemy.attackCooldown)
            {
                stateMachine.ChangeState(enemy.attackState);
            }
            else
            {
                // 冷却中，原地等待或后退？这里简单设置为移动向玩家（但攻击距离内可能想保持距离？可根据需要调整）
                enemy.MoveToPosition(playerTransform.position);
            }
        }
        else
        {
            // 不在攻击范围内，向玩家移动
            enemy.MoveToPosition(playerTransform.position);
        }

        // 如果战斗时间过长或玩家丢失（超出检测范围），返回空闲状态（或巡逻）
        if (battleTimer <= 0 || !enemy.PlayerDetected())
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}