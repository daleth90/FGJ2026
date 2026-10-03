using NUnit.Framework;
using UnityEngine;

namespace GrassRun.Tests
{
    /// <summary>
    /// 選項能不能選只看兩件事：門檻有沒有到、代價付不付得起。
    /// </summary>
    public class RulesTests
    {
        GameBalance balance;

        [SetUp]
        public void SetUp() => balance = ScriptableObject.CreateInstance<GameBalance>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(balance);

        static EventOption Option(Requirement[] requirements, params StatChange[] changes) =>
            new EventOption { title = "t", requirements = requirements, changes = changes };

        static Requirement[] Need(StatType stat, Comparison comparison, Tier tier) =>
            new[] { new Requirement(stat, comparison, tier) };

        static readonly Requirement[] NoRequirement = new Requirement[0];

        [Test]
        public void FirstCheckpoint_UsesTheAgreedNumbers()
        {
            Assert.AreEqual(4, balance.Threshold(Tier.Low, 1));
            Assert.AreEqual(7, balance.Threshold(Tier.Mid, 1));
            Assert.AreEqual(10, balance.Threshold(Tier.High, 1));
            Assert.AreEqual(12, balance.startStats.Total);
        }

        [Test]
        public void EventsComeEveryThreeSeconds_ByDefault()
        {
            Assert.AreEqual(3f, balance.eventInterval);
        }

        [Test]
        public void CheckpointIsOneLevelAboveRandomEvents()
        {
            Assert.AreEqual(2, RunRules.LevelFor(EventKind.Random, 2));
            Assert.AreEqual(3, RunRules.LevelFor(EventKind.Checkpoint, 2));
        }

        [Test]
        public void ThresholdsKeepRising()
        {
            for (int level = 0; level < 50; level++)
                Assert.Greater(balance.Threshold(Tier.Mid, level + 1), balance.Threshold(Tier.Mid, level));
        }

        [Test]
        public void AtLeast_LocksWhenBelowThreshold()
        {
            var option = Option(Need(StatType.Speed, Comparison.AtLeast, Tier.High));

            var locked = RunRules.Check(option, new StatBlock(9, 4, 4), balance, 1);
            Assert.IsFalse(locked.available);
            Assert.AreEqual(LockKind.Requirement, locked.lockKind);
            Assert.AreEqual(10, locked.threshold);
            Assert.AreEqual(1, locked.gap);

            Assert.IsTrue(RunRules.Check(option, new StatBlock(10, 4, 4), balance, 1).available);
        }

        [Test]
        public void AtMost_LocksWhenAboveThreshold()
        {
            var option = Option(Need(StatType.Moisture, Comparison.AtMost, Tier.Mid));

            Assert.IsTrue(RunRules.Check(option, new StatBlock(4, 7, 4), balance, 1).available);

            var locked = RunRules.Check(option, new StatBlock(4, 9, 4), balance, 1);
            Assert.IsFalse(locked.available);
            Assert.AreEqual(Comparison.AtMost, locked.requirement.comparison);
            Assert.AreEqual(2, locked.gap);
        }

        [Test]
        public void UnpayableCost_LocksTheOption()
        {
            var option = Option(NoRequirement, new StatChange(StatType.Moisture, -1), new StatChange(StatType.Toughness, 1));

            Assert.IsTrue(RunRules.Check(option, new StatBlock(4, 1, 4), balance, 0).available);

            var locked = RunRules.Check(option, new StatBlock(4, 0, 4), balance, 0);
            Assert.IsFalse(locked.available);
            Assert.AreEqual(LockKind.Cost, locked.lockKind);
            Assert.AreEqual(StatType.Moisture, locked.costStat);
        }

        [Test]
        public void Apply_AddsGainsAndSubtractsCosts_WithNoUpperLimit()
        {
            var option = Option(NoRequirement, new StatChange(StatType.Speed, 3), new StatChange(StatType.Moisture, -1));
            var stats = new StatBlock(50, 50, 50);

            var applied = RunRules.Apply(option, ref stats);

            Assert.AreEqual("53/49/50", stats.ToString());
            Assert.AreEqual(3, applied.speed);
            Assert.AreEqual(-1, applied.moisture);
        }

        [Test]
        public void Preview_DoesNotTouchTheOriginal()
        {
            var option = Option(NoRequirement, new StatChange(StatType.Speed, 2));
            var stats = new StatBlock(4, 4, 4);

            var after = RunRules.Preview(option, stats);

            Assert.AreEqual(6, after.speed);
            Assert.AreEqual(4, stats.speed);
        }
    }
}
