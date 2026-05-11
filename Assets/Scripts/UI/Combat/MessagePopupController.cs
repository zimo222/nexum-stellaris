using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessagePopupController : MonoBehaviour
{
    public static MessagePopupController Instance { get; private set; }

    [Header("UI 引用")]
    public RectTransform messageContainer;   // ScrollRect 的 Content
    public GameObject messagePrefab;         // 预制体（含 TMP_Text 和 CanvasGroup）

    [Header("设置")]
    public float displayDuration = 1f;       // 每条消息显示时间（秒）
    public int maxMessages = 5;              // 最多同时显示条数，超出删最早

    private Queue<GameObject> activeMessages = new Queue<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 不设置 DontDestroyOnLoad，UI 通常在场景中
    }

    /// <summary>
    /// 显示一条消息（自动添加至 Scroll 容器，并管理淡出与销毁）
    /// </summary>
    public void ShowMessage(string text)
    {
        // 实例化预制体
        GameObject newMsg = Instantiate(messagePrefab, messageContainer);
        TMP_Text txt = newMsg.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = text;

        activeMessages.Enqueue(newMsg);

        // 超出最大数量则销毁最旧的消息
        if (activeMessages.Count > maxMessages)
        {
            GameObject oldest = activeMessages.Dequeue();
            if (oldest != null) Destroy(oldest);
        }

        StartCoroutine(FadeOutAndDestroy(newMsg, displayDuration));
    }

    private IEnumerator FadeOutAndDestroy(GameObject msg, float delay)
    {
        CanvasGroup cg = msg.GetComponent<CanvasGroup>();
        if (cg == null) cg = msg.AddComponent<CanvasGroup>();

        // 等待完全显示时间
        yield return new WaitForSeconds(delay);

        // 淡出（0.2 秒）
        float fadeTime = 0.2f;
        float startAlpha = cg.alpha;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            cg.alpha = Mathf.Lerp(startAlpha, 0, t / fadeTime);
            yield return null;
        }
        cg.alpha = 0;

        // 从队列中移除该对象（遍历移除）
        var newQueue = new Queue<GameObject>();
        foreach (var m in activeMessages)
            if (m != msg) newQueue.Enqueue(m);
        activeMessages = newQueue;

        Destroy(msg);
    }
}