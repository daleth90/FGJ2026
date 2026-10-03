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

        [Tooltip("可選的事件標題；空白時畫面會顯示事件 ID。")]
        public string title;

        [TextArea(2, 5)]
        public string description;

        [Tooltip("事件發生時顯示的圖片 ID，不含副檔名。")]
        public string imageId;

        [Tooltip("固定三個選項。")]
        public EventOption[] options = { new EventOption(), new EventOption(), new EventOption() };

        public string DisplayTitle => string.IsNullOrWhiteSpace(title) ? $"事件 {eventId}" : title;
    }
}
