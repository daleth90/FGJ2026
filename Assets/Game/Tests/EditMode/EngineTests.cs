using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GrassRun.Tests
{
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
            foreach (var item in created) Object.DestroyImmediate(item);
            created.Clear();
        }

        EventDefinition MakeEvent(int id, int unlock, params EventOption[] options)
        {
            var e = ScriptableObject.CreateInstance<EventDefinition>();
            e.name = $"Event_{id}";
            e.eventId = id;
            e.unlockEventCount = unlock;
            e.description = "事件";
            e.options = options;
            created.Add(e);
            return e;
        }

        static EventOption Free(string offset = "") => new EventOption
        {
            description = "自由選項",
            offset = offset,
            resultText = "結果",
        };

        static EventOption Gated(string requirement, string offset = "") => new EventOption
        {
            description = "門檻選項",
            requirement = requirement,
            offset = offset,
            resultText = "結果",
        };

        RunEngine NewEngine(params EventDefinition[] library) =>
            new RunEngine(balance, library, new System.Random(7));

        [Test]
        public void DrawPool_SwitchesExclusivelyAtThreeAndSixResolvedEvents()
        {
            var stage1 = MakeEvent(1001, 0, Free(), Free(), Free());
            var stage2 = MakeEvent(2001, 3, Free(), Free(), Free());
            var stage3 = MakeEvent(3001, 6, Free(), Free(), Free());
            var engine = NewEngine(stage1, stage2, stage3);

            for (int i = 0; i < 9; i++)
            {
                Assert.IsTrue(engine.BeginNextEvent());
                int expected = i < 3 ? 1001 : i < 6 ? 2001 : 3001;
                Assert.AreEqual(expected, engine.CurrentEvent.eventId, $"已經歷 {i} 個事件");
                engine.Choose(0);
            }
        }

        [Test]
        public void OptionRequirements_AreCheckedAgainstCurrentFourStats()
        {
            var e = MakeEvent(1001, 0,
                Gated("spd>=10"), Gated("mor<=-5"), Free("spd:+10"));
            var engine = NewEngine(e);

            engine.BeginNextEvent();
            Assert.IsFalse(engine.CurrentChecks[0].available);
            Assert.IsFalse(engine.CurrentChecks[1].available);
            Assert.IsTrue(engine.CurrentChecks[2].available);

            engine.Choose(2);
            Assert.AreEqual(10, engine.Stats.speed);
        }

        [Test]
        public void LockedOption_CannotBeChosen()
        {
            var e = MakeEvent(1001, 0, Gated("spd>=10"), Free(), Free());
            var engine = NewEngine(e);

            engine.BeginNextEvent();
            Assert.Throws<System.InvalidOperationException>(() => engine.Choose(0));
        }

        [Test]
        public void ChoiceResult_CarriesEndingIdWithoutEndingTheRun()
        {
            var ending = Free();
            ending.endingTitleId = 2008;
            var e = MakeEvent(1001, 0, ending, Free(), Free());
            var engine = NewEngine(e);

            engine.BeginNextEvent();
            var result = engine.Choose(0);

            Assert.AreEqual(2008, result.endingTitleId);
            Assert.IsFalse(engine.IsEnded, "稱號／結局流程不在這次事件資料改版的範圍");
        }

        [Test]
        public void NoAvailableOption_EndsWithNoAvailableOptionReason()
        {
            var e = MakeEvent(1001, 0,
                Gated("spd>=10"), Gated("tgh>=10"), Gated("mor>=10"));
            var engine = NewEngine(e);

            Assert.IsTrue(engine.BeginNextEvent());
            Assert.IsTrue(engine.IsEnded);
            Assert.AreEqual(RunEndReason.NoAvailableOption, engine.EndReason);
        }

        [Test]
        public void Deck_AvoidsRecentEventsWhenTheBatchHasEnoughCandidates()
        {
            var library = new List<EventDefinition>();
            for (int i = 0; i < 6; i++)
                library.Add(MakeEvent(1001 + i, 0, Free(), Free(), Free()));
            var deck = new EventDeck(library, new System.Random(11));

            for (int round = 0; round < 20; round++)
            {
                var seen = new HashSet<EventDefinition>();
                for (int i = 0; i < 5; i++)
                    Assert.IsTrue(seen.Add(deck.Draw(0)), "最近四次不應重複事件");
            }
        }
    }
}
