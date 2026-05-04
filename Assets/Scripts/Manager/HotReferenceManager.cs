using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HotReferenceManager : Singleton<HotReferenceManager>
{
    [Header("需要热引用的 UI 物体名称")]
    public List<string> uiNamesToRefresh = new List<string>();

    protected override void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        base.Awake();
        if (gameObject.scene.name != "DontDestroyOnLoad")
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 在场景加载后调用，刷新所有已注册的 UI
    /// </summary>
    public void RefreshAllUIs()
    {
        StartCoroutine(RefreshAllUIsCoroutine());
    }

    private IEnumerator RefreshAllUIsCoroutine()
    {
        // 等待一帧，确保场景物体完全初始化
        yield return null;

        foreach (string uiName in uiNamesToRefresh)
        {
            RefreshSingleUI(uiName);
        }
    }

    // 在场景加载后调用，刷新所有 QuestTriggerZone 的按钮引用
    public void RefreshAllQuestTriggerZones()
    {
        // 使用 FindObjectsOfType 包括未激活的物体（但注意：FindObjectsOfType 默认只返回激活的，要包括未激活需要 Resources.FindObjectsOfTypeAll 并过滤）
        QuestTriggerZone[] zones = Resources.FindObjectsOfTypeAll<QuestTriggerZone>();
        List<QuestTriggerZone> validZones = new List<QuestTriggerZone>();

        foreach (var zone in zones)
        {
            // 只保留属于当前场景且未被销毁的实例
            if (zone != null && zone.gameObject.scene == SceneManager.GetActiveScene())
            {
                validZones.Add(zone);
            }
        }

        Debug.Log($"HotReferenceManager: 找到 {validZones.Count} 个 QuestTriggerZone，开始刷新按钮引用...");

        foreach (var zone in validZones)
        {
            zone.RefreshButtonReference();
        }
    }

    /// <summary>
    /// 将当前场景中找到的 WeaponSlotsUI 重新赋值给 Player 实例
    /// </summary>
    public void RefreshPlayerWeaponSlotsUI()
    {
        if (Player.Instance == null)
        {
            Debug.LogWarning("HotReferenceManager: Player.Instance 为空，无法刷新 WeaponSlotsUI");
            return;
        }

        // 查找场景中的 WeaponSlotsUI（包括未激活的物体）
        WeaponSlotsUI slotsUI = FindObjectOfType<WeaponSlotsUI>(true);
        if (slotsUI != null)
        {
            Player.Instance.weaponSlotsUI = slotsUI;
            Debug.Log("HotReferenceManager: 已刷新 Player 的 WeaponSlotsUI 引用");
        }
        else
        {
            Debug.LogWarning("HotReferenceManager: 未在当前场景中找到 WeaponSlotsUI");
        }
    }

    /// <summary>
    /// 刷新 AI 聊天界面的 Canvas 相机引用。
    /// 在场景切换后调用，确保 AICanvas 的 Render Camera 指向正确的 UICamera。
    /// </summary>
    /// <summary>
    /// 刷新 AI 聊天界面的 Canvas 相机引用（支持 DontDestroyOnLoad 中的物体）
    /// </summary>
    public void RefreshAICanvasCamera()
    {
        StartCoroutine(RefreshAICanvasCameraCoroutine());
    }

    private System.Collections.IEnumerator RefreshAICanvasCameraCoroutine()
    {
        // 等待一帧，确保场景加载完成
        yield return null;

        // 1. 查找 AICanvas（支持未激活物体，不限制场景）
        GameObject aiCanvasObj = null;
        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            if (c != null && c.gameObject.name == "AICanvas")
            {
                // DontDestroyOnLoad 物体的场景可能是 null 或名为 "DontDestroyOnLoad"
                string sceneName = c.gameObject.scene.name;
                if (string.IsNullOrEmpty(sceneName) || sceneName == "DontDestroyOnLoad" || c.gameObject.scene == SceneManager.GetActiveScene())
                {
                    aiCanvasObj = c.gameObject;
                    break;
                }
            }
        }

        if (aiCanvasObj == null)
        {
            Debug.LogError("HotReferenceManager: 未找到 AICanvas，请确认：\n" +
                           "1. 场景中是否存在名为 'AICanvas' 的物体（注意大小写）\n" +
                           "2. 该物体是否被放置在 DontDestroyOnLoad 或当前活动场景中\n" +
                           "3. 此函数是否在场景加载完成后调用（建议在 SceneManager.sceneLoaded 事件中调用）");
            yield break;
        }

        Canvas canvas = aiCanvasObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"HotReferenceManager: 物体 '{aiCanvasObj.name}' 上没有 Canvas 组件");
            yield break;
        }

        // 2. 查找 UICamera（同样允许 DontDestroyOnLoad 或当前场景）
        Camera uiCamera = null;
        Camera[] allCameras = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (Camera cam in allCameras)
        {
            if (cam != null && cam.gameObject.name == "UI Camera")
            {
                string camSceneName = cam.gameObject.scene.name;
                if (string.IsNullOrEmpty(camSceneName) || camSceneName == "DontDestroyOnLoad" || cam.gameObject.scene == SceneManager.GetActiveScene())
                {
                    uiCamera = cam;
                    break;
                }
            }
        }

        if (uiCamera == null)
        {
            Debug.LogError("HotReferenceManager: 未找到 UICamera，请确保场景中存在名为 'UICamera' 的相机物体");
            yield break;
        }

        // 3. 设置 Canvas 渲染相机
        if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }
        canvas.worldCamera = uiCamera;

        Debug.Log($"HotReferenceManager: 已成功将 {aiCanvasObj.name} 的渲染相机设置为 {uiCamera.name}");
    }
    private void RefreshSingleUI(string uiName)
    {
        // 查找物体（支持未激活、支持 DontDestroyOnLoad 和当前场景）
        GameObject targetObj = FindGameObjectAnywhere(uiName);
        if (targetObj == null)
        {
            Debug.LogWarning($"HotReferenceManager: 未找到名为 '{uiName}' 的物体，请检查名称是否正确或物体是否在 DontDestroyOnLoad 中");
            return;
        }

        // 根据名称执行对应的刷新逻辑
        switch (uiName)
        {
            case "AICanvas":
                RefreshAICanvas(targetObj);
                break;
            case "QuestManager":
                RefreshQuestManagerCanvas(targetObj);
                break;
            case "CombatManager":
                RefreshCombatManagerCanvas(targetObj);
                break;
            // 可以继续添加其他需要热引用的 UI 名称
            default:
                Debug.LogWarning($"HotReferenceManager: 未知的 UI 名称 '{uiName}'，未定义刷新逻辑");
                break;
        }
    }

    // 通用查找函数（支持未激活物体、跨场景）
    private GameObject FindGameObjectAnywhere(string name)
    {
        // 使用 Resources.FindObjectsOfTypeAll 查找所有物体
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.name == name)
            {
                // 允许物体在 DontDestroyOnLoad 或当前活动场景
                string sceneName = obj.scene.name;
                if (string.IsNullOrEmpty(sceneName) || sceneName == "DontDestroyOnLoad" || obj.scene == SceneManager.GetActiveScene())
                {
                    return obj;
                }
            }
        }
        return null;
    }

    // ========== 各 UI 的具体刷新逻辑 ==========
    private void RefreshAICanvas(GameObject canvasObj)
    {
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"HotReferenceManager: {canvasObj.name} 没有 Canvas 组件");
            return;
        }

        // 查找 UICamera
        Camera uiCamera = FindCameraAnywhere("UI Camera");
        if (uiCamera == null)
        {
            Debug.LogError("HotReferenceManager: 未找到 UICamera");
            return;
        }

        if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;

        Debug.Log($"HotReferenceManager: 已刷新 {canvasObj.name} 的相机引用");
    }

    private void RefreshQuestManagerCanvas(GameObject questManagerObj)
    {
        if (questManagerObj.GetComponent<NonSingletonMark>()) return;
        Canvas canvas = questManagerObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"HotReferenceManager: {questManagerObj.name} 物体上没有 Canvas 组件");
            return;
        }

        // 查找 UICamera
        Camera uiCamera = FindCameraAnywhere("UI Camera");
        if (uiCamera == null)
        {
            Debug.LogError("HotReferenceManager: 未找到 UICamera");
            return;
        }

        // 设置渲染模式为 ScreenSpaceCamera
        if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }
        canvas.worldCamera = uiCamera;

        Debug.Log($"HotReferenceManager: 已刷新 {questManagerObj.name} 的 Canvas 相机引用");
    }
    private void RefreshCombatManagerCanvas(GameObject combatManagerObj)
    {
        Canvas canvas = combatManagerObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"HotReferenceManager: {combatManagerObj.name} 物体上没有 Canvas 组件");
            return;
        }

        // 查找 UICamera
        Camera uiCamera = FindCameraAnywhere("UI Camera");
        if (uiCamera == null)
        {
            Debug.LogError("HotReferenceManager: 未找到 UICamera");
            return;
        }

        // 设置渲染模式为 ScreenSpaceCamera
        if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }
        canvas.worldCamera = uiCamera;

        Debug.Log($"HotReferenceManager: 已刷新 {combatManagerObj.name} 的 Canvas 相机引用");
    }

    private Camera FindCameraAnywhere(string cameraName)
    {
        Camera[] allCameras = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (Camera cam in allCameras)
        {
            if (cam != null && cam.name == cameraName)
            {
                string sceneName = cam.gameObject.scene.name;
                if (string.IsNullOrEmpty(sceneName) || sceneName == "DontDestroyOnLoad" || cam.gameObject.scene == SceneManager.GetActiveScene())
                {
                    return cam;
                }
            }
        }
        return null;
    }

    // 其他原有方法（RefreshAllQuestTriggerZones 等）保持不变...
}