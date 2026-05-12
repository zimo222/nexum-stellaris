using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MessagePopupController : MonoBehaviour
{
    public static MessagePopupController Instance { get; private set; }

    [Header("UI 引用")]
    public RectTransform messageContainer;
    public GameObject messagePrefab;

    [Header("设置")]
    public float displayDuration = 1f;
    public int maxMessages = 5;

    private Queue<GameObject> activeMessages = new Queue<GameObject>();
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowMessage(string text)
    {
        GameObject newMsg = Instantiate(messagePrefab, messageContainer);
        TMP_Text txt = newMsg.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = text;

        activeMessages.Enqueue(newMsg);
        Coroutine cor = StartCoroutine(FadeOutAndDestroy(newMsg, displayDuration));
        activeCoroutines[newMsg] = cor;

        if (activeMessages.Count > maxMessages)
        {
            GameObject oldest = activeMessages.Dequeue();
            if (oldest != null)
            {
                if (activeCoroutines.TryGetValue(oldest, out Coroutine oldCor))
                {
                    StopCoroutine(oldCor);
                    activeCoroutines.Remove(oldest);
                }
                Destroy(oldest);
            }
        }
    }

    private IEnumerator FadeOutAndDestroy(GameObject msg, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (msg == null) yield break;

        CanvasGroup cg = msg.GetComponent<CanvasGroup>();
        if (cg == null) cg = msg.AddComponent<CanvasGroup>();

        float fadeTime = 0.2f;
        float startAlpha = cg.alpha;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            if (msg == null) yield break;
            if (cg != null)
                cg.alpha = Mathf.Lerp(startAlpha, 0, t / fadeTime);
            yield return null;
        }
        if (msg != null && cg != null)
            cg.alpha = 0;

        // 从队列和字典中移除
        if (msg != null)
        {
            var newQueue = new Queue<GameObject>();
            foreach (var m in activeMessages)
                if (m != msg) newQueue.Enqueue(m);
            activeMessages = newQueue;
            activeCoroutines.Remove(msg);
            Destroy(msg);
        }
    }
}