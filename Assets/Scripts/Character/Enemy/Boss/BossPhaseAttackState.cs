using UnityEngine;
using System.Collections;

public class BossPhaseAttackState : EnemyState
{
    private Enemy_Boss boss;
    private bool hasDamaged;

    public BossPhaseAttackState(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName, Enemy_Boss boss)
        : base(enemyBase, stateMachine, animBoolName)
    {
        this.boss = boss;
    }

    public override void Enter()
    {
        base.Enter();
        hasDamaged = false;
        // 播放阶段攻击动画（例如生成一个环形特效）
        boss.anim.SetTrigger("PhaseAttack");
        // 启动协程，等待动画的某个时刻造成伤害
        boss.StartCoroutine(PerformPhaseAttack());
    }

    private IEnumerator PerformPhaseAttack()
    {
        yield return new WaitForSeconds(0.5f); // 等待动画关键帧
        if (!hasDamaged)
        {
            hasDamaged = true;
            string questId = CombatManager.Instance.CurrentCombatQuestId;
            if (questId == "MainQuest_005004")
            {
                CombatManager.Instance.EnemyDefeated(boss.transform.gameObject);
                // 前置战斗：不致死，直接胜利
                CombatManager.Instance.CombatVictory(); // 需要将 CombatVictory 设为 public
            }
            else if (questId == "MainQuest_005008")
            {
                // 最终战斗：触发致命伤害（会走记忆保护）
                CombatManager.Instance.ForceFatalDamageToPlayer();
            }
            else
            {
                // 其他情况（如果有）也可以走正常伤害或直接胜利
                CombatManager.Instance.ForceFatalDamageToPlayer();
            }
        }
        yield return new WaitForSeconds(0.5f);
        stateMachine.ChangeState(boss.battleState);
    }

    public override void Exit()
    {
        base.Exit();
        boss.anim.ResetTrigger("PhaseAttack");
    }
}