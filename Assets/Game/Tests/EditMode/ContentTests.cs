using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GrassRun.Tests
{
    public class ContentTests
    {
        const string BalancePath = "Assets/Game/Data/Balance.asset";
        const string TitleTablePath = "Assets/Game/Data/TitleTable.asset";

        GameBalance balance;
        TitleTable titleTable;
        EventDefinition[] library;

        [SetUp]
        public void SetUp()
        {
            balance = AssetDatabase.LoadAssetAtPath<GameBalance>(BalancePath);
            titleTable = AssetDatabase.LoadAssetAtPath<TitleTable>(TitleTablePath);
            library = Resources.LoadAll<EventDefinition>("Events");
            Assert.IsNotNull(balance, $"找不到 {BalancePath}");
            Assert.IsNotNull(titleTable, $"找不到 {TitleTablePath}");
            Assert.AreSame(titleTable, balance.titleTable, "Balance 應引用獨立的稱號 Table");
            Assert.IsNotEmpty(library, "Resources/Events 底下沒有事件");
        }

        [Test]
        public void EventLibrary_MatchesV2TableAndHasNoValidationIssues()
        {
            Assert.AreEqual(40, library.Length, "v2 表格應匯入 40 個事件");
            var issues = EventValidation.ValidateLibrary(library, balance);
            Assert.IsEmpty(issues, string.Join("\n", issues));
        }

        [Test]
        public void EventIdsAreUniqueAndStageCountsMatchTheTable()
        {
            var ids = new HashSet<int>();
            int stage1 = 0, stage2 = 0, stage3 = 0;
            foreach (var e in library)
            {
                Assert.IsTrue(ids.Add(e.eventId), $"事件 ID {e.eventId} 重複");
                if (e.unlockEventCount == 0) stage1++;
                else if (e.unlockEventCount == 3) stage2++;
                else if (e.unlockEventCount == 6) stage3++;
            }

            Assert.AreEqual(10, stage1);
            Assert.AreEqual(10, stage2);
            Assert.AreEqual(20, stage3);
        }

        [Test]
        public void EveryEventHasAtLeastOneOptionWithoutRequirements()
        {
            foreach (var e in library)
            {
                bool found = false;
                foreach (var option in e.options)
                    found |= string.IsNullOrEmpty(option.requirement);
                Assert.IsTrue(found, $"{e.name} 沒有保底選項");
            }
        }

        [Test]
        public void ImageIdsAndOptionLengthsMatchTheLatestTableFormat()
        {
            foreach (var e in library)
            {
                Assert.AreEqual($"event_{e.eventId}", e.imageId);
                for (int i = 0; i < e.options.Length; i++)
                {
                    Assert.LessOrEqual(e.options[i].description.Length, EventDefinition.MaxOptionTextLength,
                        $"{e.name} 選項 {(char)('A' + i)} 超過字數限制");
                    Assert.AreEqual($"event_{e.eventId}_{(char)('a' + i)}", e.options[i].resultImageId);
                }
            }
        }

        [Test]
        public void StartStatsMatchTheV2Rules()
        {
            Assert.AreEqual(0, balance.startStats.morality);
            Assert.AreEqual(50, balance.startStats.moisture);
            Assert.AreEqual(0, balance.startStats.speed);
            Assert.AreEqual(0, balance.startStats.toughness);
            Assert.AreEqual(100, balance.maxMoisture);
        }

        [Test]
        public void GeneralTitlesMatchTheV2TableOrder()
        {
            Assert.IsNotNull(titleTable.titles);
            Assert.AreEqual(12, titleTable.titles.Length);
            for (int i = 0; i < titleTable.titles.Length; i++)
            {
                Assert.AreEqual(i, titleTable.titles[i].titleId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(titleTable.titles[i].titleName));
                Assert.AreEqual($"title_{i}", titleTable.titles[i].imageId);
                Assert.IsTrue(RunRules.TryParseRequirements(titleTable.titles[i].requirement,
                    new List<Requirement>(), out string error), $"稱號 {i}：{error}");
            }
        }

        [TestCase(10, 20, 20, 7)]
        [TestCase(-10, 20, 20, 11)]
        [TestCase(0, 20, 20, 3)]
        [TestCase(0, 0, 0, 0)]
        public void GeneralTitles_SelectTheLastMatchingTableRow(
            int morality, int speed, int toughness, int expectedTitleId)
        {
            var title = RunRules.ResolveTitle(titleTable.titles,
                new StatBlock(morality, 50, speed, toughness));

            Assert.IsNotNull(title);
            Assert.AreEqual(expectedTitleId, title.titleId);
        }

        [Test]
        public void SpecialEndingTitlesMatchTheV2Table()
        {
            int[] expectedIds = { 2008, 2009, 2010, 3015, 3016, 3017, 3018, 3019, 3020 };
            Assert.IsNotNull(titleTable.specialEndingTitles);
            Assert.AreEqual(expectedIds.Length, titleTable.specialEndingTitles.Length);

            for (int i = 0; i < expectedIds.Length; i++)
            {
                var title = titleTable.specialEndingTitles[i];
                Assert.AreEqual(expectedIds[i], title.titleId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(title.titleName));
                Assert.AreEqual($"title_{expectedIds[i]}", title.imageId);
            }
        }
    }
}
