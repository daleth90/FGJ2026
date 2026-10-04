using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 場景開始時叫常駐的 MusicPlayer 播指定曲目（例如 Start 場景播 Title Theme）。
    /// </summary>
    public class MusicCueOnStart : MonoBehaviour
    {
        [SerializeField] MusicCue cue = MusicCue.Title;

        void Start()
        {
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.Play(cue);
            else Debug.LogWarning("場景裡沒有 MusicPlayer，無法播放音樂。", this);
        }
    }
}
