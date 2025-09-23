using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.UI.GameLog.Internal
{
    /// <summary>
    /// Aggregates rapid-fire per-player ability events (colonized / infested / reclaimed / toxified / poisoned kills etc.)
    /// into a single summarized line after a short debounce window (0.5s by default).
    /// Keyed by (playerId|ability|effectType).
    /// </summary>
    internal class PlayerEventAggregator
    {
        private readonly Dictionary<string,int> _counts = new();
        private readonly Dictionary<string,float> _lastEventTime = new();
        private readonly List<(string key,int playerId,string ability,string effect,FungusToast.Unity.UI.GameLog.GameLogCategory category)> _pending = new();
        private const float DEBOUNCE = 0.5f; // seconds

        private readonly System.Func<float> _getTime;
        private readonly System.Action<GameLogEntry> _emit;

        public PlayerEventAggregator(System.Func<float> getTime, System.Action<GameLogEntry> emit)
        {
            _getTime = getTime;
            _emit = emit;
        }

        public void Add(int playerId,string ability,string effect,FungusToast.Unity.UI.GameLog.GameLogCategory category)
        {
            string key = Key(playerId,ability,effect);
            if (!_counts.ContainsKey(key))
            {
                _counts[key] = 0;
                _pending.Add((key,playerId,ability,effect,category));
            }
            _counts[key]++;
            _lastEventTime[key] = _getTime();
        }

        public void Update()
        {
            if (_pending.Count == 0) return;
            float now = _getTime();
            // collect keys whose debounce expired
            var ready = _pending.Where(p => now - _lastEventTime[p.key] >= DEBOUNCE).ToList();
            if (ready.Count == 0) return;
            foreach (var r in ready)
            {
                if (!_counts.TryGetValue(r.key,out int count) || count <=0) continue;
                string msg = BuildMessage(r.ability,r.effect,count);
                _emit(new GameLogEntry(msg,r.category,null,r.playerId));
                _counts.Remove(r.key);
                _lastEventTime.Remove(r.key);
                _pending.Remove(r);
            }
        }

        public void ResetForPlayer(int playerId)
        {
            var toRemove = _counts.Keys.Where(k => k.StartsWith(playerId.ToString()+"|")).ToList();
            foreach (var k in toRemove)
            {
                _counts.Remove(k);
                _lastEventTime.Remove(k);
                _pending.RemoveAll(p=>p.key==k);
            }
        }

        public void ResetAll()
        {
            _counts.Clear();
            _lastEventTime.Clear();
            _pending.Clear();
        }

        private static string Key(int pid,string ability,string effect)=>$"{pid}|{ability}|{effect}";

        private static string BuildMessage(string ability,string effect,int count)
        {
            return effect switch
            {
                "poisoned" => count==1? $"{ability} poisoned 1 enemy cell" : $"{ability} poisoned {count} enemy cells",
                "colonized"=> count==1? $"{ability} colonized 1 empty tile" : $"{ability} colonized {count} empty tiles",
                "infested" => count==1? $"{ability} killed 1 enemy cell" : $"{ability} killed {count} enemy cells",
                "reclaimed"=> count==1? $"{ability} reclaimed 1 dead cell" : $"{ability} reclaimed {count} dead cells",
                "toxified" => count==1? $"{ability} toxified 1 empty tile" : $"{ability} toxified {count} empty tiles",
                _ => $"{ability}: {effect} {count}",
            };
        }
    }
}
