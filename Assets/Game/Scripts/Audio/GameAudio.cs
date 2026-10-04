using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 把 RunController 的流程訊號轉成音樂與音效。只訂閱，不改遊戲狀態。
    /// 放在遊戲場景裡；音樂用跨場景常駐的 MusicPlayer（通常從 Start 場景帶過來）。
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] RunController controller;
        [Tooltip("直接從這個場景開始（沒經過 Start 場景）時，用這個 prefab 建立常駐的 MusicPlayer。")]
        [SerializeField] MusicPlayer musicPlayerPrefab;

        [Header("音效（留空就不播）")]
        [SerializeField] AudioClip eventOpenedSfx;
        [Tooltip("事件打開時三個選項全鎖定。")]
        [SerializeField] AudioClip deadEndSfx;
        [SerializeField] AudioClip optionChosenSfx;
        [SerializeField] AudioClip resultClosedSfx;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;

        AudioSource sfx;
        MusicPlayer music;

        void Awake()
        {
            music = MusicPlayer.Instance;
            if (music == null && musicPlayerPrefab != null) music = Instantiate(musicPlayerPrefab);

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
        }

        void OnEnable()
        {
            if (controller == null || music == null)
            {
                Debug.LogError("GameAudio 沒有指定 RunController 或 MusicPlayer。", this);
                return;
            }

            controller.RunStarted += OnRunStarted;
            controller.EventOpened += OnEventOpened;
            controller.OptionChosen += OnOptionChosen;
            controller.ResultClosed += OnResultClosed;
            controller.RunEnded += OnRunEnded;
        }

        void OnDisable()
        {
            if (controller == null) return;

            controller.RunStarted -= OnRunStarted;
            controller.EventOpened -= OnEventOpened;
            controller.OptionChosen -= OnOptionChosen;
            controller.ResultClosed -= OnResultClosed;
            controller.RunEnded -= OnRunEnded;
        }

        void OnRunStarted()
        {
            music.SetDucked(false);
            music.Play(MusicCue.Running);
        }

        // 事件面板到結果畫面這段把音樂壓低，回到奔跑（或進結算）才恢復。
        void OnEventOpened(bool deadEnd)
        {
            music.SetDucked(true);
            PlaySfx(deadEnd ? deadEndSfx : eventOpenedSfx);
        }

        void OnOptionChosen(ChoiceResult result) => PlaySfx(optionChosenSfx);

        void OnResultClosed()
        {
            music.SetDucked(false);
            PlaySfx(resultClosedSfx);
        }

        void OnRunEnded(RunEndReason reason)
        {
            music.SetDucked(false);
            music.Play(reason == RunEndReason.SpecialEnding ? MusicCue.SpecialEnding : MusicCue.NormalEnding);
        }

        void PlaySfx(AudioClip clip)
        {
            if (clip != null) sfx.PlayOneShot(clip, sfxVolume);
        }
    }
}
