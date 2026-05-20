using UnityEngine;

public class PlayerPrimaryAttackState : PlayerState
{
    private int comboCounter;
    private float lastTimeAttacked;
    private float comboWindow = 2;

    public PlayerPrimaryAttackState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
        : base(_player, _stateMachine, _animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // 不再设置 xInput = 0，允许移动输入

        // 连击计数器逻辑
        if (comboCounter > 2 || Time.time >= lastTimeAttacked + comboWindow)
            comboCounter = 0;

        player.anim.SetInteger("ComboCounter", comboCounter);
        //player.anim.speed = 1.2f;   // 加快动画速度，可根据需要调整

        // 不再设置初始速度，让 Update 中的移动逻辑处理
    }

    public override void Exit()
    {
        base.Exit();

        //player.StartCoroutine("BusyFor", .1f);   // 短暂忙碌，避免立即切换状态时误操作
        //player.anim.speed = 1;                    // 恢复动画速度
        comboCounter++;
        lastTimeAttacked = Time.time;
    }

    public override void Update()
    {
        base.Update();   // 更新 xInput, yInput 等
        /*
        // 移动逻辑：根据输入方向移动角色
        if (xInput != 0 || yInput != 0)
        {
            Vector2 moveDir = new Vector2(xInput, yInput).normalized;
            player.SetVelocity(moveDir.x * player.moveSpeed, moveDir.y * player.moveSpeed);
        }
        else
        {
            player.SetZeroVelocity();
        }
        */

        // 删除或注释掉原来强制设置速度的代码
        // player.SetVelocity(xInput * player.moveSpeed, yInput * player.moveSpeed);

        // 如果需要攻击时也可以移动，使用 AddForce 而不是直接赋值
        if (xInput != 0 || yInput != 0)
        {
            Vector2 moveDir = new Vector2(xInput, yInput).normalized;
            // 使用 ForceMode2D.Force 持续施加力，不会覆盖现有速度
            rb.AddForce(moveDir * player.totalMoveSpeed * 0.0001f, ForceMode2D.Force);
        }

        // 动画播放完毕（triggerCalled 由 AnimationFinishTrigger 事件设置）后回到 Idle
        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}