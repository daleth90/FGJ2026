using System;
using System.Collections.Generic;

namespace GrassRun
{
    /// <summary>
    /// 依事件種類抽事件，並盡量避開最近抽過的。
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
            this.rng = rng;
            if (events == null) return;
            foreach (var e in events)
                if (e != null) library.Add(e);
        }

        public IReadOnlyList<EventDefinition> Library => library;

        public EventDefinition Draw(EventKind kind)
        {
            candidates.Clear();
            foreach (var e in library)
                if (e.kind == kind && e.weight > 0f) candidates.Add(e);
            if (candidates.Count == 0) return null;

            // 候選夠多時才排除最近抽過的，至少留一個可抽。
            int window = Math.Min(RecentWindow, candidates.Count - 1);
            for (int i = recent.Count - 1; i >= 0 && window > 0; i--)
            {
                if (candidates.Remove(recent[i])) window--;
            }

            var picked = PickWeighted();
            recent.Add(picked);
            if (recent.Count > RecentWindow) recent.RemoveAt(0);
            return picked;
        }

        EventDefinition PickWeighted()
        {
            float total = 0f;
            foreach (var e in candidates) total += e.weight;

            double roll = rng.NextDouble() * total;
            foreach (var e in candidates)
            {
                roll -= e.weight;
                if (roll < 0d) return e;
            }
            return candidates[candidates.Count - 1];
        }
    }
}
