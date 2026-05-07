using UnityEngine;

public class BlackRing : MonoBehaviour
{
    [Header("扩散参数")]
    public float expandSpeed = 15f;      // 扩散速度
    public float maxScale = 15f;         // 最大尺寸
    public float fadeDuration = 1f;      // 渐隐持续时间
    public float lifetime = 1.5f;        // 总生命周期

    private SpriteRenderer spriteRenderer;
    private float startTime;
    private float startAlpha;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            startAlpha = spriteRenderer.color.a;
        }

        startTime = Time.time;
        transform.localScale = Vector3.zero; // 从0开始扩大
    }

    void Update()
    {
        float elapsed = Time.time - startTime;

        // 扩大
        float newScale = Mathf.Min(expandSpeed * elapsed, maxScale);
        transform.localScale = new Vector3(newScale, newScale, 1f);

        // 渐隐效果
        if (spriteRenderer != null && fadeDuration > 0)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }

        // 生命周期结束销毁
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}