using System.Text;

namespace GrassRun
{
    /// <summary>
    /// 系統自動產生的文字都集中在這裡（事件本身的文案在各事件資產上）。
    /// </summary>
    public static class GameText
    {
        public const string SpeedColor = "#F4C542";
        public const string MoistureColor = "#5DB4EC";
        public const string ToughnessColor = "#E08A58";

        /// <summary>結算畫面的稱號。判定規則還沒做，先固定顯示這個。</summary>
        public const string PlaceholderTitle = "無名小草";

        public static string StatName(StatType stat)
        {
            switch (stat)
            {
                case StatType.Speed: return "速度";
                case StatType.Moisture: return "溼度";
                default: return "韌度";
            }
        }

        public static string StatColor(StatType stat)
        {
            switch (stat)
            {
                case StatType.Speed: return SpeedColor;
                case StatType.Moisture: return MoistureColor;
                default: return ToughnessColor;
            }
        }

        public static string TierName(Tier tier)
        {
            switch (tier)
            {
                case Tier.Low: return "低";
                case Tier.Mid: return "中";
                default: return "高";
            }
        }

        public static string KindName(EventKind kind) => kind == EventKind.Checkpoint ? "檢驗點" : "隨機事件";

        /// <summary>選項被鎖住的原因。不講數字，只講方向和差距大小。</summary>
        public static string LockReason(OptionCheck check, bool far)
        {
            if (check.lockKind == LockKind.Cost)
            {
                switch (check.costStat)
                {
                    case StatType.Speed: return "你已經慢到不能再慢了。";
                    case StatType.Moisture: return "你身上已經沒有水可以付出了。";
                    default: return "你已經傷痕累累，再也承受不起了。";
                }
            }

            bool atLeast = check.requirement.comparison == Comparison.AtLeast;
            switch (check.requirement.stat)
            {
                case StatType.Speed:
                    if (atLeast) return far ? "你遠遠不夠快。" : "你還不夠快。";
                    return far ? "你快得停不下來。" : "你有點太快了，慢不下來。";
                case StatType.Moisture:
                    if (atLeast) return far ? "你太乾了，遠遠不夠溼潤。" : "你還不夠溼潤。";
                    return far ? "你吸了太多水，身子太沉重了。" : "你吸的水有點多，身子偏沉。";
                default:
                    if (atLeast) return far ? "你遠遠不夠強韌。" : "你還不夠強韌。";
                    return far ? "你硬得像石頭，彎不下去。" : "你有點太硬了，彎不下去。";
            }
        }

        /// <summary>「速度 ↑↑　溼度 ↓」：用箭頭數量表示幅度，不顯示數字。</summary>
        public static string ChangeSummary(StatBlock applied)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < StatBlock.StatCount; i++)
            {
                var stat = (StatType)i;
                int amount = applied[stat];
                if (amount == 0) continue;

                if (sb.Length > 0) sb.Append("　　");
                sb.Append("<color=").Append(StatColor(stat)).Append('>');
                sb.Append(StatName(stat)).Append(' ');
                sb.Append(amount > 0 ? '↑' : '↓', System.Math.Min(3, System.Math.Abs(amount)));
                sb.Append("</color>");
            }
            return sb.Length > 0 ? sb.ToString() : "什麼也沒有改變。";
        }

        public static string RequirementDebug(Requirement requirement, int threshold) =>
            $"{StatName(requirement.stat)} {(requirement.comparison == Comparison.AtLeast ? "≥" : "≤")} {threshold}（{TierName(requirement.tier)}）";
    }
}
