using NUnit.Framework;
using UnityEngine;

namespace GrassRun.Tests
{
    /// <summary>
    /// 兩條地基規則：門檻／代價決定選項能不能選，總容量決定成長能不能留下。
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
            Assert.AreEqual(21, balance.Capacity(0));
            Assert.AreEqual(12, balance.startStats.Total);
        }

        [Test]
        public void CheckpointIsOneLevelAboveRandomEvents()
        {
            Assert.AreEqual(2, RunRules.LevelFor(EventKind.Random, 2));
            Assert.AreEqual(3, RunRules.LevelFor(EventKind.Checkpoint, 2));
        }

        [Test]
        public void FullCapacity_FitsExactlyTheThreeAgreedBuilds()
        {
            int low = balance.Threshold(Tier.Low, 1);
            int mid = balance.Threshold(Tier.Mid, 1);
            int high = balance.Threshold(Tier.High, 1);
            int capacity = balance.Capacity(0);

            Assert.LessOrEqual(high + mid + low, capacity, "一高一中一低");
            Assert.LessOrEqual(mid * 3, capacity, "三中");
            Assert.Less(capacity - high * 2, low, "兩高之後，第三項連低門檻都不到");
            Assert.Greater(high + mid * 2, capacity, "一高兩中放不下，所以沒有全能解");
        }

        [Test]
        public void ThresholdsOutgrowCapacity_SoEveryRunMustEnd()
        {
            // 門檻成長比容量快：到某一級之後，連單一項的低門檻都超過總容量。
            bool found = false;
            for (int cycle = 0; cycle < 500; cycle++)
            {
                if (balance.Threshold(Tier.Low, cycle + 1) <= balance.Capacity(cycle)) continue;
                found = true;
                break;
            }
            Assert.IsTrue(found);
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
        public void Apply_PaysCostsFirst_ThenClipsGainsAtCapacity()
        {
            // 已經滿了：扣 1 騰出 1 格，所以 +3 只留得下 1。
            var option = Option(NoRequirement, new StatChange(StatType.Speed, 3), new StatChange(StatType.Moisture, -1));
            var stats = new StatBlock(7, 7, 7);

            var applied = RunRules.Apply(option, ref stats, 21, out int overflow);

            Assert.AreEqual(new StatBlock(8, 6, 7).ToString(), stats.ToString());
            Assert.AreEqual(1, applied.speed);
            Assert.AreEqual(-1, applied.moisture);
            Assert.AreEqual(2, overflow);
            Assert.AreEqual(21, stats.Total);
        }

        [Test]
        public void Apply_FillsGainsInAuthoredOrder()
        {
            var option = Option(NoRequirement, new StatChange(StatType.Toughness, 2), new StatChange(StatType.Speed, 2));
            var stats = new StatBlock(6, 6, 6);

            RunRules.Apply(option, ref stats, 21, out int overflow);

            Assert.AreEqual("7/6/8", stats.ToString());
            Assert.AreEqual(1, overflow);
        }

        [Test]
        public void CapacityOff_KeepsEveryGain()
        {
            balance.useCapacity = false;
            var option = Option(NoRequirement, new StatChange(StatType.Speed, 3));
            var stats = new StatBlock(50, 50, 50);

            RunRules.Apply(option, ref stats, balance.Capacity(0), out int overflow);

            Assert.AreEqual(53, stats.speed);
            Assert.AreEqual(0, overflow);
        }

        [Test]
        public void Preview_DoesNotTouchTheOriginal()
        {
            var option = Option(NoRequirement, new StatChange(StatType.Speed, 2));
            var stats = new StatBlock(4, 4, 4);

            var after = RunRules.Preview(option, stats, 21);

            Assert.AreEqual(6, after.speed);
            Assert.AreEqual(4, stats.speed);
        }
    }
}
