using UnityEngine;

public class PlayerAirState : PlayerState
{
    public PlayerAirState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
        : base(_player, _stateMachine, _animBoolName) { }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
        player.isJumping = false;
    }

    public override void Update()
    {
        base.Update();

        // 继续应用重力
        float newYVelocity = rb.velocity.y - player.gravityScale * Time.deltaTime;
        rb.velocity = new Vector2(xInput * player.moveSpeed, newYVelocity);

        player.FlipController(xInput, yInput);

        // 落地条件：速度向下且 y <= 起跳高度
        if (rb.velocity.y <= 0 && player.transform.position.y <= player.jumpStartY)
        {
            Vector3 pos = player.transform.position;
            pos.y = player.jumpStartY;
            player.transform.position = pos;
            rb.velocity = new Vector2(rb.velocity.x, 0);
            stateMachine.ChangeState(player.idleState);
        }
    }
}