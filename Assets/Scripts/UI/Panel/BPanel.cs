using UnityEngine;

public class BPanel : BasePanel
{

    public void OnClickIn(string name)
    {
        UIManager.Instance.OpenPanel(name);
    }

    public void OnClickOut()
    {
        UIManager.Instance.CloseCurrentPanel();
    }
}