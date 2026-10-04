using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 一個事件。放在 Resources/Events 底下就會自動進入事件池，不需要另外註冊。
    /// </summary>
    [CreateAssetMenu(menuName = "GrassRun/Event", fileName = "NewEvent")]
    public class EventDefinition : ScriptableObject
    {
        public const int OptionCount = 3;
        public const int MaxOptionTextLength = 14;

        [Tooltip("唯一事件 ID。千位數代表內容階段。")]
        [Min(1)]
        public int eventId;

        [Tooltip("已經歷至少幾個事件後進入這一批抽選池。抽選只會使用目前最高的已解鎖批次。")]
        [Min(0)]
        public int unlockEventCount;

        [Tooltip("前置事件 ID；與前置選項一起填寫。符合時下一次必定出現，本事件不參與一般隨機抽選。0 代表沒有前置。")]
        [Min(0)]
        public int prerequisiteEventId;

        [Tooltip("前置事件必須選擇的選項：a、b 或 c；沒有前置時留空。")]
        public string prerequisiteChoice;

        [Tooltip("可選的事件標題；空白時畫面會顯示事件 ID。")]
        public string title;

        [TextArea(2, 5)]
        public string description;

        [Tooltip("事件發生時顯示的圖片。")]
        public Sprite image;

        [Tooltip("固定三個選項。")]
        public EventOption[] options = { new EventOption(), new EventOption(), new EventOption() };

        public bool HasPrerequisite => prerequisiteEventId != 0 || !string.IsNullOrEmpty(prerequisiteChoice);
        public string DisplayTitle => string.IsNullOrWhiteSpace(title) ? $"事件 {eventId}" : title;
    }
}
