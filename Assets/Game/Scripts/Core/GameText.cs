using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GrassRun
{
    public static class GameText
    {
        public const string MoralityColor = "#D9C26C";
        public const string MoistureColor = "#5DB4EC";
        public const string SpeedColor = "#F4C542";
        public const string ToughnessColor = "#E08A58";

        public static string StatName(StatType stat)
        {
            switch (stat)
            {
                case StatType.Morality: return "善良";
                case StatType.Moisture: return "溼度";
                case StatType.Speed: return "速度";
                default: return "韌度";
            }
        }

        public static string StatColor(StatType stat)
        {
            switch (stat)
            {
                case StatType.Morality: return MoralityColor;
                case StatType.Moisture: return MoistureColor;
                case StatType.Speed: return SpeedColor;
                default: return ToughnessColor;
            }
        }

        public static string LockReason(OptionCheck check)
        {
            if (check.lockKind == LockKind.InvalidData) return $"資料格式錯誤：{check.error}";
            if (check.lockKind != LockKind.Requirement) return string.Empty;

            string relation = check.requirement.comparison == Comparison.AtLeast ? "至少" : "至多";
            return $"需要{StatName(check.requirement.stat)}{relation} {check.requirement.threshold}（目前 {check.actual}）";
        }

        /// <summary>選項卡上的條件提示，例如「速度>=10」；有多個條件時全部列出。</summary>
        public static string RequirementTip(EventOption option, OptionCheck check)
        {
            if (check.lockKind == LockKind.InvalidData) return LockReason(check);

            var requirements = new List<Requirement>();
            if (!RunRules.TryParseRequirements(option.requirement, requirements, out _) || requirements.Count == 0)
                return LockReason(check);

            var sb = new StringBuilder();
            foreach (var requirement in requirements)
            {
                if (sb.Length > 0) sb.Append('　');
                sb.Append(StatName(requirement.stat))
                    .Append(requirement.comparison == Comparison.AtLeast ? ">=" : "<=")
                    .Append(requirement.threshold);
            }
            return sb.ToString();
        }

        public static string RequirementDebug(Requirement requirement) =>
            $"{StatName(requirement.stat)} {(requirement.comparison == Comparison.AtLeast ? "≥" : "≤")} {requirement.threshold}";

        public static string ChangeSummary(StatBlock applied)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < StatBlock.StatCount; i++)
            {
                var stat = (StatType)i;
                int amount = applied[stat];
                if (amount == 0) continue;
                if (sb.Length > 0) sb.Append("　");
                sb.Append("<color=").Append(StatColor(stat)).Append('>')
                    .Append(StatName(stat)).Append(amount > 0 ? " +" : " ").Append(amount)
                    .Append("</color>");
            }
            return sb.Length == 0 ? "數值沒有變化" : sb.ToString();
        }

        /// <summary>結算畫面的死因，例如「死因：你乾死了」。特殊結局顯示該結局選項的結果描述。</summary>
        public static string DeathCause(RunEndReason reason, string endingResultText)
        {
            string cause;
            switch (reason)
            {
                case RunEndReason.NoAvailableOption: cause = "無路可走"; break;
                case RunEndReason.MoistureDepleted: cause = "你乾死了"; break;
                case RunEndReason.MoistureSaturated: cause = "你淹死了"; break;
                case RunEndReason.SpecialEnding: cause = endingResultText ?? string.Empty; break;
                default: cause = string.Empty; break;
            }
            return $"死因：{cause}";
        }

        public static string EndTitle(RunEndReason reason) => "目前沒有可選的行動";

        public static string EndBody(RunEndReason reason, EventDefinition lastEvent)
        {
            string title = lastEvent != null ? lastEvent.DisplayTitle : "這個事件";
            return $"「{title}」的三個選項目前都不符合條件。";
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
