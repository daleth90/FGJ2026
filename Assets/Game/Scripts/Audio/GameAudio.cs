using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 把 RunController 的流程訊號轉成音樂與音效。只訂閱，不改遊戲狀態。
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] RunController controller;
        [SerializeField] MusicPlayer music;

        [Header("音效（留空就不播）")]
        [SerializeField] AudioClip eventOpenedSfx;
        [Tooltip("事件打開時三個選項全鎖定。")]
        [SerializeField] AudioClip deadEndSfx;
        [SerializeField] AudioClip optionChosenSfx;
        [SerializeField] AudioClip resultClosedSfx;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;

        AudioSource sfx;

        void Awake()
        {
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

        void OnRunStarted() => music.Play(MusicCue.Running);

        void OnEventOpened(bool deadEnd) => PlaySfx(deadEnd ? deadEndSfx : eventOpenedSfx);

        void OnOptionChosen(ChoiceResult result) => PlaySfx(optionChosenSfx);

        void OnResultClosed() => PlaySfx(resultClosedSfx);

        void OnRunEnded(RunEndReason reason) =>
            music.Play(reason == RunEndReason.SpecialEnding ? MusicCue.SpecialEnding : MusicCue.NormalEnding);

        void PlaySfx(AudioClip clip)
        {
            if (clip != null) sfx.PlayOneShot(clip, sfxVolume);
        }
    }
}
