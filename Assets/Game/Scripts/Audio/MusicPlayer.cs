using UnityEngine;

namespace GrassRun
{
    public enum MusicCue
    {
        Title = 0,
        Running = 1,
        NormalEnding = 2,
        SpecialEnding = 3,
    }

    /// <summary>
    /// 背景音樂：兩個 AudioSource 輪流用，換曲時交叉淡入淡出。
    /// 被動元件，只提供 Play(cue) 和 SetDucked；不讀遊戲狀態，也不判斷結局種類。
    /// 跨場景常駐（DontDestroyOnLoad）；同時只留一個，後來的重複實例會自己銷毀。
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        /// <summary>目前常駐的音樂播放器；還沒建立時是 null。</summary>
        public static MusicPlayer Instance { get; private set; }

        [Header("曲目")]
        [SerializeField] AudioClip titleTheme;
        [SerializeField] AudioClip runningMusic;
        [SerializeField] AudioClip normalEnding;
        [SerializeField] AudioClip specialEnding;

        [Header("音量與過渡")]
        [Tooltip("四首共用的音量；音檔響度本來就一致，不需要個別調。")]
        [SerializeField, Range(0f, 1f)] float volume = 0.55f;
        [Tooltip("換曲時交叉淡入淡出的秒數。")]
        [SerializeField] float crossfadeSeconds = 0.4f;

        [Header("事件壓低")]
        [Tooltip("事件進行中，音樂壓低到原本音量的幾成。")]
        [SerializeField, Range(0f, 1f)] float duckedVolume = 0.4f;
        [Tooltip("壓低和恢復各花幾秒。")]
        [SerializeField] float duckSeconds = 1.5f;

        [Header("音檔尾巴")]
        [Tooltip("循環曲的音檔不是無縫的：結尾前這麼多秒就從頭疊播下一輪，上一輪的尾巴照原樣播完。")]
        [SerializeField] float titleLoopOverlap = 3f;
        [Tooltip("同上，主玩法音樂用。")]
        [SerializeField] float runningLoopOverlap = 1.2f;
        [Tooltip("單次曲的音檔結尾是硬切的：最後這麼多秒淡出。")]
        [SerializeField] float endingFadeOut = 1.5f;

        class Voice
        {
            public AudioSource source;
            public bool live;       // Begin 之後、淡出停掉之前
            public float gain;      // 交叉淡入淡出的增益，每幀往 target 移動
            public float target;
            public float fadeOut;   // 大於 0：播到最後這麼多秒時淡出（單次曲）
            public float tail;      // 尾巴淡出的增益，只會往下走
            public float ducking;   // 0 = 原音量，1 = 完全壓低；每幀往目前的壓低狀態移動
        }

        readonly Voice[] voices = new Voice[2];
        int active;
        MusicCue? current;
        float loopOverlap;          // 目前曲目的疊播秒數；單次曲為 0
        bool ducked;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                voices[i] = new Voice { source = source };
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>切到指定曲目，一律從頭播。已經是這首就忽略。</summary>
        public void Play(MusicCue cue)
        {
            if (current == cue) return;
            current = cue;

            AudioClip clip;
            bool loop;
            float tailSeconds;
            switch (cue)
            {
                case MusicCue.Title:
                    clip = titleTheme; loop = true; tailSeconds = titleLoopOverlap;
                    break;
                case MusicCue.Running:
                    clip = runningMusic; loop = true; tailSeconds = runningLoopOverlap;
                    break;
                case MusicCue.NormalEnding:
                    clip = normalEnding; loop = false; tailSeconds = endingFadeOut;
                    break;
                default:
                    clip = specialEnding; loop = false; tailSeconds = endingFadeOut;
                    break;
            }

            // 目前這首從現有音量淡出；另一個 voice（可能還在淡出上一首）直接停掉讓給新曲，同時最多兩首在響。
            voices[active].target = 0f;
            active = 1 - active;
            var next = voices[active];

            if (clip == null)
            {
                Debug.LogWarning($"MusicPlayer 沒有指定 {cue} 的音檔。", this);
                loopOverlap = 0f;
                next.source.Stop();
                next.live = false;
                return;
            }

            loopOverlap = loop ? Mathf.Min(tailSeconds, clip.length * 0.5f) : 0f;
            // 新曲直接從目前的壓低狀態開始，不跟著上一首慢慢變。
            Begin(next, clip, loop, 0f, loop ? 0f : tailSeconds, ducked ? 1f : 0f);
        }

        /// <summary>壓低或恢復音樂音量（例如事件進行中），會慢慢變化而不是直接跳。</summary>
        public void SetDucked(bool value) => ducked = value;

        void Update()
        {
            RestartLoop();

            // unscaled：不受 Time.timeScale 影響。卡頓的那一幀最多只算 0.05 秒，免得淡入淡出直接跳完。
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float step = crossfadeSeconds > 0f ? dt / crossfadeSeconds : 1f;
            float duckStep = duckSeconds > 0f ? dt / duckSeconds : 1f;
            foreach (var voice in voices)
            {
                if (!voice.live) continue;

                voice.gain = Mathf.MoveTowards(voice.gain, voice.target, step);
                if (voice.target <= 0f && voice.gain <= 0f)
                {
                    voice.source.Stop();
                    voice.live = false;
                    continue;
                }

                voice.ducking = Mathf.MoveTowards(voice.ducking, ducked ? 1f : 0f, duckStep);

                if (voice.fadeOut > 0f)
                {
                    float remaining = voice.source.clip.length - voice.source.time;
                    voice.tail = Mathf.Min(voice.tail, Mathf.Clamp01(remaining / voice.fadeOut));
                }

                voice.source.volume = Level(voice);
            }
        }

        // 循環曲快播到尾巴時，用另一個 voice 從頭疊播下一輪；這一輪關掉 loop，尾巴照原樣播完。
        // 萬一這段期間沒有 Update（例如分頁在背景），AudioSource 自己的 loop 會接手。
        void RestartLoop()
        {
            if (loopOverlap <= 0f) return;

            var voice = voices[active];
            var clip = voice.source.clip;
            if (!voice.live || clip == null || voice.source.time < clip.length - loopOverlap) return;

            voice.source.loop = false;
            active = 1 - active;
            Begin(voices[active], clip, true, 1f, 0f, voice.ducking);
        }

        void Begin(Voice voice, AudioClip clip, bool loop, float gain, float fadeOut, float ducking)
        {
            voice.live = true;
            voice.gain = gain;
            voice.target = 1f;
            voice.fadeOut = fadeOut;
            voice.tail = 1f;
            voice.ducking = ducking;

            var source = voice.source;
            source.Stop();
            source.clip = clip;
            source.loop = loop;
            source.volume = Level(voice);
            source.Play();
        }

        float Level(Voice voice) =>
            volume * Mathf.Lerp(1f, duckedVolume, voice.ducking) * voice.gain * voice.tail;
    }
}
