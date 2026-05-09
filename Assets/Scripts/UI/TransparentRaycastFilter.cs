using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class TransparentRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    [Tooltip("透明度低于此值的像素视为完全透明，点击会穿透")]
    [Range(0f, 1f)]
    public float alphaThreshold = 0.1f;

    private Image image;
    private Sprite sprite;
    private Texture2D texture;

    void Awake()
    {
        image = GetComponent<Image>();
        if (image != null && image.sprite != null)
        {
            sprite = image.sprite;
            texture = sprite.texture;
        }
    }

    public bool IsRaycastLocationValid(Vector2 screenPos, Camera eventCamera)
    {
        // 如果没有图片或纹理不可读，使用常规阻挡逻辑（不穿透）
        if (image == null || image.sprite == null || texture == null || !texture.isReadable)
            return true; // 默认允许射线命中（不穿透）

        // 将屏幕坐标转换为图片的局部坐标
        RectTransform rectTransform = transform as RectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPos, eventCamera, out Vector2 localPoint))
            return false;

        // 获取 UV 坐标（支持 Simple、Sliced、Tiled、Filled）
        Vector2 uv = GetUV(localPoint);
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
            return false;

        // 采样纹理像素 alpha
        Rect spriteRect = sprite.rect;
        int pixelX = Mathf.FloorToInt(spriteRect.x + uv.x * spriteRect.width);
        int pixelY = Mathf.FloorToInt(spriteRect.y + uv.y * spriteRect.height);
        pixelX = Mathf.Clamp(pixelX, 0, texture.width - 1);
        pixelY = Mathf.Clamp(pixelY, 0, texture.height - 1);

        float pixelAlpha = texture.GetPixel(pixelX, pixelY).a;
        // 乘以 Image.color.a 和 CanvasGroup 的透明度
        float finalAlpha = pixelAlpha * image.color.a;
        Transform parent = transform;
        while (parent != null)
        {
            CanvasGroup group = parent.GetComponent<CanvasGroup>();
            if (group != null && !group.ignoreParentGroups)
                finalAlpha *= group.alpha;
            parent = parent.parent;
        }

        // 如果该点的透明度 >= 阈值，则阻挡点击（射线命中）；否则穿透
        return finalAlpha >= alphaThreshold;
    }

    private Vector2 GetUV(Vector2 localPoint)
    {
        Rect rect = (transform as RectTransform).rect;
        switch (image.type)
        {
            case Image.Type.Simple:
                return new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)
                );
            case Image.Type.Sliced:
            case Image.Type.Tiled:
                return GetUVForSlicedOrTiled(localPoint);
            case Image.Type.Filled:
                // 简化，按需扩展
                return new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)
                );
            default:
                return Vector2.zero;
        }
    }

    private Vector2 GetUVForSlicedOrTiled(Vector2 localPoint)
    {
        Rect rect = (transform as RectTransform).rect;
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
}