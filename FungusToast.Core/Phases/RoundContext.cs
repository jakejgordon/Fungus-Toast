using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Phases
{
    public class RoundContext
    {
        private readonly Dictionary<(int playerId, string effect), int> perPlayerEffectCounters = new();

        public int GetEffectCount(int playerId, string effect)
            => perPlayerEffectCounters.TryGetValue((playerId, effect), out var v) ? v : 0;

        public void IncrementEffectCount(int playerId, string effect, int delta = 1)
        {
            var key = (playerId, effect);
            perPlayerEffectCounters.TryAdd(key, 0);
            perPlayerEffectCounters[key] += delta;
        }

        public void Reset()
        {
            perPlayerEffectCounters.Clear();
        }

        // Optional: easy debugging
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var kvp in perPlayerEffectCounters)
                sb.AppendLine($"Player {kvp.Key.playerId}, Effect {kvp.Key.effect}: {kvp.Value}");
            return sb.ToString();
        }
    }
}
