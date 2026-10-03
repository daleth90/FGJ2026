using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GrassRun.Tests
{
    /// <summary>
    /// 一局的流程：事件節奏、檢驗點，以及「沒有選項可選」的兩種結局。
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

        EventDefinition MakeEvent(string name, EventKind kind, params EventOption[] options)
        {
            var e = ScriptableObject.CreateInstance<EventDefinition>();
            e.name = name;
            e.kind = kind;
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
            var random = MakeEvent("random", EventKind.Random, Free(), Free(), Free());
            var checkpoint = MakeEvent("checkpoint", EventKind.Checkpoint,
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
        public void PassingACheckpoint_RaisesTheThresholds()
        {
            var random = MakeEvent("random", EventKind.Random, Free(), Free(), Free());
            var checkpoint = MakeEvent("checkpoint", EventKind.Checkpoint, Free(), Free(), Free());
            var engine = NewEngine(random, checkpoint);

            engine.BeginNextEvent();
            int levelBefore = engine.CurrentLevel;
            engine.Choose(0);
            for (int i = 0; i < 5; i++)
            {
                engine.BeginNextEvent();
                engine.Choose(0);
            }
            engine.BeginNextEvent();

            Assert.AreEqual(1, engine.Cycle);
            Assert.AreEqual(EventKind.Random, engine.CurrentEvent.kind);
            Assert.AreEqual(levelBefore + 1, engine.CurrentLevel);
        }

        [Test]
        public void NoSelectableOption_OnARandomEvent_IsWithered()
        {
            var random = MakeEvent("random", EventKind.Random, Impossible(), Impossible(), Impossible());
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
            var random = MakeEvent("random", EventKind.Random, Free(), Free(), Free());
            var checkpoint = MakeEvent("checkpoint", EventKind.Checkpoint,
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
            var random = MakeEvent("random", EventKind.Random,
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
            var random = MakeEvent("random", EventKind.Random, Impossible(), Free(), Free());
            var engine = NewEngine(random);

            engine.BeginNextEvent();

            Assert.IsFalse(engine.CurrentChecks[0].available);
            Assert.Throws<System.InvalidOperationException>(() => engine.Choose(0));
        }

        [Test]
        public void NewEngine_StartsFromTheStartingStats()
        {
            // 重新開始就是建一個新的引擎：數值和進度都要回到起點。
            var random = MakeEvent("random", EventKind.Random, Free(new StatChange(StatType.Speed, 5)), Free(), Free());
            var first = NewEngine(random);
            first.BeginNextEvent();
            first.Choose(0);

            var second = NewEngine(random);

            Assert.AreEqual(balance.startStats.ToString(), second.Stats.ToString());
            Assert.AreEqual(0, second.EventsResolved);
            Assert.AreEqual(0, second.Cycle);
        }

        [Test]
        public void Deck_DoesNotRepeatWithinACycle_WhenThereAreEnoughEvents()
        {
            var library = new List<EventDefinition>();
            for (int i = 0; i < 6; i++)
                library.Add(MakeEvent("e" + i, EventKind.Random, Free(), Free(), Free()));
            var deck = new EventDeck(library, new System.Random(11));

            for (int round = 0; round < 50; round++)
            {
                var seen = new HashSet<EventDefinition>();
                for (int i = 0; i < 5; i++)
                    Assert.IsTrue(seen.Add(deck.Draw(EventKind.Random)), "同一個週期抽到重複的事件");
            }
        }
    }
}
