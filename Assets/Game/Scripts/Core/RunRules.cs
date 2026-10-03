using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GrassRun
{
    public enum LockKind
    {
        None = 0,
        Requirement = 1,
        InvalidData = 2,
    }

    public struct OptionCheck
    {
        public bool available;
        public LockKind lockKind;
        public Requirement requirement;
        public int actual;
        public string error;
    }

    /// <summary>
    /// 解析企劃表的需求／變動字串，並負責選項判定與四屬性結算。
    /// </summary>
    public static class RunRules
    {
        static readonly string[] Codes = { "mor", "hmd", "spd", "tgh" };

        public static bool Meets(Comparison comparison, int value, int threshold) =>
            comparison == Comparison.AtLeast ? value >= threshold : value <= threshold;

        public static OptionCheck Check(EventOption option, StatBlock stats)
        {
            var requirements = new List<Requirement>();
            if (!TryParseRequirements(option.requirement, requirements, out string error))
                return new OptionCheck { lockKind = LockKind.InvalidData, error = error };

            foreach (var requirement in requirements)
            {
                int actual = stats[requirement.stat];
                if (Meets(requirement.comparison, actual, requirement.threshold)) continue;
                return new OptionCheck
                {
                    lockKind = LockKind.Requirement,
                    requirement = requirement,
                    actual = actual,
                };
            }

            return new OptionCheck { available = true };
        }

        public static StatBlock NetChange(EventOption option)
        {
            var changes = new List<StatChange>();
            if (!TryParseOffsets(option.offset, changes, out _)) return default;

            var result = new StatBlock();
            foreach (var change in changes) result[change.stat] += change.amount;
            return result;
        }

        /// <summary>
        /// 套用選項變動。善惡可為負數；溼度限制在 0～上限；速度與韌度最低為 0。
        /// 回傳經過邊界裁切後真正套用的變動。
        /// </summary>
        public static StatBlock Apply(EventOption option, ref StatBlock stats, int maxMoisture)
        {
            var changes = new List<StatChange>();
            if (!TryParseOffsets(option.offset, changes, out string error))
                throw new InvalidOperationException($"選項變動格式錯誤：{error}");

            var before = stats;
            foreach (var change in changes) stats[change.stat] += change.amount;

            stats.moisture = Mathf.Clamp(stats.moisture, 0, Mathf.Max(1, maxMoisture));
            stats.speed = Mathf.Max(0, stats.speed);
            stats.toughness = Mathf.Max(0, stats.toughness);

            return new StatBlock(
                stats.morality - before.morality,
                stats.moisture - before.moisture,
                stats.speed - before.speed,
                stats.toughness - before.toughness);
        }

        public static StatBlock Preview(EventOption option, StatBlock stats, int maxMoisture)
        {
            Apply(option, ref stats, maxMoisture);
            return stats;
        }

        public static bool TryParseRequirements(string expression, List<Requirement> output, out string error)
        {
            output.Clear();
            error = null;
            if (string.IsNullOrEmpty(expression)) return true;
            if (ContainsWhitespace(expression)) return Fail("不可包含空白字元。", out error);

            int previousOrder = -1;
            foreach (string token in expression.Split(';'))
            {
                if (token.Length < 6) return Fail($"「{token}」不是完整需求。", out error);

                int operatorIndex = token.IndexOf(">=", StringComparison.Ordinal);
                Comparison comparison = Comparison.AtLeast;
                if (operatorIndex < 0)
                {
                    operatorIndex = token.IndexOf("<=", StringComparison.Ordinal);
                    comparison = Comparison.AtMost;
                }
                if (operatorIndex != 3 || token.IndexOf('=', operatorIndex + 2) >= 0)
                    return Fail($"「{token}」只能使用 >= 或 <=。", out error);

                if (!TryParseStat(token.Substring(0, 3), out StatType stat, out int order))
                    return Fail($"「{token}」的屬性代碼無效。", out error);
                if (order < previousOrder)
                    return Fail("需求必須依 mor、hmd、spd、tgh 排列。", out error);
                previousOrder = order;

                string thresholdText = token.Substring(5);
                if (!int.TryParse(thresholdText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int threshold))
                    return Fail($"「{token}」的門檻必須是整數。", out error);
                if (stat != StatType.Morality && threshold < 0)
                    return Fail($"只有 mor 可以使用負數門檻：{token}。", out error);

                output.Add(new Requirement(stat, comparison, threshold));
            }
            return true;
        }

        public static bool TryParseOffsets(string expression, List<StatChange> output, out string error)
        {
            output.Clear();
            error = null;
            if (string.IsNullOrEmpty(expression)) return true;
            if (ContainsWhitespace(expression)) return Fail("不可包含空白字元。", out error);

            int previousOrder = -1;
            foreach (string token in expression.Split(';'))
            {
                if (token.Length < 6 || token[3] != ':' || (token[4] != '+' && token[4] != '-'))
                    return Fail($"「{token}」必須使用 屬性:+/-整數。", out error);
                if (!TryParseStat(token.Substring(0, 3), out StatType stat, out int order))
                    return Fail($"「{token}」的屬性代碼無效。", out error);
                if (order <= previousOrder)
                    return Fail("變動必須依 mor、hmd、spd、tgh 排列，且同一屬性不能重複。", out error);
                previousOrder = order;

                if (!int.TryParse(token.Substring(4), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int amount))
                    return Fail($"「{token}」的變動必須是有正負號的整數。", out error);
                output.Add(new StatChange(stat, amount));
            }
            return true;
        }

        static bool TryParseStat(string code, out StatType stat, out int order)
        {
            for (int i = 0; i < Codes.Length; i++)
            {
                if (code != Codes[i]) continue;
                stat = (StatType)i;
                order = i;
                return true;
            }
            stat = default;
            order = -1;
            return false;
        }

        static bool ContainsWhitespace(string value)
        {
            foreach (char c in value)
                if (char.IsWhiteSpace(c)) return true;
            return false;
        }

        static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
