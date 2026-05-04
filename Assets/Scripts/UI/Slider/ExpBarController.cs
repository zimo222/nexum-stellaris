using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ExpBarController : MonoBehaviour
{
    [SerializeField] private Slider expSlider;          // 经验条 Slider
    [SerializeField] private float stepDuration = 0.5f; // 每段动画的持续时间（匀速变化）

    private int displayLevel;   // 当前动画显示的等级
    private int displayExp;     // 当前动画显示的经验值
    private Coroutine animateCoroutine;

    private void Start()
    {
        // 初始化显示值与实际数据同步
        var data = PlayerDataManager.Instance.CurrentPlayerData;
        displayLevel = data.Level;
        displayExp = data.Experience;

        UpdateSliderMaxValue();
        expSlider.value = displayExp;

        // 订阅事件
        PlayerDataManager.Instance.OnExperienceChanged += OnExperienceChanged;
    }

    private void OnDestroy()
    {
        PlayerDataManager.Instance.OnExperienceChanged -= OnExperienceChanged;
    }

    private void OnExperienceChanged(int oldLevel, int oldExp, int newLevel, int newExp)
    {
        // 停止当前正在播放的动画
        if (animateCoroutine != null)
            StopCoroutine(animateCoroutine);

        // 以当前显示值为起点，管理器最新数据为目标开始动画
        int startLevel = displayLevel;
        int startExp = displayExp;
        int targetLevel = PlayerDataManager.Instance.CurrentPlayerData.Level;
        int targetExp = PlayerDataManager.Instance.CurrentPlayerData.Experience;

        animateCoroutine = StartCoroutine(AnimateExperience(startLevel, startExp, targetLevel, targetExp));
    }

    private IEnumerator AnimateExperience(int startLevel, int startExp, int targetLevel, int targetExp)
    {
        int currentLevel = startLevel;
        int currentExp = startExp;

        // 处理可能的多级升级，逐级满条 -> 清空 -> 停顿
        while (currentLevel < targetLevel)
        {
            int maxExpOld = ExperienceCurve.RequiredExp(currentLevel);

            // 1. 将 value 从当前经验匀速移动到满值
            yield return MoveValueTo(currentExp, maxExpOld);

            // 2. 升级：经验置0，设置新的上限
            currentExp = 0;
            currentLevel++;
            expSlider.value = 0f;

            // 更新内部记录
            displayLevel = currentLevel;
            displayExp = currentExp;
            UpdateSliderMaxValue();

            // 3. 停顿1秒
            yield return new WaitForSeconds(1f);
        }

        // 所有升级完成后，将经验值移动到最终的溢出经验
        if (currentExp != targetExp)
        {
            yield return MoveValueTo(currentExp, targetExp);
            displayExp = targetExp;
        }

        displayLevel = targetLevel;
        animateCoroutine = null;
    }

    /// <summary>
    /// 匀速将 slider.value 从 from 移动到 to
    /// </summary>
    private IEnumerator MoveValueTo(int from, int to)
    {
        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stepDuration);
            expSlider.value = Mathf.Lerp(from, to, t);
            yield return null;
        }
        expSlider.value = to;
        displayExp = to;
    }

    private void UpdateSliderMaxValue()
    {
        expSlider.maxValue = ExperienceCurve.RequiredExp(displayLevel);
    }
}