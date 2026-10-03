using System.Collections.Generic;
using NUnit.Framework;

namespace GrassRun.Tests
{
    public class RulesTests
    {
        static EventOption Option(string requirement = "", string offset = "") => new EventOption
        {
            description = "選項",
            requirement = requirement,
            offset = offset,
            resultText = "結果",
        };

        [Test]
        public void OffsetParser_ReadsFourStatsInTableOrder()
        {
            var changes = new List<StatChange>();
            Assert.IsTrue(RunRules.TryParseOffsets("mor:-15;hmd:-10;spd:+5;tgh:+10", changes, out string error), error);
            Assert.AreEqual(4, changes.Count);
            Assert.AreEqual(StatType.Morality, changes[0].stat);
            Assert.AreEqual(-15, changes[0].amount);
            Assert.AreEqual(StatType.Toughness, changes[3].stat);
            Assert.AreEqual(10, changes[3].amount);
        }

        [TestCase("spd:10", "屬性")]
        [TestCase("spd: +10", "空白")]
        [TestCase("hmd:-10;hmd:-10", "重複")]
        [TestCase("spd:+10;hmd:-20", "排列")]
        public void OffsetParser_RejectsInvalidGrammar(string expression, string expectedMessage)
        {
            var changes = new List<StatChange>();
            Assert.IsFalse(RunRules.TryParseOffsets(expression, changes, out string error));
            StringAssert.Contains(expectedMessage, error);
        }

        [Test]
        public void RequirementParser_SupportsRangesAndNegativeMorality()
        {
            var requirements = new List<Requirement>();
            Assert.IsTrue(RunRules.TryParseRequirements("mor>=-10;mor<=10;spd>=20", requirements, out string error), error);
            Assert.AreEqual(3, requirements.Count);
            Assert.AreEqual(Comparison.AtLeast, requirements[0].comparison);
            Assert.AreEqual(-10, requirements[0].threshold);
            Assert.AreEqual(Comparison.AtMost, requirements[1].comparison);
        }

        [Test]
        public void RequirementCheck_RequiresEveryClause()
        {
            var option = Option("mor>=-10;mor<=10;spd>=20");
            Assert.IsTrue(RunRules.Check(option, new StatBlock(0, 50, 20, 0)).available);

            var locked = RunRules.Check(option, new StatBlock(15, 50, 20, 0));
            Assert.IsFalse(locked.available);
            Assert.AreEqual(StatType.Morality, locked.requirement.stat);
            Assert.AreEqual(Comparison.AtMost, locked.requirement.comparison);
        }

        [Test]
        public void Apply_UpdatesFourStatsAndClampsTheirRanges()
        {
            var stats = new StatBlock(-5, 95, 2, 3);
            var applied = RunRules.Apply(Option(offset: "mor:-10;hmd:+20;spd:-10;tgh:+5"), ref stats, 100);

            Assert.AreEqual("-15/100/0/8", stats.ToString());
            Assert.AreEqual("-10/5/-2/5", applied.ToString());
        }

        [Test]
        public void Preview_DoesNotModifyOriginal()
        {
            var stats = new StatBlock(0, 50, 0, 0);
            var after = RunRules.Preview(Option(offset: "hmd:-20;spd:+10"), stats, 100);

            Assert.AreEqual(30, after.moisture);
            Assert.AreEqual(10, after.speed);
            Assert.AreEqual(50, stats.moisture);
            Assert.AreEqual(0, stats.speed);
        }

        [Test]
        public void CharacterAppearance_SwitchesWhenEachConditionIsReached()
        {
            var appearance = CharacterAppearance.Default;
            var stats = new StatBlock(0, 50, 0, 0);

            var next = new StatBlock(0, 50, 0, 8);
            appearance = RunRules.ResolveCharacterAppearance(appearance, stats, next);
            Assert.AreEqual(CharacterAppearance.Toughness, appearance);

            stats = next;
            next = new StatBlock(0, 50, 8, 8);
            appearance = RunRules.ResolveCharacterAppearance(appearance, stats, next);
            Assert.AreEqual(CharacterAppearance.Speed, appearance);

            stats = next;
            next = new StatBlock(0, 25, 8, 8);
            appearance = RunRules.ResolveCharacterAppearance(appearance, stats, next);
            Assert.AreEqual(CharacterAppearance.Dry, appearance);

            stats = new StatBlock(0, 60, 0, 0);
            next = new StatBlock(0, 61, 0, 0);
            appearance = RunRules.ResolveCharacterAppearance(appearance, stats, next);
            Assert.AreEqual(CharacterAppearance.Wet, appearance);
        }

        [Test]
        public void CharacterAppearance_KeepsLastTriggeredStateWithoutANewMatch()
        {
            var before = new StatBlock(0, 50, 0, 8);
            var after = new StatBlock(10, 55, 0, 9);

            var appearance = RunRules.ResolveCharacterAppearance(CharacterAppearance.Toughness, before, after);

            Assert.AreEqual(CharacterAppearance.Toughness, appearance);
        }

        [Test]
        public void ResolveTitle_ChecksFromTheEndAndReturnsFirstMatch()
        {
            var titles = new[]
            {
                new TitleDefinition { titleId = 0, titleName = "保底", requirement = "" },
                new TitleDefinition { titleId = 1, titleName = "速度", requirement = "spd>=20" },
                new TitleDefinition { titleId = 3, titleName = "速度韌度", requirement = "spd>=20;tgh>=20" },
            };

            var title = RunRules.ResolveTitle(titles, new StatBlock(0, 50, 20, 20));

            Assert.IsNotNull(title);
            Assert.AreEqual(3, title.titleId);
        }

        [Test]
        public void ResolveTitle_UsesFallbackWhenNoThresholdMatches()
        {
            var titles = new[]
            {
                new TitleDefinition { titleId = 0, titleName = "保底", requirement = "" },
                new TitleDefinition { titleId = 1, titleName = "速度", requirement = "spd>=20" },
            };

            var title = RunRules.ResolveTitle(titles, new StatBlock(0, 50, 0, 0));

            Assert.IsNotNull(title);
            Assert.AreEqual(0, title.titleId);
        }

        [Test]
        public void FindTitleById_ReturnsTheMatchingSpecialEnding()
        {
            var titles = new[]
            {
                new TitleDefinition { titleId = 2008, titleName = "永生不朽" },
                new TitleDefinition { titleId = 2009, titleName = "兔子的晚餐" },
            };

            var title = RunRules.FindTitleById(titles, 2009);

            Assert.IsNotNull(title);
            Assert.AreEqual("兔子的晚餐", title.titleName);
        }
    }
}
