using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonHotkey : MonoBehaviour
{
    public KeyCode hotkey = KeyCode.None;
    public KeyCode alternativeHotkey = KeyCode.None;

    private Button button;
    private GraphicRaycaster graphicRaycaster;
    private RectTransform rectTransform;
    private Canvas parentCanvas;          // 所属 Canvas
    private Camera canvasCamera;          // Canvas 关联的相机

    void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        graphicRaycaster = GetComponentInParent<GraphicRaycaster>();

        // 查找父级 Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError($"ButtonHotkey: 在 {name} 的父级中未找到 Canvas，热键将无法正常工作。");
        }
        else
        {
            canvasCamera = parentCanvas.worldCamera;
            if (canvasCamera == null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"ButtonHotkey: Canvas 模式为 {parentCanvas.renderMode} 但未绑定相机，可能导致坐标转换错误。");
            }
        }

        if (graphicRaycaster == null)
            Debug.LogWarning($"ButtonHotkey: 在 {name} 的父级中未找到 GraphicRaycaster，遮挡检测将失效。");
    }

    void Update()
    {
        if (!button.interactable) return;

        bool pressed = Input.GetKeyDown(hotkey) ||
                       (alternativeHotkey != KeyCode.None && Input.GetKeyDown(alternativeHotkey));
        if (pressed && IsButtonClickable())
        {
            button.onClick.Invoke();
        }
    }

    /// <summary>
    /// 检查按钮中心点是否未被其他 UI 遮挡
    /// </summary>
    private bool IsButtonClickable()
    {
        if (EventSystem.current == null) return true;

        // 获取按钮中心点的屏幕坐标（仍需要正确转换）
        Vector2 screenPos;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay && canvasCamera != null)
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera, rectTransform.position);
        }
        else
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
        }

        // 边界检查
        if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
            return false;

        // 使用 EventSystem 进行全局射线检测
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0) return false;

        // 找到最上层的 Raycast 结果（已经按深度排序，results[0] 就是最上层）
        GameObject topHit = results[0].gameObject;

        // 判断最上层的 UI 是否是当前按钮或其子物体
        return topHit == gameObject || topHit.transform.IsChildOf(transform);
    }
}