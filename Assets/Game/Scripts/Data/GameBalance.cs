using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 所有規則參數集中在這裡。事件只寫階級（低／中／高），實際門檻由這裡依週期換算。
    /// </summary>
    [CreateAssetMenu(menuName = "GrassRun/Game Balance", fileName = "Balance")]
    public class GameBalance : ScriptableObject
    {
        [Header("節奏")]
        [Tooltip("每隔幾秒出現一個事件。")]
        [Min(0.5f)]
        public float eventInterval = 10f;

        [Tooltip("一個週期有幾個事件，最後一個是檢驗點。6 × 10 秒 = 每 60 秒一個檢驗點。")]
        [Min(2)]
        public int eventsPerCycle = 6;

        [Header("起始")]
        public StatBlock startStats = new StatBlock(4, 4, 4);

        [Header("門檻曲線")]
        [Tooltip("等級 0 時，一項數值的預期值。")]
        public float expectedBase = 4f;

        [Tooltip("每升一級，預期值增加多少。")]
        public float expectedPerLevel = 3f;

        [Tooltip("嚴苛度爬升：每升一級，門檻額外提高的比例。大於 0 時門檻成長會快過容量，遊戲才保證會結束。")]
        [Min(0f)]
        public float harshnessPerLevel = 0.04f;

        [Tooltip("各階級的門檻 = 當級需求值 × 這個倍率。")]
        public float lowTier = 0.6f;
        public float midTier = 0.9f;
        public float highTier = 1.4f;

        [Header("總容量（論點 B：非線性取捨）")]
        [Tooltip("開啟時三數值總和有上限，滿了之後的成長會溢出。關閉就是「越多越好」。")]
        public bool useCapacity = true;

        [Tooltip("容量 = 3 × 下一級預期值 × 這個倍率。調高會讓取捨變鬆。")]
        [Min(0.1f)]
        public float capacityFactor = 1f;

        [Header("回饋")]
        [Tooltip("差距超過門檻的這個比例時，鎖定原因會從「還不夠」變成「遠遠不夠」。")]
        [Range(0f, 1f)]
        public float farGapRatio = 0.3f;

        public float CheckpointInterval => eventInterval * eventsPerCycle;

        /// <summary>一項數值在該等級的預期值（平均配點的小草大概會長到這裡）。</summary>
        public float Expected(int level) => expectedBase + expectedPerLevel * level;

        /// <summary>該等級的需求值：預期值再乘上嚴苛度爬升。</summary>
        public float Demand(int level) => Expected(level) * (1f + harshnessPerLevel * level);

        public float TierFactor(Tier tier)
        {
            switch (tier)
            {
                case Tier.Low: return lowTier;
                case Tier.Mid: return midTier;
                default: return highTier;
            }
        }

        public int Threshold(Tier tier, int level) => RoundHalfUp(Demand(level) * TierFactor(tier));

        /// <summary>該週期的總容量。容量對齊的是「這一區檢驗點」的預期值，所以週期內有成長空間。</summary>
        public int Capacity(int cycle)
        {
            if (!useCapacity) return int.MaxValue;
            return RoundHalfUp(StatBlock.StatCount * Expected(cycle + 1) * capacityFactor);
        }

        public bool IsFarGap(int gap, int threshold) => gap > Mathf.Max(1f, threshold * farGapRatio);

        static int RoundHalfUp(float value) => Mathf.Max(0, Mathf.FloorToInt(value + 0.5f));
    }
}
