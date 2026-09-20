using System;
using System.Collections.Generic;
using System.IO;
using Resonance.Battle;
using UnityEngine;

namespace Resonance.App
{
    public sealed partial class GameRoot
    {
        bool _originalMode, _originalBattleActive, _originalSummaryOpen;
        ExpeditionFlow _expedition;
        ExpeditionHud _expeditionHud;
        ExpeditionBattleInput _originalOpening;
        string _originalNotice, _originalPreset = "single";
        public bool IsOriginalMode => _originalMode;
        public OriginalProfile OriginalProfile => _expedition?.Profile;

        void InitializeOriginalExpedition()
        {
            _save = new SaveBlob { PartyIds = ExpeditionContent.PartyIds }; // Detached view only; never written.
            _expeditionHud?.Dispose();
            _expeditionHud = new ExpeditionHud(Root());
            try
            {
                var customPath = Environment.GetEnvironmentVariable("RESONANCE_ORIGINAL_PROFILE");
                var path = string.IsNullOrEmpty(customPath)
                    ? Path.Combine(Application.persistentDataPath, "OriginalExpedition", "profile.v1.json") : customPath;
                var store = new OriginalProfileStore(path);
                _expedition = new ExpeditionFlow(store);
                if (store.RecoveredFromBackup) _originalNotice = "已读取上一个有效备份；损坏正文保留，下一次保存时修复。";
                ShowOriginalExpedition();
            }
            catch (Exception error) { ShowOriginalError(error); }
        }

        void OriginalAction(Action action)
        {
            try { action(); _originalNotice = null; ShowOriginalExpedition(); }
            catch (Exception error) { ShowOriginalError(error); }
        }

        static ExpeditionHud.Choice OriginalChoice(string id, string title, string description, Action click)
            => new ExpeditionHud.Choice { Id = id, Title = title, Description = description, Select = click };

        void ShowOriginalError(Exception error)
        {
            _originalBattleActive = false;
            _battle?.ClearExpeditionQueue();
            _originalNotice = error.Message;
            Debug.LogWarning("Original expedition: " + error.Message);
            if (_expeditionHud == null) return;
            var model = new ExpeditionHud.ScreenModel
            {
                Kind = ExpeditionHud.ScreenKind.Error, Title = "进度未确认", Notice = error.Message,
                Body = "已保存的检查点和个人档案保留。重新读取有效进度后再继续；本次操作不会显示为已领取。"
            };
            model.Options.Add(OriginalChoice("reload", "重新读取", "读取已提交的完整状态", () =>
            {
                if (_expedition == null) { InitializeOriginalExpedition(); return; }
                OriginalAction(() => _expedition.Reload());
            }));
            _expeditionHud.Render(model);
        }

