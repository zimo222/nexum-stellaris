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

        // 第三阶段：使用普通攻击动画（Attack = true）
        boss.anim.SetBool("Attack", true);

        // 启动协程，等待动画的某个时刻造成伤害 + 生成黑环特效
        boss.StartCoroutine(PerformPhaseAttack());
    }

    private IEnumerator PerformPhaseAttack()
    {
        // 等待动画关键帧（0.3秒时生成黑环特效）
        yield return new WaitForSeconds(0.3f);

        // 生成黑色圆环特效（以boss为中心，迅速扩大）
        SpawnBlackRing();

        // 再等待0.2秒，到达伤害判定帧（总共0.5秒）
        yield return new WaitForSeconds(0.2f);

        if (!hasDamaged)
        {
            hasDamaged = true;
            string questId = CombatManager.Instance.CurrentCombatQuestId;
            Debug.Log("CurrentCombatQuestIdL:" + questId);
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

    private void SpawnBlackRing()
    {
        // 尝试从boss获取黑环预制体引用（需要在Enemy_Boss中添加public字段）
        if (boss.blackRingPrefab != null)
        {
            GameObject ring = Object.Instantiate(boss.blackRingPrefab, boss.transform.position, Quaternion.identity);
            ring.transform.SetParent(null); // 不跟随boss移动
        }
        else
        {
            Debug.LogWarning("BossPhaseAttackState: blackRingPrefab is not assigned in Enemy_Boss!");
        }
    }

    public override void Exit()
    {
        base.Exit();
        // 退出时重置Attack参数（可选，如果其他状态需要不同动画）
        boss.anim.SetBool("Attack", false);
    }
}