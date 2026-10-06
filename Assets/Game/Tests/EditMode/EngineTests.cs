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

        [Test]
        public void DebugSetStats_ClampsToStatRanges()
        {
            balance.maxMoisture = 100;
            var engine = new RunEngine(balance, new EventDefinition[0], new System.Random(1));

            engine.DebugSetStats(new StatBlock(-50, 150, -3, 12));

            Assert.AreEqual(-50, engine.Stats.morality);
            Assert.AreEqual(100, engine.Stats.moisture);
            Assert.AreEqual(0, engine.Stats.speed);
            Assert.AreEqual(12, engine.Stats.toughness);

            engine.DebugSetStats(new StatBlock(30, -5, 8, 0));
            Assert.AreEqual(new StatBlock(30, 0, 8, 0).ToString(), engine.Stats.ToString());
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

        EventDefinition FollowUp(int id, int unlock, int prerequisiteEventId, string prerequisiteChoice,
            params EventOption[] options)
        {
            var e = MakeEvent(id, unlock, options);
            e.prerequisiteEventId = prerequisiteEventId;
            e.prerequisiteChoice = prerequisiteChoice;
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
        public void ChoiceResult_WithEndingIdEndsAfterApplyingStats()
        {
            var ending = Free("mor:+10;hmd:-50");
            ending.endingTitleId = 2008;
            var e = MakeEvent(1001, 0, ending, Free(), Free());
            var next = FollowUp(2001, 3, 1001, "a", Free(), Free(), Free());
            var engine = NewEngine(e, next);

            engine.BeginNextEvent();
            var result = engine.Choose(0);

            Assert.AreEqual(2008, result.endingTitleId);
            Assert.AreEqual(2008, engine.EndingTitleId);
            Assert.AreEqual(10, engine.Stats.morality, "特殊結局仍要先完成數值結算");
            Assert.AreEqual(0, engine.Stats.moisture);
            Assert.IsTrue(engine.IsEnded);
            Assert.AreEqual(RunEndReason.SpecialEnding, engine.EndReason,
                "特殊結局應優先於同次結算觸發的濕度邊界");
            Assert.IsFalse(engine.BeginNextEvent(), "本局已結束時不能再觸發後續事件");
            Assert.IsNull(engine.CurrentEvent);
        }

        [TestCase("hmd:-50", RunEndReason.MoistureDepleted, 0)]
        [TestCase("hmd:+50", RunEndReason.MoistureSaturated, 100)]
        public void MoistureBoundary_AfterChoiceEndsTheRun(string offset, RunEndReason reason, int expectedMoisture)
        {
            var e = MakeEvent(1001, 0, Free(offset), Free(), Free());
            var next = FollowUp(2001, 3, 1001, "a", Free(), Free(), Free());
            var engine = NewEngine(e, next);

            engine.BeginNextEvent();
            engine.Choose(0);

            Assert.IsTrue(engine.IsEnded);
            Assert.AreEqual(reason, engine.EndReason);
            Assert.AreEqual(expectedMoisture, engine.Stats.moisture);
            Assert.IsFalse(engine.BeginNextEvent(), "濕度已結束本局時不能再觸發後續事件");
            Assert.IsNull(engine.CurrentEvent);
        }

        [Test]
        public void MoistureInsideBounds_DoesNotEndTheRun()
        {
            var e = MakeEvent(1001, 0, Free("hmd:-10"), Free(), Free());
            var engine = NewEngine(e);

            engine.BeginNextEvent();
            engine.Choose(0);

            Assert.IsFalse(engine.IsEnded);
            Assert.AreEqual(40, engine.Stats.moisture);
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

        [TestCase("a", 0)]
        [TestCase("b", 1)]
        [TestCase("c", 2)]
        [TestCase("A", 0)]
        [TestCase("B", 1)]
        [TestCase("C", 2)]
        public void MatchingChoice_GuaranteesTheNextEventAcrossUnlockBatches(string choice, int optionIndex)
        {
            var source = MakeEvent(1001, 0, Free(), Free(), Free());
            var next = FollowUp(3001, 6, 1001, choice, Free(), Free(), Free());
            var engine = NewEngine(source, next);

            Assert.IsTrue(engine.BeginNextEvent());
            Assert.AreSame(source, engine.CurrentEvent);
            engine.Choose(optionIndex);
            Assert.AreEqual(1, engine.EventsResolved);

            Assert.IsTrue(engine.BeginNextEvent());
            Assert.AreSame(next, engine.CurrentEvent, "符合前置選擇時不受原本批次門檻限制");
        }

        [TestCase(1002, 0)]
        [TestCase(1001, 1)]
        public void DifferentEventOrChoice_DoesNotTriggerContinuation(int previousEventId, int previousOptionIndex)
        {
            var ordinary = MakeEvent(1001, 0, Free(), Free(), Free());
            var next = FollowUp(1003, 0, 1001, "a", Free(), Free(), Free());
            var deck = new EventDeck(new[] { ordinary, next }, new System.Random(7));

            Assert.AreSame(ordinary, deck.Draw(0, previousEventId, previousOptionIndex));
        }

        [Test]
        public void PrerequisiteEvents_AreExcludedFromRandomPoolAndDoNotHideEarlierOrdinaryBatch()
        {
            var ordinary = MakeEvent(1001, 0, Free(), Free(), Free());
            var sameBatch = FollowUp(1002, 0, 1001, "a", Free(), Free(), Free());
            var laterBatch = FollowUp(3001, 6, 1001, "b", Free(), Free(), Free());
            var deck = new EventDeck(new[] { ordinary, sameBatch, laterBatch }, new System.Random(7));

            foreach (int resolved in new[] { 0, 3, 6, 20 })
                Assert.AreSame(ordinary, deck.Draw(resolved), "前置事件不能自行出現或遮住一般抽選池");
        }

        [Test]
        public void ForcedContinuation_OverridesRecentEventExclusion()
        {
            var ordinary = MakeEvent(1001, 0, Free(), Free(), Free());
            var next = FollowUp(1002, 0, 1001, "a", Free(), Free(), Free());
            var deck = new EventDeck(new[] { ordinary, next }, new System.Random(7));

            Assert.AreSame(next, deck.Draw(0, 1001, 0));
            Assert.AreSame(next, deck.Draw(0, 1001, 0), "再次符合前置時，即使剛出現過也必須接續");
        }

        [Test]
        public void Continuation_CanChainThenReturnsToTheCurrentOrdinaryBatch()
        {
            var source = MakeEvent(1001, 0, Free(), Free(), Free());
            var second = FollowUp(3001, 6, 1001, "a", Free(), Free(), Free());
            var third = FollowUp(3002, 6, 3001, "b", Free(), Free(), Free());
            var stage2 = MakeEvent(2001, 3, Free(), Free(), Free());
            var engine = NewEngine(source, second, third, stage2);

            engine.BeginNextEvent();
            engine.Choose(0);
            engine.BeginNextEvent();
            Assert.AreSame(second, engine.CurrentEvent);
            engine.Choose(1);
            engine.BeginNextEvent();
            Assert.AreSame(third, engine.CurrentEvent);
            engine.Choose(2);

            Assert.AreEqual(3, engine.EventsResolved, "接續事件也應計入已經歷事件數");
            engine.BeginNextEvent();
            Assert.AreSame(stage2, engine.CurrentEvent, "連鎖結束後回到目前正常批次");
        }

        [Test]
        public void ForcedContinuation_IsConsumedByTheNextDrawOnly()
        {
            var source = MakeEvent(1001, 0, Free(), Free(), Free());
            var next = FollowUp(1002, 0, 1001, "a", Free(), Free(), Free());
            var engine = NewEngine(source, next);

            engine.BeginNextEvent();
            engine.Choose(0);
            engine.BeginNextEvent();
            Assert.AreSame(next, engine.CurrentEvent);

            engine.BeginNextEvent();
            Assert.AreSame(source, engine.CurrentEvent, "未再完成符合選擇時，不能重用上一次觸發");
        }

        [Test]
        public void NewRun_DoesNotInheritThePreviousRunsPendingContinuation()
        {
            var source = MakeEvent(1001, 0, Free(), Free(), Free());
            var next = FollowUp(1002, 0, 1001, "a", Free(), Free(), Free());
            var previousRun = NewEngine(source, next);
            previousRun.BeginNextEvent();
            previousRun.Choose(0);

            var newRun = NewEngine(source, next);
            Assert.IsTrue(newRun.BeginNextEvent());
            Assert.AreSame(source, newRun.CurrentEvent);
            Assert.AreEqual(0, newRun.EventsResolved);
            Assert.IsTrue(previousRun.BeginNextEvent());
            Assert.AreSame(next, previousRun.CurrentEvent);
        }

        [Test]
        public void Continuation_StillChecksRequirementsAgainstSettledStats()
        {
            var source = MakeEvent(1001, 0, Free("spd:+10"), Free(), Free());
            var next = FollowUp(3001, 6, 1001, "a",
                Gated("spd>=10"), Gated("tgh>=10"), Free());
            var engine = NewEngine(source, next);

            engine.BeginNextEvent();
            engine.Choose(0);
            engine.BeginNextEvent();

            Assert.AreSame(next, engine.CurrentEvent);
            Assert.IsTrue(engine.CurrentChecks[0].available);
            Assert.IsFalse(engine.CurrentChecks[1].available);
            Assert.IsTrue(engine.CurrentChecks[2].available);
            Assert.Throws<System.InvalidOperationException>(() => engine.Choose(1));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(3)]
        public void RejectedChoice_DoesNotQueueAContinuation(int optionIndex)
        {
            var source = MakeEvent(1001, 0, Gated("spd>=10"), Free(), Free());
            var next = FollowUp(1002, 0, 1001, "a", Free(), Free(), Free());
            var engine = NewEngine(source, next);

            engine.BeginNextEvent();
            Assert.Throws<System.InvalidOperationException>(() => engine.Choose(optionIndex));
            Assert.AreEqual(0, engine.EventsResolved);
            Assert.AreSame(source, engine.CurrentEvent);

            engine.BeginNextEvent();
            Assert.AreSame(source, engine.CurrentEvent, "未完成有效選擇時，不應觸發前置事件");
        }

        [Test]
        public void MultipleMatchingContinuations_AreRejectedInsteadOfDependingOnLibraryOrder()
        {
            var source = MakeEvent(1001, 0, Free(), Free(), Free());
            var first = FollowUp(1002, 0, 1001, "a", Free(), Free(), Free());
            var second = FollowUp(1003, 0, 1001, "A", Free(), Free(), Free());
            var deck = new EventDeck(new[] { source, first, second }, new System.Random(7));

            Assert.Throws<System.InvalidOperationException>(() => deck.Draw(0, 1001, 0));
        }
    }
}
