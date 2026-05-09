using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class ButtonHotkey : MonoBehaviour
{
    public KeyCode hotkey = KeyCode.None;
    public KeyCode alternativeHotkey = KeyCode.None;

    // 透明阈值，alpha 小于此值视为完全透明，可以穿透
    private const float AlphaThreshold = 0.1f;

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
    /// 检查按钮中心点是否未被非透明 UI 遮挡（支持像素级透明穿透）
    /// </summary>
    private bool IsButtonClickable()
    {
        if (EventSystem.current == null) return true;

        // 获取按钮中心点的屏幕坐标
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

        // 执行射线检测
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0) return false;

        // 从最上层往下遍历
        foreach (RaycastResult result in results)
        {
            GameObject hitGo = result.gameObject;
            // 命中按钮自身或其子物体，无论透明度如何都可点击
            if (hitGo == gameObject || hitGo.transform.IsChildOf(transform))
                return true;

            // 获取该点的实际透明度（考虑像素级透明通道）
            float alpha = GetAlphaAtScreenPosition(hitGo, screenPos);
            if (alpha < AlphaThreshold) // 透明，继续检查下一层
                continue;

            // 不透明（或半透明但超过阈值）的阻挡物
            return false;
        }

        // 所有命中物体都是透明的，无有效遮挡
        return true;
    }

    /// <summary>
    /// 获取指定屏幕坐标下，某个 UI 物体在该点的实际透明度（考虑像素级透明通道）
    /// </summary>
    /// <param name="hitGameObject">射线击中的物体</param>
    /// <param name="screenPos">屏幕坐标（像素）</param>
    /// <returns>透明度 0..1</returns>
    private float GetAlphaAtScreenPosition(GameObject hitGameObject, Vector2 screenPos)
    {
        // 尝试获取 Graphic 组件（Image、Text 等）
        Graphic graphic = hitGameObject.GetComponent<Graphic>();
        if (graphic == null) return 1f; // 非 Graphic 元素，保守视为不透明

        // 如果不是 Image，或者 Image 没有 Sprite，则使用整体透明度
        Image image = graphic as Image;
        if (image == null || image.sprite == null)
        {
            return GetOverallAlpha(graphic);
        }

        Sprite sprite = image.sprite;
        Texture2D texture = sprite.texture;
        if (texture == null || !texture.isReadable)
        {
            // 纹理不可读，回退到整体透明度
            return GetOverallAlpha(graphic);
        }

        // 将屏幕坐标转换为 UI 元素的局部坐标
        RectTransform rectTrans = hitGameObject.GetComponent<RectTransform>();
        if (rectTrans == null) return GetOverallAlpha(graphic);

        // 获取 Canvas 和相机
        Canvas canvas = graphic.canvas;
        Camera cam = canvas?.worldCamera;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            cam = null;

        // 屏幕坐标 -> 局部坐标
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTrans, screenPos, cam, out Vector2 localPoint))
        {
            return GetOverallAlpha(graphic);
        }

        // 计算 UV 坐标（根据 Image 类型）
        Vector2 uv = CalculateUV(image, localPoint);
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
            return GetOverallAlpha(graphic);

        // 将 UV 映射到纹理像素坐标
        Rect spriteRect = sprite.rect;
        int pixelX = Mathf.FloorToInt(spriteRect.x + uv.x * spriteRect.width);
        int pixelY = Mathf.FloorToInt(spriteRect.y + uv.y * spriteRect.height);
        pixelX = Mathf.Clamp(pixelX, 0, texture.width - 1);
        pixelY = Mathf.Clamp(pixelY, 0, texture.height - 1);

        Color pixelColor = texture.GetPixel(pixelX, pixelY);
        float pixelAlpha = pixelColor.a;

        // 乘以 Image.color.a 和父级 CanvasGroup 的影响
        float finalAlpha = pixelAlpha * image.color.a;
        Transform parent = image.transform;
        while (parent != null)
        {
            CanvasGroup group = parent.GetComponent<CanvasGroup>();
            if (group != null && !group.ignoreParentGroups)
            {
                finalAlpha *= group.alpha;
            }
            parent = parent.parent;
        }
        return finalAlpha;
    }

    /// <summary>
    /// 获取 Graphic 组件的整体透明度（忽略像素细节）
    /// </summary>
    private float GetOverallAlpha(Graphic graphic)
    {
        float alpha = graphic.color.a;
        Transform parent = graphic.transform;
        while (parent != null)
        {
            CanvasGroup group = parent.GetComponent<CanvasGroup>();
            if (group != null && !group.ignoreParentGroups)
            {
                alpha *= group.alpha;
            }
            parent = parent.parent;
        }
        return alpha;
    }

    /// <summary>
    /// 根据 Image 的类型和局部坐标计算出 UV 坐标
    /// </summary>
    private Vector2 CalculateUV(Image image, Vector2 localPoint)
    {
        Rect rect = image.rectTransform.rect;
        switch (image.type)
        {
            case Image.Type.Simple:
                return new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)
                );
            case Image.Type.Sliced:
            case Image.Type.Tiled:
                return GetUVForSlicedOrTiled(image, localPoint);
            case Image.Type.Filled:
                return GetUVForFilled(image, localPoint);
            default:
                return Vector2.zero;
        }
    }

    /// <summary>
    /// 为 Sliced / Tiled 模式计算 UV（九宫格映射）
    /// </summary>
    private Vector2 GetUVForSlicedOrTiled(Image image, Vector2 localPoint)
    {
        Rect rect = image.rectTransform.rect;
        Sprite sprite = image.sprite;
        Vector4 border = sprite.border;
        Rect spriteRect = sprite.rect;

        float leftBorder = border.x;
        float rightBorder = border.z;
        float topBorder = border.w;
        float bottomBorder = border.y;
        float spriteWidth = spriteRect.width;
        float spriteHeight = spriteRect.height;

        // 计算局部点相对于左下角的位置
        float x = localPoint.x - rect.xMin;
        float y = localPoint.y - rect.yMin;
        float totalWidth = rect.width;
        float totalHeight = rect.height;

        float leftWidth = leftBorder;
        float rightWidth = rightBorder;
        float centerWidth = totalWidth - leftWidth - rightWidth;
        float bottomHeight = bottomBorder;
        float topHeight = topBorder;
        float centerHeight = totalHeight - bottomHeight - topHeight;

        float u = 0, v = 0;
        // X 方向
        if (x <= leftWidth)
            u = Mathf.InverseLerp(0, leftWidth, x) * (leftBorder / spriteWidth);
        else if (x <= leftWidth + centerWidth)
            u = Mathf.InverseLerp(leftWidth, leftWidth + centerWidth, x) * ((spriteWidth - leftBorder - rightBorder) / spriteWidth) + (leftBorder / spriteWidth);
        else
            u = Mathf.InverseLerp(leftWidth + centerWidth, totalWidth, x) * (rightBorder / spriteWidth) + ((spriteWidth - rightBorder) / spriteWidth);

        // Y 方向
        if (y <= bottomHeight)
            v = Mathf.InverseLerp(0, bottomHeight, y) * (bottomBorder / spriteHeight);
        else if (y <= bottomHeight + centerHeight)
            v = Mathf.InverseLerp(bottomHeight, bottomHeight + centerHeight, y) * ((spriteHeight - bottomBorder - topBorder) / spriteHeight) + (bottomBorder / spriteHeight);
        else
            v = Mathf.InverseLerp(bottomHeight + centerHeight, totalHeight, y) * (topBorder / spriteHeight) + ((spriteHeight - topBorder) / spriteHeight);

        return new Vector2(u, v);
    }

    /// <summary>
    /// 为 Filled 模式计算 UV（简化，基于 Rect 线性映射）
    /// </summary>
    private Vector2 GetUVForFilled(Image image, Vector2 localPoint)
    {
        Rect rect = image.rectTransform.rect;
        return new Vector2(
            Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
            Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)
        );
    }
}