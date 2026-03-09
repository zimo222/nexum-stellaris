using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {

    }

    public override void Enter()
    {
        base.Enter();

        player.SetZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        //添加了 && (!player.IsWallDetected() || xInput != player.facingDir) 部分，保证贴墙且面对墙不能走，但贴墙背对墙可以走
        if ((xInput != 0 || yInput != 0) && !player.isBusy && (!player.IsWallDetected() || xInput != player.facingxDir))
        {
            stateMachine.ChangeState(player.moveState);
        }
    }
}
