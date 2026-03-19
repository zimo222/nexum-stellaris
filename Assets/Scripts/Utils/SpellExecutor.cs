using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 法术执行器：负责按时间间隔执行法术序列
/// </summary>
public class SpellExecutor : MonoBehaviour
{
    [Header("设置")]
    public float stepInterval = 0.1f;   // 每个步骤之间的间隔（秒）
    public Transform firePoint;          // 子弹生成位置（通常为玩家枪口）

    private bool isExecuting = false;
    private SpellSequence currentSequence;
    private int currentIndex;
    private float multicastRemaining = 0; // 多重释放剩余同时执行数量

    private GameObject owner; // 发射者（通常是挂载此组件的对象）
    private Player player;

    private void Awake()
    {
        owner = gameObject;
        player = owner.GetComponent<Player>();
    }

    /// <summary>
    /// 开始执行一个法术序列
    /// </summary>
    public void ExecuteSequence(SpellSequence sequence)
    {
        if (isExecuting)
        {
            Debug.LogWarning("上一个序列还未执行完，强制中断");
            StopAllCoroutines();
        }

        currentSequence = sequence;
        currentIndex = 0;
        multicastRemaining = 0;
        isExecuting = true;
        StartCoroutine(ExecuteCoroutine());
    }

    private IEnumerator ExecuteCoroutine()
    {
        while (currentIndex < currentSequence.steps.Count)
        {
            object step = currentSequence.steps[currentIndex];

            if (step is MultiCastNode multiNode)
            {
                // 记录需要同时生成的数量，但具体是接下来的几个包
                multicastRemaining = multiNode.count;
                currentIndex++;
                continue;
            }

            if (step is SpellPackage spellPackage)
            {
                if (multicastRemaining > 0)
                {
                    // 此时我们不应立即生成当前包，而应该先收集接下来 multicastRemaining 个包，然后同时生成
                    List<SpellPackage> packagesToCast = new List<SpellPackage>();
                    packagesToCast.Add(spellPackage);
                    int collected = 1;
                    // 继续从 currentIndex+1 开始收集，直到收集够 multicastRemaining 个包
                    int tempIndex = currentIndex + 1;
                    while (collected < multicastRemaining && tempIndex < currentSequence.steps.Count)
                    {
                        object nextStep = currentSequence.steps[tempIndex];
                        if (nextStep is SpellPackage pkg)
                        {
                            packagesToCast.Add(pkg);
                            collected++;
                            tempIndex++;
                        }
                        else if (nextStep is MultiCastNode)
                        {
                            // 遇到嵌套多重释放？简单处理：忽略嵌套，跳出
                            Debug.LogWarning("暂不支持多重释放嵌套");
                            break;
                        }
                        else
                        {
                            // 其他类型？跳过
                            tempIndex++;
                        }
                    }
                    // 同时生成所有收集到的包
                    foreach (var pkg in packagesToCast)
                    {
                        SpawnBulletFromPackage(pkg);
                    }
                    // 更新 currentIndex 跳过已处理的包
                    currentIndex = tempIndex;
                    multicastRemaining = 0;
                    // 生成后等待间隔
                    if (currentIndex < currentSequence.steps.Count)
                        yield return new WaitForSeconds(stepInterval);
                }
                else
                {
                    // 普通单发
                    SpawnBulletFromPackage(spellPackage);
                    currentIndex++;
                    if (currentIndex < currentSequence.steps.Count)
                        yield return new WaitForSeconds(stepInterval);
                }
            }
            else
            {
                currentIndex++;
            }
        }
        isExecuting = false;
    }

    private void SpawnBulletFromPackage(SpellPackage spellPackage)
    {
        if (firePoint == null)
        {
            Debug.LogError("未设置开火点");
            return;
        }

        // 计算发射方向（可根据玩家面向调整）
        Vector2 direction = owner.transform.right; // 假设玩家面朝右
        direction = player.dashDirection;
        // 可扩展：根据鼠标方向或输入方向

        BulletFactory.CreateBullet(spellPackage, firePoint.position, direction, owner);
    }
}