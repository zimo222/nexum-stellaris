using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingPanel : BasePanel
{
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI tipText;

    public void SetProgress(float value)
    {
        progressSlider.value = value;
        if (progressText != null)
            progressText.text = (value * 100).ToString("F0") + "%";
    }

    public void SetTip(string tip)
    {
        if (tipText != null)
            tipText.text = tip;
    }
}