        void ShowOriginalExpedition()
        {
            if (_expeditionHud == null || _expedition == null) return;
            _originalBattleActive = false;
            _battle?.ClearExpeditionQueue();
            _battle = null;
            _originalOpening = null;
            _simAcc = 0;
            var profile = _expedition.Profile;
            var run = profile.ActiveRun;
            var model = new ExpeditionHud.ScreenModel { Title = "失声剧院", Notice = _originalNotice };
            if (run == null)
            {
                if (_originalSummaryOpen && profile.LastRunSummary != null)
                {
                    var summary = profile.LastRunSummary;
                    model.Kind = ExpeditionHud.ScreenKind.Result;
                    model.Title = summary.Victory ? "终幕落下" : "远征结束";
                    model.Subtitle = summary.EndReason + (summary.Facts != null && summary.Facts.Length >= 4 ? " · 最后一战复盘" : "");
                    if (summary.Facts != null && summary.Facts.Length >= 4)
                    {
                        for (int i = 0; i < 3; i++) model.Facts.Add(summary.Facts[i]);
                        model.Body = summary.Facts[3];
                    }
                    else
                    {
                        model.Facts.Add("本趟完成 " + summary.BattlesCompleted + " 场战斗。");
                        model.Facts.Add("本趟获得 " + summary.RelicIds.Length + " 件临时强化。");
                        model.Body = "下一趟可以选择另一核心，尝试不同的强化组合。";
                    }
                    model.FooterNote = "完成 " + summary.BattlesCompleted + " 场 · 临时构筑已清空。" + (summary.UnlockedNewPreset
                        ? "首通解锁等价「横扫」配置。" : "已发现强化和已解锁配置保留。");
                    model.FooterActions.Add(OriginalChoice("home", "返回据点", "", () => { _originalSummaryOpen = false; ShowOriginalExpedition(); }));
                }
                else
                {
                    model.Kind = ExpeditionHud.ScreenKind.Home;
                    model.Subtitle = "固定五人 · 五场战斗 · 三条构筑方向";
                    model.Body = "普通攻击自动进行。点选敌人集火，使用五名队员的主动技能。生命在战斗间保留；首领门前全队恢复。";
                    AddOriginalParty(model, _originalPreset, null);
                    foreach (var preset in profile.UnlockedPresets)
                    {
                        var id = preset;
                        model.Options.Add(new ExpeditionHud.Choice { Id = "preset_" + id, Title = id == "single" ? "单体配置" : "横扫配置",
                            Description = id == "single" ? "优先快速处理一个目标" : "主攻手范围更广、单体力度较低，基础属性不变",
                            Selected = _originalPreset == id, Select = () => { _originalPreset = id; ShowOriginalExpedition(); } });
                    }
                    foreach (var id in new[] { "A01", "B01", "C01" })
                    {
                        var core = id;
                        var choice = OriginalRelicChoice(core);
                        choice.Select = () => OriginalAction(() => _expedition.StartRun(core, _originalPreset, Environment.TickCount & int.MaxValue));
                        model.Options.Add(choice);
                    }
                    model.FooterNote = "已发现 " + profile.DiscoveredRelics.Length + "/12 件强化 · 临时构筑不会带入下一趟";
                    if (profile.LastRunSummary != null)
                        model.FooterActions.Add(OriginalChoice("summary", "上趟回顾", "", () => { _originalSummaryOpen = true; ShowOriginalExpedition(); }));
                }
                _expeditionHud.Render(model);
                return;
            }
            _originalSummaryOpen = false;
            if (profile.ContentVersion != ExpeditionContent.Version || run.FrozenContentHash != ExpeditionContent.ContentHash ||
                run.RulesetId != ExpeditionContent.RulesetId)
            {
                model.Kind = ExpeditionHud.ScreenKind.Error;
                model.Title = "远征版本不兼容";
                model.Body = "此趟使用的内容版本与当前版本不匹配，无法继续。原有进度保持原样；可使用对应版本继续，或明确结束本趟后重新出发。";
                model.Options.Add(OriginalChoice("reload", "重新读取", "读取已保存状态，不迁移或清空进度", () => OriginalAction(() => _expedition.Reload())));
                model.FooterActions.Add(OriginalChoice("end_run", "结束本趟", "保留发现与永久选择，放弃此趟进度", () => OriginalAction(() => { _expedition.EndRun(); _originalSummaryOpen = true; })));
                _expeditionHud.Render(model);
                return;
            }
            model.Subtitle = NodeTitle(run.CurrentNode) + " · 种子 " + run.RunSeed;
            AddOriginalParty(model, run.PresetId, run.PartyHp);
            foreach (var relic in run.OwnedRelicIds) model.Relics.Add(OriginalRelicChoice(relic));
            model.FooterActions.Add(OriginalChoice("end_run", "结束本趟", "仅保留发现和永久选择", () => OriginalAction(() => { _expedition.EndRun(); _originalSummaryOpen = true; })));
            model.FooterNote = "离开战斗后从同场战前检查点重开；当前构筑与奖励不重抽。";
            if (run.Status == ExpeditionStatus.Reward)
            {
                model.Kind = ExpeditionHud.ScreenKind.Reward;
                model.Title = "选择一件临时强化";
                var offer = run.PendingOffer;
                foreach (var id in offer.CandidateIds)
                {
                    var relic = id; var card = OriginalRelicChoice(relic);
                    card.Relationship = relic[0] == run.InitialCoreId[0] ? "衔接初始核心" : "混搭另一条构筑";
                    card.Select = () => OriginalAction(() => _expedition.ChooseReward(offer.Id, profile.Revision, relic));
                    model.Options.Add(card);
                }
                model.Options.Add(OriginalChoice("decline", "放弃强化，稍作休整", "存活队员恢复 5% 最大生命；本次候选随之消耗", () => OriginalAction(() => _expedition.ChooseReward(offer.Id, profile.Revision))));
            }
            else if (run.Status == ExpeditionStatus.BossRetry || run.Status == ExpeditionStatus.Failed)
            {
                model.Kind = ExpeditionHud.ScreenKind.Result;
                model.Title = run.Status == ExpeditionStatus.Failed ? "规则执行失败" : "挑战未完成";
                model.Notice = run.LastError;
                model.Body = "战前配置、遗物、首领规则与战斗种子保持一致。可以改变操作与集火顺序。";
                model.Options.Add(OriginalChoice("retry", "原样重试", "恢复冻结的战前状态", () => StartOriginalBattle(true)));
            }
            else if (run.Status == ExpeditionStatus.Battle)
            {
                model.Kind = ExpeditionHud.ScreenKind.Map;
                model.Body = "本场已有战前检查点，继续会从开局重新开始。";
                model.Options.Add(OriginalChoice("resume", "继续当前战斗", "恢复同一战前配置", () => StartOriginalBattle(false)));
            }
            else if (run.CurrentNode == "N3")
            {
                model.Kind = ExpeditionHud.ScreenKind.Workshop;
                model.Title = "幕间工坊";
                model.Options.Add(OriginalChoice("rest", "休整队伍", "存活队员至少恢复至 50%；倒下队员以 35% 生命归队", () => OriginalAction(() => _expedition.ChooseWorkshop(true))));
                model.Options.Add(OriginalChoice("reinforce", "加固构筑", "放弃本次恢复，额外选择一件强化", () => OriginalAction(() => _expedition.ChooseWorkshop(false))));
            }
            else if (run.CurrentNode == "N6")
            {
                model.Kind = ExpeditionHud.ScreenKind.BossReady;
                model.Title = "白面指挥者 · 幕前整备";
                model.Body = "全队已经恢复。共鸣面具会放大终幕回响；读条期间清除面具可以降低伤害。首领生命降至一半后会再唤面具。";
                foreach (var preset in run.UnlockSnapshot)
                {
                    var id = preset;
                    model.Options.Add(new ExpeditionHud.Choice { Id = "boss_preset_" + id, Title = id == "single" ? "单体配置" : "横扫配置", Selected = run.PresetId == id,
                        Description = "确认出战后配置冻结，重试不能更换。", Select = () => OriginalAction(() => _expedition.SetBossPreset(id)) });
                }
                model.Options.Add(OriginalChoice("begin_boss", "确认出战", "冻结首领检查点", () => StartOriginalBattle(false)));
            }
            else
            {
                model.Kind = ExpeditionHud.ScreenKind.Map;
                AddOriginalMap(model, run);
                if (run.CurrentNode == "N2")
                {
                    model.Options.Add(OriginalChoice("backstage", "进入后台", "较少耐打敌人 · 奖励偏防护 / 接力", () => OriginalAction(() => _expedition.ChooseRoute("N2-backstage"))));
                    model.Options.Add(OriginalChoice("audience", "进入观众席", "更多脆弱敌人 · 奖励偏散射 / 接力", () => OriginalAction(() => _expedition.ChooseRoute("N2-audience"))));
                }
                else model.Options.Add(OriginalChoice("begin", "进入" + NodeTitle(run.CurrentNode), "生命与当前构筑带入本场", () => StartOriginalBattle(false)));
            }
            _expeditionHud.Render(model);
        }

