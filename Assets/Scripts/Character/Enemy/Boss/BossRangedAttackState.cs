using UnityEngine;
using System.Collections;

public class BossRangedAttackState : EnemyState
{
    private Enemy_Boss boss;
    private float stateDuration;
    private Coroutine fireCoroutine;
    private float spiralAngle;       // 螺旋模式专用
    private float sweepOffset;       // SweepCircle当前偏移角度
    private int sweepDirection = 1;  // 1: 增加, -1: 减少

    public BossRangedAttackState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Boss _boss)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        boss = _boss;
    }

    public override void Enter()
    {
        base.Enter();
        stateDuration = 0f;
        // 远程攻击期间静止不动
        boss.SetZeroVelocity();

        fireCoroutine = boss.StartBossCoroutine(FireRoutine());
    }

    public override void Exit()
    {
        base.Exit();
        if (fireCoroutine != null)
            boss.StopBossCoroutine(fireCoroutine);
        spiralAngle = 0f;
        sweepOffset = 0f;
        sweepDirection = 1;
    }

    public override void Update()
    {
        base.Update();
        stateDuration += Time.deltaTime;
        if (stateDuration >= boss.rangedAttackDuration)
        {
            stateMachine.ChangeState(boss.battleState);
        }
    }

    private IEnumerator FireRoutine()
    {
        float interval = 1f / boss.bulletDensity;
        WaitForSeconds wait = new WaitForSeconds(interval);

        while (true)
        {
            switch (boss.fireMode)
            {
                case Enemy_Boss.FireMode.RandomDirection:
                    FireRandomDirection();
                    break;
                case Enemy_Boss.FireMode.AimAtPlayer:
                    FireAimAtPlayer();
                    break;
                case Enemy_Boss.FireMode.Spiral:
                    FireSpiral();
                    break;
                case Enemy_Boss.FireMode.Burst:
                    yield return boss.StartBossCoroutine(FireBurst());
                    yield return wait; // 连发后额外等待
                    break;
                case Enemy_Boss.FireMode.Circle360:
                    FireCircle360();
                    break;
                case Enemy_Boss.FireMode.SweepCircle:
                    FireSweepCircle();
                    break;
            }
            yield return wait;
        }
    }

    private void FireRandomDirection()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        SpawnBullet(dir);
    }

    private void FireAimAtPlayer()
    {
        if (CombatManager.Instance?.Player == null) return;
        Vector2 dir = (CombatManager.Instance.Player.transform.position - boss.transform.position).normalized;
        SpawnBullet(dir);
    }

    private void FireSpiral()
    {
        spiralAngle += 15f;
        float rad = spiralAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        SpawnBullet(dir);
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < boss.burstCount; i++)
        {
            FireRandomDirection();
            yield return new WaitForSeconds(boss.burstInterval);
        }
    }

    /// <summary>
    /// 圆圈式：一次发射 circleBulletCount 颗子弹，均匀分布360°
    /// </summary>
    private void FireCircle360()
    {
        int count = boss.circleBulletCount;
        if (count <= 0) return;
        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            SpawnBullet(dir);
        }
    }

    /// <summary>
    /// 变化型：基于圆圈式，但整体偏移角度在 0~sweepOffsetRange 之间来回渐变
    /// 每次发射 sweepBulletCount 颗子弹，偏移 sweepOffset 度
    /// </summary>
    private void FireSweepCircle()
    {
        int count = boss.sweepBulletCount;
        if (count <= 0) return;

        // 更新偏移角度（来回摆动）
        sweepOffset += sweepDirection * boss.sweepStep;
        if (sweepOffset >= boss.sweepOffsetRange)
        {
            sweepOffset = boss.sweepOffsetRange;
            sweepDirection = -1;
        }
        else if (sweepOffset <= 0)
        {
            sweepOffset = 0;
            sweepDirection = 1;
        }

        float baseAngleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * baseAngleStep + sweepOffset;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            SpawnBullet(dir);
        }
    }

    private void SpawnBullet(Vector2 direction)
    {
        if (boss.bulletPrefabs == null || boss.bulletPrefabs.Length == 0) return;
        GameObject prefab = boss.bulletPrefabs[Random.Range(0, boss.bulletPrefabs.Length)];
        GameObject bullet = Object.Instantiate(prefab, boss.transform.position, Quaternion.identity);
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript == null) bulletScript = bullet.AddComponent<EnemyBullet>();
        bulletScript.Initialize(direction, boss.bulletSpeed, boss.bulletDamage, boss.gameObject);
    }
}