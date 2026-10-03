using System;
using System.Collections.Generic;

namespace GrassRun
{
    public struct ChoiceResult
    {
        public EventDefinition source;
        public EventOption option;
        /// <summary>實際套用的增減（已扣掉溢出）。</summary>
        public StatBlock applied;
        /// <summary>因為容量已滿而沒留下來的成長量。</summary>
        public int overflow;
        public bool enteredNewZone;
        public ZoneType zone;
    }

    /// <summary>
    /// 一局的規則狀態，不依賴場景與時間，測試和模擬都直接用它。
    /// 流程：BeginNextEvent() 抽事件並判定；若還沒結束，Choose() 結算並推進。
    /// </summary>
    public sealed class RunEngine
    {
        readonly EventDeck deck;
        readonly Random rng;
        StatBlock stats;
        ZoneType lastPickedZone;
        int samePickCount;

        public RunEngine(GameBalance balance, IEnumerable<EventDefinition> library, Random rng)
        {
            Balance = balance;
            this.rng = rng ?? new Random();
            deck = new EventDeck(library, this.rng);
            stats = balance.startStats;
            Zone = ZoneType.Meadow;
            lastPickedZone = Zone;
            NextZone = PickNextZone();
        }

        public GameBalance Balance { get; }
        public IReadOnlyList<EventDefinition> Library => deck.Library;

        public StatBlock Stats => stats;
        public int Capacity => Balance.Capacity(Cycle);

        /// <summary>
        /// 結算目前事件時用的容量。通過檢驗點就進入下一區，所以檢驗點的成長算進下一區的容量。
        /// </summary>
        public int CapacityForCurrentEvent =>
            CurrentEvent != null && CurrentEvent.kind == EventKind.Checkpoint ? Balance.Capacity(Cycle + 1) : Capacity;

        /// <summary>已通過的檢驗點數，也就是目前在第幾個區域（0 起算）。</summary>
        public int Cycle { get; private set; }
        public int EventIndexInCycle { get; private set; }
        public int EventsResolved { get; private set; }
        public ZoneType Zone { get; private set; }
        public ZoneType NextZone { get; private set; }

        public bool NextEventIsCheckpoint => EventIndexInCycle >= Balance.eventsPerCycle - 1;
        /// <summary>到檢驗點還有幾個事件（含檢驗點本身）。</summary>
        public int EventsUntilCheckpoint => Math.Max(1, Balance.eventsPerCycle - EventIndexInCycle);

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
            var drawn = deck.Draw(kind, Zone);
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
            var applied = RunRules.Apply(option, ref stats, CapacityForCurrentEvent, out int overflow);

            CurrentEvent = null;
            CurrentChecks = null;
            EventsResolved++;
            bool enteredNewZone = Advance(source.kind);

            return new ChoiceResult
            {
                source = source,
                option = option,
                applied = applied,
                overflow = overflow,
                enteredNewZone = enteredNewZone,
                zone = Zone,
            };
        }

        bool Advance(EventKind resolvedKind)
        {
            if (resolvedKind != EventKind.Checkpoint)
            {
                EventIndexInCycle++;
                return false;
            }

            Cycle++;
            EventIndexInCycle = 0;
            Zone = NextZone;
            NextZone = PickNextZone();
            return true;
        }

        /// <summary>草原只當開場；之後在乾、溼之間輪替，同一種最多連續兩次。</summary>
        ZoneType PickNextZone()
        {
            ZoneType picked;
            if (samePickCount >= 2)
                picked = lastPickedZone == ZoneType.Dry ? ZoneType.Wet : ZoneType.Dry;
            else
                picked = rng.Next(2) == 0 ? ZoneType.Dry : ZoneType.Wet;

            samePickCount = picked == lastPickedZone ? samePickCount + 1 : 1;
            lastPickedZone = picked;
            return picked;
        }
    }
}
