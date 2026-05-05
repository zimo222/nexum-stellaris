using UnityEngine;

public class Enemy_BossAnimationTriggers : MonoBehaviour
{
    private Enemy_Boss boss => GetComponentInParent<Enemy_Boss>();

    public void AnimationTrigger()
    {
        boss.AnimationFinishTrigger();
    }

    private void AttackTrigger()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(boss.attackCheck.position, boss.attackCheckRadius);
        foreach (var hit in colliders)
        {
            Player player = hit.GetComponent<Player>();
            if (player != null)
                CombatManager.Instance.ApplyDamage(boss.gameObject, player.gameObject, boss.attackDamage);
        }
    }

    protected void OpenCounterWindow() => boss.OpenCounterAttackWindow();
    protected void CloseCounterWindow() => boss.CloseCounterAttackWindow();

    // 新增：远程攻击 Intro 动画完成
    public void RangedAttackIntroFinished()
    {
        if (boss.stateMachine.currentState is BossRangedAttackState rangedState)
            rangedState.OnIntroFinished();
    }

    // 新增：远程攻击 Outro 动画完成
    public void RangedAttackOutroFinished()
    {
        if (boss.stateMachine.currentState is BossRangedAttackState rangedState)
            rangedState.OnOutroFinished();
    }
}