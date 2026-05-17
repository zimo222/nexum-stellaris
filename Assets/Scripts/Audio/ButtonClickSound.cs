using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 为 Button 添加点击音效，使用 AudioManager 中名为 "Button" 的音轨播放。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    [Header("音效设置")]
    [Tooltip("点击时播放的音频片段")]
    public AudioClip clickSound;

    [Tooltip("播放音量（仅当音轨不存在自动创建时有效，若已存在音轨则使用其已有音量）")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    private Button button;
    private bool listenerAdded = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("ButtonClickSound 需要 Button 组件！");
            return;
        }
    }

    private void OnEnable()
    {
        AddListener();
    }

    private void OnDisable()
    {
        RemoveListener();
    }

    private void AddListener()
    {
        if (button == null || listenerAdded) return;
        button.onClick.AddListener(OnButtonClick);
        listenerAdded = true;
    }

    private void RemoveListener()
    {
        if (button == null || !listenerAdded) return;
        button.onClick.RemoveListener(OnButtonClick);
        listenerAdded = false;
    }

    private void OnButtonClick()
    {
        if (clickSound == null)
        {
            Debug.LogWarning("ButtonClickSound: 未指定点击音效！");
            return;
        }

        // 确保 AudioManager 存在
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager 实例不存在，请确保场景中存在 AudioManager 对象！");
            return;
        }

        // 获取或创建 "Button" 音轨
        AudioManager.AudioTrack track = AudioManager.Instance.GetTrack("Button");
        if (track == null)
        {
            track = AudioManager.Instance.CreateTrack("Button", loop: false, volume: volume);
        }

        // 播放点击音效（无淡入，立即播放）
        track.Play(clickSound);
    }
}