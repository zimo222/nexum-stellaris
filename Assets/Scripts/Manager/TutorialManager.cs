using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI 组件")]
    public Image tipImageUI;
    public GameObject tipPanel;
    public bool pauseGameDuringTutorial = true;

    private TutorialDefineSO currentSequence;
    private int currentStepIndex = -1;
    private bool isTutorialRunning = false;
    private string activeSequenceName = "";
    private Coroutine currentStepCoroutine;
    private Coroutine delayedStartCoroutine;

    private GameObject currentHighlight;
    private Outline cachedOutline;
    private SpriteRenderer cachedSpriteRenderer;
    private Color originalColor;

    private string currentExpectedClickName;
    private bool conditionMetForCurrentStep = false;

    private void Awake()
    {
        Debug.Log($"[Tutorial] Awake");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (tipImageUI != null) tipImageUI.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isTutorialRunning) return;
        if (currentSequence == null || currentStepIndex < 0 || currentStepIndex >= currentSequence.steps.Count) return;

        TutorialStep step = currentSequence.steps[currentStepIndex];

        // 键盘条件
        if (!conditionMetForCurrentStep && step.keyCondition != null && step.keyCondition.IsSatisfied())
        {
            Debug.Log($"[Tutorial] 键盘条件满足");
            conditionMetForCurrentStep = true;
        }

        // 鼠标点击检测（每帧检测左键按下）
        if (!conditionMetForCurrentStep && Input.GetMouseButtonDown(0))
        {
            Debug.Log("[Tutorial] 检测到鼠标左键点击");
            if (!string.IsNullOrEmpty(currentExpectedClickName))
            {
                CheckMouseClick();
            }
            else
            {
                Debug.Log("[Tutorial] 当前步骤没有期望的点击目标，忽略");
            }
        }
    }

    private void CheckMouseClick()
    {
        Debug.Log($"[Tutorial] 执行鼠标点击检测，期望目标: {currentExpectedClickName}");
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem.current 为空");
            return;
        }

        Vector2 mousePos = Input.mousePosition;
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = mousePos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0)
        {
            Debug.Log("[Tutorial] 射线检测未命中任何 UI");
            return;
        }

        // 遍历所有命中结果（从最上层开始），找到第一个名称匹配（包括父级）的对象
        foreach (var result in results)
        {
            GameObject hit = result.gameObject;
            Debug.Log($"[Tutorial] 射线命中物体: {hit.name}");
            if (IsObjectOrParentMatches(hit, currentExpectedClickName))
            {
                Debug.Log($"[Tutorial] 匹配成功！点击物体 {hit.name} 符合期望 {currentExpectedClickName}");
                conditionMetForCurrentStep = true;
                return;
            }
            else
            {
                Debug.Log($"[Tutorial] 物体 {hit.name} 及父级均不匹配期望 {currentExpectedClickName}");
            }
        }
        Debug.Log("[Tutorial] 没有找到匹配的命中物体");
    }

    private bool IsObjectOrParentMatches(GameObject obj, string expectedName)
    {
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.name == expectedName)
                return true;
            t = t.parent;
        }
        return false;
    }

    public void StartTutorial(string tutorialName) => StartTutorial(tutorialName, -1f);
    public void StartTutorial(string tutorialName, float delay)
    {
        if (delayedStartCoroutine != null)
            StopCoroutine(delayedStartCoroutine);
        delayedStartCoroutine = StartCoroutine(DelayedStart(tutorialName, delay));
    }

    private IEnumerator DelayedStart(string tutorialName, float delay)
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("GameDataManager 未找到");
            yield break;
        }
        if (!GameDataManager.Instance.TutorialDict.TryGetValue(tutorialName, out TutorialDefineSO target))
        {
            Debug.LogError($"未找到教程序列 {tutorialName}");
            yield break;
        }

        float actualDelay = delay >= 0 ? delay : target.startDelay;
        if (actualDelay > 0)
            yield return new WaitForSecondsRealtime(actualDelay);

        ActuallyStartTutorial(target);
        delayedStartCoroutine = null;
    }

    private void ActuallyStartTutorial(TutorialDefineSO target)
    {
        if (isTutorialRunning)
        {
            Debug.LogWarning($"已有教程 {activeSequenceName} 运行中");
            return;
        }

        currentSequence = target;
        activeSequenceName = target.sequenceName;
        currentStepIndex = -1;
        isTutorialRunning = true;
        conditionMetForCurrentStep = false;
        currentExpectedClickName = null;

        if (pauseGameDuringTutorial) Time.timeScale = 0f;
        if (tipPanel != null) tipPanel.SetActive(true);

        NextStep();
    }

    public void EndTutorial()
    {
        if (!isTutorialRunning) return;
        StopCurrentStep();
        isTutorialRunning = false;
        currentSequence = null;
        activeSequenceName = "";
        currentExpectedClickName = null;
        if (tipPanel != null) tipPanel.SetActive(false);
        if (tipImageUI != null) tipImageUI.gameObject.SetActive(false);
        if (pauseGameDuringTutorial) Time.timeScale = 1f;
        Debug.Log($"教程 {activeSequenceName} 已结束");
    }

    public void SkipTutorial() => EndTutorial();

    private void StopCurrentStep()
    {
        if (currentStepCoroutine != null)
            StopCoroutine(currentStepCoroutine);
        ClearHighlight();
        conditionMetForCurrentStep = false;
    }

    private void NextStep()
    {
        if (!isTutorialRunning) return;
        currentStepIndex++;
        if (currentStepIndex >= currentSequence.steps.Count)
        {
            EndTutorial();
            return;
        }

        conditionMetForCurrentStep = false;
        currentStepCoroutine = StartCoroutine(RunStep(currentSequence.steps[currentStepIndex]));
    }

    private IEnumerator RunStep(TutorialStep step)
    {
        float stepStartTime = Time.unscaledTime;

        // 设置期望点击目标
        currentExpectedClickName = !string.IsNullOrEmpty(step.clickTargetName) ? step.clickTargetName : step.targetButtonName;
        Debug.Log($"[Tutorial] 当前步骤期望点击目标: {currentExpectedClickName}");

        // 图片淡入
        if (tipImageUI != null && step.tipImage != null)
        {
            tipImageUI.sprite = step.tipImage;
            tipImageUI.gameObject.SetActive(true);

            CanvasGroup cg = tipImageUI.GetComponent<CanvasGroup>();
            if (cg == null) cg = tipImageUI.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            float fadeInDuration = 1f;
            float fadeTimer = 0f;
            while (fadeTimer < fadeInDuration)
            {
                fadeTimer += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, fadeTimer / fadeInDuration);
                yield return null;
            }
            cg.alpha = 1f;
        }

        // 高亮
        if (!string.IsNullOrEmpty(step.highlightTargetName))
        {
            GameObject target = FindUIByName(step.highlightTargetName);
            if (target != null) ApplyHighlight(target);
            else Debug.LogWarning($"未找到高亮对象 {step.highlightTargetName}");
        }

        step.onStepStart?.Invoke();

        while (!conditionMetForCurrentStep)
        {
            yield return null;
        }

        float elapsed = Time.unscaledTime - stepStartTime;
        float waitBeforeFade = Mathf.Max(0, step.minStayDuration - elapsed - 1f);
        if (waitBeforeFade > 0)
            yield return new WaitForSecondsRealtime(waitBeforeFade);

        currentExpectedClickName = null; // 清除监听

        if (tipImageUI != null && tipImageUI.gameObject.activeSelf)
        {
            CanvasGroup cg = tipImageUI.GetComponent<CanvasGroup>();
            if (cg == null) cg = tipImageUI.gameObject.AddComponent<CanvasGroup>();
            float fadeDuration = 1f;
            float startAlpha = cg.alpha;
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
                yield return null;
            }
            cg.alpha = 0f;
            tipImageUI.gameObject.SetActive(false);
        }

        ClearHighlight();
        step.onStepComplete?.Invoke();

        NextStep();
    }

    private GameObject FindUIByName(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject result = FindInHierarchy(root, name);
                if (result != null) return result;
            }
        }
        return GameObject.Find(name);
    }

    private GameObject FindInHierarchy(GameObject root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root.transform)
        {
            GameObject result = FindInHierarchy(child.gameObject, name);
            if (result != null) return result;
        }
        return null;
    }

    private void ApplyHighlight(GameObject target)
    {
        currentHighlight = target;
        Outline outline = target.GetComponent<Outline>();
        if (outline == null && target.GetComponent<RectTransform>() != null)
        {
            outline = target.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(4, 4);
            cachedOutline = outline;
        }
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalColor = sr.color;
            sr.color = Color.yellow;
            cachedSpriteRenderer = sr;
        }
    }

    private void ClearHighlight()
    {
        if (currentHighlight == null) return;
        if (cachedOutline != null) Destroy(cachedOutline);
        if (cachedSpriteRenderer != null) cachedSpriteRenderer.color = originalColor;
        currentHighlight = null;
        cachedOutline = null;
        cachedSpriteRenderer = null;
    }
}