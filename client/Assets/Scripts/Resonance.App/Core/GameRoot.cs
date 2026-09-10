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
        enum ScreenId { Boot, Home, Characters, Team, Stage, Battle, Result, Archive, Library, Deep, Settings, Summon, Daily, Shop, Mail, Achieve, Title, Friend, Rest, Food, Pvp, Tutorial, Costume }

        public static GameRoot Live { get; private set; }
        public string CurrentScreen => _screen.ToString();
        public SaveBlob SaveData => _save;
        public BattleSim Battle => _battle;
        public bool QteOpen => _hud != null && _hud.QteOpen;
        public bool DriveSelectVisible => _hud != null && _hud.DriveSelectVisible;
        public BattleCueCopy.Kind VisibleStamp => _hud != null ? _hud.VisibleStamp : BattleCueCopy.Kind.None;
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
        bool _costumeOpen;
        bool _ignitionOpen;
        bool _equipNotice;
        bool _moreOpen;
        int _tutorialPage;

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

        void OnApplicationQuit() => Persist();

        void OnApplicationPause(bool paused)
        {
            if (paused) Persist();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) Persist();
        }

        void OnDestroy()
        {
            if (Live == this)
            {
                Persist();
                Live = null;
            }
        }

        public void Go(string screenName)
        {
            ScreenId id;
            if (!Enum.TryParse(screenName, true, out id)) return;
            Show(id);
        }

        public void StartVsBattle()
        {
            _save.UseHard = false;
            StartBattleAt(0);
        }

        public void SetLeader(int slot)
        {
            _save.LeaderSlot = _save.ClampLeaderSlot(slot);
            Persist();
            Show(ScreenId.Team);
        }

        public void Inspect(string id)
        {
            if (string.IsNullOrEmpty(id) || Catalog.Characters == null || !Catalog.Characters.ContainsKey(id))
                return;
            _inspectId = id;
            _inspectBack = _screen;
            _skillOpen = false;
            _costumeOpen = false;
            _ignitionOpen = false;
            _equipNotice = false;
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
            if (_hud != null) return _hud.FirePerfect();
            return SliceDriveSequence.TryFirePerfect(_battle);
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
            if (_battle == null || _screen != ScreenId.Battle) return;
            if (_battle.Paused)
            {
                _battle.Paused = false;
                var overlay = Root().Find("PauseBoard");
                if (overlay != null) DestroyImmediate(overlay.gameObject);
                return;
            }
            _battle.Paused = true;
            PauseBoard.Draw(Root(), ToggleBattlePause, () => Show(ScreenId.Home));
        }

        void Update()
        {
            if (_screen == ScreenId.Boot)
                return;
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                && _screen == ScreenId.Stage)
            {
                TryStartFromMenu();
                return;
            }
            if (_screen != ScreenId.Battle || _battle == null) return;
            _simAcc += Time.deltaTime * BattleSim.TickHz;
            var ticks = (int)_simAcc;
            _simAcc -= ticks;
            if (ticks > BattleSim.TickHz * 2) ticks = BattleSim.TickHz * 2;
            for (int i = 0; i < ticks; i++)
            {
                if (_battle.Outcome == BattleOutcome.InProgress)
                    _battle.Tick();
                else if (_battle.FeverActive)
                    _battle.TickFeverOnly();
                else
                    break;
            }
            if (_battle.Settled)
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

        string SavePath() => SaveStore.DefaultPath;

        void Persist()
        {
            if (_save == null) return;
            Costume.WriteTo(_save.Skins);
            try { SaveStore.Write(_save, SavePath()); }
            catch { }
        }

        string PullSummon()
        {
            if (_save == null) return "";
            if (!_save.SpendStone(1) && !_save.SpendGold(300)) return "";
            var ids = Catalog.PlayableIds;
            var pool = new List<string>();
            if (ids != null)
            {
                for (int i = 0; i < ids.Length; i++)
                    if (!string.IsNullOrEmpty(ids[i]) && !_save.Owns(ids[i])) pool.Add(ids[i]);
            }
            if (pool.Count == 0)
            {
                _save.AddGold(200);
                Persist();
                return "";
            }
            var pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            _save.Grant(pick);
            Persist();
            return pick;
        }

        void ClaimDaily(int index)
        {
            if (_save == null || index < 0 || index > 4) return;
            var bit = 1 << index;
            if ((_save.DailyClaimed & bit) != 0) return;
            if (index == 0) _save.AddGold(80);
            else if (index == 1) _save.AddGold(40);
            else if (index == 2) _save.AddGold(40);
            else if (index == 3) _save.AddGold(30);
            else _save.AddStone(1);
            _save.DailyClaimed |= bit;
            Persist();
            Show(ScreenId.Daily);
        }

        void BuyShop(int index)
        {
            if (_save == null) return;
            var lead = _save.LeaderId();
            var u = _save.GetUnit(lead);
            if (index == 0)
            {
                if (!_save.SpendStone(80)) return;
                _save.AddGold(12000);
            }
            else if (index == 1)
            {
                if (!_save.SpendGold(200)) return;
                _save.AddStone(180);
            }
            else if (index == 2)
            {
                if (!_save.SpendGold(110)) return;
                if (u != null && u.Level < Growth.MaxLevel) u.Level++;
            }
            else if (index == 3)
            {
                if (!_save.SpendGold(150)) return;
                if (u != null) u.Affection = Mathf.Min(100, u.Affection + 40);
            }
            else return;
            Persist();
            Show(ScreenId.Shop);
        }

        void ReadMail(int index)
        {
            if (_save == null || index < 0 || index > 2) return;
            var bit = 1 << index;
            if ((_save.MailRead & bit) != 0) return;
            if (index == 0)
            {
                _save.AddGold(200);
                _save.AddStone(2);
            }
            else if (index == 1) _save.AddGold(80);
            _save.MailRead |= bit;
            Persist();
            Show(ScreenId.Mail);
        }

        void AddIgnitionStone(UnitProgress prog, CharacterDef def, int kind)
        {
            if (_save == null || prog == null) return;
            var cap = Ignition.StoneCap;
            var cur = kind == 1 ? prog.IgnCrt : kind == 2 ? prog.IgnAgl : prog.IgnAtk;
            if (cur >= cap) return;
            if (!_save.SpendStone(1)) return;
            if (kind == 1) prog.IgnCrt = Ignition.Clamp(prog.IgnCrt + 1);
            else if (kind == 2) prog.IgnAgl = Ignition.Clamp(prog.IgnAgl + 1);
            else prog.IgnAtk = Ignition.Clamp(prog.IgnAtk + 1);
            var sum = prog.IgnAtk + prog.IgnCrt + prog.IgnAgl;
            var max = def != null ? def.IgnitionMax : 12;
            if (max < 1) max = 12;
            prog.Ignition = Mathf.Min(max, sum);
            Persist();
        }

        CharacterDef Grown(string id)
        {
            var def = Catalog.TryChar(id);
            if (def == null) return null;
            return Growth.Apply(def, _save.GetUnit(id));
        }

        int TeamPower()
        {
            var n = 0;
            if (_save == null || _save.PartyIds == null) return 0;
            for (int i = 0; i < _save.PartyIds.Length; i++)
            {
                var grown = Grown(_save.PartyIds[i]);
                if (grown == null) continue;
                n += Growth.CombatPower(grown);
            }
            return n;
        }

        void Show(ScreenId id)
        {
            _screen = id;
            _skillOpen = false;
            _costumeOpen = false;
            _ignitionOpen = false;
            _moreOpen = false;
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
                case ScreenId.Summon: DrawSummon(); break;
                case ScreenId.Daily: DrawDaily(); break;
                case ScreenId.Shop: DrawShop(); break;
                case ScreenId.Mail: DrawMail(); break;
                case ScreenId.Achieve: DrawAchieve(); break;
                case ScreenId.Title: DrawTitle(); break;
                case ScreenId.Friend: DrawFriend(); break;
                case ScreenId.Rest: DrawRest(); break;
                case ScreenId.Food: DrawFood(); break;
                case ScreenId.Pvp: DrawPvp(); break;
                case ScreenId.Tutorial: DrawTutorial(); break;
                case ScreenId.Costume: DrawCostume(); break;
            }
            MarkEntryDev(id);
        }

        void DrawBoot()
        {
            BootSplash.Draw(Root(), () =>
            {
                if (_screen == ScreenId.Boot) Show(ScreenId.Home);
            });
        }

        void DrawHome()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            // The cosmetic lives only on Home; roster, party and battle retain their real leader.
            var bunny = EmeraldLobby.TryAttach(Root(), _built);
            if (!bunny && def != null)
                _built.Add(CharacterPresenter.DrawStage(Root(), def, _save.GetUnit(lead).SkinId));
            HomeHud.Draw(Root(), _built, bunny ? null : def,
                !bunny && def != null ? _save.GetUnit(lead) : null,
                () => Show(ScreenId.Settings),
                () => Show(ScreenId.Team),
                () => Show(ScreenId.Stage));
            if (bunny) EmeraldLobby.DrawIdentity(Root(), _built);
            HomeIdleFx.Attach(Root(), _built);
            LobbyRail.DrawHome(Root(), _built,
                () => Show(ScreenId.Summon),
                () => { _moreOpen = true; RedrawHome(); },
                () => Show(ScreenId.Mail));
            Nav();
            EmeraldLobby.DrawToggle(Root(), _built, bunny, RedrawHome);
            if (_moreOpen)
            {
                LobbyRail.DrawMenu(Root(), _built, Go, () =>
                {
                    _moreOpen = false;
                    RedrawHome();
                });
            }
        }

        void RedrawHome()
        {
            if (_screen != ScreenId.Home) _screen = ScreenId.Home;
            ClearUi();
            DrawHome();
            MarkEntryDev(ScreenId.Home);
        }

        void MarkEntryDev(ScreenId id)
        {
            if (id != ScreenId.Home && id != ScreenId.Team && id != ScreenId.Stage && id != ScreenId.Result)
                return;
            var extra = "no GL numbers";
            if (id == ScreenId.Result && _battle != null)
                extra = "contrast " + _battle.Profile + " not GL";
            DevOverlay.Draw(Root(), _built, id.ToString(), extra);
        }

        void DrawLobbyStage(CharacterDef def)
        {
            LobbyStage.Draw(Root(), _built, def != null && def.Id == "C001");
        }

        void DrawArchive()
        {
            ArchiveBoard.Draw(Root(), _save.Roster, Inspect);
            CloseX(ScreenId.Home);
        }

        void DrawLibrary()
        {
            LibraryBoard.Draw(Root());
            CloseX(ScreenId.Home);
        }

        void DrawSummon()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            CurrencyPlate.Draw(Root(), _built, _save.Gold, _save.Stone);
            SummonBoard.Draw(Root(), () => Show(ScreenId.Home), () =>
            {
                var got = PullSummon();
                var pulled = Catalog.TryChar(got);
                var show = pulled != null ? pulled : def;
                VfxSummonBurst.Play(Root(), show != null ? show.Name : "契核回响", show != null ? show.NativeStar : 3);
                Show(ScreenId.Summon);
            });
        }

        void DrawDaily()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            CurrencyPlate.Draw(Root(), _built, _save.Gold, _save.Stone);
            DailyBoard.Draw(Root(), _save.DailyClaimed, () => Show(ScreenId.Home), ClaimDaily);
        }

        void DrawShop()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            CurrencyPlate.Draw(Root(), _built, _save.Gold, _save.Stone);
            ShopBoard.Draw(Root(), () => Show(ScreenId.Home), BuyShop);
        }

        void DrawMail()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            CurrencyPlate.Draw(Root(), _built, _save.Gold, _save.Stone);
            MailBoard.Draw(Root(), _save.MailRead, () => Show(ScreenId.Home), ReadMail);
        }

        void DrawAchieve()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            AchievementBoard.Draw(Root(), () => Show(ScreenId.Home));
        }

        void DrawTitle()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            TitleBoard.Draw(Root(), () => Show(ScreenId.Home));
        }

        void DrawFriend()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            FriendBoard.Draw(Root(), () => Show(ScreenId.Home));
        }

        void DrawRest()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            var pts = _save.GetUnit(lead).Affection;
            RestBoard.Draw(Root(), pts, () => Show(ScreenId.Home), () =>
            {
                var u = _save.GetUnit(lead);
                u.Affection = Mathf.Min(Bond.PointCap, u.Affection + 10);
                Persist();
                Show(ScreenId.Rest);
            });
            VfxRestSteam.Play(Root(), new Vector2(0.5f, 0.48f));
        }

        void DrawFood()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            FoodBoard.Draw(Root(), _save.Meal, () => Show(ScreenId.Home), i =>
            {
                _save.Meal = i;
                Persist();
                Show(ScreenId.Food);
            });
        }

        void DrawPvp()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            PvpBoard.Draw(Root(), _save.PvpDoor, () => Show(ScreenId.Home), () =>
            {
                _save.PvpDoor = !_save.PvpDoor;
                Persist();
                Show(ScreenId.Pvp);
            });
        }

        void DrawTutorial()
        {
            TutorialBoard.Draw(Root(), _tutorialPage, () =>
            {
                _tutorialPage = 0;
                Show(ScreenId.Home);
            }, () =>
            {
                _tutorialPage = Mathf.Min(2, _tutorialPage + 1);
                Show(ScreenId.Tutorial);
            });
        }

        void DrawCostume()
        {
            _costumeOpen = true;
            DrawInspect();
        }

        void DrawSettings()
        {
            var lead = _save.LeaderId();
            var def = Catalog.TryChar(lead);
            DrawLobbyStage(def);
            if (def != null)
                _built.Add(CharacterPresenter.DrawStage(Root(), def, _save.GetUnit(lead).SkinId));
            SettingsModal.Draw(Root(), _built, _save.Auto, _save.Speed, new SettingsHooks
            {
                onAuto = () =>
                {
                    CycleBattleAuto();
                    Show(ScreenId.Settings);
                },
                onSpeed = () =>
                {
                    _save.Speed = _save.Speed == 1 ? 2 : 1;
                    Persist();
                    Show(ScreenId.Settings);
                },
                onWipe = () =>
                {
                    _save = SaveStore.Reset(SavePath());
                    Show(ScreenId.Home);
                },
                onBack = () => Show(ScreenId.Home)
            });
            // 设置是模态：不挂底部六签。
        }

        void DrawDeep()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            DeepBoard.Draw(Root(), _save.ClearedCount, node =>
            {
                StartBattleAt(Mathf.Clamp(node, 0, 11));
            });
            if (_save.ClearedCount >= 12)
                NebulaBoard.Draw(Root(), Mathf.Max(1, _save.ClearedHard), () =>
                {
                    _save.UseHard = true;
                    Persist();
                    StartBattleAt(0);
                });
            CloseX(ScreenId.Home);
            // 深途全屏：不挂底部六签。
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
            LeftLabel(CharacterPresenter.RoleLine(def), compact ? 18 : 22, CharacterPresenter.ElementColor(def.Element),
                anchor + new Vector2(0f, compact ? -0.028f : -0.048f), true);
            if (compact)
            {
                LeftLabel("战力  " + Growth.CombatPower(grown), 28, VisualTokens.YellowValue,
                    anchor + new Vector2(0f, -0.062f), false);
                LeftLabel("生命 " + grown.Hp + "   攻击 " + grown.Atk + "   防御 " + grown.Def,
                    18, VisualTokens.TextStat, anchor + new Vector2(0f, -0.094f), false);
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
            TeamBoard.DrawRoster(Root(), _built, _save, Inspect);
            Nav();
        }

        void DrawTeam()
        {
            TeamBoard.DrawTeam(Root(), _built, _save, _editSlot,
                slot => { _editSlot = slot; Show(ScreenId.Team); },
                id =>
                {
                    _save.SetPartySlot(_editSlot, id);
                    Persist();
                    Show(ScreenId.Team);
                },
                SetLeader,
                Inspect);
            Nav();
            var goStage = OverlayDraw.Hit(Root(), _built, new Vector2(0.86f, 0.168f), new Vector2(160, 48),
                () => Show(ScreenId.Stage));
            if (goStage != null) goStage.name = "去关卡";
            var txStage = OverlayDraw.Label(Root(), _built, "去关卡", 18, VisualTokens.GoldMetal,
                new Vector2(0.86f, 0.168f), new Vector2(150, 40), false, true, 2f);
            if (txStage != null) txStage.raycastTarget = false;
        }

        void DrawPartyRow(float y, bool selectable, float chipW, float chipH)
        {
            var n = _save != null && _save.PartyIds != null ? _save.PartyIds.Length : 0;
            var dx = n <= 5 ? 0.19f : (n > 0 ? 0.76f / n : 0.19f);
            for (int i = 0; i < n; i++)
            {
                var id = _save.PartyIds[i];
                var def = Catalog.TryChar(id);
                if (def == null) continue;
                var lv = _save.GetUnit(id).Level;
                var selected = selectable && i == _editSlot;
                var slot = i;
                var chip = CharacterPresenter.DrawChip(Root(), def,
                    new Vector2(0.12f + i * dx, y), new Vector2(chipW, chipH), selected, i == _save.LeaderSlot, lv);
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
            var ids = Catalog.MatrixIds ?? Catalog.PlayableIds;
            for (int e = 0; e < 5; e++)
            {
                var y = topY - e * stepY;
                FullLabel(els[e], 18, CharacterPresenter.ElementColor((Element)e), new Vector2(0.048f, y), false);
                for (int r = 0; r < 5; r++)
                {
                    var idx = e * 5 + r;
                    var id = ids != null && idx < ids.Length ? ids[idx] : null;
                    CharacterDef def = null;
                    if (!string.IsNullOrEmpty(id) && Catalog.Characters != null)
                        Catalog.Characters.TryGetValue(id, out def);
                    var captured = def != null ? def.Id : id;
                    var marked = false;
                    if (def != null && _save.PartyIds != null)
                    {
                        for (int p = 0; p < _save.PartyIds.Length; p++)
                            if (_save.PartyIds[p] == captured) { marked = true; break; }
                    }
                    var cell = CharacterPresenter.DrawTile(Root(), def,
                        new Vector2(x0 + r * dx, y), tile, true, marked);
                    if (!string.IsNullOrEmpty(captured))
                        cell.GetComponent<Button>().onClick.AddListener(() => onClick(captured));
                    _built.Add(cell);
                }
            }
        }

        void DrawInspect()
        {
            ClearUi();
            var id = _inspectId ?? Catalog.DefaultParty[0];
            if (Catalog.Characters == null || !Catalog.Characters.ContainsKey(id))
                id = Catalog.DefaultParty[0];
            if (Catalog.Characters == null || !Catalog.Characters.ContainsKey(id))
                return;
            var def = Catalog.TryChar(id);
            if (def == null) return;
            DrawLobbyStage(def);
            var prog = _save.GetUnit(id);
            if (!Costume.Unlocked(id, prog.SkinId))
                prog.SkinId = "";
            _built.Add(CharacterPresenter.DrawStage(Root(), def, prog.SkinId));
            if (def != null && def.Id == "C001")
                IceParticles.Attach(Root(), _built, true);
            AttachInspectSwipe();
            InspectBoard.Draw(Root(), _built, def, prog, new InspectHooks
            {
                onClose = () => Show(_inspectBack),
                onSkills = ToggleSkill,
                onEquipSlot = slot =>
                {
                    var current = SlotGear(prog, slot);
                    var next = GearCatalog.CycleSlot(slot, current);
                    if (string.IsNullOrEmpty(current) && string.IsNullOrEmpty(next))
                    {
                        _equipNotice = true;
                        DrawInspect();
                        return;
                    }
                    SetSlotGear(prog, slot, next);
                    Persist();
                    DrawInspect();
                },
                onEquipPlus = slot => UpgradePlus(prog, slot),
                onIgnition = () =>
                {
                    _ignitionOpen = true;
                    DrawInspect();
                },
                onLevel = () =>
                {
                    prog.Level = prog.Level >= Growth.MaxLevel ? 1 : prog.Level + 1;
                    Persist();
                    DrawInspect();
                    VfxLevelUp.Play(Root(), prog.Level);
                },
                onUncap = () =>
                {
                    prog.Uncap = prog.Uncap >= def.UncapMax ? 0 : prog.Uncap + 1;
                    Persist();
                    DrawInspect();
                    VfxUncap.Play(Root(), OverlayDraw.StarCount(def, prog));
                },
                onAffection = () =>
                {
                    prog.Affection = Growth.CycleAffection(prog.Affection);
                    Persist();
                    DrawInspect();
                    VfxAffection.Play(Root(), new Vector2(0.22f, 0.42f), prog.Affection);
                },
                onSkin = () =>
                {
                    _costumeOpen = true;
                    DrawInspect();
                },
                onJoinParty = () =>
                {
                    _save.SetPartySlot(_editSlot, id);
                    Persist();
                    Show(ScreenId.Team);
                },
                onPrev = () => CycleInspect(-1),
                onNext = () => CycleInspect(1)
            });
            if (_skillOpen) InspectBoard.DrawSkillSheet(Root(), _built, def, prog, ToggleSkill);
            if (_costumeOpen)
            {
                CostumeBoard.Draw(Root(), id, prog.SkinId, () =>
                {
                    _costumeOpen = false;
                    DrawInspect();
                }, skin =>
                {
                    if (string.IsNullOrEmpty(skin) || Costume.Unlocked(id, skin))
                    {
                        prog.SkinId = skin ?? "";
                    }
                    else if (_save.SpendStone(40))
                    {
                        Costume.Unlock(id, skin);
                        prog.SkinId = skin;
                    }
                    else return;
                    Persist();
                    _costumeOpen = false;
                    DrawInspect();
                });
            }
            if (_ignitionOpen)
            {
                IgnitionBoard.Draw(Root(), prog.IgnAtk, prog.IgnCrt, prog.IgnAgl, Ignition.StoneCap,
                    () =>
                    {
                        _ignitionOpen = false;
                        DrawInspect();
                    }, kind =>
                    {
                        AddIgnitionStone(prog, def, kind);
                        DrawInspect();
                    });
            }
            else if (_equipNotice && !_skillOpen && !_costumeOpen)
            {
                InspectBoard.DrawEmptyNotice(Root(), _built, () =>
                {
                    _equipNotice = false;
                    DrawInspect();
                });
            }
        }

        void AttachInspectSwipe()
        {
            var go = new GameObject("InspectSwipe", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root(), false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.15f);
            rt.anchorMax = new Vector2(0.82f, 0.92f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Pixel());
            img.color = Color.clear;
            img.raycastTarget = true;
            var cr = img.canvasRenderer;
            if (cr != null) cr.cullTransparentMesh = false;
            var pad = go.AddComponent<InspectSwipePad>();
            pad.OnPrev = () => CycleInspect(-1);
            pad.OnNext = () => CycleInspect(1);
            _built.Add(go);
        }

        void CycleInspect(int dir)
        {
            if (dir == 0) return;
            var ids = InspectRoster();
            if (ids == null || ids.Count < 2) return;
            var cur = _inspectId ?? "";
            var idx = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == cur)
                {
                    idx = i;
                    break;
                }
            }
            for (int n = 0; n < ids.Count; n++)
            {
                idx += dir;
                if (idx < 0) idx += ids.Count;
                if (idx >= ids.Count) idx -= ids.Count;
                var id = ids[idx];
                if (string.IsNullOrEmpty(id) || id == cur) continue;
                if (Catalog.TryChar(id) == null) continue;
                _inspectId = id;
                _skillOpen = false;
                _costumeOpen = false;
                _ignitionOpen = false;
                _equipNotice = false;
                DrawInspect();
                return;
            }
        }

        List<string> InspectRoster()
        {
            var list = new List<string>();
            if (_save != null && _save.Roster != null && _save.Roster.Count > 0)
            {
                for (int i = 0; i < _save.Roster.Count; i++)
                {
                    var id = _save.Roster[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    if (Catalog.TryChar(id) == null) continue;
                    list.Add(id);
                }
                if (list.Count > 0) return list;
            }
            var play = Catalog.PlayableIds;
            if (play == null) return list;
            for (int i = 0; i < play.Length; i++)
            {
                var id = play[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (Catalog.TryChar(id) == null) continue;
                list.Add(id);
            }
            return list;
        }

        void DrawWells(UnitProgress prog)
        {
            var gears = new[] { prog.Gear0, prog.Gear1, prog.Gear2, prog.Gear3 };
            for (int i = 0; i < 4; i++)
            {
                var slot = i;
                var x = 0.16f + i * 0.22f;
                FullLabel(GearCatalog.SlotNames[i], 15, VisualTokens.TextMuted, new Vector2(x, 0.248f), false);
                var filled = !string.IsNullOrEmpty(gears[i]);
                var go = MakeButton(filled ? GearCatalog.Label(gears[i]) : "空", new Vector2(x, 0.214f), new Vector2(150, 72),
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

        static int SlotPlus(UnitProgress p, int slot)
        {
            switch (slot)
            {
                case 0: return p.Plus0;
                case 1: return p.Plus1;
                case 2: return p.Plus2;
                default: return p.Plus3;
            }
        }

        static void SetSlotPlus(UnitProgress p, int slot, int plus)
        {
            switch (slot)
            {
                case 0: p.Plus0 = plus; break;
                case 1: p.Plus1 = plus; break;
                case 2: p.Plus2 = plus; break;
                default: p.Plus3 = plus; break;
            }
        }

        void UpgradePlus(UnitProgress prog, int slot)
        {
            if (_save == null || prog == null) return;
            var current = SlotPlus(prog, slot);
            if (current >= 15) return;
            var cost = 80 * (current + 1);
            if (!_save.SpendGold(cost)) return;
            SetSlotPlus(prog, slot, current + 1);
            Persist();
            DrawInspect();
        }

        void ToggleSkill()
        {
            _skillOpen = !_skillOpen;
            DrawInspect();
        }

        void DrawStage()
        {
            var hardLocked = _save.ClearedCount < 12;
            if (hardLocked) _save.UseHard = false;
            var table = Catalog.Chapter(_save.UseHard);
            if (table == null || table.Length == 0) table = Catalog.Stages;
            _stageIndex = Mathf.Clamp(_stageIndex, 0, Mathf.Max(0, table.Length - 1));
            while (_stageIndex > 0 && StageLocked(_stageIndex)) _stageIndex--;

            CharacterPresenter.StageBackdrop(Root(), _built, _stageIndex, _save.UseHard, false);
            StageBoard.Draw(Root(), _built, table, _stageIndex, _save.UseHard, hardLocked,
                _save.UseHard ? _save.ClearedHard : _save.ClearedCount,
                StageLocked,
                idx => { _stageIndex = idx; Show(ScreenId.Stage); },
                () =>
                {
                    _save.UseHard = false;
                    Persist();
                    Show(ScreenId.Stage);
                },
                () =>
                {
                    if (hardLocked) return;
                    _save.UseHard = true;
                    Persist();
                    Show(ScreenId.Stage);
                },
                () => StartBattleAt(_stageIndex),
                () => Show(ScreenId.Home),
                () => Show(ScreenId.Team));
        }

        bool StageLocked(int index) => _save.IsStageLocked(index);

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
            var mods = new BattleMods
            {
                FoodAtkMul = Food.AtkMulOf(_save.Meal),
                CartaMul = PvpRules.CartaMulIfPvp(_save.PvpDoor)
            };
            _battle = new BattleSim(_save.PartyIds, _save.LeaderSlot, _save.LastSeed, stage, _save.ProgressForParty(), mods)
            {
                Speed = _save.Speed,
                Auto = _save.Auto,
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
            _simAcc = 0f;
            Show(ScreenId.Battle);
        }

        void DrawBattle()
        {
            if (_battle == null)
            {
                Show(ScreenId.Home);
                return;
            }
            _hud = new BattleHud(this, Root(), _built);
            _hud.Build(_activeStage);
        }

        void DrawResult()
        {
            CharacterPresenter.MosaicFloor(Root(), _built);
            var win = _resultTitle == "胜利";
            var cleared = _save.UseHard ? _save.ClearedHard : _save.ClearedCount;
            System.Action next = null;
            if (win && cleared > 0 && cleared < 12)
                next = () => { _stageIndex = cleared; StartBattleAt(_stageIndex); };
            else
                next = () => StartBattleAt(_activeStage);
            ResultBoard.Draw(Root(), win, _battle, _lootLine, () => Show(ScreenId.Home), next);
            VfxStageClear.Play(Root(), win);
            if (win && !string.IsNullOrEmpty(_lootLine))
                VfxLootDrop.Play(Root(), new Vector2(0.5f, 0.44f), _lootLine);
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
            var sel = 0;
            if (_screen == ScreenId.Characters || _screen == ScreenId.Team) sel = 1;
            else if (_screen == ScreenId.Stage) sel = 2;
            else if (_screen == ScreenId.Archive) sel = 3;
            else if (_screen == ScreenId.Library) sel = 4;
            else if (_screen == ScreenId.Deep) sel = 5;
            UiChrome.TabBar(Root(), _built, sel, null, i =>
            {
                if (i == 1) Show(ScreenId.Team);
                else if (i == 2) Show(ScreenId.Stage);
                else if (i == 3) Show(ScreenId.Archive);
                else if (i == 4) Show(ScreenId.Library);
                else if (i == 5) Show(ScreenId.Deep);
                else Show(ScreenId.Home);
            });
        }

        void CloseX(ScreenId back)
        {
            UiChrome.CloseX(Root(), _built, new Vector2(0.93f, 0.95f), () => Show(back));
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
            UiChrome.Confirm(Root(), _built, label, anchor, () => click());
        }

        GameObject GhostBtn(string label, Vector2 anchor, UnityEngine.Events.UnityAction click)
        {
            var go = MakeButton(label, anchor, new Vector2(72, 32), VisualTokens.TextPrimary, click);
            var img = go.GetComponent<Image>();
            img.color = Color.clear;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.ignoreParentGroups = true;
            cg.blocksRaycasts = true;
            cg.interactable = true;
            var tx = go.GetComponentInChildren<Text>();
            tx.fontSize = 20;
            tx.raycastTarget = false;
            var ol = tx.gameObject.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2, -2);
            return go;
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
            cam.cullingMask &= ~((1 << Sprite2DStandee.WorldLayer)
                | (1 << IceParticles.BackLayer) | (1 << IceParticles.FrontLayer)
                | (1 << CutoutRig.WorldLayer));
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

    sealed class InspectSwipePad : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action OnPrev;
        public Action OnNext;
        Vector2 _start;
        bool _dragged;

        public void OnBeginDrag(PointerEventData e)
        {
            _start = e.position;
            _dragged = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (Mathf.Abs(e.position.x - _start.x) > 28f) _dragged = true;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_dragged) return;
            var dx = e.position.x - _start.x;
            if (dx > 72f)
            {
                if (OnPrev != null) OnPrev();
            }
            else if (dx < -72f)
            {
                if (OnNext != null) OnNext();
            }
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
