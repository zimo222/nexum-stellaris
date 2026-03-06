using UnityEngine;
//面板基类
public abstract class BasePanel : MonoBehaviour
{
    [SerializeField] protected string panelName; // 面板标识
    public string PanelName => panelName;

    // 打开面板时的回调
    public virtual void OnOpen() { gameObject.SetActive(true); }

    // 关闭面板时的回调
    public virtual void OnClose() { gameObject.SetActive(false); }
}