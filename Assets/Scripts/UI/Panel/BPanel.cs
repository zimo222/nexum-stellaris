using UnityEngine;

public class BPanel : BasePanel
{

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