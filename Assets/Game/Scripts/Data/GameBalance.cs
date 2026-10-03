using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 事件節奏與玩家起始數值。
    /// </summary>
    [CreateAssetMenu(menuName = "GrassRun/Game Balance", fileName = "Balance")]
    public class GameBalance : ScriptableObject
    {
        [Header("節奏")]
        [Tooltip("每隔幾秒出現一個事件。")]
        [Min(0.5f)]
        public float eventInterval = 10f;

        [Header("起始")]
        public StatBlock startStats = new StatBlock(0, 50, 0, 0);

        [Tooltip("溼度上限；溼度與其他非善惡屬性的下限都是 0。")]
        [Min(1)]
        public int maxMoisture = 100;

        [Header("內容")]
        public TitleTable titleTable;
    }
}
