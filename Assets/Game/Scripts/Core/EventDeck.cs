using System;
using System.Collections.Generic;

namespace GrassRun
{
    /// <summary>
    /// 指定接續優先；一般事件依目前最高的已解鎖批次抽選，並盡量避開最近四次。
    /// </summary>
    public sealed class EventDeck
    {
        const int RecentWindow = 4;

        readonly List<EventDefinition> library = new List<EventDefinition>();
        readonly List<EventDefinition> recent = new List<EventDefinition>();
        readonly List<EventDefinition> candidates = new List<EventDefinition>();
        readonly Random rng;

        public EventDeck(IEnumerable<EventDefinition> events, Random rng)
        {
            this.rng = rng ?? new Random();
            if (events == null) return;
            foreach (var e in events)
                if (e != null) library.Add(e);
        }

        public IReadOnlyList<EventDefinition> Library => library;

        public EventDefinition Draw(int eventsResolved, int previousEventId = 0, int previousOptionIndex = -1)
        {
            var followUp = FindFollowUp(previousEventId, previousOptionIndex);
            if (followUp != null) return Remember(followUp);

            int activeUnlock = -1;
            foreach (var e in library)
                if (!e.HasPrerequisite && e.unlockEventCount <= eventsResolved && e.unlockEventCount > activeUnlock)
                    activeUnlock = e.unlockEventCount;

            if (activeUnlock < 0) return null;

            candidates.Clear();
            foreach (var e in library)
                if (!e.HasPrerequisite && e.unlockEventCount == activeUnlock) candidates.Add(e);
            if (candidates.Count == 0) return null;

            int window = Math.Min(RecentWindow, candidates.Count - 1);
            for (int i = recent.Count - 1; i >= 0 && window > 0; i--)
            {
                if (candidates.Remove(recent[i])) window--;
            }

            return Remember(candidates[rng.Next(candidates.Count)]);
        }

        EventDefinition FindFollowUp(int previousEventId, int previousOptionIndex)
        {
            if (previousEventId <= 0 || previousOptionIndex < 0 || previousOptionIndex >= EventDefinition.OptionCount)
                return null;

            string choice = ((char)('a' + previousOptionIndex)).ToString();
            EventDefinition matched = null;
            foreach (var e in library)
            {
                if (e.prerequisiteEventId != previousEventId ||
                    !string.Equals(e.prerequisiteChoice, choice, StringComparison.OrdinalIgnoreCase)) continue;
                if (matched != null)
                    throw new InvalidOperationException($"事件 {previousEventId} 選項 {choice} 指定了多個後續事件。");
                matched = e;
            }
            return matched;
        }

        EventDefinition Remember(EventDefinition picked)
        {
            recent.Add(picked);
            if (recent.Count > RecentWindow) recent.RemoveAt(0);
            return picked;
        }
    }
}
