using System;

namespace GrassRun
{
    public enum StatType
    {
        Speed = 0,
        Moisture = 1,
        Toughness = 2,
    }

    /// <summary>門檻階級。事件只標階級，實際數字由 <see cref="GameBalance"/> 依週期換算。</summary>
    public enum Tier
    {
        Low = 0,
        Mid = 1,
        High = 2,
    }

    public enum Comparison
    {
        /// <summary>數值要夠高。</summary>
        AtLeast = 0,
        /// <summary>數值要夠低（例如乾區的「溼度不能太高」）。</summary>
        AtMost = 1,
    }

    public enum ZoneType
    {
        Meadow = 0,
        Dry = 1,
        Wet = 2,
    }

    [Flags]
    public enum ZoneMask
    {
        None = 0,
        Meadow = 1,
        Dry = 2,
        Wet = 4,
        All = Meadow | Dry | Wet,
    }

    public enum EventKind
    {
        Random = 0,
        Checkpoint = 1,
    }

    public enum RunEndReason
    {
        None = 0,
        /// <summary>檢驗點上沒有任何選項可選。</summary>
        Death = 1,
        /// <summary>隨機事件上沒有任何選項可選。</summary>
        Withered = 2,
    }

    public static class ZoneTypeExtensions
    {
        public static ZoneMask ToMask(this ZoneType zone)
        {
            switch (zone)
            {
                case ZoneType.Meadow: return ZoneMask.Meadow;
                case ZoneType.Dry: return ZoneMask.Dry;
                default: return ZoneMask.Wet;
            }
        }
    }
}
