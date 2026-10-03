using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GrassRun.Tests
{
    /// <summary>
    /// 針對專案裡實際的事件資料與平衡設定。新增或修改事件後，這裡會抓出規則被破壞的情況。
    /// </summary>
    public class ContentTests
    {
        const string BalancePath = "Assets/Game/Data/Balance.asset";
        const int Runs = 200;

        GameBalance balance;
        EventDefinition[] library;

        [SetUp]
        public void SetUp()
        {
            balance = AssetDatabase.LoadAssetAtPath<GameBalance>(BalancePath);
            library = Resources.LoadAll<EventDefinition>("Events");
            Assert.IsNotNull(balance, $"找不到 {BalancePath}");
            Assert.IsNotEmpty(library, "Resources/Events 底下沒有事件");
        }

        [Test]
        public void EventLibrary_HasNoValidationIssues()
        {
            var issues = EventValidation.ValidateLibrary(library, balance);
            Assert.IsEmpty(issues, string.Join("\n", issues));
        }

        [Test]
        public void EventsComeEveryThreeSeconds()
        {
            Assert.AreEqual(3f, balance.eventInterval);
        }

        [Test]
        public void FreshGrass_CanAlwaysActOnItsFirstEvent()
        {
            foreach (var e in library)
            {
                if (e.kind != EventKind.Random) continue;

                bool any = false;
                foreach (var option in e.options)
                    any |= RunRules.Check(option, balance.startStats, balance, 0).available;
                Assert.IsTrue(any, $"{e.name}：剛出發的小草沒有任何選項可選");
            }
        }

        [Test]
        public void EveryStrategy_EventuallyEnds()
        {
            var policies = new (string name, System.Func<int, RunSimulator.Policy> create)[]
            {
                ("亂選", seed => RunSimulator.RandomPolicy(seed)),
                ("平均", _ => RunSimulator.BalancedPolicy()),
                ("專精速度", _ => RunSimulator.FavorPolicy(StatType.Speed)),
                ("專精溼度", _ => RunSimulator.FavorPolicy(StatType.Moisture)),
                ("專精韌度", _ => RunSimulator.FavorPolicy(StatType.Toughness)),
                ("規劃", _ => RunSimulator.PlannerPolicy()),
            };

            foreach (var (name, create) in policies)
            {
                for (int seed = 1; seed <= Runs; seed++)
                {
                    var result = RunSimulator.Run(balance, library, create(seed), seed);
                    Assert.IsFalse(result.hitEventLimit, $"策略「{name}」種子 {seed} 跑了 {result.eventsResolved} 個事件還沒結束");
                }
            }
        }

        [Test]
        public void APlayerWhoPreparesForTheCheckpoint_UsuallyPassesTheFirstOne()
        {
            int passed = 0;
            for (int seed = 1; seed <= Runs; seed++)
            {
                var result = RunSimulator.Run(balance, library, RunSimulator.PlannerPolicy(), seed);
                if (result.checkpointsPassed >= 1) passed++;
            }

            Assert.GreaterOrEqual(passed, Runs * 9 / 10, $"{Runs} 局裡只有 {passed} 局通過第一個檢驗點");
        }
    }
}