        static string NodeTitle(string id)
        {
            switch (id)
            {
                case "N0": return "据点"; case "N1": return "前厅"; case "N2": return "双向分岔";
                case "N2-backstage": return "后台"; case "N2-audience": return "观众席"; case "N3": return "工坊";
                case "N4": return "排练厅"; case "N5": return "返场回廊"; case "N6": return "首领门前"; case "N7": return "白面指挥者";
                default: return id;
            }
        }

        static void AddOriginalMap(ExpeditionHud.ScreenModel model, ExpeditionState run)
        {
            foreach (var id in new[] { "N0", "N1", "N2-backstage", "N2-audience", "N3", "N4", "N5", "N6", "N7" })
                model.Nodes.Add(new ExpeditionHud.MapNode { Id = id, Title = NodeTitle(id),
                    Current = id == run.CurrentNode || (run.CurrentNode == "N2" && id.StartsWith("N2-")),
                    Visited = Array.IndexOf(run.VisitedNodeIds, id) >= 0,
                    Description = id == "N2-backstage" ? "耐打敌人 · A/C 倾向" : id == "N2-audience" ? "多目标 · B/C 倾向" : "",
                    State = Array.IndexOf(run.VisitedNodeIds, id) >= 0 ? "已完成" : id == run.CurrentNode ? "当前" : "" });
        }

