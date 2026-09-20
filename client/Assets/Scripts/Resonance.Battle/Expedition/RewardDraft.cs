using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public static class RewardDraft
    {
        // Independent from BattleSim and UnityEngine.Random; stable across processes.
        public static int DeriveSeed(int seed, string identity)
        {
            unchecked
            {
                uint hash = 2166136261U ^ (uint)seed;
                foreach (char c in identity ?? "") { hash ^= c; hash *= 16777619U; }
                return (int)(hash & 0x7fffffffU);
            }
        }

        public static RewardOffer Generate(ExpeditionState run, string rewardId, string nextNode, long revision)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            var owned = new HashSet<string>(run.OwnedRelicIds, StringComparer.Ordinal);
            var pool = new List<string>();
            foreach (var relic in ExpeditionContent.Relics)
                if (!owned.Contains(relic.Id) && relic.IsEligible(owned)) pool.Add(relic.Id);
            pool.Sort(StringComparer.Ordinal);
            uint rng = (uint)DeriveSeed(run.RunSeed, "reward/" + rewardId + "/" + run.CurrentNode) + 1U;
            var chosen = new List<string>();
            if (rewardId == "R1" || rewardId == "R2" || rewardId == "R4")
            {
                string guaranteed = null;
                if (rewardId == "R4")
                {
                    var amplifier = run.InitialCoreId.Substring(0, 1) + "04";
                    if (pool.Contains(amplifier)) guaranteed = amplifier;
                }
                if (guaranteed == null)
                {
                    var successors = pool.FindAll(id => id[0] == run.InitialCoreId[0] && id != run.InitialCoreId);
                    if (successors.Count > 0) guaranteed = successors[Next(ref rng, successors.Count)];
                }
                if (guaranteed != null) { chosen.Add(guaranteed); pool.Remove(guaranteed); }
            }
            while (chosen.Count < 3 && pool.Count > 0)
            {
                var weighted = new List<string>();
                foreach (var id in pool)
                {
                    int weight = 1;
                    if (rewardId == "R2")
                    {
                        bool preferred = run.CurrentNode == "N2-backstage" ? id[0] == 'A' || id[0] == 'C' : id[0] == 'B' || id[0] == 'C';
                        if (preferred) weight = 3;
                    }
                    for (int i = 0; i < weight; i++) weighted.Add(id);
                }
                var pick = weighted[Next(ref rng, weighted.Count)];
                chosen.Add(pick); pool.Remove(pick);
            }
            // Placement is also persisted; UI ordering cannot become another random draw.
            for (int i = chosen.Count - 1; i > 0; i--)
            {
                int j = Next(ref rng, i + 1); var value = chosen[i]; chosen[i] = chosen[j]; chosen[j] = value;
            }
            return new RewardOffer { Id = run.RunId + "/" + rewardId, RewardId = rewardId,
                Revision = revision, CandidateIds = chosen.ToArray(), NextNode = nextNode,
                IsRecoveryOnly = chosen.Count == 0 };
        }

        static int Next(ref uint state, int count)
        {
            unchecked { state ^= state << 13; state ^= state >> 17; state ^= state << 5; }
            return (int)(state % (uint)count);
        }
    }
}
