using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public sealed class ExpeditionFlow
    {
        readonly OriginalProfileStore _store;
        OriginalProfile _profile;
        public OriginalProfile Profile => OriginalProfileStore.Clone(_profile);
        public bool RecoveredFromBackup => _store.RecoveredFromBackup;

        public ExpeditionFlow(OriginalProfileStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _profile = store.Load();
        }

        public void Reload() { _profile = _store.Load(); }

        public void StartRun(string coreId, string presetId, int seed)
        {
            if (_profile.ActiveRun != null) throw new InvalidOperationException("请先结束当前远征。");
            if (coreId != "A01" && coreId != "B01" && coreId != "C01") throw new ArgumentException("请选择一个初始核心。");
            if (!Contains(_profile.UnlockedPresets, presetId)) throw new InvalidOperationException("该起始配置尚未解锁。");
            var next = Copy();
            next.ContentVersion = ExpeditionContent.Version;
            next.ActiveRun = new ExpeditionState
            {
                RunId = Guid.NewGuid().ToString("N"), RunSeed = seed, RulesetId = ExpeditionContent.RulesetId,
                FrozenContentHash = ExpeditionContent.ContentHash, UnlockSnapshot = (string[])next.UnlockedPresets.Clone(),
                InitialCoreId = coreId, PresetId = presetId, CurrentNode = "N1", Status = ExpeditionStatus.Ready,
                PartyHp = ExpeditionContent.GetPartyMaxHp(presetId), OwnedRelicIds = new[] { coreId },
                VisitedNodeIds = new[] { "N0" }, SelectedChoices = new[] { "N0:" + coreId }
            };
            next.DiscoveredRelics = Add(next.DiscoveredRelics, coreId);
            Commit(next);
        }

        public void ChooseRoute(string nodeId)
        {
            Require("N2", ExpeditionStatus.Ready);
            if (nodeId != "N2-backstage" && nodeId != "N2-audience") throw new ArgumentException("Unknown branch.");
            var next = Copy(); var run = next.ActiveRun;
            run.CurrentNode = nodeId;
            run.SelectedChoices = Add(run.SelectedChoices, "N2:" + nodeId);
            Commit(next);
        }

        public void ChooseWorkshop(bool rest)
        {
            Require("N3", ExpeditionStatus.Ready);
            var next = Copy(); var run = next.ActiveRun;
            run.VisitedNodeIds = Add(run.VisitedNodeIds, "N3");
            run.SelectedChoices = Add(run.SelectedChoices, "N3:" + (rest ? "rest" : "reinforce"));
            if (rest)
            {
                var max = ExpeditionContent.GetPartyMaxHp(run.PresetId);
                for (int i = 0; i < max.Length; i++)
                    run.PartyHp[i] = run.PartyHp[i] > 0 ? Math.Max(run.PartyHp[i], Percent(max[i], 50)) : Math.Max(1, Percent(max[i], 35));
                run.CurrentNode = "N4";
            }
            else OpenOffer(next, "R3", "N4");
            Commit(next);
        }

        public void ChooseReward(string offerId, long expectedRevision, string relicId = null)
        {
            var current = Require(null, ExpeditionStatus.Reward);
            if (_profile.Revision != expectedRevision || current.PendingOffer.Id != offerId || current.PendingOffer.Revision != expectedRevision)
                throw new InvalidOperationException("此奖励已处理或版本已改变，请重新载入。");
            if (relicId != null && !Contains(current.PendingOffer.CandidateIds, relicId)) throw new ArgumentException("该强化不在本次候选中。");
            var next = Copy(); var run = next.ActiveRun;
            if (relicId == null) RecoverLiving(run, 5);
            else
            {
                run.OwnedRelicIds = Add(run.OwnedRelicIds, relicId);
                next.DiscoveredRelics = Add(next.DiscoveredRelics, relicId);
            }
            run.SelectedChoices = Add(run.SelectedChoices, run.PendingOffer.RewardId + ":" + (relicId ?? "rest"));
            run.CurrentNode = run.PendingOffer.NextNode; run.PendingOffer = null; run.Status = ExpeditionStatus.Ready;
            Commit(next);
        }

        public void SetBossPreset(string presetId)
        {
            var current = Require("N6", ExpeditionStatus.Ready);
            if (!Contains(current.UnlockSnapshot, presetId)) throw new InvalidOperationException("本趟未拥有该配置。");
            var next = Copy(); next.ActiveRun.PresetId = presetId;
            next.ActiveRun.PartyHp = ExpeditionContent.GetPartyMaxHp(presetId);
            Commit(next);
        }

        public ExpeditionBattleInput BeginBattle()
        {
            var current = Require(null, null);
            if (current.Status == ExpeditionStatus.Battle)
                return current.CurrentBattleCheckpoint.DeepClone(); // Reopen starts exactly at the persisted opening.
            if (current.Status != ExpeditionStatus.Ready) throw new InvalidOperationException("当前节点尚不能出战。");
            var node = current.CurrentNode == "N6" ? "N7" : current.CurrentNode;
            if (node != "N1" && node != "N2-backstage" && node != "N2-audience" && node != "N4" && node != "N5" && node != "N7")
                throw new InvalidOperationException("请先完成当前节点的选择。");
            var next = Copy(); var run = next.ActiveRun;
            run.BattleOrdinal++;
            int seed = RewardDraft.DeriveSeed(run.RunSeed, "battle/" + node + "/" + run.BattleOrdinal);
            var input = RunBattleFactory.CreateInput(node, run.PresetId, seed, run.OwnedRelicIds, run.PartyHp);
            input.RunId = run.RunId; input.EncounterId = run.RunId + "/" + node + "/" + run.BattleOrdinal;
            input.BattleOrdinal = run.BattleOrdinal; input.AttemptId = Guid.NewGuid().ToString("N");
            run.CurrentNode = node; run.Status = ExpeditionStatus.Battle; run.LastError = null;
            run.CurrentBattleCheckpoint = input.DeepClone();
            if (node == "N7")
            {
                run.VisitedNodeIds = Add(run.VisitedNodeIds, "N6");
                run.BossCheckpoint = input.DeepClone();
            }
            Commit(next);
            return input.DeepClone();
        }

        public ExpeditionBattleInput RetryBattle()
        {
            var current = Require(null, null);
            if (current.Status != ExpeditionStatus.BossRetry && current.Status != ExpeditionStatus.Failed && current.Status != ExpeditionStatus.Battle)
                throw new InvalidOperationException("当前没有可恢复的战前检查点。");
            var next = Copy(); var run = next.ActiveRun;
            var input = (run.BossCheckpoint ?? run.CurrentBattleCheckpoint).DeepClone();
            input.AttemptId = Guid.NewGuid().ToString("N");
            run.CurrentBattleCheckpoint = input.DeepClone(); run.Status = ExpeditionStatus.Battle; run.LastError = null;
            Commit(next);
            return input.DeepClone();
        }

        public bool CompleteBattle(string encounterId, BattleOutcome outcome, int[] finalHp, string error = null, string[] facts = null, string attemptId = null)
        {
            if (_profile.ActiveRun == null)
            {
                if (_profile.LastRunSummary != null && _profile.LastRunSummary.LastEncounterId == encounterId) return false;
                throw new InvalidOperationException("没有对应的活动远征。");
            }
            var current = Require(null, null);
            if (Contains(current.SettledBattleIds, encounterId)) return false;
            if (current.Status != ExpeditionStatus.Battle || current.CurrentBattleCheckpoint == null || current.CurrentBattleCheckpoint.EncounterId != encounterId)
                throw new InvalidOperationException("战斗结算身份不匹配。");
            if (attemptId != null && current.CurrentBattleCheckpoint.AttemptId != attemptId) throw new InvalidOperationException("过期的战斗尝试不能结算。");
            if (outcome == BattleOutcome.InProgress) throw new InvalidOperationException("战斗尚未结束。");
            var next = Copy(); var run = next.ActiveRun;
            if (outcome == BattleOutcome.Failed)
            {
                run.Status = ExpeditionStatus.Failed;
                run.LastError = string.IsNullOrEmpty(error) ? "战斗规则执行失败，已保留战前检查点。" : error;
                Commit(next); return true;
            }
            if (outcome != BattleOutcome.Victory && outcome != BattleOutcome.Defeat) throw new ArgumentOutOfRangeException(nameof(outcome));
            ValidateHp(run, finalHp);
            if (outcome == BattleOutcome.Defeat)
            {
                if (run.CurrentNode == "N7") { run.Status = ExpeditionStatus.BossRetry; run.LastError = null; }
                else Finish(next, false, "队伍全灭", encounterId, facts);
                Commit(next); return true;
            }
            run.PartyHp = (int[])finalHp.Clone();
            run.SettledBattleIds = Add(run.SettledBattleIds, encounterId);
            run.VisitedNodeIds = Add(run.VisitedNodeIds, run.CurrentNode);
            if (run.CurrentNode == "N7") Finish(next, true, "失声剧院通关", encounterId, facts);
            else
            {
                RecoverLiving(run, 15); run.CurrentBattleCheckpoint = null; run.Status = ExpeditionStatus.Ready;
                switch (run.CurrentNode)
                {
                    case "N1": OpenOffer(next, "R1", "N2"); break;
                    case "N2-backstage": case "N2-audience": OpenOffer(next, "R2", "N3"); break;
                    case "N4": OpenOffer(next, "R4", "N5"); break;
                    case "N5": run.CurrentNode = "N6"; run.PartyHp = ExpeditionContent.GetPartyMaxHp(run.PresetId); break;
                    default: throw new InvalidOperationException("Unknown battle node.");
                }
            }
            Commit(next); return true;
        }

        public void EndRun()
        {
            // Ending an incompatible run is an explicit user choice, not a silent reset on load.
            var run = _profile.ActiveRun ?? throw new InvalidOperationException("当前没有活动远征。");
            var next = Copy(); Finish(next, false, "主动结束远征", run.CurrentBattleCheckpoint?.EncounterId, null); Commit(next);
        }

        static void Finish(OriginalProfile profile, bool victory, string reason, string encounterId, string[] facts)
        {
            var run = profile.ActiveRun;
            bool unlock = victory && !Contains(profile.UnlockedPresets, "sweep");
            run.ResultApplied = true;
            if (victory)
            {
                profile.UnlockedPresets = Add(profile.UnlockedPresets, "sweep");
                profile.ClearedChapters = Add(profile.ClearedChapters, "silent-theatre");
            }
            profile.LastRunSummary = new ExpeditionRunSummary
            {
                RunId = run.RunId, LastEncounterId = encounterId, Victory = victory, ResultApplied = true,
                UnlockedNewPreset = unlock, RelicIds = (string[])run.OwnedRelicIds.Clone(),
                VisitedNodeIds = (string[])run.VisitedNodeIds.Clone(), BattlesCompleted = run.SettledBattleIds.Length,
                EndReason = reason, Facts = facts == null ? Array.Empty<string>() : (string[])facts.Clone()
            };
            profile.ActiveRun = null;
        }

        static void OpenOffer(OriginalProfile profile, string rewardId, string nextNode)
        {
            var run = profile.ActiveRun;
            run.PendingOffer = RewardDraft.Generate(run, rewardId, nextNode, checked(profile.Revision + 1));
            run.Status = ExpeditionStatus.Reward;
        }

        ExpeditionState Require(string node, ExpeditionStatus? status)
        {
            var run = _profile.ActiveRun ?? throw new InvalidOperationException("请先从据点开始远征。");
            if (run.FrozenContentHash != ExpeditionContent.ContentHash || run.RulesetId != ExpeditionContent.RulesetId || _profile.ContentVersion != ExpeditionContent.Version)
                throw new InvalidOperationException("本趟远征的内容版本不同，请使用匹配版本恢复或结束远征。");
            if ((node != null && run.CurrentNode != node) || (status.HasValue && run.Status != status.Value))
                throw new InvalidOperationException("该操作不适用于当前远征节点。");
            return run;
        }

        static void ValidateHp(ExpeditionState run, int[] values)
        {
            var max = ExpeditionContent.GetPartyMaxHp(run.PresetId);
            if (values == null || values.Length != max.Length) throw new ArgumentException("需要五人的实际结算生命。");
            for (int i = 0; i < values.Length; i++) if (values[i] < 0 || values[i] > max[i]) throw new ArgumentOutOfRangeException(nameof(values));
        }

        static void RecoverLiving(ExpeditionState run, int percent)
        {
            var max = ExpeditionContent.GetPartyMaxHp(run.PresetId);
            for (int i = 0; i < run.PartyHp.Length; i++)
                if (run.PartyHp[i] > 0) run.PartyHp[i] = (int)Math.Min(max[i], (long)run.PartyHp[i] + Percent(max[i], percent));
        }

        static int Percent(int value, int percent) => (int)((long)value * percent / 100L);
        OriginalProfile Copy() => OriginalProfileStore.Clone(_profile);
        void Commit(OriginalProfile next) { _profile = _store.Save(_profile.Revision, next); }
        static bool Contains(string[] values, string value) => Array.IndexOf(values, value) >= 0;
        static string[] Add(string[] values, string value)
        {
            if (Contains(values, value)) return values;
            var result = new string[values.Length + 1]; Array.Copy(values, result, values.Length); result[values.Length] = value; return result;
        }
    }
}
