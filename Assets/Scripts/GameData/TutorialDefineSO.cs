using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class KeyCodeCondition
{
    public List<KeyCode> keys = new List<KeyCode>();
    public bool anyKey = false;

    public bool IsSatisfied()
    {
        if (anyKey && Input.anyKeyDown) return true;
        foreach (KeyCode key in keys)
            if (Input.GetKeyDown(key)) return true;
        return false;
    }
}

[System.Serializable]
public class TutorialStep
{
    [Header("提示内容")]
    public Sprite tipImage;

    [Header("高亮设置")]
    public string highlightTargetName;

    [Header("完成条件")]
    public KeyCodeCondition keyCondition;
    public string targetButtonName;

    [Header("时间控制")]
    [Tooltip("步骤最短逗留时间（秒），包含最后 1 秒淡出动画")]
    public float minStayDuration = 2f;

    [Header("步骤回调")]
    public UnityEngine.Events.UnityEvent onStepStart;
    public UnityEngine.Events.UnityEvent onStepComplete;
}

[CreateAssetMenu(fileName = "NewTutorial", menuName = "GameData/TutorialDefine")]
public class TutorialDefineSO : ScriptableObject
{
    public string sequenceName;

    [Tooltip("教程启动前的延迟时间（秒），0表示立即开始")]
    public float startDelay = 0f;

    public List<TutorialStep> steps;
}