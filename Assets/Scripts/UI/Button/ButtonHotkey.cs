using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonHotkey : MonoBehaviour
{
    public KeyCode hotkey = KeyCode.None;       // 主快捷键
    public KeyCode alternativeHotkey = KeyCode.None; // 备用快捷键（可选）

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Update()
    {
        // 仅当按钮可交互时才检测按键
        if (button.interactable &&
            (Input.GetKeyDown(hotkey) ||
             (alternativeHotkey != KeyCode.None && Input.GetKeyDown(alternativeHotkey))))
        {
            // 模拟按钮点击
            button.onClick.Invoke();

            // 可选：播放点击音效或触发视觉反馈
            // ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        }
    }
}