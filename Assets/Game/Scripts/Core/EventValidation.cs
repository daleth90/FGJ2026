using System.Collections.Generic;

namespace GrassRun
{
    /// <summary>
    /// 事件資料的檢查。回傳問題清單，空清單代表沒問題。
    /// </summary>
    public static class EventValidation
    {
        public static List<string> Validate(EventDefinition e)
        {
            var issues = new List<string>();
            if (e == null)
            {
                issues.Add("事件是空的。");
                return issues;
            }

            string name = e.name;
            if (string.IsNullOrWhiteSpace(e.title)) issues.Add($"{name}：沒有標題。");
            if (string.IsNullOrWhiteSpace(e.description)) issues.Add($"{name}：沒有情境描述。");
            if (e.zones == ZoneMask.None) issues.Add($"{name}：沒有指定任何區域，永遠不會出現。");
            if (e.weight <= 0f) issues.Add($"{name}：權重是 0，永遠不會被抽到。");

            if (e.options == null || e.options.Length != EventDefinition.OptionCount)
            {
                issues.Add($"{name}：選項數量必須是 {EventDefinition.OptionCount} 個。");
                return issues;
            }

            for (int i = 0; i < e.options.Length; i++)
            {
                var option = e.options[i];
                string label = $"{name} 選項 {i + 1}";
                if (option == null)
                {
                    issues.Add($"{label}：是空的。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(option.title)) issues.Add($"{label}：沒有標題。");
                if (string.IsNullOrWhiteSpace(option.description)) issues.Add($"{label}：沒有說明文字，玩家看不出需求和風險。");
                if (string.IsNullOrWhiteSpace(option.resultText)) issues.Add($"{label}：沒有結果敘事。");

                // 「夠低」的門檻不會隨週期變難，只靠它的檢驗點選項等於永遠過得了。
                bool hasRisingRequirement = false;
                if (option.requirements != null)
                {
                    foreach (var requirement in option.requirements)
                        hasRisingRequirement |= requirement.comparison == Comparison.AtLeast;
                }
                if (e.kind == EventKind.Checkpoint && !hasRisingRequirement)
                    issues.Add($"{label}：檢驗點的選項至少要有一條「夠高」的門檻，否則永遠不會失敗。");
            }

            return issues;
        }

        /// <summary>整個事件池的檢查：每個區域都要有檢驗點，隨機事件也要夠一個週期不重複。</summary>
        public static List<string> ValidateLibrary(IReadOnlyList<EventDefinition> library, GameBalance balance)
        {
            var issues = new List<string>();
            foreach (var e in library) issues.AddRange(Validate(e));

            int randomPerCycle = balance != null ? balance.eventsPerCycle - 1 : 5;
            foreach (ZoneType zone in System.Enum.GetValues(typeof(ZoneType)))
            {
                int randoms = 0;
                int checkpoints = 0;
                foreach (var e in library)
                {
                    if (e == null || !e.AppearsIn(zone) || e.weight <= 0f) continue;
                    if (e.kind == EventKind.Checkpoint) checkpoints++;
                    else randoms++;
                }

                string zoneName = GameText.ZoneName(zone);
                if (checkpoints == 0) issues.Add($"{zoneName}：沒有檢驗點事件。");
                if (randoms < randomPerCycle)
                    issues.Add($"{zoneName}：隨機事件只有 {randoms} 個，一個週期需要 {randomPerCycle} 個才不會重複。");
            }

            return issues;
        }
    }
}
