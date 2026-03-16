using UnityEngine;

public class SkeletonGroundedState : EnemyState
{
    protected Enemy_Skeleton enemy;
    protected Transform player;

    public SkeletonGroundedState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();

        // 从 CombatManager 获取玩家 Transform
        if (CombatManager.Instance != null && CombatManager.Instance.Player != null)
        {
            player = CombatManager.Instance.Player.transform;
        }
        else
        {
            Debug.LogWarning("CombatManager 或 Player 未找到，请确保玩家已注册到 CombatManager");
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (player == null) return;

        // 检测玩家是否进入战斗触发范围（例如攻击距离的2倍，可自定义）
        float distanceToPlayer = Vector2.Distance(enemy.transform.position, player.position);
        if (distanceToPlayer < enemy.attackDistance * 2f) // 也可单独设置一个 battleTriggerDistance
        {
            stateMachine.ChangeState(enemy.battleState);
        }
    }
}