using UnityEngine;

public class Enemy_BossAnimationTriggers : MonoBehaviour
{
    private Enemy_Boss boss => GetComponentInParent<Enemy_Boss>();

    // 在动画的最后一帧调用，通知状态机动画已完成
    public void AnimationTrigger()
    {
        boss.AnimationFinishTrigger();
    }

    // 在攻击动画的特定帧调用，用于近战伤害判定
    private void AttackTrigger()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(boss.attackCheck.position, boss.attackCheckRadius);
        foreach (var hit in colliders)
        {
            Player player = hit.GetComponent<Player>();
            if (player != null)
            {
                CombatManager.Instance.ApplyDamage(boss.gameObject, player.gameObject, boss.attackDamage);
            }
        }
    }

    // 以下两个方法用于防反窗口（如果 Boss 也需要被弹反，可保留）
    protected void OpenCounterWindow() => boss.OpenCounterAttackWindow();
    protected void CloseCounterWindow() => boss.CloseCounterAttackWindow();
}