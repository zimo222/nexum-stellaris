using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MemoryProtectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text memoryText;
    [SerializeField] private string[] memoryLines;   // 在 Inspector 中填入纯白的台词（至少5条）

    private Queue<string> memoryQueue;
    private System.Action onCloseCallback;

    private void Awake()
    {
        panel.SetActive(false);
        ShuffleQueue();
    }

    private void ShuffleQueue()
    {
        List<string> list = new List<string>(memoryLines);
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
        memoryQueue = new Queue<string>(list);
    }

    public void Show(System.Action onComplete)
    {
        onCloseCallback = onComplete;
        if (memoryQueue.Count == 0) ShuffleQueue();
        string line = memoryQueue.Dequeue();
        memoryText.text = line;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private void Update()
    {
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            Hide();
            onCloseCallback?.Invoke();
        }
    }
}