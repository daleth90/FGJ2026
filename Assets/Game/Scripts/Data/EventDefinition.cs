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

        [Tooltip("隨機事件每隔一段時間出現；檢驗點每個週期的最後出現，門檻比隨機事件高一級。")]
        public EventKind kind = EventKind.Random;

        [Tooltip("抽中的權重。調高會更常出現。")]
        [Min(0f)]
        public float weight = 1f;

        public string title;

        [TextArea(2, 5)]
        public string description;

        [Tooltip("固定三個選項。")]
        public EventOption[] options = { new EventOption(), new EventOption(), new EventOption() };
    }
}
