using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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

    // 高亮缓存
    private GameObject currentHighlight;
    private Outline cachedOutline;
    private SpriteRenderer cachedSpriteRenderer;
    private Color originalColor;

    private Button cachedButton;
    private bool conditionMetForCurrentStep = false;

    private void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
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
        if (!conditionMetForCurrentStep && step.keyCondition != null && step.keyCondition.IsSatisfied())
        {
            conditionMetForCurrentStep = true;
        }
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
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(OnButtonClicked);
            cachedButton = null;
        }
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

        // ------------------- 图片淡入（新增 1 秒淡入） -------------------
        if (tipImageUI != null)
        {
            tipImageUI.sprite = step.tipImage;
            tipImageUI.gameObject.SetActive(true);

            // 确保有 CanvasGroup 组件，初始透明度为 0
            CanvasGroup cg = tipImageUI.GetComponent<CanvasGroup>();
            if (cg == null) cg = tipImageUI.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // 淡入动画，持续 1 秒
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
        // ----------------------------------------------------------------

        // 高亮
        if (!string.IsNullOrEmpty(step.highlightTargetName))
        {
            GameObject target = FindUIByName(step.highlightTargetName);
            if (target != null) ApplyHighlight(target);
            else Debug.LogWarning($"未找到高亮对象 {step.highlightTargetName}");
        }

        // 按钮监听
        if (!string.IsNullOrEmpty(step.targetButtonName))
        {
            GameObject btnObj = FindUIByName(step.targetButtonName);
            if (btnObj != null)
            {
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(OnButtonClicked);
                    btn.onClick.AddListener(OnButtonClicked);
                    cachedButton = btn;
                }
                else Debug.LogWarning($"对象 {step.targetButtonName} 没有 Button 组件");
            }
            else Debug.LogWarning($"未找到按钮 {step.targetButtonName}");
        }

        step.onStepStart?.Invoke();

        // 等待完成条件
        while (!conditionMetForCurrentStep)
        {
            yield return null;
        }

        // 计算需要等待的时间（保证总停留时间 >= minStayDuration，留出 1 秒淡出）
        float elapsed = Time.unscaledTime - stepStartTime;
        float waitBeforeFade = Mathf.Max(0, step.minStayDuration - elapsed - 1f);
        if (waitBeforeFade > 0)
        {
            yield return new WaitForSecondsRealtime(waitBeforeFade);
        }

        // 开始淡出（1秒）
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(OnButtonClicked);
            cachedButton = null;
        }

        if (tipImageUI != null)
        {
            CanvasGroup cg = tipImageUI.GetComponent<CanvasGroup>();
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

    private void OnButtonClicked()
    {
        if (!isTutorialRunning) return;
        if (!conditionMetForCurrentStep && currentStepIndex >= 0 && currentStepIndex < currentSequence.steps.Count)
        {
            TutorialStep step = currentSequence.steps[currentStepIndex];
            if (!string.IsNullOrEmpty(step.targetButtonName) && cachedButton != null)
            {
                conditionMetForCurrentStep = true;
            }
        }
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