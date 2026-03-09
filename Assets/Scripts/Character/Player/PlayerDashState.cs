using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDashState : PlayerState
{
    public PlayerDashState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // player.skill.clone.CreateClone(player.transform);  // 如果有克隆技能可保留
        stateTimer = player.dashDuration;
    }

    public override void Exit()
    {
        base.Exit();
        player.SetVelocity(0, rb.velocity.y);  // 重置水平速度，保留垂直速度
    }

    public override void Update()
    {
        base.Update();

        // 移除了墙检测切换
        player.SetVelocity(
            player.dashSpeed * player.dashDirection.x,
            player.dashSpeed * player.dashDirection.y
        );

        if (stateTimer < 0)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}