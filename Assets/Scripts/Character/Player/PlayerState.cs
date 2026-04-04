using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    protected PlayerStateMachine stateMachine;
    protected Player player;

    protected Rigidbody2D rb;

    protected float xInput;
    protected float yInput;
    private string animBoolName;

    protected float stateTimer;
    protected bool triggerCalled;

    public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }

    public virtual void Enter()
    {
        player.anim.SetBool(animBoolName, true);
        rb = player.rb;
        triggerCalled = false;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;

        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");
        player.anim.SetFloat("xVelocity", rb.velocity.x);
        player.anim.SetFloat("yVelocity", rb.velocity.y);
        player.anim.SetFloat("xDir", player.dashDirection.x);
        player.anim.SetFloat("yDir", player.dashDirection.y);
        CheckSpellSlotInput();
    }

    public virtual void Exit()
    {
        player.anim.SetBool(animBoolName, false);
    }

    public virtual void AnimationFinishTrigger()
    {
        triggerCalled = true;
    }

    private void CheckSpellSlotInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) player.selectedSpellIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) player.selectedSpellIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) player.selectedSpellIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) player.selectedSpellIndex = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) player.selectedSpellIndex = 4;
        else if (Input.GetKeyDown(KeyCode.Alpha6)) player.selectedSpellIndex = 5;
        else if (Input.GetKeyDown(KeyCode.Alpha7)) player.selectedSpellIndex = 6;
    }
}
