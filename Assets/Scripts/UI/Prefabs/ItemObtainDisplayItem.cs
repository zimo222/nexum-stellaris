using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemObtainDisplayItem : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text typeText;

    private CanvasGroup canvasGroup;
    private float lifeTime = 3f;
    private float fadeDuration = 0.5f;
    private System.Action<ItemObtainDisplayItem> onDestroyCallback;

    public void Initialize(Sprite icon, string itemName, string itemType, 
        float lifeTime, float fadeDuration, System.Action<ItemObtainDisplayItem> onDestroy)
    {
        iconImage.sprite = icon;
        nameText.text = itemName;
        typeText.text = itemType;
        this.lifeTime = lifeTime;
        this.fadeDuration = fadeDuration;
        this.onDestroyCallback = onDestroy;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        StartCoroutine(LifecycleRoutine());
    }

    private IEnumerator LifecycleRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        onDestroyCallback?.Invoke(this);
        Destroy(gameObject);
    }
}