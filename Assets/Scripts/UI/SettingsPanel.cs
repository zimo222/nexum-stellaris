using UnityEngine;

public class SettingsPanel : BasePanel
{
    public void OnBack()
    {
        UIManager.Instance.CloseCurrentPanel();
    }

    public void OnClickIn(string name)
    {
        Debug.Log(name);
        UIManager.Instance.OpenPanel(name);
    }

    public void OnClickOut()
    {
        UIManager.Instance.CloseCurrentPanel();
    }
}