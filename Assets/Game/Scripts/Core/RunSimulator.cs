using System;
using System.Collections.Generic;

namespace GrassRun
{
    public struct SimulationResult
    {
        public int checkpointsPassed;
        public int eventsResolved;
        public RunEndReason endReason;
        public StatBlock finalStats;
        /// <summary>跑到事件上限還沒結束（代表這個策略在目前的平衡下停不下來）。</summary>
        public bool hitEventLimit;
    }

    /// <summary>
    /// 不經過畫面、直接用規則跑完一局，用來檢查平衡：這套規則會不會結束、哪種配點活最久。
    /// </summary>
    public static class RunSimulator
    {
        /// <summary>策略：看目前的事件，回傳要選的選項（必須是可選的）。</summary>
        public delegate int Policy(RunEngine engine);

        public static SimulationResult Run(GameBalance balance, IReadOnlyList<EventDefinition> library, Policy policy, int seed, int maxEvents = 3000)
        {
            var engine = new RunEngine(balance, library, new Random(seed));
            int steps = 0;
            while (steps < maxEvents)
            {
                steps++;
                if (!engine.BeginNextEvent()) continue;
                if (engine.IsEnded) break;
                engine.Choose(policy(engine));
            }

            return new SimulationResult
            {
                checkpointsPassed = engine.Cycle,
                eventsResolved = engine.EventsResolved,
                endReason = engine.EndReason,
                finalStats = engine.Stats,
                hitEventLimit = !engine.IsEnded,
            };
        }

        /// <summary>亂選：任選一個可選的選項。</summary>
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

        /// <summary>專精：永遠挑對指定數值最有利的選項。</summary>
        public static Policy FavorPolicy(StatType favored)
        {
            return engine => PickBest(engine, (after, before) =>
                (after[favored] - before[favored]) * 100f + (after.Total - before.Total));
        }

        /// <summary>平均：盡量把最低的數值拉高。</summary>
        public static Policy BalancedPolicy()
        {
            return engine => PickBest(engine, (after, before) =>
            {
                int min = Math.Min(after.speed, Math.Min(after.moisture, after.toughness));
                return min * 100f + after.Total;
            });
        }

        /// <summary>
        /// 規劃：看得懂預告的玩家。挑能讓「這一區檢驗點」過關選項最多、差距最小的選項。
        /// </summary>
        public static Policy PlannerPolicy()
        {
            return engine => PickBest(engine, (after, before) =>
            {
                int level = RunRules.LevelFor(EventKind.Checkpoint, engine.Cycle);
                int passable = 0;
                int closestGap = int.MaxValue;

                foreach (var e in engine.Library)
                {
                    if (e.kind != EventKind.Checkpoint || !e.AppearsIn(engine.Zone)) continue;
                    foreach (var option in e.options)
                    {
                        var check = RunRules.Check(option, after, engine.Balance, level);
                        if (check.available)
                        {
                            passable++;
                            closestGap = 0;
                        }
                        else if (check.lockKind == LockKind.Requirement)
                        {
                            closestGap = Math.Min(closestGap, check.gap);
                        }
                    }
                }

                if (closestGap == int.MaxValue) closestGap = 0;
                return passable * 1000f - closestGap * 10f + after.Total;
            });
        }

        static int PickBest(RunEngine engine, Func<StatBlock, StatBlock, float> score)
        {
            int best = -1;
            float bestScore = float.NegativeInfinity;
            var before = engine.Stats;

            for (int i = 0; i < engine.CurrentChecks.Length; i++)
            {
                if (!engine.CurrentChecks[i].available) continue;
                var after = RunRules.Preview(engine.CurrentEvent.options[i], before, engine.CapacityForCurrentEvent);
                float s = score(after, before);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = i;
                }
            }
            return best;
        }
    }
}
