using UnityEngine;
using System.Collections;

public class BossRangedAttackState : EnemyState
{
    private enum Phase { Intro, Loop, Outro }
    private Phase currentPhase;

    private Enemy_Boss boss;
    private float stateDuration;          // 仅在 Loop 阶段计时
    private Coroutine fireCoroutine;
    private Transform playerTransform;

    // 弹幕模式私有变量
    private float spiralAngle;
    private float sweepOffset;
    private int sweepDirection = 1;

    public BossRangedAttackState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Boss _boss)
        : base(_enemyBase, _stateMachine, _animBoolName)
    {
        boss = _boss;
    }

    public override void Enter()
    {
        base.Enter();
        currentPhase = Phase.Intro;
        stateDuration = 0f;
        playerTransform = CombatManager.Instance?.Player?.transform;

        // 保证 Intro 期间不移动、不发射
        boss.SetZeroVelocity();

        // 注意：不要在这里启动 FireRoutine，等到 Intro 结束后再启动
        // 动画参数 RangedAttack 已经在 base.Enter 中设为 true，动画控制器会自动播放 Intro
    }

    public override void Exit()
    {
        base.Exit();
        StopFire();
        // 重置弹幕状态变量
        spiralAngle = 0f;
        sweepOffset = 0f;
        sweepDirection = 1;
    }

    public override void Update()
    {
        base.Update();

        switch (currentPhase)
        {
            case Phase.Intro:
                // Intro 阶段什么都不做，等待动画事件触发 OnIntroFinished
                break;

            case Phase.Loop:
                UpdateLoop();
                break;

            case Phase.Outro:
                // Outro 阶段可以移动，但不发射弹幕（协程已停止）
                UpdateMovement();
                // 等待动画事件触发 OnOutroFinished
                break;
        }
    }

    private void UpdateLoop()
    {
        stateDuration += Time.deltaTime;

        // 持续跟随玩家
        UpdateMovement();

        // 持续时间结束，退出到 Outro
        if (stateDuration >= boss.rangedAttackDuration)
        {
            StartOutro();
        }
    }

    private void UpdateMovement()
    {
        if (playerTransform != null)
            boss.MoveToPosition(playerTransform.position);
    }

    private void StartOutro()
    {
        if (currentPhase == Phase.Outro) return;

        currentPhase = Phase.Outro;
        StopFire();               // 停止发射新弹幕
        // 注意：不要立即调用 ChangeState，等待 Outro 动画完成
        // 这里可以重置动画参数或播放 Outro 动画（动画控制器根据同一 bool 参数自动切换，需设计好过渡）
        // 如果您的动画控制器需要单独的 Outro 触发，可以设置另一个 bool 参数，但为了简单，我们依赖动画事件
        enemyBase.anim.SetBool("RangedAttack", false);
    }

    private void StopFire()
    {
        if (fireCoroutine != null)
            boss.StopBossCoroutine(fireCoroutine);
        fireCoroutine = null;
    }

    // 由动画事件调用：Intro 动画结束，进入 Loop 阶段
    public void OnIntroFinished()
    {
        if (currentPhase != Phase.Intro) return;
        currentPhase = Phase.Loop;
        // 开始发射弹幕
        fireCoroutine = boss.StartBossCoroutine(FireRoutine());
    }

    // 由动画事件调用：Outro 动画结束，真正切换状态
    public void OnOutroFinished()
    {
        if (currentPhase != Phase.Outro) return;
        Debug.Log("追击");
        stateMachine.ChangeState(boss.battleState);
    }

    // --------------------------------------------------------------------
    // 以下所有 FireXXX 方法保持不变，与之前完全一致
    // --------------------------------------------------------------------
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
                    yield return wait;
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

    private void FireSweepCircle()
    {
        int count = boss.sweepBulletCount;
        if (count <= 0) return;

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