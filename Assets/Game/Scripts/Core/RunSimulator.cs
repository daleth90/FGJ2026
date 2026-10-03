using System;
using System.Collections.Generic;

namespace GrassRun
{
    public struct SimulationResult
    {
        public int eventsResolved;
        public RunEndReason endReason;
        public StatBlock finalStats;
        public bool hitEventLimit;
    }

    /// <summary>不經畫面驅動事件，供測試與之後的平衡工具使用。</summary>
    public static class RunSimulator
    {
        public delegate int Policy(RunEngine engine);

        public static SimulationResult Run(GameBalance balance, IReadOnlyList<EventDefinition> library,
            Policy policy, int seed, int maxEvents = 100)
        {
            var engine = new RunEngine(balance, library, new Random(seed));
            while (engine.EventsResolved < maxEvents && !engine.IsEnded)
            {
                if (!engine.BeginNextEvent()) break;
                if (engine.IsEnded) break;
                engine.Choose(policy(engine));
            }

            return new SimulationResult
            {
                eventsResolved = engine.EventsResolved,
                endReason = engine.EndReason,
                finalStats = engine.Stats,
                hitEventLimit = !engine.IsEnded && engine.EventsResolved >= maxEvents,
            };
        }

        public static Policy RandomPolicy(int seed)
        {
            var rng = new Random(seed);
            var available = new List<int>();
            return engine =>
            {
                available.Clear();
                for (int i = 0; i < engine.CurrentChecks.Length; i++)
                    if (engine.CurrentChecks[i].available) available.Add(i);
                return available[rng.Next(available.Count)];
            };
        }

        public static Policy FavorPolicy(StatType favored) => engine =>
            PickBest(engine, after => after[favored] * 1000f + after.moisture + after.speed + after.toughness);

        public static Policy BalancedPolicy() => engine => PickBest(engine, after =>
            Math.Min(after.moisture, Math.Min(after.speed, after.toughness)) * 1000f +
            after.moisture + after.speed + after.toughness);

        static int PickBest(RunEngine engine, Func<StatBlock, float> score)
        {
            int best = -1;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < engine.CurrentChecks.Length; i++)
            {
                if (!engine.CurrentChecks[i].available) continue;
                var after = RunRules.Preview(engine.CurrentEvent.options[i], engine.Stats,
                    engine.Balance.maxMoisture);
                float current = score(after);
                if (current <= bestScore) continue;
                bestScore = current;
                best = i;
            }
            return best;
        }
    }
}
