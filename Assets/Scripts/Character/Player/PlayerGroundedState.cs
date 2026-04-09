using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
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
        /*
        if (Input.GetKey(KeyCode.Q))
        {
            stateMachine.ChangeState(player.counterAttack);
        }
        */
        if (Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            // 根据具体按下的键设置 attackType
            if (Input.GetKey(KeyCode.J))
                player.attackType = 0;          // 例如：普通攻击
            else if (Input.GetKey(KeyCode.UpArrow))
                player.attackType = 1;          // 上方向攻击
            else if (Input.GetKey(KeyCode.DownArrow))
                player.attackType = 2;          // 下方向攻击
            else if (Input.GetKey(KeyCode.LeftArrow))
                player.attackType = 3;          // 左方向攻击
            else if (Input.GetKey(KeyCode.RightArrow))
                player.attackType = 4;          // 右方向攻击
            stateMachine.ChangeState(player.primaryAttack);
        }

        if (!player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.airState);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            stateMachine.ChangeState(player.jumpState);
        }
    }
}
