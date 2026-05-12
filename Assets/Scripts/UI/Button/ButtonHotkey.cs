using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class ButtonHotkey : MonoBehaviour
{
    public KeyCode hotkey = KeyCode.None;
    public KeyCode alternativeHotkey = KeyCode.None;

    // ????????alpha §³?????????????????????
    private const float AlphaThreshold = 0.1f;

    private Button button;
    private GraphicRaycaster graphicRaycaster;
    private RectTransform rectTransform;
    private Canvas parentCanvas;          // ???? Canvas
    private Camera canvasCamera;          // Canvas ?????????

    void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        graphicRaycaster = GetComponentInParent<GraphicRaycaster>();

        // ??????? Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError($"ButtonHotkey: ?? {name} ???????¦Ä??? Canvas????????????????????");
        }
        else
        {
            canvasCamera = parentCanvas.worldCamera;
            if (canvasCamera == null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"ButtonHotkey: Canvas ??? {parentCanvas.renderMode} ??¦Ä?????????????????????????");
            }
        }

        if (graphicRaycaster == null)
            Debug.LogWarning($"ButtonHotkey: ?? {name} ???????¦Ä??? GraphicRaycaster?????????§¹??");
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
    /// ??ö™?????????¦Ä??????? UI ?????????????????????
    /// </summary>
    private bool IsButtonClickable()
    {
        if (EventSystem.current == null) return true;

        // ???????????????????
        Vector2 screenPos;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay && canvasCamera != null)
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera, rectTransform.position);
        }
        else
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
        }

        // ?????
        if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
            return false;

        // ?????????
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0) return false;

        // ??????????¡À???
        foreach (RaycastResult result in results)
        {
            GameObject hitGo = result.gameObject;
            // ???§Ñ???????????????ÈÉ???????????¦Æ?????
            if (hitGo == gameObject || hitGo.transform.IsChildOf(transform))
                return true;

            // ?????????????????????????????????
            float alpha = GetAlphaAtScreenPosition(hitGo, screenPos);
            if (alpha < AlphaThreshold) // ?????????????????
                continue;

            // ???????????????????????????Ú\??
            return false;
        }

        // ???????????ŽZ????????????§¹???
        return true;
    }

    /// <summary>
    /// ??????????????????? UI ???????????????????????????????????
    /// </summary>
    /// <param name="hitGameObject">??????§Ö?????</param>
    /// <param name="screenPos">????????????</param>
    /// <returns>????? 0..1</returns>
    private float GetAlphaAtScreenPosition(GameObject hitGameObject, Vector2 screenPos)
    {
        // ?????? Graphic ?????Image??Text ???
        Graphic graphic = hitGameObject.GetComponent<Graphic>();
        if (graphic == null) return 1f; // ?? Graphic ????????????????

        // ??????? Image?????? Image ??? Sprite????????????????
        Image image = graphic as Image;
        if (image == null || image.sprite == null)
        {
            return GetOverallAlpha(graphic);
        }

        Sprite sprite = image.sprite;
        Texture2D texture = sprite.texture;
        if (texture == null || !texture.isReadable)
        {
            // ?????????????????????????
            return GetOverallAlpha(graphic);
        }

        // ????????????? UI ??????????
        RectTransform rectTrans = hitGameObject.GetComponent<RectTransform>();
        if (rectTrans == null) return GetOverallAlpha(graphic);

        // ??? Canvas ?????
        Canvas canvas = graphic.canvas;
        Camera cam = canvas?.worldCamera;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            cam = null;

        // ??????? -> ???????
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTrans, screenPos, cam, out Vector2 localPoint))
        {
            return GetOverallAlpha(graphic);
        }

        // ???? UV ???????? Image ?????
        Vector2 uv = CalculateUV(image, localPoint);
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
            return GetOverallAlpha(graphic);

        // ?? UV ???????????????
        Rect spriteRect = sprite.rect;
        int pixelX = Mathf.FloorToInt(spriteRect.x + uv.x * spriteRect.width);
        int pixelY = Mathf.FloorToInt(spriteRect.y + uv.y * spriteRect.height);
        pixelX = Mathf.Clamp(pixelX, 0, texture.width - 1);
        pixelY = Mathf.Clamp(pixelY, 0, texture.height - 1);

        Color pixelColor = texture.GetPixel(pixelX, pixelY);
        float pixelAlpha = pixelColor.a;

        // ???? Image.color.a ????? CanvasGroup ?????
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
    /// ??? Graphic ???????????????????????????
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
    /// ???? Image ????????????????? UV ????
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
    /// ? Sliced / Tiled ?????? UV??????????
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

        // ???????????????????¦Ë??
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
        // X ????
        if (x <= leftWidth)
            u = Mathf.InverseLerp(0, leftWidth, x) * (leftBorder / spriteWidth);
        else if (x <= leftWidth + centerWidth)
            u = Mathf.InverseLerp(leftWidth, leftWidth + centerWidth, x) * ((spriteWidth - leftBorder - rightBorder) / spriteWidth) + (leftBorder / spriteWidth);
        else
            u = Mathf.InverseLerp(leftWidth + centerWidth, totalWidth, x) * (rightBorder / spriteWidth) + ((spriteWidth - rightBorder) / spriteWidth);

        // Y ????
        if (y <= bottomHeight)
            v = Mathf.InverseLerp(0, bottomHeight, y) * (bottomBorder / spriteHeight);
        else if (y <= bottomHeight + centerHeight)
            v = Mathf.InverseLerp(bottomHeight, bottomHeight + centerHeight, y) * ((spriteHeight - bottomBorder - topBorder) / spriteHeight) + (bottomBorder / spriteHeight);
        else
            v = Mathf.InverseLerp(bottomHeight + centerHeight, totalHeight, y) * (topBorder / spriteHeight) + ((spriteHeight - topBorder) / spriteHeight);

        return new Vector2(u, v);
    }

    /// <summary>
    /// ? Filled ?????? UV?????????? Rect ???????
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