        static void AddOriginalParty(ExpeditionHud.ScreenModel model, string preset, int[] hp)
        {
            var input = RunBattleFactory.CreateInput("N1", preset, 1, new string[0], hp);
            foreach (var id in input.PartyIds)
            {
                var def = Array.Find(input.Characters, x => x.Id == id);
                var slot = Array.IndexOf(input.PartyIds, id);
                var skill = Array.Find(input.Skills, x => x.Id == def.TapSkillId);
                model.Party.Add(new ExpeditionHud.PartyMember { Id = id, Name = def.Name,
                    Role = CharacterPresenter.RoleLabel(def.Role), Skill = skill.Name, Hp = input.OpeningHp[slot], MaxHp = def.Hp });
            }
        }

        static ExpeditionHud.Choice OriginalRelicChoice(string id)
        {
            var relic = ExpeditionContent.FindRelic(id);
            return new ExpeditionHud.Choice { Id = id, Title = id + " · " + relic.Name, Description = relic.ShortDescription,
                Family = relic.Family, Detail = relic.Description };
        }

        void StartOriginalBattle(bool retry)
        {
            try
            {
                _originalOpening = retry ? _expedition.RetryBattle() : _expedition.BeginBattle();
                _battle = new BattleSim(_originalOpening);
                _battle.FreezeInitialHeader();
                _simAcc = 0; _originalBattleActive = true; _originalNotice = null;
                _expeditionHud.RenderBattle(_battle, OriginalBattleView(), new ExpeditionHud.BattleActions
                {
                    OnSkill = OriginalSkill, OnFocus = OriginalFocus, OnClear = ClearOriginalQueuedSkill, OnPause = ToggleOriginalPause,
                    OnSpeed = ToggleBattleSpeed, OnExit = ShowOriginalExpedition
                });
            }
            catch (Exception error) { ShowOriginalError(error); }
        }

