using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制 AI 聊天界面的打开与关闭。
/// 挂载在“打开聊天按钮”上，自动关联关闭按钮。
/// </summary>
public class AICanvasController : MonoBehaviour
{
    [Header("聊天界面 Canvas 物体（AICanvas）")]
    [Tooltip("需要被激活/关闭的 AICanvas 物体")]
    public GameObject aiCanvas;

    [Header("AICanvas 内部的关闭按钮")]
    [Tooltip("AICanvas 下的关闭按钮，点击会关闭整个聊天界面")]
    public Button closeButton;

    private void Start()
    {
        // 安全性检查：确保 aiCanvas 已赋值
        if (aiCanvas == null)
        {
            Debug.LogError("AICanvasController: aiCanvas 未赋值！请将 AICanvas 拖拽到脚本上。");
            return;
        }

        // 获取自身按钮组件（挂载这个脚本的按钮）
        Button openButton = GetComponent<Button>();
        if (openButton != null)
        {
            // 添加打开监听
            openButton.onClick.AddListener(OpenAICanvas);
        }
        else
        {
            Debug.LogError("AICanvasController: 本脚本必须挂载在 Button 上，但未找到 Button 组件。");
        }

        // 为关闭按钮添加监听（如果已赋值）
        if (closeButton != null)
        {
            // 移除已有的监听防止重复（如果多次挂载）
            closeButton.onClick.RemoveListener(CloseAICanvas);
            closeButton.onClick.AddListener(CloseAICanvas);
        }
        else
        {
            Debug.LogWarning("AICanvasController: closeButton 未赋值，请将 AICanvas 下的关闭按钮拖拽到脚本上。");
        }
    }

    private void OpenAICanvas()
    {
        if (aiCanvas != null && !aiCanvas.activeSelf)
        {
            aiCanvas.SetActive(true);
            Debug.Log("AI 聊天界面已打开");
        }
    }

    private void CloseAICanvas()
    {
        if (aiCanvas != null && aiCanvas.activeSelf)
        {
            aiCanvas.SetActive(false);
            Debug.Log("AI 聊天界面已关闭");
        }
    }

    // 可选：对象销毁时移除监听，防止残留
    private void OnDestroy()
    {
        Button openButton = GetComponent<Button>();
        if (openButton != null)
            openButton.onClick.RemoveListener(OpenAICanvas);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseAICanvas);
    }
}