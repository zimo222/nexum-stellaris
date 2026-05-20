using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
        : base(_player, _stateMachine, _animBoolName) { }

    public override void Enter()
    {
        base.Enter();
        player.jumpStartY = player.transform.position.y;
        player.isJumping = true;
        rb.velocity = new Vector2(rb.velocity.x, player.jumpForce);
    }

    public override void Update()
    {
        base.Update();

        // 应用自定义重力（使上升速度逐渐减小）
        float newYVelocity = rb.velocity.y - player.gravityScale * Time.deltaTime;
        rb.velocity = new Vector2(xInput * player.totalMoveSpeed, newYVelocity);

        // 根据水平速度翻转
        player.FlipController(xInput, yInput);

        // 当速度转为向下时，切换到空中状态
        if (rb.velocity.y < 0)
        {
            stateMachine.ChangeState(player.airState);
        }
    }
}