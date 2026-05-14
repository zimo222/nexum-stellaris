using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class ButtonHotkey : MonoBehaviour
{
    public KeyCode hotkey = KeyCode.None;
    public KeyCode alternativeHotkey = KeyCode.None;

    // 像素级点击检测的透明度阈值（低于此值视为透明区域）
    private const float AlphaThreshold = 0.1f;

    private Button button;
    private GraphicRaycaster graphicRaycaster;
    private RectTransform rectTransform;
    private Canvas parentCanvas;          // 所属的 Canvas（每次 OnEnable 重新获取）

    // 实时获取当前有效的 Camera（根据 Canvas 的渲染模式）
    private Camera CurrentCamera
    {
        get
        {
            if (parentCanvas == null) return null;
            // ScreenSpace - Overlay 模式不需要 Camera
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return parentCanvas.worldCamera;
        }
    }

    void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        // 注意：graphicRaycaster 和 parentCanvas 可能在场景切换后变化，所以放在 OnEnable 中刷新
    }

    void OnEnable()
    {
        // 重新获取父级 Canvas（场景切换后可能会变化）
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError($"ButtonHotkey: 物体 {name} 所在的父级中没有 Canvas！快捷键将无法正确检测点击区域。");
        }
        else
        {
            // 可选：验证 Camera 是否有效（仅非 Overlay 模式）
            if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay && parentCanvas.worldCamera == null)
            {
                Debug.LogWarning($"ButtonHotkey: Canvas 渲染模式为 {parentCanvas.renderMode}，但未指定 World Camera！点击区域检测可能不准确。");
            }
        }

        // 重新获取 GraphicRaycaster（可能随 Canvas 变化）
        graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            Debug.LogWarning($"ButtonHotkey: 物体 {name} 的父级中未找到 GraphicRaycaster，快捷键穿透检测将无效。");
        }
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
    /// 检查按钮是否处于可点击状态（没有被其他 UI 遮挡，且自身在可视区域内）
    /// </summary>
    private bool IsButtonClickable()
    {
        if (EventSystem.current == null) return true;

        // 获取按钮中心点的屏幕坐标
        Vector2 screenPos;
        Camera cam = CurrentCamera; // 实时获取最新的 Camera
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay && cam != null)
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);
        }
        else
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
        }

        // 超出屏幕范围不可点击
        if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
            return false;

        // 射线检测该屏幕位置下的所有 UI 元素
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0) return false;

        // 检查是否有非透明 UI 元素挡住了按钮
        foreach (RaycastResult result in results)
        {
            GameObject hitGo = result.gameObject;
            // 如果射线击中的是自身或自身子物体，视为可点击
            if (hitGo == gameObject || hitGo.transform.IsChildOf(transform))
                return true;

            // 获取该 UI 元素在点击位置的透明度
            float alpha = GetAlphaAtScreenPosition(hitGo, screenPos);
            if (alpha < AlphaThreshold) // 完全透明区域，忽略
                continue;

            // 存在不透明的其他 UI 元素挡住，不可点击
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取某个 GameObject（UI）在指定屏幕位置处的透明度（考虑 Sprite Alpha、Image.color.a 和 CanvasGroup）
    /// </summary>
    private float GetAlphaAtScreenPosition(GameObject hitGameObject, Vector2 screenPos)
    {
        Graphic graphic = hitGameObject.GetComponent<Graphic>();
        if (graphic == null) return 1f;

        Image image = graphic as Image;
        if (image == null || image.sprite == null)
        {
            return GetOverallAlpha(graphic);
        }

        Sprite sprite = image.sprite;
        Texture2D texture = sprite.texture;
        if (texture == null || !texture.isReadable)
        {
            return GetOverallAlpha(graphic);
        }

        RectTransform rectTrans = hitGameObject.GetComponent<RectTransform>();
        if (rectTrans == null) return GetOverallAlpha(graphic);

        // 获取该 Graphic 所在的 Canvas 以及正确的 Camera
        Canvas canvas = graphic.canvas;
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTrans, screenPos, cam, out Vector2 localPoint))
        {
            return GetOverallAlpha(graphic);
        }

        Vector2 uv = CalculateUV(image, localPoint);
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
            return GetOverallAlpha(graphic);

        Rect spriteRect = sprite.rect;
        int pixelX = Mathf.FloorToInt(spriteRect.x + uv.x * spriteRect.width);
        int pixelY = Mathf.FloorToInt(spriteRect.y + uv.y * spriteRect.height);
        pixelX = Mathf.Clamp(pixelX, 0, texture.width - 1);
        pixelY = Mathf.Clamp(pixelY, 0, texture.height - 1);

        Color pixelColor = texture.GetPixel(pixelX, pixelY);
        float pixelAlpha = pixelColor.a;

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
        if (x <= leftWidth)
            u = Mathf.InverseLerp(0, leftWidth, x) * (leftBorder / spriteWidth);
        else if (x <= leftWidth + centerWidth)
            u = Mathf.InverseLerp(leftWidth, leftWidth + centerWidth, x) * ((spriteWidth - leftBorder - rightBorder) / spriteWidth) + (leftBorder / spriteWidth);
        else
            u = Mathf.InverseLerp(leftWidth + centerWidth, totalWidth, x) * (rightBorder / spriteWidth) + ((spriteWidth - rightBorder) / spriteWidth);

        if (y <= bottomHeight)
            v = Mathf.InverseLerp(0, bottomHeight, y) * (bottomBorder / spriteHeight);
        else if (y <= bottomHeight + centerHeight)
            v = Mathf.InverseLerp(bottomHeight, bottomHeight + centerHeight, y) * ((spriteHeight - bottomBorder - topBorder) / spriteHeight) + (bottomBorder / spriteHeight);
        else
            v = Mathf.InverseLerp(bottomHeight + centerHeight, totalHeight, y) * (topBorder / spriteHeight) + ((spriteHeight - topBorder) / spriteHeight);

        return new Vector2(u, v);
    }

    private Vector2 GetUVForFilled(Image image, Vector2 localPoint)
    {
        Rect rect = image.rectTransform.rect;
        return new Vector2(
            Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
            Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)
        );
    }
}