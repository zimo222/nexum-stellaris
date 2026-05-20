using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    public PlayerMoveState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {

    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        //player.SetVelocity(xInput * player.moveSpeed, yInput * player.moveSpeed);
        // 如果正在跳跃，不允许设置垂直速度（或者只允许设置水平速度）
        if (player.isJumping)
        {
            player.SetVelocity(xInput * player.totalMoveSpeed, rb.velocity.y);
        }
        else
        {
            player.SetVelocity(xInput * player.totalMoveSpeed, yInput * player.totalMoveSpeed);
        }

        if ((xInput == 0 && yInput == 0) || player.IsWallDetected())
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}
