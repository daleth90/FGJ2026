using System;
using System.Collections.Generic;

namespace GrassRun
{
    /// <summary>
    /// 依已經歷事件數選出目前最高的已解鎖批次，並盡量避開最近四次事件。
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

        public EventDefinition Draw(int eventsResolved)
        {
            int activeUnlock = -1;
            foreach (var e in library)
                if (e.unlockEventCount <= eventsResolved && e.unlockEventCount > activeUnlock)
                    activeUnlock = e.unlockEventCount;

            if (activeUnlock < 0) return null;

            candidates.Clear();
            foreach (var e in library)
                if (e.unlockEventCount == activeUnlock) candidates.Add(e);
            if (candidates.Count == 0) return null;

            int window = Math.Min(RecentWindow, candidates.Count - 1);
            for (int i = recent.Count - 1; i >= 0 && window > 0; i--)
            {
                if (candidates.Remove(recent[i])) window--;
            }

            var picked = candidates[rng.Next(candidates.Count)];
            recent.Add(picked);
            if (recent.Count > RecentWindow) recent.RemoveAt(0);
            return picked;
        }
    }
}
