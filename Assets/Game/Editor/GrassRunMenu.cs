using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GrassRun.EditorTools
{
    /// <summary>
    /// 企劃用的工具選單：檢查事件資料、模擬平衡。
    /// </summary>
    public static class GrassRunMenu
    {
        const string BalancePath = "Assets/Game/Data/Balance.asset";
        const int RunsPerPolicy = 300;

        [MenuItem("GrassRun/檢查事件資料")]
        public static void ValidateEvents()
        {
            var balance = LoadBalance();
            var library = LoadLibrary();
            var issues = EventValidation.ValidateLibrary(library, balance);

            if (issues.Count == 0)
                Debug.Log($"事件資料沒有問題（共 {library.Length} 個事件）。");
            else
                Debug.LogWarning($"事件資料有 {issues.Count} 個問題：\n" + string.Join("\n", issues));
        }

        [MenuItem("GrassRun/模擬平衡（每種策略 300 局）")]
        public static void SimulateBalance()
        {
            Debug.Log(BuildSimulationReport(LoadBalance(), LoadLibrary(), RunsPerPolicy));
        }

        /// <summary>
        /// 用幾種固定策略各跑多局，看通過幾個檢驗點、怎麼結束。
        /// 用來回答「這套規則會不會結束」「哪種配點最強」。
        /// </summary>
        public static string BuildSimulationReport(GameBalance balance, IReadOnlyList<EventDefinition> library, int runs)
        {
            if (balance == null) return $"找不到 {BalancePath}。";

            var policies = new (string name, System.Func<int, RunSimulator.Policy> create)[]
            {
                ("亂選", seed => RunSimulator.RandomPolicy(seed)),
                ("平均", _ => RunSimulator.BalancedPolicy()),
                ("專精速度", _ => RunSimulator.FavorPolicy(StatType.Speed)),
                ("專精溼度", _ => RunSimulator.FavorPolicy(StatType.Moisture)),
                ("專精韌度", _ => RunSimulator.FavorPolicy(StatType.Toughness)),
                ("規劃（看預告配點）", _ => RunSimulator.PlannerPolicy()),
            };

            var sb = new StringBuilder();
            sb.AppendLine($"平衡模擬：每種策略 {runs} 局，容量{(balance.useCapacity ? "開" : "關")}，事件 {library.Count} 個");
            sb.AppendLine("策略｜通過檢驗點（中位數／平均／最多）｜死於檢驗點／枯竭／沒結束");

            foreach (var (name, create) in policies)
            {
                var passed = new List<int>(runs);
                int deaths = 0, withered = 0, unfinished = 0;
                for (int seed = 1; seed <= runs; seed++)
                {
                    var result = RunSimulator.Run(balance, library, create(seed), seed);
                    passed.Add(result.checkpointsPassed);
                    if (result.hitEventLimit) unfinished++;
                    else if (result.endReason == RunEndReason.Death) deaths++;
                    else withered++;
                }

                passed.Sort();
                sb.AppendLine($"{name}｜{passed[passed.Count / 2]}／{passed.Average():0.0}／{passed[passed.Count - 1]}｜{deaths}／{withered}／{unfinished}");
            }

            return sb.ToString();
        }

        static GameBalance LoadBalance() => AssetDatabase.LoadAssetAtPath<GameBalance>(BalancePath);

        static EventDefinition[] LoadLibrary() => Resources.LoadAll<EventDefinition>("Events");
    }
}