        ExpeditionHud.BattleModel OriginalBattleView()
        {
            var names = new string[5];
            if (_originalOpening != null)
                for (int i = 0; i < names.Length; i++)
                {
                    var unit = _battle.Allies[i];
                    var skill = Array.Find(_originalOpening.Skills, x => x.Id == unit.Def.TapSkillId);
                    names[i] = skill?.Name ?? "主动技能";
                }
            var queued = new string[5];
            var order = new List<string>();
            if (_battle != null)
                foreach (var command in _battle.ExpeditionQueue)
                {
                    var text = _battle.Allies[command.ActorSlot].Def.Name;
                    if (command.RequiredEnemyGeneration > 0)
                        text += "→" + _battle.Enemies[command.RequiredEnemySlot].Def.Name;
                    queued[command.ActorSlot] = text;
                    order.Add((order.Count + 1) + "." + text);
                }
            var execution = new List<string>();
            if (_battle != null)
                foreach (var entry in _battle.ExpeditionQueueResults)
                    execution.Add(_battle.Allies[entry.ActorSlot].Def.Name + (entry.Result.Accepted ? "已执行" : "：" + OriginalReject(entry.Result.Reason)));
            var model = new ExpeditionHud.BattleModel
            {
                Title = _originalOpening?.Stage.Name, Subtitle = "点敌集火 · 普攻自动 · 主动技能由你指挥",
                SkillNames = names, RelicIds = _originalOpening?.RelicIds, Notice = _originalNotice,
                QueuedCommands = queued, QueueSummary = order.Count > 0 ? "恢复时按序校验：" + string.Join("  ", order)
                    : execution.Count > 0 ? string.Join("；", execution) : null
            };
            if (_battle != null)
            {
                var facts = ExpeditionBattleFeedback.Capture(_battle);
                var ids = model.RelicIds ?? Array.Empty<string>();
                model.RelicTriggerSerials = new long[ids.Length];
                for (int i = 0; i < ids.Length; i++) model.RelicTriggerSerials[i] = _battle.ExpeditionRelics.GetTriggerSerial(ids[i]);
                var actors = 0;
                for (int i = 0; i < 5; i++) if ((facts.HarmonyActorBits & (1 << i)) != 0) actors++;
                model.RelicState = (Array.IndexOf(ids, "A01") >= 0 ? "A 蓄能 " + facts.BarrierEnergy + "/" + facts.BarrierThreshold : "A 未持有")
                    + " · " + (Array.IndexOf(ids, "C03") >= 0 ? "C 和声 " + actors + "/3" : Array.IndexOf(ids, "C01") >= 0 ? "C 接力" : "C 未持有")
                    + (Array.IndexOf(ids, "C04") >= 0 ? " · 强奏" + (facts.ForteStored ? "已储存" : "未储存") : "");
                if (Array.IndexOf(ids, "B01") >= 0)
                {
                    var targets = new List<string>();
                    foreach (var target in facts.LatestScatterTargets)
                        targets.Add("敌" + (target.Slot + 1) + "（" + target.EffectiveDamage + "）");
                    model.RelicState += "\nB 最近散射：" + (targets.Count > 0 ? string.Join("、", targets) : "尚未触发");
                }
                model.Feedback = "本场对敌 " + facts.EnemyEffectiveDamage + " · 己方吸收 " + facts.AllyShieldAbsorbed
                    + "\n有效治疗 " + facts.AllyEffectiveHealing + " · 成功主动 " + facts.SuccessfulActiveCommands + " 次";
            }
            var intent = _battle?.OriginalIntentSnapshot;
            if (intent != null && !intent.Cancelled)
            {
                model.IntentTitle = intent.IsCasting ? intent.AreaName + " · 正在蓄势" : intent.AreaName + " · 准备中";
                model.IntentDescription = intent.IsCasting
                    ? "全队范围 · " + intent.RemainingCastSec.ToString("0.0") + " 秒后结算"
                    : "距离下一次预告 " + intent.NextIntentSec.ToString("0.0") + " 秒";
                model.IntentDescription += intent.IsBoss
                    ? "\n阶段 " + intent.Phase + " · 面具 " + intent.AliveMasks + "/2 · 群攻力度 " + intent.AreaMultiplier.ToString("0.0") + "×"
                    : "\n护盾可吸收群攻；技能和治疗由你决定时机。";
                model.IntentProgress = intent.IsCasting && intent.CastDurationSec > 0f
                    ? 1f - intent.RemainingCastSec / intent.CastDurationSec : 0f;
            }
            return model;
        }

