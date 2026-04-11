using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 提示按钮脚本：点击按钮后展示图片序列，按空格或点击屏幕切换图片
/// </summary>
public class TipButton : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("触发按钮（如果不指定则从当前对象获取）")]
    [SerializeField] private Button triggerButton;

    [Tooltip("用于显示图片的Image组件")]
    [SerializeField] private GameObject Tip;
    [SerializeField] private Image displayImage;

    [Header("图片序列")]
    [Tooltip("需要依次展示的图片")]
    [SerializeField] private Sprite[] imageSequence;

    private int currentIndex;          // 当前显示的图片索引
    private bool isShowing;             // 是否正在展示图片序列

    private void Start()
    {
        // 自动获取组件
        if (triggerButton == null)
            triggerButton = GetComponent<Button>();

        if (displayImage == null)
            displayImage = GetComponentInChildren<Image>();

        // 注册按钮点击事件
        if (triggerButton != null)
            triggerButton.onClick.AddListener(OnButtonClick);

        // 初始隐藏图片显示区域
        if (displayImage != null)
            displayImage.gameObject.SetActive(false);

        // 检查序列是否为空
        if (imageSequence == null || imageSequence.Length == 0)
            Debug.LogWarning("TipButton: 图片序列为空，请分配图片！", this);
    }

    private void OnDestroy()
    {
        // 移除监听，避免内存泄漏
        if (triggerButton != null)
            triggerButton.onClick.RemoveListener(OnButtonClick);
    }

    private void Update()
    {
        // 仅在展示模式下检测输入
        if (!isShowing) return;

        // 空格键 或 鼠标左键点击 → 切换下一张
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            NextImage();
        }
    }

    /// <summary>
    /// 按钮点击回调：开始展示图片序列
    /// </summary>
    private void OnButtonClick()
    {
        if (isShowing) return;
        if (imageSequence == null || imageSequence.Length == 0)
        {
            Debug.LogWarning("TipButton: 图片序列为空，无法展示！", this);
            return;
        }

        StartShowing();
    }

    /// <summary>
    /// 开始展示序列
    /// </summary>
    private void StartShowing()
    {
        isShowing = true;
        currentIndex = 0;

        // 显示第一张图片
        displayImage.sprite = imageSequence[currentIndex];
        Tip.gameObject.SetActive(true);
        displayImage.gameObject.SetActive(true);

        // 禁用按钮，防止重复触发
        if (triggerButton != null)
            triggerButton.interactable = false;
    }

    /// <summary>
    /// 切换到下一张图片（由输入事件调用）
    /// </summary>
    private void NextImage()
    {
        if (currentIndex + 1 < imageSequence.Length)
        {
            // 还有下一张：更新图片
            currentIndex++;
            displayImage.sprite = imageSequence[currentIndex];
        }
        else
        {
            // 已是最后一张：结束展示
            EndShowing();
        }
    }

    /// <summary>
    /// 结束展示，恢复按钮初始状态
    /// </summary>
    private void EndShowing()
    {
        isShowing = false;
        Tip.gameObject.SetActive(false);
        displayImage.gameObject.SetActive(false);

        if (triggerButton != null)
            triggerButton.interactable = true;
    }
}