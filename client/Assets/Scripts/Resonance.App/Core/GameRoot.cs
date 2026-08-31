using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.App
{
    public sealed class GameRoot : MonoBehaviour
    {
        enum ScreenId { Boot, Home, Characters, Team, Stage, Battle, Result, Archive, Library, Deep, Settings }

        public static GameRoot Live { get; private set; }
        public string CurrentScreen => _screen.ToString();
        public SaveBlob SaveData => _save;
        public BattleSim Battle => _battle;
        public bool QteOpen => _hud != null && _hud.QteOpen;
        public string ResultTitle => _resultTitle;
        public bool FeverOn => _battle != null && _battle.FeverActive;
        public bool FeverSeen => _battle != null && _battle.FeverEver;
        public int ActiveStageIndex => _activeStage;

        SaveBlob _save;
        ScreenId _screen = ScreenId.Boot;
        ScreenId _inspectBack = ScreenId.Characters;
        float _bootT;
        BattleSim _battle;
        string _inspectId;
        string _resultTitle;
        Canvas _canvas;
        readonly List<GameObject> _built = new List<GameObject>();
        BattleHud _hud;
        float _simAcc;
        int _stageIndex;
        int _activeStage;
        int _editSlot;
        string _lootLine;
        bool _skillOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (FindFirstObjectByType<GameRoot>() == null)
                new GameObject("GameRoot").AddComponent<GameRoot>();
        }

        void Awake()
        {
            if (Live != null && Live != this)
            {
                Destroy(gameObject);
                return;
            }
            Live = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            _save = SaveStore.LoadOrNew(SavePath());
            EnsureEventSystem();
            BuildCanvas();
            Show(ScreenId.Boot);
            VerticalSliceSmokeRuntime.TrySpawn();
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        public void Go(string screenName)
        {
            Show((ScreenId)Enum.Parse(typeof(ScreenId), screenName));
        }

        public void StartVsBattle()
        {
            _save.UseHard = false;
            StartBattleAt(0);
        }

        public void SetLeader(int slot)
        {
            _save.LeaderSlot = Mathf.Clamp(slot, 0, 4);
            Persist();
            Show(ScreenId.Team);
        }

        public void Inspect(string id)
        {
            _inspectId = id;
            _inspectBack = _screen;
            _skillOpen = false;
            DrawInspect();
        }

        public bool Tap(int slot) => _battle != null && _battle.TryTap(slot);

        public bool Slide(int slot) => _battle != null && _battle.TrySlide(slot);

        public void EnsureAutoOn()
        {
            if (_save.Auto == AutoMode.Full) return;
            _save.Auto = AutoMode.Full;
            if (_battle != null) _battle.Auto = AutoMode.Full;
            Persist();
        }

        public void EnsureSpeed2()
        {
            if (_battle == null || _battle.Speed == 2) return;
            ToggleBattleSpeed();
        }

        public bool FireDrivePerfect()
        {
            return _hud != null && _hud.FirePerfect();
        }

        public void CycleBattleAuto()
        {
            var n = ((int)_save.Auto + 1) % 3;
            _save.Auto = (AutoMode)n;
            if (_battle != null) _battle.Auto = _save.Auto;
            Persist();
        }

        public void ToggleBattleSpeed()
        {
            if (_battle == null) return;
            _battle.Speed = _battle.Speed == 1 ? 2 : 1;
            _save.Speed = _battle.Speed;
            Persist();
        }

        public void ToggleBattlePause()
        {
            if (_battle == null) return;
            _battle.Paused = !_battle.Paused;
        }

        void Update()
        {
            if (_screen == ScreenId.Boot)
            {
                _bootT += Time.unscaledDeltaTime;
                if (_bootT > 1.55f) Show(ScreenId.Home);
                return;
            }
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                && (_screen == ScreenId.Home || _screen == ScreenId.Stage))
            {
                TryStartFromMenu();
                return;
            }
            if (_screen != ScreenId.Battle || _battle == null) return;
            if (_hud != null && _hud.QteOpen)
            {
                _hud.Tick();
                return;
            }
            _simAcc += Time.deltaTime * BattleSim.TickHz;
            var ticks = (int)_simAcc;
            _simAcc -= ticks;
            for (int i = 0; i < ticks; i++)
            {
                if (_battle.Outcome == BattleOutcome.InProgress)
                    _battle.Tick();
                else if (_battle.FeverActive)
                    _battle.TickFeverOnly();
                else
                    break;
            }
            if (_battle.Outcome != BattleOutcome.InProgress && !_battle.FeverActive)
            {
                if (_battle.Outcome == BattleOutcome.Victory)
                    _lootLine = SaveStore.ApplyVictory(_save, _activeStage, _save.UseHard);
                Persist();
                _resultTitle = _battle.Outcome == BattleOutcome.Victory ? "胜利" : "失败";
                if (_battle.Outcome != BattleOutcome.Victory) _lootLine = "";
                Show(ScreenId.Result);
                return;
            }
            if (_hud != null) _hud.Tick();
        }

        string SavePath() => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

        void Persist() => SaveStore.Write(_save, SavePath());

        CharacterDef Grown(string id)
        {
            return Growth.Apply(Catalog.MustChar(id), _save.GetUnit(id));
        }

        int TeamPower()
        {
            var n = 0;
            for (int i = 0; i < 5; i++)
                n += Growth.CombatPower(Grown(_save.PartyIds[i]));
            return n;
        }

        void Show(ScreenId id)
        {
            _screen = id;
            _skillOpen = false;
            ClearUi();
            switch (id)
            {
                case ScreenId.Boot: DrawBoot(); break;
                case ScreenId.Home: DrawHome(); break;
                case ScreenId.Characters: DrawCharacters(); break;
                case ScreenId.Team: DrawTeam(); break;
                case ScreenId.Stage: DrawStage(); break;
                case ScreenId.Battle: DrawBattle(); break;
                case ScreenId.Result: DrawResult(); break;
                case ScreenId.Archive: DrawArchive(); break;
                case ScreenId.Library: DrawLibrary(); break;
                case ScreenId.Deep: DrawDeep(); break;
                case ScreenId.Settings: DrawSettings(); break;
            }
        }

        void DrawBoot()
        {
            CharacterPresenter.CheckerFloor(Root(), _built);
            CharacterPresenter.Embers(Root(), _built, 16);
            var wash = MakeImage("wash", new Vector2(0.5f, 0.58f), new Vector2(720, 720),
                new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.28f));
            UiSprites.Apply(wash, UiSprites.Soft());
            wash.raycastTarget = false;
            wash.gameObject.AddComponent<PulseGlow>().Seed(0.40f, 3.4f, 0f);
            FullLabel("契灵回响", 64, VisualTokens.GoldSelect, new Vector2(0.5f, 0.58f), true);
            FullLabel("点按  ·  上滑  ·  驱动", 28, VisualTokens.TextPrimary, new Vector2(0.5f, 0.48f), true);
            FullLabel("二十五契灵  ·  五色五职", 20, VisualTokens.YellowValue, new Vector2(0.5f, 0.42f), false);
        }

        void DrawHome()
        {
            CharacterPresenter.CheckerFloor(Root(), _built);
            CharacterPresenter.Embers(Root(), _built, 8);
            var lead = _save.PartyIds[Mathf.Clamp(_save.LeaderSlot, 0, 4)];
            var def = Catalog.MustChar(lead);
            _built.Add(CharacterPresenter.DrawStage(Root(), def, _save.GetUnit(lead).SkinId));
            LeftLabel(def.Name, 36, VisualTokens.TextPrimary, new Vector2(0.06f, 0.14f), true);
            GhostBtn("设定", new Vector2(0.90f, 0.94f), () => Show(ScreenId.Settings));
            Nav();
        }

        void DrawArchive()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            FullLabel("图录", 28, VisualTokens.GoldTitle, new Vector2(0.5f, 0.965f), false);
            FullLabel("二十五契灵  ·  五色五职", 18, VisualTokens.YellowValue, new Vector2(0.5f, 0.93f), false);
            DrawElementRoleGrid(0.80f, 0.16f, new Vector2(168, 196), Inspect);
            CloseX(ScreenId.Home);
        }

        void DrawLibrary()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            FullLabel("书库", 28, VisualTokens.GoldTitle, new Vector2(0.5f, 0.965f), false);
            FullLabel("点开看  点按 / 上滑 / 驱动  ·  无抽卡", 18, VisualTokens.YellowValue, new Vector2(0.5f, 0.93f), false);
            DrawElementRoleGrid(0.80f, 0.16f, new Vector2(168, 188), Inspect);
            CloseX(ScreenId.Home);
        }

        void DrawSettings()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            FullLabel("设定", 28, VisualTokens.GoldTitle, new Vector2(0.5f, 0.88f), false);
            FullLabel("只改本机  ·  不联网", 18, VisualTokens.TextMuted, new Vector2(0.5f, 0.83f), false);
            Confirm(_save.Auto == AutoMode.Manual ? "自动  关"
                : _save.Auto == AutoMode.Semi ? "半自动" : "全自动", new Vector2(0.5f, 0.68f), () =>
            {
                CycleBattleAuto();
                Show(ScreenId.Settings);
            });
            Confirm(_save.Speed == 2 ? "倍速  2×" : "倍速  1×", new Vector2(0.5f, 0.56f), () =>
            {
                _save.Speed = _save.Speed == 1 ? 2 : 1;
                Persist();
                Show(ScreenId.Settings);
            });
            FullLabel("契灵回响  MVP v0.1.0", 18, VisualTokens.TextSecondary, new Vector2(0.5f, 0.42f), false);
            GhostBtn("清除本地存档", new Vector2(0.5f, 0.28f), () =>
            {
                _save = SaveStore.Reset(SavePath());
                Show(ScreenId.Home);
            });
            Confirm("回首页", new Vector2(0.5f, 0.16f), () => Show(ScreenId.Home));
        }

        void DrawDeep()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            FullLabel("深途", 28, VisualTokens.GoldTitle, new Vector2(0.5f, 0.88f), false);
            var n = _save.ClearedCount;
            if (n < 12)
            {
                FullLabel("裂口还没走完\n普通 " + n + " / 12\n先去「关卡」封上城门守核", 24, VisualTokens.TextPrimary, new Vector2(0.5f, 0.55f), false);
                Confirm("去关卡", new Vector2(0.5f, 0.22f), () => Show(ScreenId.Stage));
            }
            else
            {
                FullLabel("第1章裂口已封\n困难关在「关卡」里切换\n更深的层仍关闭（不做星云/公会）", 24, VisualTokens.TextPrimary, new Vector2(0.5f, 0.55f), false);
                Confirm("去困难", new Vector2(0.5f, 0.22f), () =>
                {
                    _save.UseHard = true;
                    Persist();
                    Show(ScreenId.Stage);
                });
            }
            CloseX(ScreenId.Home);
        }

        void IdentityBlock(CharacterDef def, UnitProgress prog, Vector2 anchor, bool compact = false, bool plate = false)
        {
            if (plate)
            {
                var plateImg = MakeImage("idplate", anchor + new Vector2(0.20f, compact ? -0.06f : -0.10f),
                    new Vector2(500, compact ? 280 : 360),
                    new Color(0.04f, 0.03f, 0.03f, 0.90f));
                UiSprites.Apply(plateImg, UiSprites.Round());
                plateImg.raycastTarget = false;
            }
            var grown = Growth.Apply(def, prog);
            LeftLabel(def.Name, compact ? 40 : 48, VisualTokens.TextPrimary, anchor, true);
            LeftLabel(CharacterPresenter.RoleLine(def), 22, CharacterPresenter.ElementColor(def.Element),
                anchor + new Vector2(0f, compact ? -0.040f : -0.048f), true);
            if (compact)
            {
                LeftLabel("战力  " + Growth.CombatPower(grown), 32, VisualTokens.YellowValue,
                    anchor + new Vector2(0f, -0.082f), false);
                return;
            }
            LeftLabel(CharacterPresenter.Flavor(def.Id), 20, VisualTokens.TextSecondary, anchor + new Vector2(0f, -0.090f), true);
            LeftLabel(StarLine(def, prog), 22, VisualTokens.StarEvolved, anchor + new Vector2(0f, -0.128f), false);
            LeftLabel("战力  " + Growth.CombatPower(grown), 36, VisualTokens.YellowValue, anchor + new Vector2(0f, -0.172f), false);
            LeftLabel("生命 " + grown.Hp + "   攻击 " + grown.Atk + "   防御 " + grown.Def,
                22, VisualTokens.TextStat, anchor + new Vector2(0f, -0.214f), false);
        }

        static string StarLine(CharacterDef def, UnitProgress prog)
        {
            var n = def.NativeStar + (prog != null && prog.Uncap > 0 ? 1 : 0);
            if (n > def.MaxStar) n = def.MaxStar;
            var s = "";
            for (int i = 0; i < n; i++) s += "★";
            return s;
        }

        void HexRail()
        {
            var labels = new[] { "设", "编", "图", "关" };
            ScreenId[] jumps = { ScreenId.Settings, ScreenId.Team, ScreenId.Archive, ScreenId.Stage };
            for (int i = 0; i < 4; i++)
            {
                var y = 0.88f - i * 0.09f;
                var dest = jumps[i];
                var go = MakeButton(labels[i], new Vector2(0.93f, y), new Vector2(88, 88), VisualTokens.RailIcon, () => Show(dest));
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, UiSprites.Hex());
                img.color = VisualTokens.RailFill;
            }
        }

        void DrawCharacters()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            FullLabel("契灵", 28, VisualTokens.GoldTitle, new Vector2(0.5f, 0.965f), false);
            FullLabel("五色五职  ·  战力 " + TeamPower(), 22, VisualTokens.YellowValue, new Vector2(0.5f, 0.93f), false);
            DrawElementRoleGrid(0.80f, 0.22f, new Vector2(168, 176), Inspect);
            Confirm("编队", new Vector2(0.5f, 0.125f), () => Show(ScreenId.Team));
            CloseX(ScreenId.Home);
        }

        void DrawTeam()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            FullLabel("编队", 28, VisualTokens.GoldTitle, new Vector2(0.5f, 0.965f), false);
            FullLabel("点选槽位换人 · 委任队长", 18, VisualTokens.TextSecondary, new Vector2(0.5f, 0.93f), false);
            DrawPartyRow(0.84f, true, 118f, 148f);
            var sel = Catalog.MustChar(_save.PartyIds[_editSlot]);
            FullLabel(sel.Name + "  战力 " + Growth.CombatPower(Grown(sel.Id)), 22, VisualTokens.YellowValue, new Vector2(0.38f, 0.72f), false);
            GhostBtn("詳細", new Vector2(0.78f, 0.72f), () => Inspect(_save.PartyIds[_editSlot]));
            GhostBtn("队长", new Vector2(0.22f, 0.72f), () => SetLeader(_editSlot));
            DrawElementRoleGrid(0.60f, 0.14f, new Vector2(156, 148), id =>
            {
                _save.SetPartySlot(_editSlot, id);
                Persist();
                Show(ScreenId.Team);
            });
            Nav();
        }

        void DrawPartyRow(float y, bool selectable, float chipW, float chipH)
        {
            for (int i = 0; i < 5; i++)
            {
                var id = _save.PartyIds[i];
                var def = Catalog.MustChar(id);
                var lv = _save.GetUnit(id).Level;
                var selected = selectable && i == _editSlot;
                var slot = i;
                var chip = CharacterPresenter.DrawChip(Root(), def,
                    new Vector2(0.12f + i * 0.19f, y), new Vector2(chipW, chipH), selected, i == _save.LeaderSlot, lv);
                var btn = chip.AddComponent<Button>();
                btn.targetGraphic = chip.GetComponent<Image>();
                btn.onClick.AddListener(() =>
                {
                    if (!selectable)
                    {
                        Inspect(id);
                        return;
                    }
                    _editSlot = slot;
                    Show(ScreenId.Team);
                });
                _built.Add(chip);
            }
        }

        void DrawElementRoleGrid(float topY, float botY, Vector2 tile, System.Action<string> onClick)
        {
            var roles = new[] { "攻", "防", "扰", "疗", "辅" };
            var els = new[] { "火", "水", "木", "光", "暗" };
            var stepY = (topY - botY) / 4f;
            const float x0 = 0.145f;
            const float dx = 0.172f;
            for (int r = 0; r < 5; r++)
                FullLabel(roles[r], 16, VisualTokens.TextMuted, new Vector2(x0 + r * dx, topY + 0.042f), false);
            var ids = Catalog.MatrixIds;
            for (int e = 0; e < 5; e++)
            {
                var y = topY - e * stepY;
                FullLabel(els[e], 18, CharacterPresenter.ElementColor((Element)e), new Vector2(0.048f, y), false);
                for (int r = 0; r < 5; r++)
                {
                    var id = ids[e * 5 + r];
                    if (string.IsNullOrEmpty(id) || !Catalog.Characters.ContainsKey(id)) continue;
                    var def = Catalog.MustChar(id);
                    var captured = id;
                    var marked = false;
                    if (_save.PartyIds != null)
                    {
                        for (int p = 0; p < _save.PartyIds.Length; p++)
                            if (_save.PartyIds[p] == id) { marked = true; break; }
                    }
                    var cell = CharacterPresenter.DrawTile(Root(), def,
                        new Vector2(x0 + r * dx, y), tile, true, marked);
                    cell.GetComponent<Button>().onClick.AddListener(() => onClick(captured));
                    _built.Add(cell);
                }
            }
        }

        void DrawInspect()
        {
            ClearUi();
            CharacterPresenter.CheckerFloor(Root(), _built);
            CharacterPresenter.Embers(Root(), _built);
            var id = _inspectId ?? Catalog.DefaultParty[0];
            var def = Catalog.MustChar(id);
            var prog = _save.GetUnit(id);
            var grown = Growth.Apply(def, prog);
            var br = Growth.BreakDown(def, prog);
            _built.Add(CharacterPresenter.DrawStage(Root(), def, prog.SkinId));
            IdentityBlock(def, prog, new Vector2(0.06f, 0.62f), false, false);
            FullLabel("契体 " + br.BodyAtk + "  好感 +" + br.AffAtk + "  装备 +" + br.GearAtk,
                18, VisualTokens.TextSecondary, new Vector2(0.5f, 0.355f), false);

            DrawWells(prog);

            var pips = Growth.IgnitionPips(prog.Ignition);
            FullLabel("燃起  " + pips + " / 6", 20, VisualTokens.GoldTitle, new Vector2(0.22f, 0.235f), false);
            for (int i = 0; i < 6; i++)
            {
                var on = i < pips;
                var pip = MakeImage("pip", new Vector2(0.40f + i * 0.08f, 0.235f), new Vector2(56, 56),
                    on ? VisualTokens.GoldSelect : VisualTokens.SlotWell);
                UiSprites.Apply(pip, UiSprites.Circle());
                pip.raycastTarget = false;
            }
            GhostBtn("点亮", new Vector2(0.90f, 0.235f), () =>
            {
                prog.Ignition = Growth.CycleIgnition(prog.Ignition, def.IgnitionMax);
                Persist();
                DrawInspect();
            });

            FullLabel("技能预约   T=点按   S=上滑", 16, VisualTokens.TextMuted, new Vector2(0.5f, 0.188f), false);
            var rsv = Growth.NormalizedReserve(prog.Reserve);
            for (int i = 0; i < 5; i++)
            {
                var slot = i;
                var x = 0.14f + i * 0.18f;
                var mark = rsv[i].ToString();
                var fill = mark == "S" ? VisualTokens.SlideGreen : mark == "T" ? VisualTokens.YellowConfirm : VisualTokens.SlotWell;
                var go = MakeButton(mark, new Vector2(x, 0.145f), new Vector2(88, 88),
                    mark == "E" ? VisualTokens.TextMuted : VisualTokens.TextOnYellow, () =>
                    {
                        prog.Reserve = Growth.CycleReserveSlot(prog.Reserve, slot);
                        Persist();
                        DrawInspect();
                    });
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, UiSprites.Circle());
                img.color = fill;
                go.GetComponentInChildren<Text>().fontSize = 32;
            }

            GhostBtn("LV-", new Vector2(0.10f, 0.072f), () =>
            {
                prog.Level = Mathf.Max(1, prog.Level - 1);
                Persist();
                DrawInspect();
            });
            FullLabel("LV " + prog.Level, 18, VisualTokens.YellowValue, new Vector2(0.22f, 0.072f), false);
            GhostBtn("LV+", new Vector2(0.34f, 0.072f), () =>
            {
                prog.Level = Mathf.Min(Growth.MaxLevel, prog.Level + 1);
                Persist();
                DrawInspect();
            });
            GhostBtn("突破+" + prog.Uncap, new Vector2(0.50f, 0.072f), () =>
            {
                prog.Uncap = prog.Uncap >= def.UncapMax ? 0 : prog.Uncap + 1;
                Persist();
                DrawInspect();
            });
            GhostBtn("好感" + Growth.AffectionRank(prog.Affection), new Vector2(0.66f, 0.072f), () =>
            {
                prog.Affection = Growth.CycleAffection(prog.Affection);
                Persist();
                DrawInspect();
            });
            GhostBtn(SkinCatalog.Label(prog.SkinId), new Vector2(0.84f, 0.072f), () =>
            {
                prog.SkinId = SkinCatalog.Cycle(prog.SkinId);
                Persist();
                DrawInspect();
            });

            Confirm("技能", new Vector2(0.78f, 0.028f), ToggleSkill);
            GhostBtn("编入", new Vector2(0.22f, 0.028f), () =>
            {
                _save.SetPartySlot(_editSlot, id);
                Persist();
                Show(ScreenId.Team);
            });
            GhostBtn("返回", new Vector2(0.50f, 0.028f), () => Show(_inspectBack));
            GhostBtn("×", new Vector2(0.93f, 0.95f), () => Show(_inspectBack));
            if (_skillOpen) DrawSkillModal(def, grown);
        }

        void DrawWells(UnitProgress prog)
        {
            var gears = new[] { prog.Gear0, prog.Gear1, prog.Gear2, prog.Gear3 };
            for (int i = 0; i < 4; i++)
            {
                var slot = i;
                var x = 0.16f + i * 0.22f;
                FullLabel(GearCatalog.SlotNames[i], 16, VisualTokens.TextMuted, new Vector2(x, 0.325f), false);
                var filled = !string.IsNullOrEmpty(gears[i]);
                var go = MakeButton(filled ? GearCatalog.Label(gears[i]) : "空", new Vector2(x, 0.285f), new Vector2(150, 88),
                    filled ? VisualTokens.YellowValue : VisualTokens.TextMuted, () =>
                    {
                        var next = GearCatalog.CycleSlot(slot, SlotGear(prog, slot));
                        SetSlotGear(prog, slot, next);
                        Persist();
                        DrawInspect();
                    });
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, UiSprites.Round());
                img.color = filled ? new Color(0.18f, 0.14f, 0.06f, 1f) : VisualTokens.SlotWell;
                go.GetComponentInChildren<Text>().fontSize = 18;
            }
        }

        static string SlotGear(UnitProgress p, int slot)
        {
            switch (slot)
            {
                case 0: return p.Gear0;
                case 1: return p.Gear1;
                case 2: return p.Gear2;
                default: return p.Gear3;
            }
        }

        static void SetSlotGear(UnitProgress p, int slot, string id)
        {
            switch (slot)
            {
                case 0: p.Gear0 = id; break;
                case 1: p.Gear1 = id; break;
                case 2: p.Gear2 = id; break;
                default: p.Gear3 = id; break;
            }
        }

        void ToggleSkill()
        {
            _skillOpen = !_skillOpen;
            DrawInspect();
        }

        void DrawSkillModal(CharacterDef def, CharacterDef grown)
        {
            var dim = MakeImage("dim", new Vector2(0.5f, 0.5f), new Vector2(1080, 1920), VisualTokens.OverlayDim);
            dim.raycastTarget = true;
            PanelBox("技能", new Vector2(0.5f, 0.52f), new Vector2(860, 720));
            var lines = KitLine("连击", def.AutoSkillId, def, grown)
                + "\n" + KitLine("点按", def.TapSkillId, def, grown)
                + "\n" + KitLine("上滑", def.SlideSkillId, def, grown)
                + "\n" + KitLine("驱动", def.DriveSkillId, def, grown)
                + "\n" + KitLine("队长", def.LeaderSkillId, def, grown)
                + "\n生命 " + grown.Hp + "  攻击 " + grown.Atk + "  战力 " + Growth.CombatPower(grown);
            FullLabel(lines, 22, VisualTokens.TextStat, new Vector2(0.5f, 0.52f), false);
            Confirm("关闭", new Vector2(0.5f, 0.26f), ToggleSkill);
        }

        static string KitLine(string tag, string skillId, CharacterDef def, CharacterDef grown)
        {
            var s = Catalog.MustSkill(skillId);
            var extra = "";
            if (s.AtkCoef > 0f || s.FlatPower > 0)
            {
                var dmg = DamageMath.ComputeSkill(s.Type, grown.Atk, s.AtkCoef, s.FlatPower, 650, def.Element, Element.Wood, false, 1f, 1f);
                extra = "  ~" + dmg;
            }
            else if (s.HealCoef > 0f || s.FlatHeal > 0)
                extra = "  治疗";
            return tag + "  " + s.Name + extra;
        }

        void DrawStage()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            FullLabel("第1章  废都裂口", 30, VisualTokens.GoldTitle, new Vector2(0.5f, 0.96f), false);
            GhostBtn(_save.UseHard ? "普通" : "普通·", new Vector2(0.28f, 0.915f), () =>
            {
                _save.UseHard = false;
                Persist();
                Show(ScreenId.Stage);
            });
            GhostBtn(_save.UseHard ? "困难·" : "困难", new Vector2(0.72f, 0.915f), () =>
            {
                _save.UseHard = true;
                Persist();
                Show(ScreenId.Stage);
            });
            var table = Catalog.Chapter(_save.UseHard);
            var cleared = _save.UseHard ? _save.ClearedHard : _save.ClearedCount;
            FullLabel((_save.UseHard ? "困难 " : "普通 ") + cleared + " / 12", 20, VisualTokens.YellowValue, new Vector2(0.5f, 0.875f), false);
            for (int i = 0; i < table.Length; i++)
            {
                var idx = i;
                var locked = StageLocked(idx);
                var selected = i == _stageIndex;
                var st = table[i];
                var raw = st.Name ?? "";
                var sp = raw.LastIndexOf(' ');
                var shortName = sp >= 0 && sp + 1 < raw.Length ? raw.Substring(sp + 1) : raw;
                var label = (locked ? "锁\n" : (i + 1).ToString("00") + "\n") + shortName;
                var col = i % 3;
                var row = i / 3;
                var go = MakeButton(label, new Vector2(0.18f + col * 0.32f, 0.76f - row * 0.125f), new Vector2(300, 150),
                    locked ? VisualTokens.TextMuted : VisualTokens.TextPrimary, () =>
                    {
                        if (locked) return;
                        _stageIndex = idx;
                        Show(ScreenId.Stage);
                    });
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, UiSprites.Round());
                img.color = selected ? new Color(0.18f, 0.14f, 0.04f) : VisualTokens.PanelFill;
                var tx = go.GetComponentInChildren<Text>();
                if (tx != null) tx.fontSize = 20;
                if (selected)
                {
                    var ol = go.AddComponent<Outline>();
                    ol.effectColor = VisualTokens.GoldSelect;
                    ol.effectDistance = new Vector2(2, -2);
                }
            }
            DrawWavePreview(table[Mathf.Clamp(_stageIndex, 0, table.Length - 1)]);
            var pick = table[Mathf.Clamp(_stageIndex, 0, table.Length - 1)];
            FullLabel((pick != null ? pick.Name : "") + "  ·  PHASE 1/2", 18, VisualTokens.TextSecondary, new Vector2(0.5f, 0.138f), false);
            Confirm("战斗开始", new Vector2(0.5f, 0.078f), () => StartBattleAt(_stageIndex));
            CloseX(ScreenId.Home);
        }

        bool StageLocked(int index) => _save.IsStageLocked(index);

        void DrawWavePreview(StageDef st)
        {
            if (st == null || st.Wave0 == null) return;
            FullLabel("本关敌人（前波）", 16, VisualTokens.TextMuted, new Vector2(0.22f, 0.255f), false);
            var n = Math.Min(5, st.Wave0.Length);
            for (int i = 0; i < n; i++)
            {
                var id = st.Wave0[i];
                if (!Catalog.Characters.ContainsKey(id)) continue;
                var def = Catalog.MustChar(id);
                var chip = CharacterPresenter.DrawChip(Root(), def, new Vector2(0.16f + i * 0.17f, 0.195f),
                    new Vector2(110, 128), false, false, 1);
                chip.GetComponent<Image>().raycastTarget = false;
                _built.Add(chip);
            }
            if (st.Wave1 != null && st.Wave1.Length > 0 && Catalog.Characters.ContainsKey(st.Wave1[0]))
                FullLabel("第二波  BOSS  " + Catalog.MustChar(st.Wave1[0]).Name, 16, VisualTokens.YellowValue, new Vector2(0.5f, 0.155f), false);
        }

        void TryStartFromMenu()
        {
            if (_screen == ScreenId.Stage)
            {
                StartBattleAt(_stageIndex);
                return;
            }
            var n = _save.ClearedCount;
            StartBattleAt(n > 11 ? 11 : n);
        }

        void StartBattleAt(int index)
        {
            var table = Catalog.Chapter(_save.UseHard);
            if (table == null || table.Length == 0)
            {
                table = Catalog.Stages;
                _save.UseHard = false;
            }
            if (index < 0) index = 0;
            if (index >= table.Length) index = table.Length - 1;
            if (StageLocked(index))
            {
                while (index > 0 && StageLocked(index)) index--;
            }
            if (StageLocked(index)) return;
            _activeStage = index;
            _save.LastSeed = unchecked(Environment.TickCount);
            var stage = table[index];
            _battle = new BattleSim(_save.PartyIds, _save.LeaderSlot, _save.LastSeed, stage, _save.ProgressForParty())
            {
                Speed = _save.Speed,
                Auto = _save.Auto,
                Deterministic = false
            };
            _simAcc = 0f;
            Show(ScreenId.Battle);
        }

        void DrawBattle()
        {
            _hud = new BattleHud(this, Root(), _built);
            _hud.Build(_activeStage);
        }

        void DrawResult()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            MakeImage("dim", new Vector2(0.5f, 0.5f), new Vector2(1080, 1920), VisualTokens.OverlayDim);
            var win = _resultTitle == "胜利";
            FullLabel(win ? "胜利" : "失败", 56, win ? VisualTokens.GoldSelect : VisualTokens.StarEvolved, new Vector2(0.5f, 0.64f), true);
            var cleared = _save.UseHard ? _save.ClearedHard : _save.ClearedCount;
            var track = _save.UseHard ? "困难" : "普通";
            FullLabel(track + "  " + cleared + " / 12  已写入本地", 22, VisualTokens.TextPrimary, new Vector2(0.5f, 0.54f), false);
            if (!string.IsNullOrEmpty(_lootLine))
                FullLabel("掉落  " + _lootLine + "    全队好感 +4", 22, VisualTokens.YellowValue, new Vector2(0.5f, 0.46f), false);
            else
                FullLabel(win ? "掉落  无额外装备    全队好感 +4" : "掉落  无", 20, VisualTokens.TextMuted, new Vector2(0.5f, 0.46f), false);
            if (_battle != null)
            {
                int tap = 0, slide = 0, drive = 0, auto = 0;
                for (int i = 0; i < _battle.Casts.Count; i++)
                {
                    var fx = _battle.Casts[i];
                    if (!fx.CasterAlly || fx.Fever) continue;
                    if (fx.Type == SkillType.Slide) slide++;
                    else if (fx.Type == SkillType.Drive) drive++;
                    else if (fx.Type == SkillType.Tap) tap++;
                    else if (fx.Type == SkillType.Auto) auto++;
                }
                FullLabel("连击 " + auto + "   点按 " + tap + "   上滑 " + slide + "   驱动 " + drive,
                    20, VisualTokens.TextSecondary, new Vector2(0.5f, 0.38f), false);
                FullLabel(_battle.FeverEver ? "狂热时间  有" : "狂热时间  无",
                    22, _battle.FeverEver ? VisualTokens.YellowValue : VisualTokens.TextMuted, new Vector2(0.5f, 0.335f), false);
                if (tap + slide + drive == 0)
                    FullLabel("点按头像  ·  上滑头像  ·  Drive 满了点翼标", 18, VisualTokens.YellowValue, new Vector2(0.5f, 0.29f), false);
            }
            var y = 0.20f;
            if (win && cleared > 0 && cleared < 12)
            {
                Confirm("下一关", new Vector2(0.5f, y), () =>
                {
                    _stageIndex = cleared;
                    StartBattleAt(_stageIndex);
                });
                y -= 0.075f;
            }
            Confirm("再战", new Vector2(0.5f, y), () => StartBattleAt(_activeStage));
            Confirm("回首页", new Vector2(0.5f, y - 0.075f), () => Show(ScreenId.Home));
        }

        void DrawStub(string title, string body)
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            FullLabel(title, 40, VisualTokens.GoldTitle, new Vector2(0.5f, 0.62f), true);
            FullLabel(body, 24, VisualTokens.TextSecondary, new Vector2(0.5f, 0.48f), false);
            CloseX(ScreenId.Home);
        }

        void Nav()
        {
            Tab("首页", 0, ScreenId.Home);
            Tab("契灵", 1, ScreenId.Characters);
            Tab("关卡", 2, ScreenId.Stage);
            Tab("图录", 3, ScreenId.Archive);
            Tab("书库", 4, ScreenId.Library);
            Tab("深途", 5, ScreenId.Deep);
        }

        void CloseX(ScreenId back)
        {
            GhostBtn("×", new Vector2(0.93f, 0.95f), () => Show(back));
        }

        void Tab(string label, int index, ScreenId id)
        {
            var on = TabLit(id);
            var x = (index + 0.5f) / 6f;
            var go = MakeButton(label, new Vector2(x, 0.042f), new Vector2(160, 78),
                on ? VisualTokens.TextPrimary : VisualTokens.GoldMetal, () => Show(id));
            var img = go.GetComponent<Image>();
            img.color = Color.clear;
            if (on)
            {
                var glow = MakeImage("glow", new Vector2(x, 0.055f), new Vector2(90, 90), new Color(1f, 0.8f, 0f, 0.18f));
                UiSprites.Apply(glow, UiSprites.Soft());
                glow.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
            }
            var mark = MakeImage("ico", new Vector2(x, 0.070f), new Vector2(42, 42),
                on ? VisualTokens.YellowNavOn : VisualTokens.GoldMetal);
            UiSprites.Apply(mark, UiSprites.Stamp(index));
            var tx = go.GetComponentInChildren<Text>();
            tx.fontSize = 18;
            var trt = tx.rectTransform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 0.55f);
            go.transform.SetAsLastSibling();
        }

        bool TabLit(ScreenId id)
        {
            if (id == ScreenId.Characters)
                return _screen == ScreenId.Characters || _screen == ScreenId.Team;
            if (id == ScreenId.Home)
                return _screen == ScreenId.Home || _screen == ScreenId.Settings;
            return _screen == id;
        }

        void Confirm(string label, Vector2 anchor, UnityEngine.Events.UnityAction click)
        {
            var go = MakeButton(label, anchor, new Vector2(320, 84), VisualTokens.TextOnYellow, click);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Pill());
            img.color = VisualTokens.YellowConfirm;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = VisualTokens.TextOnYellow;
            ol.effectDistance = new Vector2(2, -2);
        }

        void GhostBtn(string label, Vector2 anchor, UnityEngine.Events.UnityAction click)
        {
            var go = MakeButton(label, anchor, new Vector2(140, 56), VisualTokens.TextPrimary, click);
            go.GetComponent<Image>().color = Color.clear;
            var tx = go.GetComponentInChildren<Text>();
            var ol = tx.gameObject.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2, -2);
        }

        Text PillKeep(string label, Vector2 anchor, UnityEngine.Events.UnityAction click)
        {
            var go = MakeButton(label, anchor, new Vector2(200, 72), VisualTokens.TextOnYellow, click);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Pill());
            img.color = VisualTokens.YellowConfirm;
            return go.GetComponentInChildren<Text>();
        }

        GameObject PanelBox(string label, Vector2 anchor, Vector2 size)
        {
            var go = MakeButton(label, anchor, size, VisualTokens.GoldTitle, null);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = VisualTokens.PanelFill;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(2, -2);
            return go;
        }

        Image MakeImage(string name, Vector2 anchor, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root(), false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            _built.Add(go);
            return img;
        }

        GameObject MakeButton(string label, Vector2 anchor, Vector2 size, Color textColor, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(Root(), false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = VisualTokens.PanelFill;
            var btn = go.GetComponent<Button>();
            if (click != null) btn.onClick.AddListener(click);
            var tgo = new GameObject("t", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8, 6);
            trt.offsetMax = new Vector2(-8, -6);
            var tx = tgo.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = textColor;
            tx.fontSize = 24;
            tx.text = label;
            go.AddComponent<ButtonPress>();
            _built.Add(go);
            return go;
        }

        Text LeftLabel(string text, int size, Color color, Vector2 anchor, bool outline)
        {
            var tx = FullLabel(text, size, color, anchor, outline);
            var rt = tx.rectTransform;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(640, 90);
            tx.alignment = TextAnchor.MiddleLeft;
            return tx;
        }

        Text FullLabel(string text, int size, Color color, Vector2 anchor, bool outline)
        {
            var go = new GameObject("label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(Root(), false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(1000, 160);
            rt.anchoredPosition = Vector2.zero;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.fontSize = size;
            tx.text = text;
            tx.raycastTarget = false;
            if (outline)
            {
                var ol = go.AddComponent<Outline>();
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(3f, -3f);
            }
            _built.Add(go);
            return tx;
        }

        void ClearUi()
        {
            if (_canvas != null)
            {
                var t = _canvas.transform;
                for (int i = t.childCount - 1; i >= 0; i--)
                    DestroyImmediate(t.GetChild(i).gameObject);
            }
            _built.Clear();
            _hud = null;
        }

        Transform Root() => _canvas.transform;

        void BuildCanvas()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.backgroundColor = VisualTokens.BgVoid;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.depth = -100;
            cam.enabled = true;
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 0.5f;
            go.AddComponent<CanvasShake>();
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }
    }

    public sealed class PortraitGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public Action OnTap;
        public Action OnSlide;
        Vector2 _start;
        float _t;
        bool _held;

        public void OnPointerDown(PointerEventData e)
        {
            _start = e.position;
            _t = Time.unscaledTime;
            _held = true;
        }

        public void OnDrag(PointerEventData e) { }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_held) return;
            _held = false;
            var dy = e.position.y - _start.y;
            if (dy > 80f) OnSlide?.Invoke();
            else OnTap?.Invoke();
        }
    }
}
