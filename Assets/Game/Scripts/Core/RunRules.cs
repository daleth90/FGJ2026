using UnityEngine;

namespace GrassRun
{
    public enum LockKind
    {
        None = 0,
        /// <summary>門檻沒到。</summary>
        Requirement = 1,
        /// <summary>代價付不起。</summary>
        Cost = 2,
    }

    public struct OptionCheck
    {
        public bool available;
        public LockKind lockKind;

        /// <summary>沒達成的那條門檻（lockKind 為 Requirement 時有效）。</summary>
        public Requirement requirement;
        public int threshold;
        public int gap;

        /// <summary>付不起的那項數值（lockKind 為 Cost 時有效）。</summary>
        public StatType costStat;
    }

    /// <summary>
    /// 選項的判定與結算。整個遊戲只有這兩條規則會讓選項不可選：門檻沒到、代價付不起。
    /// </summary>
    public static class RunRules
    {
        /// <summary>檢驗點考的是下一級，所以比同區的隨機事件高一級。</summary>
        public static int LevelFor(EventKind kind, int cycle) => kind == EventKind.Checkpoint ? cycle + 1 : cycle;

        public static bool Meets(Comparison comparison, int value, int threshold) =>
            comparison == Comparison.AtLeast ? value >= threshold : value <= threshold;

        public static StatBlock NetChange(EventOption option)
        {
            var net = new StatBlock();
            if (option.changes == null) return net;
            foreach (var change in option.changes) net[change.stat] += change.amount;
            return net;
        }

        public static OptionCheck Check(EventOption option, StatBlock stats, GameBalance balance, int level)
        {
            if (option.requirements != null)
            {
                foreach (var requirement in option.requirements)
                {
                    int threshold = balance.Threshold(requirement.tier, level);
                    int value = stats[requirement.stat];
                    if (Meets(requirement.comparison, value, threshold)) continue;

                    return new OptionCheck
                    {
                        lockKind = LockKind.Requirement,
                        requirement = requirement,
                        threshold = threshold,
                        gap = Mathf.Abs(value - threshold),
                    };
                }
            }

            var net = NetChange(option);
            for (int i = 0; i < StatBlock.StatCount; i++)
            {
                var stat = (StatType)i;
                if (stats[stat] + net[stat] < 0)
                    return new OptionCheck { lockKind = LockKind.Cost, costStat = stat };
            }

            return new OptionCheck { available = true };
        }

        /// <summary>
        /// 結算一個選項：先扣代價騰出空間，再依 changes 的順序成長；超出容量的成長溢出。
        /// 回傳實際套用的增減。
        /// </summary>
        public static StatBlock Apply(EventOption option, ref StatBlock stats, int capacity, out int overflow)
        {
            var net = NetChange(option);
            var applied = new StatBlock();
            overflow = 0;

            for (int i = 0; i < StatBlock.StatCount; i++)
            {
                var stat = (StatType)i;
                if (net[stat] >= 0) continue;
                stats[stat] += net[stat];
                applied[stat] = net[stat];
            }

            if (option.changes == null) return applied;

            foreach (var change in option.changes)
            {
                var stat = change.stat;
                int gain = net[stat];
                if (gain <= 0) continue;
                net[stat] = 0; // 同一項數值只結算一次

                int room = Mathf.Max(0, capacity - stats.Total);
                int taken = Mathf.Min(gain, room);
                stats[stat] += taken;
                applied[stat] = taken;
                overflow += gain - taken;
            }

            return applied;
        }

        /// <summary>試算選了之後的數值，不改動原本的。</summary>
        public static StatBlock Preview(EventOption option, StatBlock stats, int capacity)
        {
            Apply(option, ref stats, capacity, out _);
            return stats;
        }
    }
}
