using UnityEngine;

public abstract class BasePanel : MonoBehaviour
{
    [SerializeField] protected string panelName; // 面板标识
    public string PanelName => panelName;

    [SerializeField] protected bool initializeVisible = false; // 是否初始可见（不通过堆栈）
    public bool InitializeVisible => initializeVisible;

    private void Awake()
    {
        // 注册到UIManager
        UIManager.Instance.RegisterPanel(this);
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterPanel(this);
    }

    // 打开面板时的回调
    public virtual void OnOpen() { gameObject.SetActive(true); }

    // 关闭面板时的回调
    public virtual void OnClose() { gameObject.SetActive(false); }
}