        void OriginalSkill(int slot)
        {
            if (!_originalBattleActive || _battle == null) return;
            if (_battle.Paused)
            {
                var queued = _battle.QueueExpeditionTap(slot);
                _originalNotice = queued == CommandReject.None ? "已编排；恢复时检查充能和目标。" : "无法编排：" + OriginalReject(queued);
                return;
            }
            var result = _battle.Submit(BattleCommand.Tap(slot, CommandSource.Player));
            _originalNotice = result.Accepted ? null : "技能未执行：" + OriginalReject(result.Reason);
        }

        static string OriginalReject(CommandReject reason)
        {
            switch (reason)
            {
                case CommandReject.NotCharged: return "充能尚未就绪";
                case CommandReject.UnitDead: return "该队员已倒下";
                case CommandReject.Paused: return "当前处于暂停";
                case CommandReject.Silenced: return "当前无法使用技能";
                case CommandReject.ActionLocked: return "行动受到控制";
                case CommandReject.NotInProgress: return "战斗已结束";
                case CommandReject.TargetInvalid: return "原目标已失效，请重新选敌";
                case CommandReject.SlideOnCooldown: return "技能仍在冷却";
                default: return reason.ToString();
            }
        }

        void OriginalFocus(int slot)
        {
            if (_battle == null) return;
            var result = _battle.Submit(BattleCommand.FocusEnemy(slot, CommandSource.Player));
            _originalNotice = result.Accepted ? null : "目标不可选：" + OriginalReject(result.Reason);
        }

        void ToggleOriginalPause()
        {
            if (!_originalBattleActive || _battle == null) return;
            var result = _battle.Submit(new BattleCommand { Kind = _battle.Paused ? BattleCommandKind.Resume : BattleCommandKind.Pause, Source = CommandSource.Player });
            if (!result.Accepted) _originalNotice = OriginalReject(result.Reason);
            else if (!_battle.Paused)
            {
                var accepted = 0; var rejected = 0; string firstReason = null;
                foreach (var entry in _battle.ExpeditionQueueResults)
                {
                    if (entry.Result.Accepted) accepted++;
                    else { rejected++; if (firstReason == null) firstReason = OriginalReject(entry.Result.Reason); }
                }
                _originalNotice = rejected > 0 ? "已执行 " + accepted + " 条；未执行 " + rejected + " 条：" + firstReason
                    : accepted > 0 ? "已按顺序执行 " + accepted + " 条指令。" : null;
            }
            else _originalNotice = null;
        }

        void ClearOriginalQueuedSkill(int slot)
        {
            if (_battle == null) return;
            var result = _battle.ClearExpeditionQueuedCommand(slot);
            _originalNotice = result == CommandReject.None ? null : OriginalReject(result);
        }

        void UpdateOriginalExpedition()
        {
            if (!_originalBattleActive || _battle == null) return;
            try
            {
                _simAcc += Time.deltaTime * BattleSim.TickHz;
                int count = Math.Min((int)_simAcc, BattleSim.TickHz * 2);
                _simAcc -= (int)_simAcc;
                for (int i = 0; i < count && _battle.Outcome == BattleOutcome.InProgress; i++) _battle.Tick();
            }
            catch (Exception error) { _battle.Outcome = BattleOutcome.Failed; _battle.FailedReason = error.Message; }
            if (_battle.Settled)
            {
                var hp = new int[_battle.Allies.Length];
                for (int i = 0; i < hp.Length; i++) hp[i] = _battle.Allies[i].Hp;
                OriginalAction(() =>
                {
                    var summary = ExpeditionBattleFeedback.Summarize(_battle);
                    var facts = new List<string>(summary.Facts) { summary.Advice };
                    _expedition.CompleteBattle(_originalOpening.EncounterId, _battle.Outcome, hp, _battle.FailedReason, facts.ToArray(), _originalOpening.AttemptId);
                    _originalSummaryOpen = _expedition.Profile.ActiveRun == null;
                });
                return;
            }
            _expeditionHud.RefreshBattle(_battle, OriginalBattleView());
        }

    }
}
