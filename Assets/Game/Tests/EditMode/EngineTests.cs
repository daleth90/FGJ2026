using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GrassRun.Tests
{
    /// <summary>
    /// 一局的流程：事件節奏、檢驗點、區域輪替，以及「沒有選項可選」的兩種結局。
    /// </summary>
    public class EngineTests
    {
        readonly List<Object> created = new List<Object>();
        GameBalance balance;

        [SetUp]
        public void SetUp()
        {
            balance = ScriptableObject.CreateInstance<GameBalance>();
            created.Add(balance);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        EventDefinition MakeEvent(string name, EventKind kind, ZoneMask zones, params EventOption[] options)
        {
            var e = ScriptableObject.CreateInstance<EventDefinition>();
            e.name = name;
            e.kind = kind;
            e.zones = zones;
            e.title = name;
            e.description = name;
            e.options = options;
            created.Add(e);
            return e;
        }

        static EventOption Free(params StatChange[] changes) =>
            new EventOption { title = "free", description = "d", resultText = "r", changes = changes };

        static EventOption Gated(StatType stat, Comparison comparison, Tier tier, params StatChange[] changes) =>
            new EventOption
            {
                title = "gated", description = "d", resultText = "r", changes = changes,
                requirements = new[] { new Requirement(stat, comparison, tier) },
            };

        static EventOption Impossible() => Gated(StatType.Speed, Comparison.AtMost, Tier.Low, new StatChange(StatType.Speed, -999));

        RunEngine NewEngine(params EventDefinition[] library) => new RunEngine(balance, library, new System.Random(7));

        [Test]
        public void EverySixthEvent_IsACheckpoint()
        {
            var random = MakeEvent("random", EventKind.Random, ZoneMask.All, Free(), Free(), Free());
            var checkpoint = MakeEvent("checkpoint", EventKind.Checkpoint, ZoneMask.All,
                Gated(StatType.Speed, Comparison.AtLeast, Tier.Low), Free(), Free());
            var engine = NewEngine(random, checkpoint);

            var kinds = new List<EventKind>();
            for (int i = 0; i < 12; i++)
            {
                Assert.IsTrue(engine.BeginNextEvent());
                kinds.Add(engine.CurrentEvent.kind);
                engine.Choose(1);
            }

            for (int i = 0; i < kinds.Count; i++)
                Assert.AreEqual(i % 6 == 5 ? EventKind.Checkpoint : EventKind.Random, kinds[i], $"第 {i + 1} 個事件");
            Assert.AreEqual(2, engine.Cycle);
        }

        [Test]
        public void PassingACheckpoint_EntersTheForecastZone_AndRaisesCapacity()
        {
            var random = MakeEvent("random", EventKind.Random, ZoneMask.All, Free(), Free(), Free());
            var checkpoint = MakeEvent("checkpoint", EventKind.Checkpoint, ZoneMask.All, Free(), Free(), Free());
            var engine = NewEngine(random, checkpoint);

            Assert.AreEqual(ZoneType.Meadow, engine.Zone);
            var forecast = engine.NextZone;
            int capacityBefore = engine.Capacity;

            ChoiceResult last = default;
            for (int i = 0; i < 6; i++)
            {
                engine.BeginNextEvent();
                last = engine.Choose(0);
            }

            Assert.IsTrue(last.enteredNewZone);
            Assert.AreEqual(forecast, engine.Zone);
            Assert.AreNotEqual(ZoneType.Meadow, engine.Zone);
            Assert.Greater(engine.Capacity, capacityBefore);
            Assert.AreEqual(0, engine.EventIndexInCycle);
        }

        [Test]
        public void CheckpointGrowth_CountsAgainstTheNextZonesCapacity()
        {
            balance.eventsPerCycle = 2;
            int room = balance.Capacity(0) - balance.startStats.Total;
            var fill = MakeEvent("fill", EventKind.Random, ZoneMask.All,
                Free(new StatChange(StatType.Speed, room)), Free(), Free());
            var checkpoint = MakeEvent("checkpoint", EventKind.Checkpoint, ZoneMask.All,
                Free(new StatChange(StatType.Toughness, 2)), Free(), Free());
            var engine = NewEngine(fill, checkpoint);

            engine.BeginNextEvent();
            engine.Choose(0);
            Assert.AreEqual(engine.Capacity, engine.Stats.Total, "先把這一區的容量填滿");

            engine.BeginNextEvent();
            var result = engine.Choose(0);

            Assert.AreEqual(0, result.overflow);
            Assert.AreEqual(balance.startStats.toughness + 2, engine.Stats.toughness);
        }

        [Test]
        public void NoSelectableOption_OnARandomEvent_IsWithered()
        {
            var random = MakeEvent("random", EventKind.Random, ZoneMask.All, Impossible(), Impossible(), Impossible());
            var engine = NewEngine(random);

            Assert.IsTrue(engine.BeginNextEvent());

            Assert.IsTrue(engine.IsEnded);
            Assert.AreEqual(RunEndReason.Withered, engine.EndReason);
            Assert.Throws<System.InvalidOperationException>(() => engine.Choose(0));
        }

        [Test]
        public void NoSelectableOption_OnACheckpoint_IsDeath()
        {
            balance.eventsPerCycle = 2;
            var random = MakeEvent("random", EventKind.Random, ZoneMask.All, Free(), Free(), Free());
            var checkpoint = MakeEvent("checkpoint", EventKind.Checkpoint, ZoneMask.All,
                Gated(StatType.Speed, Comparison.AtLeast, Tier.High),
                Gated(StatType.Moisture, Comparison.AtLeast, Tier.High),
                Gated(StatType.Toughness, Comparison.AtLeast, Tier.High));
            var engine = NewEngine(random, checkpoint);

            engine.BeginNextEvent();
            engine.Choose(0);
            engine.BeginNextEvent();

            Assert.AreEqual(EventKind.Checkpoint, engine.CurrentEvent.kind);
            Assert.AreEqual(RunEndReason.Death, engine.EndReason);
        }

        [Test]
        public void SelectableOptions_NeverEndTheRun()
        {
            // 風險只以代價呈現：就算選了代價最重的選項，只要付得起就不會結束。
            var random = MakeEvent("random", EventKind.Random, ZoneMask.All,
                Free(new StatChange(StatType.Toughness, -4)), Impossible(), Impossible());
            var engine = NewEngine(random);

            engine.BeginNextEvent();
            Assert.IsFalse(engine.IsEnded);
            engine.Choose(0);

            Assert.IsFalse(engine.IsEnded);
            Assert.AreEqual(0, engine.Stats.toughness);
        }

        [Test]
        public void LockedOption_CannotBeChosen()
        {
            var random = MakeEvent("random", EventKind.Random, ZoneMask.All, Impossible(), Free(), Free());
            var engine = NewEngine(random);

            engine.BeginNextEvent();

            Assert.IsFalse(engine.CurrentChecks[0].available);
            Assert.Throws<System.InvalidOperationException>(() => engine.Choose(0));
        }

        [Test]
        public void Deck_OnlyDrawsEventsOfTheCurrentZone()
        {
            var meadow = MakeEvent("meadow", EventKind.Random, ZoneMask.Meadow, Free(), Free(), Free());
            var dry = MakeEvent("dry", EventKind.Random, ZoneMask.Dry, Free(), Free(), Free());
            var deck = new EventDeck(new[] { meadow, dry }, new System.Random(3));

            for (int i = 0; i < 20; i++)
            {
                Assert.AreSame(meadow, deck.Draw(EventKind.Random, ZoneType.Meadow));
                Assert.AreSame(dry, deck.Draw(EventKind.Random, ZoneType.Dry));
            }
        }

        [Test]
        public void Deck_DoesNotRepeatWithinACycle_WhenThereAreEnoughEvents()
        {
            var library = new List<EventDefinition>();
            for (int i = 0; i < 6; i++)
                library.Add(MakeEvent("e" + i, EventKind.Random, ZoneMask.All, Free(), Free(), Free()));
            var deck = new EventDeck(library, new System.Random(11));

            for (int round = 0; round < 50; round++)
            {
                var seen = new HashSet<EventDefinition>();
                for (int i = 0; i < 5; i++)
                    Assert.IsTrue(seen.Add(deck.Draw(EventKind.Random, ZoneType.Meadow)), "同一個週期抽到重複的事件");
            }
        }

        [Test]
        public void ZoneRotation_StartsInMeadow_ThenNeverRepeatsMoreThanTwice()
        {
            balance.eventsPerCycle = 2;
            var random = MakeEvent("random", EventKind.Random, ZoneMask.All, Free(), Free(), Free());
            var checkpoint = MakeEvent("checkpoint", EventKind.Checkpoint, ZoneMask.All, Free(), Free(), Free());

            for (int seed = 1; seed <= 20; seed++)
            {
                var engine = new RunEngine(balance, new[] { random, checkpoint }, new System.Random(seed));
                Assert.AreEqual(ZoneType.Meadow, engine.Zone);

                var zones = new List<ZoneType>();
                for (int i = 0; i < 80; i++)
                {
                    engine.BeginNextEvent();
                    if (engine.Choose(0).enteredNewZone) zones.Add(engine.Zone);
                }

                Assert.IsFalse(zones.Contains(ZoneType.Meadow), "草原只當開場");
                for (int i = 2; i < zones.Count; i++)
                    Assert.IsFalse(zones[i] == zones[i - 1] && zones[i] == zones[i - 2], "同一種區域連續三次");
            }
        }
    }
}
