using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 为任意 GameObject 添加音效触发功能，支持多种触发方式，包括 IPointerClickHandler。
/// 依赖 AudioManager 脚本。
/// </summary>
public class AudioEventTrigger : MonoBehaviour, IPointerClickHandler
{
    public enum TriggerType
    {
        OnMouseDown,        // 鼠标点击（需要 Collider）
        OnMouseEnter,       // 鼠标进入
        OnMouseExit,        // 鼠标离开
        OnMouseOver,        // 鼠标悬停（每帧触发，慎用）
        OnCollisionEnter,   // 碰撞开始（需要 Collider）
        OnCollisionExit,    // 碰撞结束
        OnTriggerEnter,     // 触发器开始（需要 Collider 设为 Trigger）
        OnTriggerExit,      // 触发器结束
        OnButtonClick,      // UI Button 点击（自动查找 Button 组件）
        OnPointerClick,     // 任意实现 IPointerClickHandler 的 UI 元素点击（如自定义 Item）
        Manual              // 手动调用（通过代码调用 PlaySound() 方法）
    }

    [Header("触发设置")]
    public TriggerType trigger = TriggerType.OnPointerClick; // 默认改为 OnPointerClick

    [Header("音效设置")]
    public string trackName = "Click";          // 使用的音轨名称
    public AudioClip soundClip;               // 播放的音频片段
    [Range(0f, 1f)] public float volume = 0.8f;
    public bool loop = false;
    public bool useFade = false;
    public float fadeInTime = 0.2f;
    public float fadeOutTime = 0.2f;
    public bool stopOnExit = false;           // 对 OnMouseExit / OnCollisionExit / OnTriggerExit 是否淡出停止

    private Button button;
    private AudioManager.AudioTrack audioTrack;
    private bool isPlaying = false;

    private void Awake()
    {
        if (trigger == TriggerType.OnButtonClick)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"GameObject {name} 没有 Button 组件，无法使用 OnButtonClick 触发方式！");
                enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        if (trigger == TriggerType.OnButtonClick && button != null)
            button.onClick.AddListener(OnButtonClickHandler);
    }

    private void OnDisable()
    {
        if (trigger == TriggerType.OnButtonClick && button != null)
            button.onClick.RemoveListener(OnButtonClickHandler);
    }

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager 实例不存在，请确保场景中有 AudioManager 对象！");
            return;
        }

        audioTrack = AudioManager.Instance.GetTrack(trackName);
        if (audioTrack == null)
        {
            audioTrack = AudioManager.Instance.CreateTrack(trackName, loop, volume);
        }
    }

    public void PlaySound()
    {
        if (soundClip == null)
        {
            Debug.LogWarning($"AudioEventTrigger on {name} 未设置 soundClip！");
            return;
        }
        if (audioTrack == null) return;

        if (useFade)
        {
            audioTrack.PlayWithFadeIn(soundClip, fadeInTime);
            isPlaying = true;
        }
        else
        {
            audioTrack.Play(soundClip);
        }
    }

    public void StopSound()
    {
        if (audioTrack == null) return;
        if (useFade && audioTrack.IsPlaying)
            audioTrack.FadeOutAndStop(fadeOutTime);
        else
            audioTrack.Stop();
        isPlaying = false;
    }

    // --- 鼠标事件（需要 Collider）---
    private void OnMouseDown()
    {
        if (trigger == TriggerType.OnMouseDown) PlaySound();
    }

    private void OnMouseEnter()
    {
        if (trigger == TriggerType.OnMouseEnter) PlaySound();
    }

    private void OnMouseExit()
    {
        if (trigger == TriggerType.OnMouseExit)
        {
            if (stopOnExit) StopSound();
            else PlaySound();
        }
    }

    private void OnMouseOver()
    {
        if (trigger == TriggerType.OnMouseOver) PlaySound();
    }

    // --- 碰撞事件---
    private void OnCollisionEnter(Collision collision)
    {
        if (trigger == TriggerType.OnCollisionEnter) PlaySound();
    }

    private void OnCollisionExit(Collision collision)
    {
        if (trigger == TriggerType.OnCollisionExit)
        {
            if (stopOnExit) StopSound();
            else PlaySound();
        }
    }

    // --- 触发器事件---
    private void OnTriggerEnter(Collider other)
    {
        if (trigger == TriggerType.OnTriggerEnter) PlaySound();
    }

    private void OnTriggerExit(Collider other)
    {
        if (trigger == TriggerType.OnTriggerExit)
        {
            if (stopOnExit) StopSound();
            else PlaySound();
        }
    }

    // --- Button 事件---
    private void OnButtonClickHandler()
    {
        PlaySound();
    }

    // --- IPointerClickHandler 实现（用于任意 UI 元素点击，包括自定义 Item）---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (trigger == TriggerType.OnPointerClick)
        {
            PlaySound();
        }
    }

    // 2D 版本的支持
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (trigger == TriggerType.OnCollisionEnter) PlaySound();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (trigger == TriggerType.OnCollisionExit)
        {
            if (stopOnExit) StopSound();
            else PlaySound();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (trigger == TriggerType.OnTriggerEnter) PlaySound();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (trigger == TriggerType.OnTriggerExit)
        {
            if (stopOnExit) StopSound();
            else PlaySound();
        }
    }
}