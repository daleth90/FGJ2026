using System;
using System.Collections.Generic;

namespace GrassRun
{
    public struct ChoiceResult
    {
        public EventDefinition source;
        public EventOption option;
        public StatBlock applied;
        public int endingTitleId;
    }

    /// <summary>
    /// 一局的事件狀態。優先接續上次選擇指定的事件，否則抽目前階段的事件。
    /// 特殊結局或濕度抵達邊界時結束本局。
    /// </summary>
    public sealed class RunEngine
    {
        readonly EventDeck deck;
        StatBlock stats;
        int previousEventId;
        int previousOptionIndex = -1;

        public RunEngine(GameBalance balance, IEnumerable<EventDefinition> library, Random rng)
        {
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            deck = new EventDeck(library, rng);
            stats = balance.startStats;
        }

        public GameBalance Balance { get; }
        public IReadOnlyList<EventDefinition> Library => deck.Library;
        public StatBlock Stats => stats;
        public int EventsResolved { get; private set; }
        public EventDefinition CurrentEvent { get; private set; }
        public OptionCheck[] CurrentChecks { get; private set; }
        public RunEndReason EndReason { get; private set; }
        public int EndingTitleId { get; private set; }
        public bool IsEnded => EndReason != RunEndReason.None;

        public bool BeginNextEvent()
        {
            if (IsEnded) return false;

            var drawn = deck.Draw(EventsResolved, previousEventId, previousOptionIndex);
            previousEventId = 0;
            previousOptionIndex = -1;
            if (drawn == null || drawn.options == null || drawn.options.Length == 0)
            {
                CurrentEvent = null;
                CurrentChecks = null;
                return false;
            }

            CurrentEvent = drawn;
            CurrentChecks = new OptionCheck[drawn.options.Length];

            bool anyAvailable = false;
            for (int i = 0; i < drawn.options.Length; i++)
            {
                CurrentChecks[i] = RunRules.Check(drawn.options[i], stats);
                anyAvailable |= CurrentChecks[i].available;
            }

            if (!anyAvailable) EndReason = RunEndReason.NoAvailableOption;
            return true;
        }

        public ChoiceResult Choose(int optionIndex)
        {
            if (CurrentEvent == null) throw new InvalidOperationException("目前沒有進行中的事件。");
            if (IsEnded) throw new InvalidOperationException("這一局已經結束。");
            if (optionIndex < 0 || optionIndex >= CurrentChecks.Length || !CurrentChecks[optionIndex].available)
                throw new InvalidOperationException($"選項 {optionIndex} 不可選。");

            var source = CurrentEvent;
            var option = source.options[optionIndex];
            var applied = RunRules.Apply(option, ref stats, Balance.maxMoisture);

            if (option.endingTitleId != 0)
            {
                EndingTitleId = option.endingTitleId;
                EndReason = RunEndReason.SpecialEnding;
            }
            else if (stats.moisture <= 0) EndReason = RunEndReason.MoistureDepleted;
            else if (stats.moisture >= Balance.maxMoisture) EndReason = RunEndReason.MoistureSaturated;

            previousEventId = IsEnded ? 0 : source.eventId;
            previousOptionIndex = IsEnded ? -1 : optionIndex;
            CurrentEvent = null;
            CurrentChecks = null;
            EventsResolved++;

            return new ChoiceResult
            {
                source = source,
                option = option,
                applied = applied,
                endingTitleId = option.endingTitleId,
            };
        }
    }
}
