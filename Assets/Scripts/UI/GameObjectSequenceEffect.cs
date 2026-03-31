using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 为任意 GameObject 列表提供入场动画（缩放 + 淡入），支持相邻目标依次延迟播放。
/// 透明度使用 CanvasGroup 控制（自动添加/获取），缩放直接修改 transform。
/// 播放模式：
/// - Always: 每次激活都播放
/// - Once: 每个实例生命周期内仅首次播放（实例变量）
/// - OnceGlobally: 全局仅首次播放（基于唯一ID的静态字典，跨实例持久）
/// </summary>
public class GameObjectSequenceEffect : MonoBehaviour
{
    [Header("目标列表（可手动拖拽，也可自动获取子物体）")]
    public GameObject[] targets;

    [Header("自动获取设置（当 targets 为空时生效）")]
    public bool autoGetChildren = true;           // 是否自动获取所有子物体
    public bool includeInactive = false;           // 自动获取时是否包含非激活物体

    [Header("动画时间控制")]
    public float duration = 0.5f;
    public float delayBetween = 0.1f;
    public float startDelay = 0f;

    [Header("缩放参数（常态大小为1）")]
    public float initialScale = 0f;
    public float maxScale = 1.2f;                 // 两阶段缩放时的最大大小
    [Range(0.1f, 0.9f)]
    public float expandRatio = 0.6f;              // 放大阶段占比

    [Header("透明度参数")]
    [Range(0f, 1f)]
    public float initialAlpha = 0f;               // 初始透明度

    [Header("缩放模式")]
    public bool useElasticScale = true;            // true=弹性缩放，false=两阶段缩放

    [Header("播放模式")]
    public AnimationPlayMode playMode = AnimationPlayMode.Once; // 默认仅首次播放

    [Header("全局唯一标识（仅当 PlayMode = OnceGlobally 时需要）")]
    [Tooltip("用于全局标记的唯一字符串，相同 ID 的实例将共享播放状态。建议使用物体名称或其他不会冲突的标识。")]
    public string uniqueID = "";

    // 实例级别标记（用于 Once 模式）
    private bool _hasPlayedForThisInstance = false;

    // 静态字典：存储全局已播放状态（键为 uniqueID）
    private static Dictionary<string, bool> _globalPlayedStates = new Dictionary<string, bool>();

    void OnEnable()
    {
        bool shouldPlay = ShouldPlay();

        if (shouldPlay)
        {
            InitializeTargets();
            StartCoroutine(PlaySequenceCoroutine());
            MarkAsPlayed();
        }
        else
        {
            SetTargetsToFinalState();
        }
    }

    void OnDisable()
    {
        DOTween.Kill(this);
    }

    /// <summary>
    /// 根据播放模式判断是否应该播放动画
    /// </summary>
    bool ShouldPlay()
    {
        switch (playMode)
        {
            case AnimationPlayMode.Always:
                return true;

            case AnimationPlayMode.Once:
                return !_hasPlayedForThisInstance;

            case AnimationPlayMode.OnceGlobally:
                if (string.IsNullOrEmpty(uniqueID))
                {
                    Debug.LogWarning($"{name} 的 PlayMode 为 OnceGlobally 但未设置 uniqueID，将回退为 Once 模式。", this);
                    return !_hasPlayedForThisInstance;
                }
                if (!_globalPlayedStates.ContainsKey(uniqueID))
                    return true;
                return !_globalPlayedStates[uniqueID];

            default:
                return true;
        }
    }

    /// <summary>
    /// 标记该实例/ID 已经播放过
    /// </summary>
    void MarkAsPlayed()
    {
        switch (playMode)
        {
            case AnimationPlayMode.Once:
                _hasPlayedForThisInstance = true;
                break;
            case AnimationPlayMode.OnceGlobally:
                if (!string.IsNullOrEmpty(uniqueID))
                {
                    _globalPlayedStates[uniqueID] = true;
                }
                else
                {
                    _hasPlayedForThisInstance = true; // 降级
                }
                break;
        }
    }

    void InitializeTargets()
    {
        // 如果未手动指定目标，且允许自动获取，则获取所有子物体
        if ((targets == null || targets.Length == 0) && autoGetChildren)
        {
            // 获取所有子物体（包括自身？不包括，只获取直接子物体）
            Transform[] childrenTransforms = GetComponentsInChildren<Transform>(includeInactive);
            List<GameObject> childrenGOs = new List<GameObject>();
            foreach (var t in childrenTransforms)
            {
                if (t != transform) // 排除自身
                    childrenGOs.Add(t.gameObject);
            }
            targets = childrenGOs.ToArray();
        }

        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning($"{name} 没有可用的目标物体，请手动指定或确保有子物体。", this);
            return;
        }

        // 为每个目标准备初始状态（添加 CanvasGroup，设置初始 alpha 和 scale）
        foreach (GameObject obj in targets)
        {
            if (obj == null) continue;

            // 缩放：设置初始缩放
            obj.transform.localScale = Vector3.one * initialScale;

            // 透明度：确保有 CanvasGroup 并设置初始 alpha
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = obj.AddComponent<CanvasGroup>();
            cg.alpha = initialAlpha;
        }
    }

    void SetTargetsToFinalState()
    {
        if (targets == null) return;

        foreach (GameObject obj in targets)
        {
            if (obj == null) continue;

            // 缩放恢复为 1
            obj.transform.localScale = Vector3.one;

            // 透明度设为 1
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 1f;
        }
    }

    IEnumerator PlaySequenceCoroutine()
    {
        yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                PlayTargetAnimation(targets[i]);

            if (i < targets.Length - 1)
                yield return new WaitForSeconds(delayBetween);
        }
    }

    void PlayTargetAnimation(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            Debug.LogWarning($"目标物体 {obj.name} 没有 CanvasGroup，无法播放透明度动画。请确保 InitializeTargets 已执行。", obj);
            return;
        }

        Transform tr = obj.transform;
        tr.localScale = Vector3.one * initialScale;

        Sequence seq = DOTween.Sequence();

        if (useElasticScale)
        {
            seq.Append(tr.DOScale(1f, duration).SetEase(Ease.OutBack));
        }
        else
        {
            seq.Append(tr.DOScale(maxScale, duration * expandRatio).SetEase(Ease.OutQuad));
            seq.Append(tr.DOScale(1f, duration * (1f - expandRatio)).SetEase(Ease.InQuad));
        }

        seq.Join(cg.DOFade(1f, duration));
        seq.Play();
    }
}

/// <summary>
/// 动画播放模式枚举
/// </summary>
public enum AnimationPlayMode
{
    Always,          // 每次激活都播放
    Once,            // 每个实例生命周期内仅首次播放（对象不销毁则持续）
    OnceGlobally     // 全局仅首次播放（基于唯一ID，跨实例持久，直到游戏关闭）
}