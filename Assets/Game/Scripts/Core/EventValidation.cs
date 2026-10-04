using System.Collections.Generic;

namespace GrassRun
{
    /// <summary>依 v3 事件表格式檢查單一事件與整個事件池。</summary>
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
            if (e.eventId <= 0) issues.Add($"{name}：事件 ID 必須大於 0。");
            if (e.unlockEventCount < 0) issues.Add($"{name}：解鎖事件數不可為負數。");
            bool hasPrerequisiteId = e.prerequisiteEventId != 0;
            bool hasPrerequisiteChoice = !string.IsNullOrEmpty(e.prerequisiteChoice);
            if (hasPrerequisiteId != hasPrerequisiteChoice)
                issues.Add($"{name}：前置任務 ID 與前置選項必須同時填寫或同時留空。");
            if (hasPrerequisiteId && e.prerequisiteEventId <= 0)
                issues.Add($"{name}：前置任務 ID 必須大於 0。");
            if (hasPrerequisiteChoice && !IsPrerequisiteChoice(e.prerequisiteChoice))
                issues.Add($"{name}：前置選項必須是 a、b 或 c（不分大小寫）。");
            if (string.IsNullOrWhiteSpace(e.description)) issues.Add($"{name}：沒有事件敘述。");
            string expectedEventImage = $"event_{e.eventId}";
            if (e.image == null)
                issues.Add($"{name}：沒有指定事件圖片 {expectedEventImage}。");
            else if (e.image.name != expectedEventImage)
                issues.Add($"{name}：事件圖片應為 {expectedEventImage}。");

            int stage = e.eventId / 1000;
            int expectedUnlock = stage == 1 ? 0 : stage == 2 ? 3 : stage == 3 ? 6 : -1;
            if (expectedUnlock < 0)
                issues.Add($"{name}：事件 ID {e.eventId} 不在 1xxx、2xxx、3xxx 階段。");
            else if (e.unlockEventCount != expectedUnlock)
                issues.Add($"{name}：{stage}xxx 事件的解鎖事件數應為 {expectedUnlock}。");

            if (e.options == null || e.options.Length != EventDefinition.OptionCount)
            {
                issues.Add($"{name}：選項數量必須是 {EventDefinition.OptionCount} 個。");
                return issues;
            }

            for (int i = 0; i < e.options.Length; i++)
            {
                var option = e.options[i];
                string label = $"{name} 選項 {(char)('A' + i)}";
                if (option == null)
                {
                    issues.Add($"{label}：是空的。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(option.description)) issues.Add($"{label}：沒有選項文案。");
                else if (option.description.Length > EventDefinition.MaxOptionTextLength)
                    issues.Add($"{label}：選項文案不可超過 {EventDefinition.MaxOptionTextLength} 字。");
                if (string.IsNullOrWhiteSpace(option.resultText)) issues.Add($"{label}：沒有選後回饋。");
                string expectedResultImage = $"event_{e.eventId}_{(char)('a' + i)}";
                if (option.resultImageId != expectedResultImage)
                    issues.Add($"{label}：結果圖片 ID 應為 {expectedResultImage}。");

                var requirements = new List<Requirement>();
                if (!RunRules.TryParseRequirements(option.requirement, requirements, out string requirementError))
                    issues.Add($"{label}：需求格式錯誤：{requirementError}");

                var changes = new List<StatChange>();
                if (!RunRules.TryParseOffsets(option.offset, changes, out string offsetError))
                    issues.Add($"{label}：變動格式錯誤：{offsetError}");

                if (option.endingTitleId == 0 && changes.Count == 0)
                    issues.Add($"{label}：沒有結局 ID 時，至少要有一項數值變動。");
                if (option.endingTitleId < 0)
                    issues.Add($"{label}：結局 ID 不可為負數。");

                bool mustLoseMoisture = stage == 1 ? i == 2 : stage == 2 ? i >= 1 : stage == 3;
                if (option.endingTitleId != 0) mustLoseMoisture = false;
                if (mustLoseMoisture && !HasNegativeMoisture(changes))
                    issues.Add($"{label}：依階段規則必須包含負數 hmd 變動。");
            }

            return issues;
        }

        public static List<string> ValidateLibrary(IReadOnlyList<EventDefinition> library, GameBalance balance = null)
        {
            var issues = new List<string>();
            if (library == null || library.Count == 0)
            {
                issues.Add("事件池是空的。");
                return issues;
            }

            var ids = new HashSet<int>();
            var unlocks = new HashSet<int>();
            foreach (var e in library)
            {
                issues.AddRange(Validate(e));
                if (e == null) continue;
                if (!ids.Add(e.eventId)) issues.Add($"事件 ID {e.eventId} 重複。");
                unlocks.Add(e.unlockEventCount);
            }

            foreach (int required in new[] { 0, 3, 6 })
                if (!unlocks.Contains(required)) issues.Add($"缺少解鎖事件數為 {required} 的事件批次。");

            var followUps = new Dictionary<string, int>();
            foreach (var e in library)
            {
                if (e == null || e.prerequisiteEventId <= 0) continue;
                if (!ids.Contains(e.prerequisiteEventId))
                    issues.Add($"{e.name}：找不到前置任務 ID {e.prerequisiteEventId}。");
                if (!IsPrerequisiteChoice(e.prerequisiteChoice)) continue;

                string key = $"{e.prerequisiteEventId}/{e.prerequisiteChoice.ToLowerInvariant()}";
                if (followUps.TryGetValue(key, out int existingId))
                    issues.Add($"前置任務／選項 {key} 同時指定後續事件 {existingId} 與 {e.eventId}。");
                else
                    followUps.Add(key, e.eventId);
            }
            return issues;
        }

        static bool IsPrerequisiteChoice(string choice)
        {
            if (string.IsNullOrEmpty(choice) || choice.Length != 1) return false;
            char value = char.ToLowerInvariant(choice[0]);
            return value >= 'a' && value <= 'c';
        }

        static bool HasNegativeMoisture(List<StatChange> changes)
        {
            foreach (var change in changes)
                if (change.stat == StatType.Moisture && change.amount < 0) return true;
            return false;
        }
    }
}
