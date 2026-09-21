using System;
using System.Collections.Generic;
using System.Globalization;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Original expedition presentation. State and transactions belong to GameRoot/ExpeditionFlow;
    /// this view only displays snapshots and forwards ordinary player input.
    /// </summary>
    public sealed class ExpeditionHud : IDisposable
    {
        public enum ScreenKind { Home, Map, Reward, Workshop, BossReady, Result, Error }

        public sealed class PartyMember
        {
            public string Id, Name, Role, Skill;
            public int Hp, MaxHp;
        }

        public sealed class Choice
        {
            public string Id, Title, Description, Detail, Family, Relationship;
            public bool Enabled = true, Selected;
            public Action Select;
        }

        public sealed class MapNode
        {
            public string Id, Title, Description, State;
            public bool Current, Visited;
        }

        public sealed class ScreenModel
        {
            public ScreenKind Kind;
            public string Title, Subtitle, Notice, Body, FooterNote;
            public readonly List<PartyMember> Party = new List<PartyMember>();
            public readonly List<MapNode> Nodes = new List<MapNode>();
            public readonly List<Choice> Options = new List<Choice>();
            public readonly List<Choice> Relics = new List<Choice>();
            public readonly List<Choice> FooterActions = new List<Choice>();
            public readonly List<string> Facts = new List<string>();
        }

        public sealed class BattleActions
        {
            public Action<int> OnSkill, OnFocus, OnClear;
            public Action OnPause, OnSpeed, OnExit;
        }

        public sealed class BattleModel
        {
            public string Title, Subtitle, IntentTitle, IntentDescription;
            public float IntentProgress = -1f;
            public string RelicState, Feedback, QueueSummary, Notice;
            public string[] SkillNames, QueuedCommands;
            public float[] PrimaryCooldowns;
            public string[] RelicIds;
            // Advance only for a real structured runtime trigger. The short glow is presentation time.
            public long RelicTriggerSerial;
            public string LastTriggeredRelicId;
            public long[] RelicTriggerSerials;
        }

        static readonly Color Ink = new Color(0.045f, 0.06f, 0.09f, 1f);
        static readonly Color Panel = new Color(0.09f, 0.12f, 0.17f, 1f);
        static readonly Color Line = new Color(0.2f, 0.27f, 0.34f, 1f);
        static readonly Color Paper = new Color(0.94f, 0.95f, 0.95f, 1f);
        static readonly Color Muted = new Color(0.66f, 0.75f, 0.8f, 1f);
        static readonly Color Gold = new Color(0.94f, 0.76f, 0.4f, 1f);
        static readonly Color Mint = new Color(0.44f, 0.84f, 0.71f, 1f);
        static readonly Color Warning = new Color(1f, 0.56f, 0.45f, 1f);

        readonly Transform _parent;
        GameObject _root;
        bool _isBattle;
        Text _battleTitle, _battleSubtitle, _intentTitle, _intentBody, _relicState, _feedback, _queue, _battleNotice;
        Image _intentFill;
        Button _pause, _speed;
        Text _pauseLabel, _speedLabel;
        readonly UnitView[] _allies = new UnitView[5];
        readonly UnitView[] _enemies = new UnitView[5];
        readonly Image[] _relicIcons = new Image[5];
        readonly Text[] _relicIconLabels = new Text[5];
        readonly long[] _triggerSerials = new long[5];
        readonly float[] _triggerUntil = new float[5];
        BattleActions _actions;

        sealed class UnitView
        {
            public GameObject Root;
            public Text Name, Identity, Health, Charge, Status, Skill, Queue;
            public Image HealthFill, ChargeFill, Face;
            public Button Main, Clear;
        }

        public GameObject Root => _root;
        public bool IsBattle => _isBattle;

        public ExpeditionHud(Transform parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            _parent = parent;
        }

        public void Dispose()
        {
            if (_root != null)
            {
                // Deactivate before deferred destruction so the departing screen cannot intercept input.
                _root.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(_root);
                else UnityEngine.Object.DestroyImmediate(_root);
            }
            _root = null;
            _isBattle = false;
            _actions = null;
        }

        public void Render(ScreenModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            NewRoot("Expedition_" + model.Kind);
            LabelAt(_root.transform, "ChapterLabel", "原创远征 / 第一章", 25, Gold, 28, 30, 1024, 42);
            // Bundled NotoSansSC needs 79 px for this 54 px line; a shorter Truncate box generates no glyphs.
            LabelAt(_root.transform, "ScreenTitle", model.Title ?? "失声剧院", 54, Paper, 28, 74, 1024, 81, true);
            LabelAt(_root.transform, "ScreenSubtitle", model.Subtitle, 26, Muted, 28, 157, 1024, 68);
            var footerHeight = model.FooterActions.Count > 0 ? 190f : 86f;
            var content = ScrollArea(_root.transform, 240, footerHeight);
            if (!string.IsNullOrEmpty(model.Notice)) Paragraph(content, "Notice", model.Notice, 28, Warning, true);
            if (!string.IsNullOrEmpty(model.Body)) Paragraph(content, "Body", model.Body, 30, Paper);
            if (model.Party.Count > 0) PartyRow(content, model.Party);
            if (model.Options.Count > 0)
            {
                Section(content, model.Kind == ScreenKind.Home ? "选择出发配置与核心" : "当前选择");
                foreach (var option in model.Options) ChoiceCard(content, option, true);
            }
            if (model.Nodes.Count > 0) Map(content, model.Nodes);
            if (model.Facts.Count > 0)
            {
                Section(content, "本次远征");
                for (var i = 0; i < model.Facts.Count; i++)
                    Paragraph(content, "Fact_" + i, (i + 1) + ". " + model.Facts[i], 30, Paper, true);
            }
            if (model.Relics.Count > 0)
            {
                Section(content, "当前构筑 · 仅本趟有效");
                foreach (var relic in model.Relics) ChoiceCard(content, relic, false);
            }
            var footer = Box(_root.transform, "Footer", Ink, false);
            var fr = footer.GetComponent<RectTransform>();
            fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(1, 0);
            fr.pivot = new Vector2(0.5f, 0); fr.sizeDelta = new Vector2(0, footerHeight);
            fr.anchoredPosition = Vector2.zero;
            LabelAt(footer.transform, "FooterNote", model.FooterNote ?? "进度在节点选择与战斗开始前保存。", 24, Muted, 28, 18, 1024, 52);
            for (var i = 0; i < model.FooterActions.Count; i++)
            {
                var choice = model.FooterActions[i];
                var width = (1024f - 16 * (model.FooterActions.Count - 1)) / model.FooterActions.Count;
                var button = ButtonAt(footer.transform, "Action_" + choice.Id, choice.Title,
                    28 + i * (width + 16), 78, width, 82, choice.Select, i == 0);
                button.interactable = choice.Enabled;
            }
            Canvas.ForceUpdateCanvases();
        }

        public void RenderBattle(BattleSim sim, BattleModel model, BattleActions actions)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            NewRoot("Expedition_Battle");
            _isBattle = true;
            _actions = actions ?? new BattleActions();
            _battleTitle = LabelAt(_root.transform, "BattleTitle", "", 48, Paper, 28, 34, 750, 73, true);
            _battleSubtitle = LabelAt(_root.transform, "BattleSubtitle", "", 26, Muted, 28, 111, 1024, 56);
            var intent = BoxAt(_root.transform, "Intent", Panel, 28, 190, 1024, 190);
            _intentTitle = LabelAt(intent.transform, "IntentTitle", "", 36, Gold, 22, 14, 980, 54, true);
            _intentBody = LabelAt(intent.transform, "IntentDescription", "", 28, Paper, 22, 74, 980, 87);
            _intentFill = MeterAt(intent.transform, "IntentProgress", 22, 162, 980, 12, Warning);
            LabelAt(_root.transform, "TargetLabel", "点选敌人集火 · 金色边框为当前目标", 26, Muted, 28, 405, 1024, 48);
            for (var i = 0; i < 5; i++)
            {
                var slot = i;
                _enemies[i] = EnemyCard(_root.transform, i, () => _actions.OnFocus?.Invoke(slot));
            }
            var relicPanel = BoxAt(_root.transform, "RelicFeedback", Panel, 28, 746, 1024, 247);
            _relicState = LabelAt(relicPanel.transform, "RelicState", "", 28, Gold, 22, 15, 980, 87, true);
            for (var i = 0; i < _relicIcons.Length; i++)
            {
                var icon = BoxAt(relicPanel.transform, "RelicIcon_" + i, Line, 22 + i * 198, 104, 185, 46);
                _relicIcons[i] = icon.GetComponent<Image>();
                _relicIconLabels[i] = LabelAt(icon.transform, "RelicId", "", 25, Paper, 5, 5, 175, 36, true);
                _relicIconLabels[i].alignment = TextAnchor.MiddleCenter;
            }
            _feedback = LabelAt(relicPanel.transform, "Feedback", "", 27, Paper, 22, 159, 980, 82);
            _queue = LabelAt(_root.transform, "PauseQueue", "", 26, Mint, 28, 1010, 1024, 110);
            LabelAt(_root.transform, "PartyLabel", "五人小队 · 普攻自动 · 主要技能手动", 26, Muted, 28, 1135, 1024, 48);
            for (var i = 0; i < 5; i++)
            {
                var slot = i;
                _allies[i] = AllyCard(_root.transform, i, () => _actions.OnSkill?.Invoke(slot), () => _actions.OnClear?.Invoke(slot));
            }
            _battleNotice = LabelAt(_root.transform, "BattleNotice", "", 24, Warning, 28, 1684, 1024, 52);
            _pause = ButtonAt(_root.transform, "Pause", "暂停 / 编排", 28, 1740, 430, 100, () => _actions.OnPause?.Invoke(), true);
            _pauseLabel = _pause.GetComponentInChildren<Text>();
            _speed = ButtonAt(_root.transform, "Speed", "1×", 474, 1740, 222, 100, () => _actions.OnSpeed?.Invoke());
            _speedLabel = _speed.GetComponentInChildren<Text>();
            ButtonAt(_root.transform, "ExitBattle", "返回节点", 712, 1740, 340, 100, () => _actions.OnExit?.Invoke());
            LabelAt(_root.transform, "CheckpointNote", "中途离开后，从本场已保存的战前状态重新开始。", 22, Muted, 28, 1856, 1024, 42);
            RefreshBattle(sim, model);
        }

        public void RefreshBattle(BattleSim sim, BattleModel model)
        {
            if (!_isBattle || _root == null || sim == null) return;
            model = model ?? new BattleModel();
            _battleTitle.text = model.Title ?? "失声剧院";
            _battleSubtitle.text = (model.Subtitle ?? "") + "  ·  " + (sim.Paused ? "战术暂停" : sim.Speed + "×")
                + "  ·  剩余 " + Mathf.CeilToInt(Mathf.Max(0, sim.TimeLeft)) + " 秒";
            _intentTitle.text = string.IsNullOrEmpty(model.IntentTitle) ? "敌方行动" : model.IntentTitle;
            _intentBody.text = string.IsNullOrEmpty(model.IntentDescription) ? "观察敌方状态，选择集火目标。" : model.IntentDescription;
            SetMeter(_intentFill, model.IntentProgress < 0 ? 0 : model.IntentProgress);
            _relicState.text = model.RelicState ?? "本场构筑";
            _feedback.text = model.Feedback ?? "遗物触发与实际伤害、护盾吸收、治疗将在这里显示。";
            for (var i = 0; i < _relicIcons.Length; i++)
            {
                var id = At(model.RelicIds, i);
                _relicIcons[i].gameObject.SetActive(!string.IsNullOrEmpty(id));
                if (string.IsNullOrEmpty(id)) continue;
                var serial = model.RelicTriggerSerials != null && i < model.RelicTriggerSerials.Length
                    ? model.RelicTriggerSerials[i] : id == model.LastTriggeredRelicId ? model.RelicTriggerSerial : 0;
                if (serial > _triggerSerials[i])
                {
                    _triggerSerials[i] = serial;
                    _triggerUntil[i] = Time.unscaledTime + 1.2f;
                }
                var lit = _triggerSerials[i] > 0 && Time.unscaledTime < _triggerUntil[i];
                _relicIcons[i].color = lit ? FamilyColor(id.Substring(0, 1)) : Line;
                _relicIconLabels[i].color = lit ? Ink : Paper;
                _relicIconLabels[i].text = (lit ? "◆ " : "") + id;
            }
            _queue.text = model.QueueSummary ?? (sim.Paused
                ? "已暂停：点击技能加入队列；每人最多一条，再次点击覆盖。恢复时按顺序校验。"
                : "需要配合技能时，先暂停，再为不同队员编排指令。未就绪指令会被拒绝。" );
            _battleNotice.text = model.Notice ?? "";
            _pauseLabel.text = sim.Paused ? "恢复 / 执行队列" : "暂停 / 编排";
            _speedLabel.text = sim.Speed + "× 速度";
            _pause.interactable = _speed.interactable = sim.Outcome == BattleOutcome.InProgress;
            for (var i = 0; i < 5; i++)
            {
                var ally = i < sim.Allies.Length ? sim.Allies[i] : null;
                RefreshUnit(_allies[i], ally, false, false);
                var a = _allies[i];
                var queued = At(model.QueuedCommands, i);
                var cd = model.PrimaryCooldowns != null && i < model.PrimaryCooldowns.Length ? model.PrimaryCooldowns[i] : 0;
                a.Charge.text = ally == null ? "" : "充能 " + Mathf.FloorToInt(ally.Charge) + "/100";
                SetMeter(a.ChargeFill, ally == null ? 0 : ally.Charge / 100f);
                a.Skill.text = At(model.SkillNames, i) ?? "主要技能";
                if (ally != null && !ally.Alive) a.Skill.text = "已倒下";
                else if (cd > 0) a.Skill.text += "\n冷却 " + cd.ToString("0.0", CultureInfo.InvariantCulture) + "s";
                else if (!string.IsNullOrEmpty(queued)) a.Skill.text += "\n已编排";
                else if (sim.Paused) a.Skill.text += "\n加入队列";
                else if (ally != null && ally.Charge < 100) a.Skill.text += "\n充能中";
                else a.Skill.text += "\n可释放";
                a.Queue.text = queued ?? "";
                a.Clear.gameObject.SetActive(sim.Paused && !string.IsNullOrEmpty(queued));
                a.Main.interactable = sim.Outcome == BattleOutcome.InProgress && ally != null && ally.Alive;
                a.Main.targetGraphic.color = ally != null && ally.Alive && ally.Charge >= 100 && cd <= 0 ? Mint : Line;
                UnitState enemy = null;
                for (var j = 0; j < sim.Enemies.Count; j++) if (sim.Enemies[j].Slot == i) { enemy = sim.Enemies[j]; break; }
                RefreshUnit(_enemies[i], enemy, true, sim.FocusEnemySlot == i);
            }
        }

        void NewRoot(string name)
        {
            Dispose();
            Array.Clear(_triggerSerials, 0, _triggerSerials.Length);
            Array.Clear(_triggerUntil, 0, _triggerUntil.Length);
            _root = Box(_parent, name, Ink, false);
            Stretch(_root.GetComponent<RectTransform>());
        }

        static UnitView EnemyCard(Transform parent, int slot, Action select)
        {
            var u = new UnitView();
            u.Root = BoxAt(parent, "Enemy_" + slot, Panel, 28 + slot * 207, 466, 196, 255);
            u.Face = u.Root.GetComponent<Image>();
            u.Main = u.Root.AddComponent<Button>();
            u.Main.targetGraphic = u.Face;
            u.Face.raycastTarget = true;
            u.Main.onClick.AddListener(() => select?.Invoke());
            u.Name = LabelAt(u.Root.transform, "Name", "", 30, Paper, 10, 16, 176, 84, true);
            u.Identity = LabelAt(u.Root.transform, "Identity", "", 19, Muted, 10, 102, 176, 42);
            u.Health = LabelAt(u.Root.transform, "HP", "", 25, Paper, 10, 153, 176, 36);
            u.HealthFill = MeterAt(u.Root.transform, "Health", 10, 198, 176, 10, Warning);
            u.Status = LabelAt(u.Root.transform, "Status", "", 21, Gold, 10, 214, 176, 32);
            return u;
        }

        static UnitView AllyCard(Transform parent, int slot, Action skill, Action clear)
        {
            var u = new UnitView();
            u.Root = BoxAt(parent, "Ally_" + slot, Panel, 28 + slot * 207, 1200, 196, 406);
            u.Face = u.Root.GetComponent<Image>();
            u.Name = LabelAt(u.Root.transform, "Name", "", 29, Paper, 10, 12, 176, 54, true);
            u.Identity = LabelAt(u.Root.transform, "Identity", "", 18, Muted, 10, 68, 176, 30);
            u.Health = LabelAt(u.Root.transform, "HP", "", 24, Paper, 10, 106, 176, 39);
            u.HealthFill = MeterAt(u.Root.transform, "Health", 10, 153, 176, 9, Mint);
            u.Charge = LabelAt(u.Root.transform, "Charge", "", 23, Paper, 10, 172, 176, 35);
            u.ChargeFill = MeterAt(u.Root.transform, "ChargeBar", 10, 216, 176, 9, Gold);
            u.Status = LabelAt(u.Root.transform, "Status", "", 22, Mint, 10, 232, 176, 34);
            u.Main = ButtonAt(u.Root.transform, "MainSkill_" + slot, "主要技能", 8, 275, 180, 116, skill, true, 26);
            u.Skill = u.Main.GetComponentInChildren<Text>();
            u.Queue = LabelAt(parent, "Queued_" + slot, "", 20, Mint, 28 + slot * 207, 1560, 196, 39);
            u.Queue.gameObject.SetActive(false); // Full queued order is shown in the shared queue panel.
            u.Clear = ButtonAt(parent, "ClearQueue_" + slot, "清除此人", 28 + slot * 207, 1616, 196, 64, clear, false, 23);
            u.Clear.gameObject.SetActive(false);
            return u;
        }

        static void RefreshUnit(UnitView view, UnitState unit, bool enemy, bool focused)
        {
            if (view == null) return;
            view.Root.SetActive(unit != null);
            if (unit == null) return;
            view.Name.text = unit.Def.Name;
            // Explicit ID placeholder. Never fall back to another character's artwork.
            view.Identity.text = unit.Def.Id;
            view.Health.text = Math.Max(0, unit.Hp) + " / " + unit.MaxHp;
            SetMeter(view.HealthFill, unit.MaxHp > 0 ? (float)unit.Hp / unit.MaxHp : 0);
            view.Status.text = !unit.Alive ? "已倒下" : unit.Shield > 0 ? "护盾 " + unit.Shield : unit.ActionLocked ? "无法行动" : unit.SkillLocked ? "沉默" : focused ? "◆ 集火目标" : "";
            view.Face.color = !unit.Alive ? new Color(0.09f, 0.09f, 0.1f) : focused ? new Color(0.32f, 0.25f, 0.13f) : Panel;
            if (enemy) view.Main.interactable = unit.Alive;
        }

        static void PartyRow(Transform content, IList<PartyMember> party)
        {
            Section(content, "固定五人 · 出场顺序");
            var row = Box(content, "Party", Color.clear, false);
            Height(row, 218);
            var group = row.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 12; group.childControlWidth = true; group.childForceExpandWidth = true;
            group.childControlHeight = true; group.childForceExpandHeight = true;
            for (var i = 0; i < party.Count; i++)
            {
                var p = party[i];
                var card = Box(row.transform, "Party_" + p.Id, Panel, false);
                card.AddComponent<LayoutElement>().flexibleWidth = 1;
                var text = (i + 1) + ". " + p.Name + "\n" + p.Role + "\n" + p.Skill;
                if (p.MaxHp > 0) text += "\n" + p.Hp + "/" + p.MaxHp;
                var tx = Label(card.transform, "Member", text, 25, Paper, true);
                Stretch(tx.rectTransform, 10, 10);
                tx.alignment = TextAnchor.UpperCenter;
            }
        }

        static void Map(Transform content, IList<MapNode> nodes)
        {
            Section(content, "路线 · 分岔只选一侧");
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (IsBranch(node) && i + 1 < nodes.Count && IsBranch(nodes[i + 1]))
                {
                    var branch = Box(content, "MutuallyExclusiveBranches", Color.clear, false);
                    var group = branch.AddComponent<HorizontalLayoutGroup>();
                    group.spacing = 16; group.childControlWidth = true; group.childForceExpandWidth = true;
                    group.childControlHeight = true; group.childForceExpandHeight = false;
                    MapCard(branch.transform, node);
                    MapCard(branch.transform, nodes[++i]);
                    Fit(branch);
                }
                else MapCard(content, node);
            }
        }

        static bool IsBranch(MapNode node) => node != null && node.Id != null && node.Id.StartsWith("N2-", StringComparison.Ordinal);

        static void MapCard(Transform parent, MapNode node)
        {
            var row = Box(parent, "Node_" + node.Id, node.Current ? new Color(0.15f, 0.24f, 0.25f) : Panel, false);
            Vertical(row, 14, 8);
            var status = node.Current ? "▶ 当前" : node.Visited ? "✓ 已完成" : node.State ?? "待抵达";
            Paragraph(row.transform, "NodeTitle", node.Title + "  ·  " + status, 30, node.Current ? Mint : Paper);
            if (!string.IsNullOrEmpty(node.Description)) Paragraph(row.transform, "NodeDescription", node.Description, 25, Muted);
            Fit(row);
        }

        static void ChoiceCard(Transform content, Choice choice, bool actionable)
        {
            var card = Box(content, "Choice_" + choice.Id, choice.Selected ? new Color(0.15f, 0.24f, 0.25f) : Panel, false);
            Vertical(card, 22, 10);
            Paragraph(card.transform, "ChoiceTitle", (choice.Selected ? "✓ " : "") + choice.Title, 34, FamilyColor(choice.Family), true);
            if (!string.IsNullOrEmpty(choice.Description)) Paragraph(card.transform, "ChoiceDescription", choice.Description, 28, Paper);
            if (!string.IsNullOrEmpty(choice.Relationship)) Paragraph(card.transform, "Relationship", choice.Relationship, 25, Mint);
            if (!string.IsNullOrEmpty(choice.Detail))
            {
                var detail = Paragraph(card.transform, "ChoiceDetail", choice.Detail, 25, Muted);
                detail.gameObject.SetActive(false);
                var more = FlowButton(card.transform, "Details_" + choice.Id, "查看效果细节", null, false, 24, 58);
                more.onClick.AddListener(() =>
                {
                    detail.gameObject.SetActive(!detail.gameObject.activeSelf);
                    more.GetComponentInChildren<Text>().text = detail.gameObject.activeSelf ? "收起效果细节" : "查看效果细节";
                    LayoutRebuilder.MarkLayoutForRebuild(card.GetComponent<RectTransform>());
                });
            }
            if (actionable)
            {
                var button = FlowButton(card.transform, "Select_" + choice.Id, choice.Selected ? "已选中" : "选择 · " + choice.Title,
                    choice.Select, true, 29, 84);
                button.interactable = choice.Enabled && choice.Select != null;
            }
            Fit(card);
        }

        static Transform ScrollArea(Transform parent, float top, float bottom)
        {
            var scroll = Box(parent, "ContentScroll", Color.clear, false);
            var sr = scroll.AddComponent<ScrollRect>();
            var rect = scroll.GetComponent<RectTransform>();
            Stretch(rect); rect.offsetMin = new Vector2(28, bottom); rect.offsetMax = new Vector2(-28, -top);
            var viewport = Box(scroll.transform, "Viewport", Color.clear, true);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<RectTransform>().offsetMax = new Vector2(-40, 0);
            viewport.AddComponent<RectMask2D>();
            var content = Box(viewport.transform, "Content", Color.clear, false);
            var cr = content.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1); cr.pivot = new Vector2(0.5f, 1);
            cr.anchoredPosition = Vector2.zero; cr.sizeDelta = Vector2.zero;
            Vertical(content, 0, 16); Fit(content);
            sr.viewport = viewport.GetComponent<RectTransform>(); sr.content = cr;
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
            var track = Box(scroll.transform, "ContentScrollbar", Panel, true);
            var tr = track.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(1, 0); tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(-28, 0); tr.offsetMax = Vector2.zero;
            var handle = Box(track.transform, "ScrollbarHandle", Muted, true);
            Stretch(handle.GetComponent<RectTransform>());
            var scrollbar = track.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.GetComponent<RectTransform>();
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            sr.verticalScrollbar = scrollbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            sr.scrollSensitivity = 65; sr.verticalNormalizedPosition = 1;
            return content.transform;
        }

        static Text Paragraph(Transform parent, string name, string text, int size, Color color, bool bold = false)
        {
            var tx = Label(parent, name, text, size, color, bold);
            var le = tx.gameObject.AddComponent<LayoutElement>();
            le.minHeight = size + 12; le.flexibleWidth = 1;
            // The parent layout uses Text.preferredHeight after assigning the available width.
            // A second height owner on every paragraph would fight the parent's layout pass.
            return tx;
        }

        static void Section(Transform parent, string text) => Paragraph(parent, "Section", text, 28, Gold, true);

        static VerticalLayoutGroup Vertical(GameObject go, int padding, float spacing)
        {
            var group = go.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(padding, padding, padding, padding); group.spacing = spacing;
            group.childControlWidth = true; group.childControlHeight = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = false;
            return group;
        }

        static void Fit(GameObject go) => go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        static void Height(GameObject go, float value) { var le = go.AddComponent<LayoutElement>(); le.minHeight = le.preferredHeight = value; }

        static Button FlowButton(Transform parent, string name, string text, Action action, bool primary, int size, float height)
        {
            var button = MakeButton(parent, name, text, action, primary, size);
            Height(button.gameObject, height);
            return button;
        }

        static Button ButtonAt(Transform parent, string name, string text, float x, float y, float w, float h, Action action, bool primary = false, int size = 29)
        {
            var button = MakeButton(parent, name, text, action, primary, size);
            Place(button.GetComponent<RectTransform>(), x, y, w, h);
            return button;
        }

        static Button MakeButton(Transform parent, string name, string text, Action action, bool primary, int size)
        {
            var go = Box(parent, name, primary ? Mint : Line, true);
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f); colors.pressedColor = new Color(0.75f, 0.8f, 0.85f);
            colors.disabledColor = new Color(0.4f, 0.45f, 0.5f, 0.8f); button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            var tx = Label(go.transform, "Label", text, size, primary ? Ink : Paper, true);
            Stretch(tx.rectTransform, 10, 6); tx.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        static Text LabelAt(Transform parent, string name, string text, int size, Color color, float x, float y, float w, float h, bool bold = false)
        {
            var tx = Label(parent, name, text, size, color, bold);
            Place(tx.rectTransform, x, y, w, h);
            return tx;
        }

        static Text Label(Transform parent, string name, string text, int size, Color color, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var tx = go.GetComponent<Text>(); tx.font = CharacterPresenter.UiFont(); tx.text = text ?? "";
            tx.fontSize = size; tx.color = color; tx.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            tx.alignment = TextAnchor.UpperLeft; tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            tx.verticalOverflow = VerticalWrapMode.Truncate; tx.lineSpacing = 1.1f;
            tx.supportRichText = false; tx.raycastTarget = false;
            return tx;
        }

        static GameObject BoxAt(Transform parent, string name, Color color, float x, float y, float w, float h)
        {
            var go = Box(parent, name, color, false); Place(go.GetComponent<RectTransform>(), x, y, w, h); return go;
        }

        static GameObject Box(Transform parent, string name, Color color, bool hit)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); UiSprites.Apply(image, UiSprites.Round());
            image.color = color; image.raycastTarget = hit;
            return go;
        }

        static Image MeterAt(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var track = BoxAt(parent, name, Line, x, y, w, h);
            var fill = Box(track.transform, "Fill", color, false).GetComponent<Image>();
            Stretch(fill.rectTransform); SetMeter(fill, 0); return fill;
        }

        static void SetMeter(Image fill, float fraction)
        {
            if (fill == null) return;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        }

        static void Stretch(RectTransform rt, float x = 0, float y = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(x, y); rt.offsetMax = new Vector2(-x, -y);
        }

        static string At(string[] items, int index) => items != null && index < items.Length ? items[index] : null;
        static Color FamilyColor(string family) => family == "A" ? Mint : family == "B" ? Gold : family == "C" ? new Color(0.77f, 0.65f, 1f) : Paper;
    }
}
