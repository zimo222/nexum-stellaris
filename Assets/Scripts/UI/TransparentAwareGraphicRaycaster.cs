using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class TransparentAwareGraphicRaycaster : GraphicRaycaster
{
    [Tooltip("???????????? UI ???????????????????")]
    [Range(0f, 1f)]
    public float alphaThreshold = 0.1f;

    // ???????§Ò??????????????
    private List<RaycastResult> m_RaycastResults = new List<RaycastResult>();

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        // ???????????????????????§Ö? UI???????????
        base.Raycast(eventData, m_RaycastResults);
        if (m_RaycastResults.Count == 0) return;

        // ???????????§ß??????????????
        foreach (var result in m_RaycastResults)
        {
            GameObject go = result.gameObject;
            // ?????????????????????????????
            float alpha = GetAlphaAtScreenPosition(go, eventData.position);
            if (alpha < alphaThreshold)
            {
                // ??????‰Ø?????????????????
                continue;
            }

            // ??????????????????ÈÉ??????????§Ò?????????
            resultAppendList.Add(result);
            break;
        }

        m_RaycastResults.Clear();
    }

    /// <summary>
    /// ????????????????UI ???????????????????????????????????
    /// </summary>
    private float GetAlphaAtScreenPosition(GameObject target, Vector2 screenPos)
    {
        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic == null) return 1f; // ?? Graphic ????????????

        // ????? Image ????§á?????????????????????
        Image image = graphic as Image;
        if (image != null && image.sprite != null)
        {
            Texture2D tex = image.sprite.texture;
            if (tex != null && tex.isReadable)
            {
                // ????????????? UI ??????????
                RectTransform rectTrans = target.GetComponent<RectTransform>();
                if (rectTrans != null)
                {
                    Canvas canvas = graphic.canvas;
                    Camera cam = canvas?.worldCamera;
                    if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        cam = null;

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTrans, screenPos, cam, out Vector2 localPoint))
                    {
                        Vector2 uv = CalculateUV(image, localPoint);
                        if (uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1)
                        {
                            Rect spriteRect = image.sprite.rect;
                            int pixelX = Mathf.FloorToInt(spriteRect.x + uv.x * spriteRect.width);
                            int pixelY = Mathf.FloorToInt(spriteRect.y + uv.y * spriteRect.height);
                            pixelX = Mathf.Clamp(pixelX, 0, tex.width - 1);
                            pixelY = Mathf.Clamp(pixelY, 0, tex.height - 1);
                            float pixelAlpha = tex.GetPixel(pixelX, pixelY).a;

                            // ???? Image.color.a ????? CanvasGroup ?????
                            float finalAlpha = pixelAlpha * image.color.a;
                            Transform parent = image.transform;
                            while (parent != null)
                            {
                                CanvasGroup group = parent.GetComponent<CanvasGroup>();
                                if (group != null && !group.ignoreParentGroups)
                                    finalAlpha *= group.alpha;
                                parent = parent.parent;
                            }
                            return finalAlpha;
                        }
                    }
                }
            }
        }

        // ?????????????????
        float alpha = graphic.color.a;
        Transform t = graphic.transform;
        while (t != null)
        {
            CanvasGroup group = t.GetComponent<CanvasGroup>();
            if (group != null && !group.ignoreParentGroups)
                alpha *= group.alpha;
            t = t.parent;
        }
        return alpha;
    }

    /// <summary>
    /// ???? Image ??????????????? UV ??????? Simple??Sliced??Tiled??Filled??
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
                // ???????????????????????????
                return new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)
                );

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
}