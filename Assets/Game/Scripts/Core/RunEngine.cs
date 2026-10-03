using System;
using System.Collections.Generic;

namespace GrassRun
{
    public struct ChoiceResult
    {
        public EventDefinition source;
        public EventOption option;
        /// <summary>實際套用的增減。</summary>
        public StatBlock applied;
    }

    /// <summary>
    /// 一局的規則狀態，不依賴場景與時間，測試和模擬都直接用它。
    /// 流程：BeginNextEvent() 抽事件並判定；若還沒結束，Choose() 結算並推進。
    /// </summary>
    public sealed class RunEngine
    {
        readonly EventDeck deck;
        StatBlock stats;

        public RunEngine(GameBalance balance, IEnumerable<EventDefinition> library, Random rng)
        {
            Balance = balance;
            deck = new EventDeck(library, rng ?? new Random());
            stats = balance.startStats;
        }

        public GameBalance Balance { get; }
        public IReadOnlyList<EventDefinition> Library => deck.Library;

        public StatBlock Stats => stats;

        /// <summary>已通過的檢驗點數。門檻跟著它提高。</summary>
        public int Cycle { get; private set; }
        public int EventIndexInCycle { get; private set; }
        public int EventsResolved { get; private set; }

        public bool NextEventIsCheckpoint => EventIndexInCycle >= Balance.eventsPerCycle - 1;

        public EventDefinition CurrentEvent { get; private set; }
        public int CurrentLevel { get; private set; }
        public OptionCheck[] CurrentChecks { get; private set; }

        public RunEndReason EndReason { get; private set; }
        public bool IsEnded => EndReason != RunEndReason.None;

        /// <summary>
        /// 抽出下一個事件並判定每個選項。沒有任何選項可選時，這一局就此結束。
        /// 事件池裡沒有合適的事件時回傳 false，並直接跳過這個時段。
        /// </summary>
        public bool BeginNextEvent()
        {
            if (IsEnded) return false;

            var kind = NextEventIsCheckpoint ? EventKind.Checkpoint : EventKind.Random;
            var drawn = deck.Draw(kind);
            if (drawn == null || drawn.options == null || drawn.options.Length == 0)
            {
                CurrentEvent = null;
                CurrentChecks = null;
                Advance(kind);
                return false;
            }

            CurrentEvent = drawn;
            CurrentLevel = RunRules.LevelFor(kind, Cycle);
            CurrentChecks = new OptionCheck[drawn.options.Length];

            bool anyAvailable = false;
            for (int i = 0; i < drawn.options.Length; i++)
            {
                CurrentChecks[i] = RunRules.Check(drawn.options[i], stats, Balance, CurrentLevel);
                anyAvailable |= CurrentChecks[i].available;
            }

            if (!anyAvailable)
                EndReason = kind == EventKind.Checkpoint ? RunEndReason.Death : RunEndReason.Withered;

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
            var applied = RunRules.Apply(option, ref stats);

            CurrentEvent = null;
            CurrentChecks = null;
            EventsResolved++;
            Advance(source.kind);

            return new ChoiceResult { source = source, option = option, applied = applied };
        }

        void Advance(EventKind resolvedKind)
        {
            if (resolvedKind == EventKind.Checkpoint)
            {
                Cycle++;
                EventIndexInCycle = 0;
            }
            else
            {
                EventIndexInCycle++;
            }
        }
    }
}
