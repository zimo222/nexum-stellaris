using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private Dictionary<string, AudioTrack> tracks = new Dictionary<string, AudioTrack>();
    private AudioSource oneShotSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
    }

    public AudioTrack CreateTrack(string trackName, bool loop = false, float volume = 1f)
    {
        if (tracks.ContainsKey(trackName))
        {
            Debug.LogWarning($"音轨 {trackName} 已存在，返回现有音轨。");
            return tracks[trackName];
        }

        GameObject trackObj = new GameObject($"Track_{trackName}");
        trackObj.transform.SetParent(transform);
        AudioSource audioSource = trackObj.AddComponent<AudioSource>();
        audioSource.loop = loop;
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.playOnAwake = false;

        AudioTrack track = new AudioTrack(trackObj, audioSource, this);
        track.SetDesiredVolume(volume); // 记录期望音量
        tracks.Add(trackName, track);
        return track;
    }

    public AudioTrack GetTrack(string trackName)
    {
        tracks.TryGetValue(trackName, out AudioTrack track);
        return track;
    }

    public void RemoveTrack(string trackName)
    {
        if (tracks.TryGetValue(trackName, out AudioTrack track))
        {
            track.Stop();
            Destroy(track.gameObject);
            tracks.Remove(trackName);
        }
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
            oneShotSource.PlayOneShot(clip, volume);
    }

    public void SetGlobalVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        foreach (var track in tracks.Values)
        {
            track.SetVolume(volume);
        }
    }

    public void StopAllTracks() => tracks.Values.ForEach(t => t.Stop());
    public void PauseAllTracks() => tracks.Values.ForEach(t => t.Pause());
    public void ResumeAllTracks() => tracks.Values.ForEach(t => t.Resume());

    public class AudioTrack
    {
        public GameObject gameObject { get; private set; }
        private AudioSource audioSource;
        private MonoBehaviour coroutineRunner;
        private Coroutine currentFadeCoroutine;

        private float desiredVolume;   // 用户期望的音量（不受淡出影响）
        private float currentVolume;   // 实时音量（用于渐变）

        public AudioTrack(GameObject obj, AudioSource source, MonoBehaviour runner)
        {
            gameObject = obj;
            audioSource = source;
            coroutineRunner = runner;
            desiredVolume = source.volume;
            currentVolume = source.volume;
        }

        // 设置期望音量（立即生效，并停止渐变）
        public void SetVolume(float volume)
        {
            StopFade();
            desiredVolume = Mathf.Clamp01(volume);
            currentVolume = desiredVolume;
            audioSource.volume = currentVolume;
        }

        public float GetVolume() => currentVolume;
        public float GetDesiredVolume() => desiredVolume;

        public void SetDesiredVolume(float volume)
        {
            desiredVolume = Mathf.Clamp01(volume);
        }

        // --- 基础播放（无淡入）---
        public void Play(AudioClip clip = null)
        {
            if (clip != null) audioSource.clip = clip;
            if (audioSource.clip != null) audioSource.Play();
        }

        // --- 带淡入的播放 ---
        /// <summary>
        /// 播放音频，并淡入到期望音量（或指定目标音量）
        /// </summary>
        /// <param name="clip">音频片段（可选）</param>
        /// <param name="fadeInTime">淡入时长</param>
        /// <param name="targetVol">目标音量，-1表示使用当前期望音量</param>
        public void PlayWithFadeIn(AudioClip clip = null, float fadeInTime = 0.5f, float targetVol = -1f)
        {
            if (fadeInTime <= 0f)
            {
                Play(clip);
                return;
            }

            StopFade();

            if (clip != null) audioSource.clip = clip;
            if (audioSource.clip == null)
            {
                Debug.LogWarning("没有可播放的音频片段");
                return;
            }

            float endVolume = targetVol >= 0 ? Mathf.Clamp01(targetVol) : desiredVolume;
            if (endVolume <= 0f)
            {
                Debug.LogWarning($"目标音量为0，无法淡入。请检查 desiredVolume 或传入有效的 targetVol。当前 desiredVolume = {desiredVolume}");
                return;
            }

            // 从0开始淡入
            audioSource.volume = 0f;
            currentVolume = 0f;
            audioSource.Play();

            currentFadeCoroutine = coroutineRunner.StartCoroutine(FadeRoutine(0f, endVolume, fadeInTime, () =>
            {
                desiredVolume = endVolume; // 淡入完成后同步期望音量
            }));
        }

        public void Stop()
        {
            StopFade();
            audioSource.Stop();
        }

        public void Pause()
        {
            StopFade();
            audioSource.Pause();
        }

        public void Resume()
        {
            if (!audioSource.isPlaying)
                audioSource.UnPause();
        }

        public void SetLoop(bool loop) => audioSource.loop = loop;
        public bool IsPlaying => audioSource.isPlaying;
        public AudioClip Clip
        {
            get => audioSource.clip;
            set => audioSource.clip = value;
        }
        public AudioSource AudioSource => audioSource;

        // --- 淡入淡出扩展 ---
        /// <summary>
        /// 淡入到当前期望音量（不会改变期望音量）
        /// </summary>
        public void FadeIn(float fadeTime)
        {
            if (desiredVolume <= 0f)
            {
                Debug.LogWarning("期望音量为0，无法淡入。请先通过 SetVolume 设置有效音量。");
                return;
            }
            FadeTo(desiredVolume, fadeTime);
        }

        /// <summary>
        /// 淡出到0，并可选择停止播放（不会改变期望音量）
        /// </summary>
        public void FadeOutAndStop(float fadeTime, bool stopAfterFade = true)
        {
            FadeTo(0f, fadeTime, () =>
            {
                if (stopAfterFade) Stop();
            });
        }

        /// <summary>
        /// 通用的音量渐变
        /// </summary>
        /// <param name="endVolume">目标音量（如果是0，不会修改 desiredVolume）</param>
        /// <param name="duration">渐变时长</param>
        /// <param name="onComplete">完成回调</param>
        public void FadeTo(float endVolume, float duration, System.Action onComplete = null)
        {
            if (duration <= 0f)
            {
                StopFade();
                audioSource.volume = endVolume;
                currentVolume = endVolume;
                if (endVolume != 0) desiredVolume = endVolume;
                onComplete?.Invoke();
                return;
            }

            StopFade();
            float startVolume = currentVolume;
            currentFadeCoroutine = coroutineRunner.StartCoroutine(FadeRoutine(startVolume, endVolume, duration, () =>
            {
                if (endVolume != 0) desiredVolume = endVolume;
                onComplete?.Invoke();
            }));
        }

        private IEnumerator FadeRoutine(float start, float end, float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t); // 平滑曲线
                currentVolume = Mathf.Lerp(start, end, eased);
                audioSource.volume = currentVolume;
                yield return null;
            }
            currentVolume = end;
            audioSource.volume = end;
            currentFadeCoroutine = null;
            onComplete?.Invoke();
        }

        private void StopFade()
        {
            if (currentFadeCoroutine != null)
            {
                coroutineRunner.StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
            }
        }

        public void CrossFade(AudioClip newClip, float fadeOutTime, float fadeInTime, bool loopAfterSwitch = false)
        {
            if (newClip == null) return;

            if (!IsPlaying && audioSource.clip == null)
            {
                audioSource.clip = newClip;
                audioSource.loop = loopAfterSwitch;
                audioSource.volume = 0f;
                currentVolume = 0f;
                audioSource.Play();
                FadeIn(fadeInTime);
                return;
            }

            FadeOutAndStop(fadeOutTime, true);
            coroutineRunner.StartCoroutine(CrossFadeRoutine(newClip, fadeOutTime, fadeInTime, loopAfterSwitch));
        }

        private IEnumerator CrossFadeRoutine(AudioClip newClip, float fadeOutTime, float fadeInTime, bool loopAfterSwitch)
        {
            while (currentFadeCoroutine != null)
                yield return null;

            Stop();
            audioSource.clip = newClip;
            audioSource.loop = loopAfterSwitch;
            audioSource.volume = 0f;
            currentVolume = 0f;
            audioSource.Play();
            FadeIn(fadeInTime);
        }
    }

    // 切换场景时调用（根据场景名切换 BGM）
    public void ChangeSceneMusic(string targetScene)
    {
        var bgmTrack = AudioManager.Instance.GetTrack("BGM");
        if (bgmTrack == null) return;

        Debug.Log($"切换到场景: {targetScene}");
        AudioClip newClip = null;

        switch (targetScene)
        {
            case "1_TheNestOfWarmLight":
                newClip = Resources.Load<AudioClip>("Audio/BGM/VillageTheme1");
                break;
            case "2_TheArgentCorridor":
                newClip = Resources.Load<AudioClip>("Audio/BGM/PaleCorridorTheme1");
                break;
            case "3_TheVerdantMeadow":
                newClip = Resources.Load<AudioClip>("Audio/BGM/WildernessTheme1");
                break;
            case "4_TheWorkshopOfPassion":
                newClip = Resources.Load<AudioClip>("Audio/BGM/GalleryTheme1");
                break;
            case "5_TheHallOfUniversalConcord":
                newClip = Resources.Load<AudioClip>("Audio/BGM/HallTheme1");
                break;
            case "6_TheStellarWish":
                newClip = Resources.Load<AudioClip>("Audio/BGM/MainTheme2");
                break;
        }

        if (newClip != null)
        {
            // 使用 CrossFade 平滑切换，会先淡出当前 → 切换 → 淡入新音乐
            bgmTrack.CrossFade(newClip, fadeOutTime: 1.5f, fadeInTime: 1.5f, loopAfterSwitch: true);
        }
    }
}

public static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, System.Action<T> action)
    {
        foreach (var item in source) action(item);
    }
}