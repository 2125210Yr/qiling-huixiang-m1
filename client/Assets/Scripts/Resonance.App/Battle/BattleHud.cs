using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Combat HUD chrome. Cue copy lives in <see cref="BattleCueCopy"/>
    /// (M1-G2-UI-CUES). Layout is <see cref="LayoutStatus"/> — not measured
    /// from primary GT, not the ragna_gl three-column crop, not memorial lobby.
    /// SHOWTIME (开演) and the Fever banner (狂热时间) must stay distinct.
    /// </summary>
    public sealed class BattleHud
    {
        public bool QteOpen { get; private set; }
        public bool DriveSelectVisible { get; private set; }
        public BattleCueCopy.Kind VisibleStamp { get; private set; }
        public string VisibleStampTitle { get; private set; }
        public bool FeverBannerOn { get; private set; }

        readonly GameRoot _host;
        readonly Transform _root;
        readonly List<GameObject> _built;

        Image _archHp;
        Text _archNum;
        Text _archDrive;
        Text _archMeta;
        Text _timerLabel;
        Text _speedLabel;
        Text _autoLabel;
        Text _pauseLabel;
        Text _escapeLabel;
        Text _repeatLabel;
        Text _skipLabel;
        Text _logLabel;
        Text _archBoss;
        Image _partyHp;
        Text _partyHpLabel;
        Image _driveBar;
        Text _driveLabel;
        Image _feverBar;
        Text _feverLabel;
        Text _feverToward;
        Image _feverFlash;
        Image _showtime;
        Text _stamp;
        Text _stampSub;
        Text _skillBanner;
        BattleFighter[] _fieldAllies;
        BattleFighter[] _fieldEnemies;
        Transform _fieldLayer;
        Image[] _chargeRing;
        Image[] _portraitRim;
        Image[] _feverArc;
        Image[] _tapFlash;
        Image[] _readyDash;
        Image[] _hpBar;
        Text[] _hpNum;
        Text[] _nameTag;
        Text[] _lvTag;
        Text[] _costumeTag;
        Text[] _readyTag;
        Text[] _slidePip;
        Text[] _drivePip;
        CanvasGroup[] _portraitDim;
        Image[] _enemyPips;
        float[] _portFlashT;
        bool[] _tapWas;
        bool[] _slideWas;
        bool[] _driveWas;
        PixelCombatFx _pix;
        int _logCursor;
        int _castCursor;
        int _fieldWave = -1;
        float _showtimeT;
        float _stampT;
        Color _stampTint = VisualTokens.YellowConfirm;
        /// <summary>Display mirror of the core QTE elapsed time; never authoritative (Q01).</summary>
        float _qteT;
        int _judgeShownForTick = -1;
        float _warnT;
        float _feverBurstT;
        bool _feverWas;
        bool _judgeFromQte;
        float _cutHoldT;
        float _feverCueDelay;
        bool _feverCueQueued;
        bool _tipSlideShown;
        bool _tipSlideMonaShown;
        bool _tipSlidePowerShown;
        bool _tipDriveShown;
        bool _tipDriveTimingShown;
        bool _tipDriveTablesShown;
        bool _tipSpeedShown;
        bool _tipTapShown;
        float _tipTapDelay;
        bool _tipKeepShown;
        float _tipKeepDelay;
        bool _tipTeamHpShown;
        float _tipTeamHpDelay;
        bool _tipChildsShown;
        float _tipChildsDelay;
        bool _tipChildsMoreShown;
        float _tipChildsMoreDelay;
        bool _tipSkillReadyShown;
        bool _tipFeverGaugeShown;
        bool _feverSessionSeen;
        float _driveTotalLabelT;
        CanvasGroup _driveSelect;
        Text _driveSelectTitle;
        Text _driveSelectHint;

        static readonly Color FeverPink = new Color(0.78f, 0.28f, 0.73f, 1f);
        static readonly Color PartyGreen = new Color(0.18f, 0.70f, 0.34f, 1f);
        static readonly Color DockNight = new Color(0.05f, 0.055f, 0.07f, 0.96f);

        public const string LayoutStatus = "NEEDS_REFERENCE";

        // NEEDS_REFERENCE: not measured from primary GT. Not T27.
        // ragna_gl ui/ mid-column and memorial lobby stills are not battle pixels.
        const float PortY = 0.16f;
        const float PortSize = 150f;
        const float PortLeft = 0.11f;
        const float PortSpread = 0.195f;
        const float PortArc = 0.018f;

        ICharacterPresentation _presentation;
        readonly List<string> _hudFlags = new List<string>(8);

        public BattleHud(GameRoot host, Transform root, List<GameObject> built)
        {
            _host = host;
            _root = root;
            _built = built;
            VisibleStampTitle = "";
        }

        public void Build(int stageIndex)
        {
            if (_host == null || _root == null || _built == null) return;
            var battle = _host.Battle;
            if (battle == null) return;
            _tipSlideShown = false;
            _tipSlideMonaShown = false;
            _tipSlidePowerShown = false;
            _tipDriveShown = false;
            _tipDriveTimingShown = false;
            _tipDriveTablesShown = false;
            _tipSpeedShown = false;
            _tipTapShown = false;
            _tipTapDelay = 0.85f;
            _tipKeepShown = false;
            _tipKeepDelay = 0f;
            _tipTeamHpShown = false;
            _tipTeamHpDelay = 0f;
            _tipChildsShown = false;
            _tipChildsDelay = 0f;
            _tipChildsMoreShown = false;
            _tipChildsMoreDelay = 0f;
            _tipSkillReadyShown = false;
            _tipFeverGaugeShown = false;
            _feverSessionSeen = false;
            _driveTotalLabelT = 0f;
            CharacterPresenter.StageArena(_root, _built, stageIndex, _host.SaveData != null && _host.SaveData.UseHard);
            EnsureField();
            _pix = PixelCombatFx.Ensure(_root);
            if (_pix != null && _built != null) _built.Add(_pix.gameObject);
            SpawnAllies();
            SpawnEnemies();

            _feverFlash = Img("feverFlash", new Vector2(0.5f, 0.56f), new Vector2(1, 1), Color.clear);
            _showtime = Img("showtimePlate", new Vector2(0.5f, 0.62f), new Vector2(980, 260), Color.clear);
            UiSprites.Apply(_showtime, UiSprites.Slash());

            _skillBanner = Label("", 24, VisualTokens.TapWhite, new Vector2(0.5f, 0.835f), new Vector2(880, 48), true);
            if (_skillBanner != null) _skillBanner.gameObject.name = "cueBanner";
            _stamp = Label("", 64, VisualTokens.SlideGreen, new Vector2(0.5f, 0.60f), new Vector2(980, 140), true);
            if (_stamp != null) _stamp.gameObject.name = "cueStamp";
            var sc = _stamp.color;
            sc.a = 0f;
            _stamp.color = sc;
            _stampSub = Label("", 28, VisualTokens.YellowValue, new Vector2(0.5f, 0.52f), new Vector2(880, 56), true);
            if (_stampSub != null) _stampSub.gameObject.name = "cueStampSub";
            var s2 = _stampSub.color;
            s2.a = 0f;
            _stampSub.color = s2;

            BuildTopBar();
            BuildBottomBar();
            BuildPortraits();
            BuildDriveSelect();
            _presentation = CharacterPresenter.BindExisting(
                _fieldLayer != null ? _fieldLayer : _root, _fieldAllies, _fieldEnemies);
            HideGood();
            Refresh();
            ShowCue(BattleCueCopy.Kind.BattleStart, 1.05f, "");
        }

        public void Tick()
        {
            if (_host == null) return;
            var battle = _host.Battle;
            if (battle == null) return;
            if (_pix != null) _pix.Tick();
            TickCutHold(battle);
            if (battle.Paused)
            {
                Refresh();
                return;
            }
            if (QteOpen)
                TickQte(battle);
            else if (battle.PendingDriveSlot >= 0)
                OpenOrAutoQte(battle);
            Refresh();
        }

        public bool FirePerfect()
        {
            if (_host == null) return false;
            var b = _host.Battle;
            if (b == null) return false;
            var casts = b.Casts != null ? b.Casts.Count : 0;
            if (b.PendingDriveSlot < 0)
            {
                var n = b.Allies != null ? b.Allies.Length : 0;
                for (int i = 0; i < n; i++)
                    if (b.Submit(BattleCommand.DriveBegin(i, CommandSource.Fixture)).Accepted) break;
            }
            if (b.PendingDriveSlot >= 0)
            {
                QteOpen = false;
                HideGood();
                if (_pix != null && _pix.Showing) _pix.HideNow();
                var res = b.Submit(BattleCommand.DriveResolve(DriveTiming.Perfect, CommandSource.Fixture));
                if (!res.Accepted) return false;
                _judgeShownForTick = b.LastDriveResolveTick;
                _judgeFromQte = true;
                ShowJudge(b.LastDriveTiming);
                return true;
            }
            if (b.Casts == null) return false;
            for (int i = casts; i < b.Casts.Count; i++)
                if (b.Casts[i] != null && b.Casts[i].CasterAlly && b.Casts[i].Type == SkillType.Drive)
                    return true;
            return false;
        }

        void BuildTopBar()
        {
            var archBg = Img("archBg", new Vector2(0.5f, 0.990f), new Vector2(1080, 8), new Color(0.10f, 0.02f, 0.02f, 0.92f));
            UiSprites.Apply(archBg, UiSprites.Pixel());
            _archHp = Img("arch", new Vector2(0.5f, 0.990f), new Vector2(1068, 4), VisualTokens.StarEvolved);
            UiSprites.Apply(_archHp, UiSprites.Pixel());
            _archHp.type = Image.Type.Filled;
            _archHp.fillMethod = Image.FillMethod.Horizontal;
            _archHp.fillAmount = 1f;
            var archWireT = Img("archWireT", new Vector2(0.5f, 0.990f), new Vector2(1086, 2),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.95f));
            archWireT.rectTransform.anchoredPosition = new Vector2(0f, 9f);
            var archWireB = Img("archWireB", new Vector2(0.5f, 0.990f), new Vector2(1086, 2),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.95f));
            archWireB.rectTransform.anchoredPosition = new Vector2(0f, -9f);
            var archCapL = Img("archCapL", new Vector2(0.5f, 0.990f), new Vector2(3, 20), VisualTokens.GoldMetal);
            archCapL.rectTransform.anchoredPosition = new Vector2(-544f, 0f);
            var archCapR = Img("archCapR", new Vector2(0.5f, 0.990f), new Vector2(3, 20), VisualTokens.GoldMetal);
            archCapR.rectTransform.anchoredPosition = new Vector2(544f, 0f);

            // Primary GT: heart crest above stage name (P0/Robin top center).
            var crest = Img("crest", new Vector2(0.5f, 0.992f), new Vector2(26, 26), VisualTokens.GoldSelect);
            UiSprites.Apply(crest, UiSprites.Heart());
            crest.raycastTarget = false;
            crest.rectTransform.anchoredPosition = new Vector2(0f, 18f);

            _pauseLabel = Antique(BattleCueCopy.PauseHud, new Vector2(0.09f, 0.950f), new Vector2(120, 34), () => _host.ToggleBattlePause());
            // Primary GT: >> Xn SPEED / > FULL AUTO / || PAUSE.
            _speedLabel = Antique(">> X1 SPEED", new Vector2(0.11f, 0.910f), new Vector2(168, 32), () => _host.ToggleBattleSpeed());
            _autoLabel = Antique(AutoWord(AutoMode.Manual), new Vector2(0.91f, 0.950f), new Vector2(160, 34), () => _host.CycleBattleAuto());
            // Primary P0 ~t120 dialogue: stacked >> / SKIP (top-right). Layout stub; no VN wire yet.
            _skipLabel = Label(BattleCueCopy.BattleSkipLine, 14, VisualTokens.TapWhite, new Vector2(0.92f, 0.880f), new Vector2(100, 28), true);
            AntiqueType(_skipLabel);
            if (_skipLabel != null)
            {
                _skipLabel.alignment = TextAnchor.UpperCenter;
                _skipLabel.lineSpacing = 0.75f;
                _skipLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _skipLabel.verticalOverflow = VerticalWrapMode.Overflow;
                var col = _skipLabel.color;
                col.a = 0.55f;
                _skipLabel.color = col;
            }
            // P0 t400 VN: stacked ... / Log under SKIP.
            _logLabel = Label(BattleCueCopy.BattleLog, 12, VisualTokens.TapWhite, new Vector2(0.92f, 0.825f), new Vector2(64, 36), true);
            AntiqueType(_logLabel);
            if (_logLabel != null)
            {
                _logLabel.alignment = TextAnchor.UpperCenter;
                _logLabel.lineSpacing = 0.75f;
                _logLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _logLabel.verticalOverflow = VerticalWrapMode.Overflow;
                var col = _logLabel.color;
                col.a = 0.50f;
                _logLabel.color = col;
            }
            // ESCAPE is Hell Metro / contrast only — primary P0/Robin frames show PAUSE + Repeat, not ESCAPE.
            _escapeLabel = Antique("ESCAPE", new Vector2(0.5f, 0.832f), new Vector2(88, 26), () => _host.ToggleBattlePause());
            if (_escapeLabel != null) _escapeLabel.gameObject.SetActive(false);
            // Repeat is PauseBoard-only (Robin pause). Do not park it on the live field.
            _repeatLabel = Antique("Repeat", new Vector2(0.5f, 0.800f), new Vector2(100, 26), () => _host.RepeatCurrentBattle());
            if (_repeatLabel != null) _repeatLabel.gameObject.SetActive(false);
            if (_skipLabel != null) _skipLabel.gameObject.SetActive(false);
            if (_logLabel != null) _logLabel.gameObject.SetActive(false);

            _archMeta = Label("", 15, VisualTokens.GoldTitle, new Vector2(0.5f, 0.968f), new Vector2(560, 40), true);
            AntiqueType(_archMeta);
            if (_archMeta != null)
            {
                _archMeta.horizontalOverflow = HorizontalWrapMode.Wrap;
                _archMeta.verticalOverflow = VerticalWrapMode.Overflow;
                _archMeta.alignment = TextAnchor.UpperCenter;
            }
            _archNum = Label("", 18, VisualTokens.TapWhite, new Vector2(0.5f, 0.930f), new Vector2(280, 40), true);
            AntiqueType(_archNum);
            if (_archNum != null)
            {
                _archNum.supportRichText = true;
                _archNum.lineSpacing = 0.75f;
                _archNum.horizontalOverflow = HorizontalWrapMode.Overflow;
                _archNum.verticalOverflow = VerticalWrapMode.Overflow;
            }
            _archDrive = Label(BattleCueCopy.PctOverLabel(0, BattleCueCopy.EnemyDrive), 14, VisualTokens.TextMuted, new Vector2(0.5f, 0.900f), new Vector2(280, 36), true);
            AntiqueType(_archDrive);
            if (_archDrive != null)
            {
                _archDrive.supportRichText = true;
                _archDrive.lineSpacing = 0.75f;
                _archDrive.horizontalOverflow = HorizontalWrapMode.Overflow;
                _archDrive.verticalOverflow = VerticalWrapMode.Overflow;
            }
            // Robin ND Hard: boss name under enemy HP % (r25 NEW YEAR'S KRAMPUS role).
            _archBoss = Label("", 11, VisualTokens.GoldTitle, new Vector2(0.5f, 0.918f), new Vector2(420, 18), true);
            AntiqueType(_archBoss);
            _timerLabel = Label("00:00\n" + BattleCueCopy.BattleTime, 16, Color.white, new Vector2(0.5f, 0.870f), new Vector2(280, 40), true);
            AntiqueType(_timerLabel);
            if (_timerLabel != null)
            {
                _timerLabel.supportRichText = true;
                _timerLabel.lineSpacing = 0.75f;
                _timerLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _timerLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }

            _enemyPips = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var pip = Img("pip" + i, new Vector2(0.44f + i * 0.06f, 0.858f), new Vector2(22, 3), new Color(0.26f, 0.19f, 0.08f, 0.85f));
                UiSprites.Apply(pip, UiSprites.Pixel());
                _enemyPips[i] = pip;
            }
        }

        static void AntiqueType(Text tx)
        {
            if (tx == null) return;
            tx.fontStyle = FontStyle.Bold;
            var ol = tx.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.14f, 0.09f, 0.02f, 0.95f);
            ol.effectDistance = new Vector2(1.6f, 1.6f);
            var sh = tx.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.90f);
            sh.effectDistance = new Vector2(0f, -2.6f);
        }

        void BuildBottomBar()
        {
            var dock = Stretch("dock", new Vector2(0f, 0f), new Vector2(1f, 0.108f), DockNight);
            dock.raycastTarget = false;
            var wire = Stretch("dockWire", new Vector2(0f, 0.108f), new Vector2(1f, 0.108f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.60f));
            var wrt = wire.rectTransform;
            wrt.offsetMin = new Vector2(0f, -2f);
            wrt.offsetMax = new Vector2(0f, 0f);
            var plate = Stretch("stone", new Vector2(0f, 0.006f), new Vector2(1f, 0.094f), new Color(0.085f, 0.080f, 0.072f, 0.55f));
            plate.raycastTarget = false;
            var plateWireT = Stretch("stoneWireT", new Vector2(0f, 0.094f), new Vector2(1f, 0.094f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.55f));
            var pwt = plateWireT.rectTransform;
            pwt.offsetMin = new Vector2(0f, -2f);
            pwt.offsetMax = new Vector2(0f, 0f);
            var plateWireB = Stretch("stoneWireB", new Vector2(0f, 0.006f), new Vector2(1f, 0.006f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.45f));
            var pwb = plateWireB.rectTransform;
            pwb.offsetMin = new Vector2(0f, 0f);
            pwb.offsetMax = new Vector2(0f, 2f);

            var phpBg = Img("phpBg", new Vector2(0.5f, 0.070f), new Vector2(1040, 10), new Color(0.03f, 0.04f, 0.03f, 0.88f));
            UiSprites.Apply(phpBg, UiSprites.Pixel());
            _partyHp = Img("php", new Vector2(0.5f, 0.070f), new Vector2(1034, 4), PartyGreen);
            UiSprites.Apply(_partyHp, UiSprites.Pixel());
            _partyHp.type = Image.Type.Filled;
            _partyHp.fillMethod = Image.FillMethod.Horizontal;
            var phpWire = Img("phpWire", new Vector2(0.5f, 0.070f), new Vector2(1048, 16),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.90f));
            UiSprites.Apply(phpWire, UiSprites.WireFrame());
            var phpCapL = Img("phpCapL", new Vector2(0.5f, 0.070f), new Vector2(2, 14), VisualTokens.GoldMetal);
            phpCapL.rectTransform.anchoredPosition = new Vector2(-522f, 0f);
            var phpCapR = Img("phpCapR", new Vector2(0.5f, 0.070f), new Vector2(2, 14), VisualTokens.GoldMetal);
            phpCapR.rectTransform.anchoredPosition = new Vector2(522f, 0f);
            _partyHpLabel = Label("100%\n" + BattleCueCopy.TeamHpTotal, 12, PartyGreen, new Vector2(0.14f, 0.078f), new Vector2(200, 36), true);
            if (_partyHpLabel != null)
            {
                _partyHpLabel.alignment = TextAnchor.MiddleLeft;
                _partyHpLabel.supportRichText = true;
                _partyHpLabel.lineSpacing = 0.75f;
                _partyHpLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _partyHpLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }

            var dBg = Img("dBg", new Vector2(0.5f, 0.0355f), new Vector2(460, 3), new Color(0f, 0f, 0f, 0.60f));
            UiSprites.Apply(dBg, UiSprites.Pixel());
            _driveBar = Img("drive", new Vector2(0.5f, 0.0355f), new Vector2(452, 2), VisualTokens.YellowValue);
            UiSprites.Apply(_driveBar, UiSprites.Pixel());
            _driveBar.type = Image.Type.Filled;
            _driveBar.fillMethod = Image.FillMethod.Horizontal;
            var dCapL = Img("dCapL", new Vector2(0.5f, 0.0355f), new Vector2(2, 8), VisualTokens.GoldMetal);
            dCapL.rectTransform.anchoredPosition = new Vector2(-231f, 0f);
            var dCapR = Img("dCapR", new Vector2(0.5f, 0.0355f), new Vector2(2, 8), VisualTokens.GoldMetal);
            dCapR.rectTransform.anchoredPosition = new Vector2(231f, 0f);
            _driveLabel = Label("0%\n" + BattleCueCopy.SkillGaugeTotal, 11, VisualTokens.YellowValue, new Vector2(0.5f, 0.052f), new Vector2(240, 32), true);
            if (_driveLabel != null)
            {
                _driveLabel.supportRichText = true;
                _driveLabel.lineSpacing = 0.75f;
                _driveLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _driveLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }

            var fBg = Img("fBg", new Vector2(0.5f, 0.0110f), new Vector2(460, 3), new Color(0f, 0f, 0f, 0.60f));
            UiSprites.Apply(fBg, UiSprites.Pixel());
            _feverBar = Img("feverBar", new Vector2(0.5f, 0.0110f), new Vector2(452, 2), FeverPink);
            UiSprites.Apply(_feverBar, UiSprites.Pixel());
            _feverBar.type = Image.Type.Filled;
            _feverBar.fillMethod = Image.FillMethod.Horizontal;
            var fCapL = Img("fCapL", new Vector2(0.5f, 0.0110f), new Vector2(2, 8), VisualTokens.GoldMetal);
            fCapL.rectTransform.anchoredPosition = new Vector2(-231f, 0f);
            var fCapR = Img("fCapR", new Vector2(0.5f, 0.0110f), new Vector2(2, 8), VisualTokens.GoldMetal);
            fCapR.rectTransform.anchoredPosition = new Vector2(231f, 0f);
            _feverLabel = Label(BattleCueCopy.FeverLine(false, 0), 11, FeverPink, new Vector2(0.5f, 0.0240f), new Vector2(240, 16), true);
            if (_feverLabel != null) _feverLabel.gameObject.name = "feverLabel";
            // Primary Robin ~t56: large "n% TO FEVER" above the FEVER n% arc.
            _feverToward = Label("", 13, VisualTokens.SlideGreen, new Vector2(0.5f, 0.058f), new Vector2(280, 20), true);
            if (_feverToward != null) _feverToward.gameObject.name = "feverToward";
        }

        int PartySlots()
        {
            var b = _host != null ? _host.Battle : null;
            if (b != null && b.Allies != null && b.Allies.Length > 0) return b.Allies.Length;
            return FightStats.DefaultPartyCap;
        }

        void BuildDriveSelect()
        {
            var go = new GameObject("driveSelect", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            // Tall plate: title dominates upper half; hint stays below.
            rt.anchorMin = new Vector2(0.05f, 0.34f);
            rt.anchorMax = new Vector2(0.95f, 0.58f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var plate = go.GetComponent<Image>();
            UiSprites.Apply(plate, UiSprites.Pixel());
            plate.color = new Color(0.03f, 0.02f, 0.01f, 0.94f);
            plate.raycastTarget = false;
            _driveSelect = go.GetComponent<CanvasGroup>();
            _driveSelect.blocksRaycasts = false;
            _driveSelect.interactable = false;
            _driveSelect.alpha = 0f;

            var wire = ChildImg(go.transform, "wire", Vector2.zero, Vector2.one,
                new Vector2(2f, 2f), new Vector2(-2f, -2f),
                new Color(VisualTokens.DriveOrange.r, VisualTokens.DriveOrange.g, VisualTokens.DriveOrange.b, 0.95f));
            UiSprites.Apply(wire, UiSprites.WireFrame());
            wire.raycastTarget = false;

            _driveSelectTitle = ChildLabel(go.transform, BattleCueCopy.DriveSelect, 52, VisualTokens.DriveOrange,
                new Vector2(0.5f, 0.70f), new Vector2(820f, 64f));
            _driveSelectTitle.fontStyle = FontStyle.Bold;
            var tOl = _driveSelectTitle.gameObject.AddComponent<Outline>();
            tOl.effectColor = Color.black;
            tOl.effectDistance = new Vector2(3.5f, -3.5f);
            var tSh = _driveSelectTitle.gameObject.AddComponent<Shadow>();
            tSh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            tSh.effectDistance = new Vector2(2f, -2f);

            _driveSelectHint = ChildLabel(go.transform, BattleCueCopy.DriveSelectHint, 22, VisualTokens.YellowValue,
                new Vector2(0.5f, 0.26f), new Vector2(820f, 36f));
            var hOl = _driveSelectHint.gameObject.AddComponent<Outline>();
            hOl.effectColor = Color.black;
            hOl.effectDistance = new Vector2(1.8f, -1.8f);

            go.SetActive(false);
            _built.Add(go);
        }

        void RefreshDriveSelect(BattleSim battle)
        {
            var show = battle != null
                && battle.Outcome == BattleOutcome.InProgress
                && battle.Drive >= 100f
                && battle.PendingDriveSlot < 0
                && !QteOpen
                && !battle.FeverActive
                && WavePreview.VisibleKind != WaveCueKind.WaveAdvance
                && WavePreview.VisibleKind != WaveCueKind.WaveEnter;
            // P0 t365 / t380: Drive is tap-the-ready-icon + DRIVE SKILL READY.
            // No DRIVE SELECT plate. Keep the flag for QTE / smoke / exclusivity.
            DriveSelectVisible = show;
            HideDriveSelectPlate();
            if (show)
                WavePreview.SuppressDeathCues();
        }

        void HideDriveSelect()
        {
            DriveSelectVisible = false;
            HideDriveSelectPlate();
        }

        void HideDriveSelectPlate()
        {
            if (_driveSelect == null) return;
            _driveSelect.alpha = 0f;
            _driveSelect.gameObject.SetActive(false);
        }

        void BuildPortraits()
        {
            var battle = _host != null ? _host.Battle : null;
            var n = PartySlots();
            _chargeRing = new Image[n];
            _portraitRim = new Image[n];
            _feverArc = new Image[n];
            _tapFlash = new Image[n];
            _readyDash = new Image[n];
            _hpBar = new Image[n];
            _hpNum = new Text[n];
            _nameTag = new Text[n];
            _lvTag = new Text[n];
            _costumeTag = new Text[n];
            _readyTag = new Text[n];
            _slidePip = new Text[n];
            _drivePip = new Text[n];
            _portraitDim = new CanvasGroup[n];
            _portFlashT = new float[n];
            _tapWas = new bool[n];
            _slideWas = new bool[n];
            _driveWas = new bool[n];
            for (int i = 0; i < n; i++)
            {
                var slot = i;
                var port = PortAnchor(i);
                var ally = battle != null && battle.Allies != null && i < battle.Allies.Length ? battle.Allies[i] : null;
                var def = ally != null ? ally.Def : null;
                var go = new GameObject("p" + i, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(PortraitGesture));
                go.transform.SetParent(_root, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = port;
                rt.sizeDelta = new Vector2(PortSize, PortSize);
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, UiSprites.Circle());
                img.color = new Color(0.06f, 0.05f, 0.05f, 0.96f);
                _portraitDim[i] = go.GetComponent<CanvasGroup>();

                var ring = ChildImg(go.transform, "ring", Vector2.zero, Vector2.one, new Vector2(-6f, -6f), new Vector2(6f, 6f), Color.white);
                UiSprites.Apply(ring, UiSprites.Circle());
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillOrigin = (int)Image.Origin360.Top;
                ring.fillClockwise = true;
                ring.fillAmount = 0f;
                ring.raycastTarget = false;
                ring.transform.SetAsFirstSibling();
                _chargeRing[i] = ring;

                var feverArc = ChildImg(go.transform, "farc", Vector2.zero, Vector2.one, new Vector2(-9f, -9f), new Vector2(9f, 9f), Color.clear);
                UiSprites.Apply(feverArc, UiSprites.Circle());
                feverArc.type = Image.Type.Filled;
                feverArc.fillMethod = Image.FillMethod.Radial360;
                feverArc.fillOrigin = (int)Image.Origin360.Top;
                feverArc.fillClockwise = true;
                feverArc.fillAmount = 0.22f;
                feverArc.raycastTarget = false;
                feverArc.transform.SetAsFirstSibling();
                _feverArc[i] = feverArc;

                var owire = ChildImg(go.transform, "owire", Vector2.zero, Vector2.one, new Vector2(-3.5f, -3.5f), new Vector2(3.5f, 3.5f),
                    new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.85f));
                UiSprites.Apply(owire, UiSprites.Circle());
                owire.raycastTarget = false;

                var rim = ChildImg(go.transform, "rim", Vector2.zero, Vector2.one, new Vector2(-2f, -2f), new Vector2(2f, 2f), VisualTokens.GoldMetal);
                UiSprites.Apply(rim, UiSprites.Circle());
                rim.color = Color.Lerp(VisualTokens.GoldMetal, VisualTokens.Element(def != null ? def.Element : Element.Dark), 0.16f);
                rim.raycastTarget = false;
                _portraitRim[i] = rim;

                var face = ChildImg(go.transform, "face", Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f), new Color(0.08f, 0.07f, 0.07f, 1f));
                UiSprites.Apply(face, UiSprites.Circle());
                var mask = face.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;
                var pix = ChildImg(face.transform, "pix", Vector2.zero, Vector2.one, new Vector2(6f, -18f), new Vector2(-6f, 10f), Color.white);
                var faceSpr = def != null ? CharacterArt.Face(def.Id) : null;
                pix.sprite = faceSpr != null ? faceSpr : PixelStandIn.Get(def, "", 0);
                pix.preserveAspect = true;
                pix.type = Image.Type.Simple;
                // Primary P0 t435: costume title inside portrait (Lv.n Eclipse / Dark Water).
                _costumeTag[i] = ChildLabel(go.transform, "", 11, VisualTokens.TapWhite,
                    new Vector2(0.5f, 0.42f), new Vector2(PortSize - 8f, 36f));
                _costumeTag[i].supportRichText = true;
                _costumeTag[i].alignment = TextAnchor.MiddleCenter;
                _costumeTag[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                _costumeTag[i].verticalOverflow = VerticalWrapMode.Overflow;
                _costumeTag[i].lineSpacing = 0.8f;
                var costOl = _costumeTag[i].gameObject.AddComponent<Outline>();
                costOl.effectColor = Color.black;
                costOl.effectDistance = new Vector2(1.2f, -1.2f);

                var flash = ChildImg(go.transform, "ping", Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f), Color.clear);
                UiSprites.Apply(flash, UiSprites.Soft());
                _tapFlash[i] = flash;

                // Tip t352/t358: outer yellow ring when skill ready (tap/slide). Dashed circle approx via Circle glow.
                var dash = ChildImg(go.transform, "readyDash", Vector2.zero, Vector2.one, new Vector2(-12f, -12f), new Vector2(12f, 12f), Color.clear);
                UiSprites.Apply(dash, UiSprites.Circle());
                dash.raycastTarget = false;
                dash.transform.SetAsFirstSibling();
                _readyDash[i] = dash;

                var g = go.GetComponent<PortraitGesture>();
                g.OnTap = () => OnPortraitTap(slot);
                g.OnSlide = () =>
                {
                    if (QteOpen) return;
                    var battle = _host != null ? _host.Battle : null;
                    if (battle == null) return;
                    // R07: gesture → one command through the shared entry (probe follows accept/reject).
                    HitChainProbe.Input("slide");
                    var res = battle.Submit(BattleCommand.Slide(slot, CommandSource.Player));
                    if (!res.Accepted) HitChainProbe.Cancel();
                };

                // Hard r58: stacked green "+" over red SLIDE when both ready.
                _slidePip[i] = ChildLabel(go.transform, BattleCueCopy.SlidePipEn, 11, VisualTokens.SlideGreen, new Vector2(0.82f, 0.98f), new Vector2(64, 44));
                _slidePip[i].supportRichText = true;
                _slidePip[i].alignment = TextAnchor.LowerCenter;
                _slidePip[i].lineSpacing = 0.72f;
                var sOl = _slidePip[i].gameObject.AddComponent<Outline>();
                sOl.effectColor = Color.black;
                sOl.effectDistance = new Vector2(1f, -1f);
                _slidePip[i].color = Color.clear;

                _drivePip[i] = ChildLabel(go.transform, BattleCueCopy.DriveCast, 11, VisualTokens.DriveOrange, new Vector2(0.18f, 0.92f), new Vector2(56, 22));
                var dOl = _drivePip[i].gameObject.AddComponent<Outline>();
                dOl.effectColor = Color.black;
                dOl.effectDistance = new Vector2(1f, -1f);
                _drivePip[i].color = Color.clear;

                _readyTag[i] = ChildLabel(go.transform, "", 14, VisualTokens.YellowConfirm, new Vector2(0.5f, 1.18f), new Vector2(PortSize, 52));
                _readyTag[i].supportRichText = true;
                _readyTag[i].alignment = TextAnchor.LowerCenter;
                _readyTag[i].lineSpacing = 0.85f;
                var rOl = _readyTag[i].gameObject.AddComponent<Outline>();
                rOl.effectColor = Color.black;
                rOl.effectDistance = new Vector2(2f, -2f);

                _hpNum[i] = ChildLabel(go.transform, "0", 20, Color.white, new Vector2(0.5f, -0.06f), new Vector2(PortSize, 26));
                var hpOl = _hpNum[i].gameObject.AddComponent<Outline>();
                hpOl.effectColor = Color.black;
                hpOl.effectDistance = new Vector2(2f, -2f);
                // Primary tray: thin HP fill under portrait (P0/Robin orange/green stub).
                var hpTrack = ChildImg(go.transform, "hpTrack", new Vector2(0.5f, -0.14f), new Vector2(0.5f, -0.14f),
                    new Vector2(-PortSize * 0.38f, -3f), new Vector2(PortSize * 0.38f, 3f), new Color(0.08f, 0.06f, 0.05f, 0.85f));
                UiSprites.Apply(hpTrack, UiSprites.Pixel());
                hpTrack.raycastTarget = false;
                var hpFill = ChildImg(go.transform, "hpFill", new Vector2(0.5f, -0.14f), new Vector2(0.5f, -0.14f),
                    new Vector2(-PortSize * 0.38f, -3f), new Vector2(PortSize * 0.38f, 3f), VisualTokens.YellowValue);
                UiSprites.Apply(hpFill, UiSprites.Pixel());
                hpFill.type = Image.Type.Filled;
                hpFill.fillMethod = Image.FillMethod.Horizontal;
                hpFill.fillAmount = 1f;
                hpFill.raycastTarget = false;
                _hpBar[i] = hpFill;
                _nameTag[i] = ChildLabel(go.transform, "", 14, Color.white, new Vector2(0.5f, -0.28f), new Vector2(PortSize + 8f, 22));
                _nameTag[i].fontStyle = FontStyle.Bold;
                var nameOl = _nameTag[i].gameObject.AddComponent<Outline>();
                nameOl.effectColor = Color.black;
                nameOl.effectDistance = new Vector2(2.4f, -2.4f);
                // Primary P0 t83: "60 Name" + separate MAX badge (costume Lv.n Title inventory).
                var lvSeed = PortraitMaxBadgeText(def != null ? def.Id : null);
                _lvTag[i] = ChildLabel(go.transform, lvSeed, 11, VisualTokens.YellowValue, new Vector2(0.82f, -0.46f), new Vector2(56, 18));
                var lvOl = _lvTag[i].gameObject.AddComponent<Outline>();
                lvOl.effectColor = Color.black;
                lvOl.effectDistance = new Vector2(1.6f, -1.6f);
                if (_nameTag[i] != null)
                    _nameTag[i].text = PortraitTrayNameText(def != null ? def.Id : null, def != null ? def.Name : "");
                _built.Add(go);
            }
        }

        /// <summary>
        /// Portrait tap resolves to exactly one command (R07): FeverTap while Fever is active,
        /// DriveBegin when the Drive gauge is full, otherwise Tap. Rejections are logged in the core CommandLog.
        /// </summary>
        void OnPortraitTap(int slot)
        {
            var b = _host != null ? _host.Battle : null;
            if (b == null || QteOpen) return;
            BattleCommand cmd;
            if (b.FeverActive) cmd = BattleCommand.FeverTap(slot, CommandSource.Player);
            else if (b.Drive >= 100f && b.PendingDriveSlot < 0) cmd = BattleCommand.DriveBegin(slot, CommandSource.Player);
            else cmd = BattleCommand.Tap(slot, CommandSource.Player);
            if (cmd.Kind == BattleCommandKind.Tap) HitChainProbe.Input("tap");
            var res = b.Submit(cmd);
            if (!res.Accepted)
            {
                if (cmd.Kind == BattleCommandKind.Tap) HitChainProbe.Cancel();
                return;
            }
            if (cmd.Kind == BattleCommandKind.DriveBegin && b.PendingDriveSlot == slot)
                OpenQte();
        }

        /// <summary>
        /// Q01: the core owns the QTE clock. The HUD only mirrors <see cref="BattleSim.QteRemaining"/> and,
        /// when the core has already resolved (timeout / auto), shows the judge for that resolution once.
        /// </summary>
        void TickQte(BattleSim battle)
        {
            if (battle == null || battle.Paused) return;
            if (battle.PendingDriveSlot < 0)
            {
                if (QteOpen && battle.LastDriveResolveTick >= 0 && battle.LastDriveResolveTick != _judgeShownForTick)
                {
                    QteOpen = false;
                    HideGood();
                    if (_pix != null && _pix.Showing) _pix.HideNow();
                    _judgeShownForTick = battle.LastDriveResolveTick;
                    _judgeFromQte = true;
                    ShowJudge(battle.LastDriveTiming);
                    return;
                }
                QteOpen = false;
                HideGood();
                return;
            }
            _qteT = battle.QteElapsed;
        }

        void OpenOrAutoQte(BattleSim battle)
        {
            if (battle == null) return;
            if (battle.Auto == AutoMode.Full)
                FinishQte(DriveTiming.Great);
            else
                OpenQte();
        }

        void OpenQte()
        {
            var b = _host != null ? _host.Battle : null;
            if (b == null || b.PendingDriveSlot < 0) return;
            if (b.Auto == AutoMode.Full)
            {
                FinishQte(DriveTiming.Great);
                return;
            }
            VfxShowtime.KillAll();
            HideDriveSelect();
            QteOpen = true;
            _qteT = 0f;
            BattleFighter caster = null;
            if (_fieldAllies != null && b.PendingDriveSlot >= 0 && b.PendingDriveSlot < _fieldAllies.Length)
                caster = _fieldAllies[b.PendingDriveSlot];
            var skillName = "";
            if (b.Allies != null && b.PendingDriveSlot >= 0 && b.PendingDriveSlot < b.Allies.Length
                && b.Allies[b.PendingDriveSlot] != null && b.Allies[b.PendingDriveSlot].Def != null)
            {
                var sk = Catalog.TrySkill(b.Allies[b.PendingDriveSlot].Def.DriveSkillId);
                if (sk != null) skillName = sk.Name;
            }
            BeginShowtime(caster, BattleCueCopy.DriveReady, skillName, BattleCueCopy.DriveCast, VisualTokens.DriveOrange, 0.70f, CombatCut.Drive, false);
            // t382 HERE IS A POINT!! is tutorial/inventory. Ordinary-PVE QTE unseen — coin owns PRESS BUTTON.
            VfxGoodButton.Show(_root, hit =>
            {
                if (!QteOpen) return;
                FinishQte(hit ? DriveTiming.Perfect : DriveTiming.Good);
            });
            CombatFeel.ScreenTint(_fieldLayer != null ? _fieldLayer : _root, VisualTokens.DriveOrange, 0.18f, 0.20f);
            CanvasShake.Punch(18f, 0.18f);
            // t382: PRESS BUTTON lives on the QTE coin, not as a second field stamp.
        }

        /// <summary>Q03/Q04: only an accepted core resolution produces a judge; late/paused/terminal callbacks are no-ops.</summary>
        void FinishQte(DriveTiming timing)
        {
            var b = _host != null ? _host.Battle : null;
            QteOpen = false;
            HideGood();
            if (_pix != null && _pix.Showing) _pix.HideNow();
            if (b == null) return;
            var res = b.Submit(BattleCommand.DriveResolve(timing, CommandSource.Player));
            if (!res.Accepted) return;
            _judgeShownForTick = b.LastDriveResolveTick;
            _judgeFromQte = true;
            ShowJudge(b.LastDriveTiming);
        }

        void HideGood()
        {
            VfxGoodButton.Hide();
        }

        void ShowJudge(DriveTiming timing)
        {
            var gain = Mathf.RoundToInt(BattleSim.QteFever(timing));
            var feverGain = BattleCueCopy.QteFeverGainLine(gain);
            switch (timing)
            {
                case DriveTiming.Perfect:
                    // Robin ND DAMAGE 150% stays inventory. Ordinary-PVE QTE mul unseen.
                    ShowCue(BattleCueCopy.Kind.QtePerfect, 1.20f, feverGain);
                    CombatFeel.ScreenTint(_fieldLayer != null ? _fieldLayer : _root, Color.white, 0.55f, 0.10f);
                    VfxJudge.Play(_root, "perfect", 0f, gain);
                    break;
                case DriveTiming.Great:
                    ShowCue(BattleCueCopy.Kind.QteGreat, 0.90f, feverGain);
                    VfxJudge.Play(_root, "great", 1.2f, gain);
                    break;
                case DriveTiming.Good:
                    ShowCue(BattleCueCopy.Kind.QteGood, 0.70f, feverGain);
                    VfxJudge.Play(_root, "good", 1f, gain);
                    break;
                default:
                    ShowCue(BattleCueCopy.Kind.QteBad, 0.60f, feverGain);
                    break;
            }
        }

        void Refresh()
        {
            if (_host == null) return;
            var battle = _host.Battle;
            if (battle == null) return;
            VfxTipPlate.SetSuppressed(TutorialPresentationBlocked(battle));
            if (_fieldWave != battle.WaveIndex) SpawnEnemies();

            int hpNow = 0, hpMax = 0, phpNow = 0, phpMax = 0;
            if (battle.Enemies != null)
            {
                for (int i = 0; i < battle.Enemies.Count; i++)
                {
                    var foe = battle.Enemies[i];
                    if (foe == null) continue;
                    hpNow += foe.Hp;
                    hpMax += foe.MaxHp;
                }
            }
            if (battle.Allies != null)
            {
                for (int i = 0; i < battle.Allies.Length; i++)
                {
                    var ally = battle.Allies[i];
                    if (ally == null) continue;
                    phpNow += ally.Hp;
                    phpMax += ally.MaxHp;
                }
            }
            if (_archHp != null)
                _archHp.fillAmount = hpMax <= 0 ? 0f : (float)hpNow / hpMax;
            var pct = hpMax <= 0 ? 0 : Mathf.RoundToInt(100f * hpNow / hpMax);
            var hard = _host.SaveData != null && _host.SaveData.UseHard;
            if (_archNum != null)
            {
                // Primary P0/Robin mid-fight: ENEMY HP TOTAL (r62/r68).
                // Hard QTE PERFECT window (r55): short ENEMY HP.
                // P0 t510 PHASE splash: TOTAL + (CURRENT) stack.
                // P0 t360 SHOWTIME: ENEMY HP LEFT.
                var phaseSplash = WavePreview.VisibleKind == WaveCueKind.WaveAdvance;
                var showtime = _showtimeT > 0.05f || VfxShowtime.AnyLive();
                string hpLab;
                if (hard && VfxJudge.AnyLive())
                    hpLab = BattleCueCopy.EnemyHpShort;
                else if (!hard && phaseSplash)
                    hpLab = BattleCueCopy.EnemyHpLeft + "\n" + BattleCueCopy.EnemyHpCurrent;
                else if (!hard && showtime)
                    hpLab = BattleCueCopy.EnemyHpLeftAlt;
                else
                    hpLab = BattleCueCopy.EnemyHpLeft;
                _archNum.text = BattleCueCopy.PctOverLabel(pct, hpLab);
            }
            if (_archBoss != null)
            {
                var bossName = "";
                if (hard && battle.Enemies != null)
                {
                    for (int i = 0; i < battle.Enemies.Count; i++)
                    {
                        var foe = battle.Enemies[i];
                        if (foe == null || !foe.Alive || foe.Def == null || !foe.Def.IsBoss) continue;
                        bossName = (foe.Def.Name ?? "").ToUpperInvariant();
                        break;
                    }
                }
                _archBoss.text = bossName;
            }
            var enemyDrivePct = 0;
            if (battle.Enemies != null)
            {
                var maxCh = 0f;
                for (int i = 0; i < battle.Enemies.Count; i++)
                    if (battle.Enemies[i] != null && battle.Enemies[i].Alive)
                        maxCh = Mathf.Max(maxCh, battle.Enemies[i].Charge);
                enemyDrivePct = Mathf.RoundToInt(Mathf.Clamp(maxCh, 0f, 100f));
            }
            if (_archDrive != null)
            {
                // Ordinary mid-fight: ENEMY DRIVE.
                // Hard mid: PREPARATION. Hard SHOWTIME: ENEMY DRIVE (r62).
                // Hard Fever window: DMG OF TOTAL (r67).
                var driveLab = hard
                    ? (battle.FeverActive
                        ? BattleCueCopy.DmgOfTotal
                        : ((_showtimeT > 0.05f || VfxShowtime.AnyLive())
                            ? BattleCueCopy.EnemyDrive
                            : BattleCueCopy.EnemyPreparation))
                    : BattleCueCopy.EnemyDrive;
                _archDrive.text = BattleCueCopy.PctOverLabel(enemyDrivePct, driveLab);
            }
            if (_archMeta != null)
            {
                var table = Catalog.Chapter(hard);
                var stName = "";
                var idx = _host.ActiveStageIndex;
                StageDef stage = null;
                if (table != null && idx >= 0 && idx < table.Length && table[idx] != null)
                {
                    stage = table[idx];
                    var raw = stage.Name ?? "";
                    if (hard)
                    {
                        // Hard r30/r70: full EN arch "Stage 8 You Won't Get Away! (Hard)".
                        // Do not take only the last token (breaks multi-word titles).
                        var stub = BattleCueCopy.HardStageArchStub(idx + 1);
                        if (!string.IsNullOrEmpty(stub))
                            stName = stub;
                        else
                        {
                            stName = raw.Trim();
                            if (stName.EndsWith("困难"))
                                stName = stName.Substring(0, stName.Length - 2).TrimEnd();
                            if (!stName.EndsWith("(Hard)"))
                                stName = stName + " (Hard)";
                        }
                    }
                    else
                    {
                        // Prefer name after chapter prefix for ordinary.
                        var sp = raw.LastIndexOf(' ');
                        stName = sp >= 0 && sp + 1 < raw.Length ? raw.Substring(sp + 1) : raw;
                    }
                }
                var maxPhase = StagePhaseCount(stage);
                var phaseN = HudPhaseDuringSplash(battle, maxPhase);
                // Primary GT: stage name over PHASE n/m (P0 t250/t510; Hard mid r13/r38).
                // Hard SHOWTIME OCR FINAL/AREA inventory only (r21/r15) — live stays PHASE.
                // P0 PHASE splash: HUD stays on the previous n/m until first-off.
                _archMeta.text = string.IsNullOrEmpty(stName)
                    ? BattleCueCopy.PhaseLine(phaseN, maxPhase)
                    : (stName + "\n" + BattleCueCopy.PhaseLine(phaseN, maxPhase));
                VfxPhaseBar.Draw(_root, phaseN, maxPhase);
            }
            else
                VfxPhaseBar.Draw(_root, HudPhaseDuringSplash(battle, 2), 2);
            // P0 live field has no left TOTAL/DPS/HEAL box. Tip t444 is a different plate.
            // Fever combo words live on VfxComboBanner (N COMBO / N DAMAGE).
            VfxDpsPanel.Hide(_root);
            if (_timerLabel != null)
            {
                var sec = Mathf.Max(0, Mathf.CeilToInt(battle.TimeLeft));
                // Primary GT: stacked mm:ss over BATTLE TIME (P0 t250; Hard r70).
                // BATTLE NO. n (r55) stays inventory — trigger UNKNOWN.
                _timerLabel.text = "<size=16><b>" + (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00")
                    + "</b></size>\n<size=11>" + BattleCueCopy.BattleTime + "</size>";
            }
            if (_enemyPips != null && battle.Enemies != null)
            {
                for (int i = 0; i < _enemyPips.Length; i++)
                {
                    if (_enemyPips[i] == null) continue;
                    var lit = enemyDrivePct >= (i + 1) * (100f / _enemyPips.Length);
                    _enemyPips[i].color = lit ? VisualTokens.StarEvolved : new Color(0.26f, 0.19f, 0.08f, 0.75f);
                }
            }
            if (_partyHp != null)
                _partyHp.fillAmount = phpMax <= 0 ? 0f : (float)phpNow / phpMax;
            if (_partyHpLabel != null)
            {
                // Team HP tip window: SHARED HP TOTAL (t345).
                // Drive tip after: PARTY HP TOTAL (t380). Pre-Drive low-level: CHILD (t446/t512).
                // After Fever session on low-level: MY HP TOTAL (t456).
                // Cap story mid / cap PHASE splash: SKILL HP TOTAL (t452/t065).
                // Hard mid: MY (r70); Hard SHOWTIME / Drive QTE judge: PARTY (r50/r55).
                var phpPct = phpMax <= 0 ? 0 : Mathf.RoundToInt(100f * phpNow / phpMax);
                var maxAllyLv = MaxAllyLevel(battle);
                var phaseSplash = WavePreview.VisibleKind == WaveCueKind.WaveAdvance;
                string partyLabel;
                if (hard && (_showtimeT > 0.05f || VfxJudge.AnyLive() || VfxShowtime.AnyLive()))
                    partyLabel = BattleCueCopy.PartyHpTotal;
                else if (hard)
                    partyLabel = BattleCueCopy.MyHpTotal;
                // P0 t280/t340 early Tutorial PHASE splash: CHILD HP TOTAL (pre SkillReady tip).
                // P0 t372 after SkillReady tip / t065 cap: SKILL HP TOTAL.
                // P0 t510 Stage4 PHASE: TEAM HP TOTAL + (CURRENT) after speed/fever tips.
                else if (!battle.FeverActive && phaseSplash)
                    partyLabel = (_tipSpeedShown || _feverSessionSeen)
                        ? (BattleCueCopy.TeamHpTotalAlt + "\n" + BattleCueCopy.TeamHpCurrent)
                        : (maxAllyLv < Growth.MaxLevel && !_tipSkillReadyShown
                            ? BattleCueCopy.ChildHpTotal
                            : BattleCueCopy.TeamHpTotal);
                // P0 t80 Slide tip on cap story: MY HP TOTAL over PARTY HP TOTAL stack.
                else if (!battle.FeverActive && VfxTipPlate.AnyLive() && maxAllyLv >= Growth.MaxLevel)
                    partyLabel = BattleCueCopy.MyHpTotal + "\n" + BattleCueCopy.PartyHpTotal;
                // P0 t435 Fever tip: SKILL HP TOTAL on low-level tray.
                else if (!battle.FeverActive && VfxTipPlate.FeverTipSession())
                    partyLabel = BattleCueCopy.TeamHpTotal;
                else if (!battle.FeverActive && _tipTeamHpShown && !_tipChildsShown)
                    // P0 t345 tip: TOTAL HP TOTAL on the green arc (SHARED stays inventory OCR).
                    partyLabel = BattleCueCopy.TotalHpTotal;
                else if (!battle.FeverActive && _tipChildsShown && !_tipSkillReadyShown && !_tipDriveShown)
                    // P0 t250 Childs tip: arc stays SKILL HP TOTAL (CURRENT HP inventory).
                    partyLabel = BattleCueCopy.TeamHpTotal;
                // P0 t352 SkillReady tip: SKILL UP TOTAL on the green arc.
                else if (!battle.FeverActive && _tipSkillReadyShown && !_tipSlideShown && !_tipDriveShown)
                    partyLabel = BattleCueCopy.SkillUpTotal;
                // P0 t358 Slide tip: SKILL HP TOTAL (not CHILD).
                else if (!battle.FeverActive && maxAllyLv < Growth.MaxLevel
                    && _tipSlideShown && !_tipDriveShown)
                    partyLabel = BattleCueCopy.TeamHpTotal;
                else if (!battle.FeverActive && maxAllyLv < Growth.MaxLevel && _feverSessionSeen)
                    partyLabel = BattleCueCopy.MyHpTotal;
                else if (!battle.FeverActive && maxAllyLv < Growth.MaxLevel
                    && _tipDriveShown && !phaseSplash)
                    partyLabel = BattleCueCopy.PartyHpTotal;
                else if (!battle.FeverActive && maxAllyLv < Growth.MaxLevel)
                    partyLabel = BattleCueCopy.ChildHpTotal;
                else if (!battle.FeverActive && battle.WaveIndex <= 0 && !phaseSplash)
                    partyLabel = BattleCueCopy.ChildHpTotal;
                else
                    partyLabel = BattleCueCopy.TeamHpTotal;
                _partyHpLabel.text = BattleCueCopy.PctOverLabel(phpPct, partyLabel);
            }
            if (_driveBar != null)
            {
                _driveBar.fillAmount = Mathf.Clamp01(battle.Drive / 100f);
                if (battle.Drive >= 100f)
                {
                    var pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6.2f));
                    _driveBar.color = Color.Lerp(VisualTokens.DriveOrange, VisualTokens.YellowConfirm, pulse);
                }
                else _driveBar.color = VisualTokens.YellowValue;
            }
            if (_driveLabel != null)
            {
                // Mid-fight tutorial: SKILL GAUGE TOTAL (P0 t500).
                // Cap story mid (Heaven's Dust t64/t88): SKILL UP TOTAL on the thin secondary.
                // SHOWTIME / PHASE splash: DRIVE GAUGE (t096).
                // Drive Tables tip window / Drive Ready field: DRIVE TOTAL (t388; Hard r48).
                var phaseSplash = WavePreview.VisibleKind == WaveCueKind.WaveAdvance;
                var maxAllyLv = MaxAllyLevel(battle);
                string gauge;
                if (_driveTotalLabelT > 0.05f
                    || (battle.Drive >= 100f && !battle.FeverActive && _showtimeT <= 0.05f && !phaseSplash))
                    gauge = BattleCueCopy.DriveTotal;
                else if (_showtimeT > 0.05f || phaseSplash)
                    gauge = BattleCueCopy.DriveGauge;
                else if (_tipDriveShown && !_tipDriveTablesShown)
                    // P0 t365 Drive tip: short SKILL GAUGE (not TOTAL).
                    gauge = BattleCueCopy.SkillGauge;
                else if (!hard && maxAllyLv >= Growth.MaxLevel
                    && !battle.FeverActive && battle.Drive < 100f
                    && !VfxTipPlate.AnyLive())
                {
                    // P0 t64 kill/phase: SKILL UP TOTAL at 0%. P0 t90 mid: short SKILL GAUGE N%.
                    var enemyEmpty = hpMax > 0 && hpNow <= 0;
                    gauge = enemyEmpty
                        ? BattleCueCopy.SkillUpTotal
                        : BattleCueCopy.SkillGauge;
                }
                else
                    gauge = BattleCueCopy.SkillGaugeTotal;
                // P0 t64: SKILL UP TOTAL reads 0% while the green arc still tracks Drive fill.
                var gaugePct = gauge == BattleCueCopy.SkillUpTotal
                    ? 0
                    : Mathf.RoundToInt(battle.Drive);
                _driveLabel.text = BattleCueCopy.PctOverLabel(gaugePct, gauge);
                _driveLabel.color = battle.Drive >= 100f
                    ? VisualTokens.YellowConfirm
                    : VisualTokens.YellowValue;
            }
            FeverBannerOn = battle.FeverActive;
            if (_feverBar != null && _feverLabel != null)
            {
                // P0 t440/t442: bottom chrome stays FEVER n% even while the window runs (0% once spent).
                var g = Mathf.RoundToInt(battle.FeverGauge);
                _feverBar.fillAmount = Mathf.Clamp01(battle.FeverGauge / 100f);
                _feverBar.color = FeverPink;
                _feverLabel.text = BattleCueCopy.FeverLine(false, g);
                _feverLabel.color = FeverPink;
                if (_feverToward != null)
                {
                    if (battle.FeverActive)
                        _feverToward.text = "";
                    else
                    {
                        // Show fill callout when banking toward Fever (Robin mid-fight).
                        _feverToward.text = g > 0 && g < 100
                            ? BattleCueCopy.FeverTowardLine(g)
                            : "";
                        _feverToward.color = VisualTokens.SlideGreen;
                        // P0 tip t420: gauge tip once while banking.
                        if (!_tipFeverGaugeShown && g >= 40 && g < 100
                            && CanShowTutorialTip(battle))
                        {
                            _tipFeverGaugeShown = true;
                            VfxTipPlate.Show(_root, BattleCueCopy.TipFeverActivate,
                                BattleCueCopy.TipFeverGaugeBody);
                        }
                    }
                }
            }
            var feverStart = battle.FeverActive && !_feverWas;
            var feverEnd = !battle.FeverActive && _feverWas;
            if (feverStart)
            {
                VfxShowtime.KillAll();
                _feverBurstT = 0.90f;
                _feverSessionSeen = true;
                if (_pix != null) _pix.SetFever(true);
                // P0 t442: TIP! Activate Fever Time! plate sits with live FEVER TIME field.
                VfxTipPlate.ShowFeverActivate(_root);
                if ((IsQteStamp(VisibleStamp) && _showtimeT > 0.12f) || VfxJudge.AnyLive())
                {
                    _feverCueQueued = true;
                    _feverCueDelay = Mathf.Max(_showtimeT, 0.90f) + 0.06f;
                }
                else
                    BeginFeverBanner();
            }
            _feverWas = battle.FeverActive;
            if (feverEnd)
            {
                if (_pix != null) _pix.SetFever(false);
                VfxSpeedLines.Hide();
                VfxFeverOverlay.Hide();
                VfxTipPlate.Hide();
                VfxRouter.EndFever(_root);
            }
            if (_feverBurstT > 0f) _feverBurstT -= Time.unscaledDeltaTime;
            if (_feverFlash != null) _feverFlash.color = Color.clear;
            if (_speedLabel != null)
            {
                var sp = battle.Speed < 1 ? 1 : (battle.Speed > 3 ? 3 : battle.Speed);
                _speedLabel.text = ">> X" + sp + " SPEED";
            }
            if (_autoLabel != null) _autoLabel.text = AutoWord(battle.Auto);
            if (_pauseLabel != null)
            {
                // Primary GT: || PAUSE (P0 + Hard r30/r70). II PAUSE stays OCR inventory.
                _pauseLabel.text = battle.Paused ? BattleCueCopy.ResumeHud : BattleCueCopy.PauseHud;
            }
            if (_escapeLabel != null)
            {
                // Keep ESCAPE hidden on primary GT path (contrast-only control).
                _escapeLabel.gameObject.SetActive(false);
            }
            // Repeat: PauseBoard always. Hard field Repeat (Robin r55/r70 under PAUSE).
            // Ordinary P0 t440 live field has no mid Repeat / SKIP / Log.
            if (_repeatLabel != null)
            {
                _repeatLabel.text = "Repeat";
                _repeatLabel.gameObject.SetActive(hard);
            }
            if (_skipLabel != null) _skipLabel.gameObject.SetActive(false);
            if (_logLabel != null) _logLabel.gameObject.SetActive(false);

            if (_fieldAllies != null)
            {
                for (int i = 0; i < _fieldAllies.Length; i++)
                {
                    if (_fieldAllies[i] == null) continue;
                    var pulse = battle.FeverActive ? 1f + 0.06f * (((int)CombatFeel.PixelBeat + i) & 1) : 1f;
                    _fieldAllies[i].transform.localScale = Vector3.one * pulse;
                    var ally = battle.Allies != null && i < battle.Allies.Length ? battle.Allies[i] : null;
                    _fieldAllies[i].Apply(ally);
                }
            }
            if (_fieldEnemies != null && battle.Enemies != null)
            {
                var focus = battle.FocusEnemySlot;
                for (int i = 0; i < _fieldEnemies.Length && i < battle.Enemies.Count; i++)
                {
                    if (_fieldEnemies[i] == null) continue;
                    _fieldEnemies[i].Apply(battle.Enemies[i]);
                    _fieldEnemies[i].SetFocused(i == focus);
                }
            }
            SetFieldSplashHidden(WavePreview.VisibleKind == WaveCueKind.WaveAdvance);

            TickOverlays();
            DrainCombatLog();
            DrainCasts();
            RefreshPortraits();
            RefreshDriveSelect(battle);
            // A cast/grade may have started after the first check in this same frame.
            VfxTipPlate.SetSuppressed(TutorialPresentationBlocked(battle));
        }

        void RefreshPortraits()
        {
            var battle = _host != null ? _host.Battle : null;
            if (battle == null || battle.Allies == null || _chargeRing == null) return;
            var hard = _host.SaveData != null && _host.SaveData.UseHard;
            for (int i = 0; i < _chargeRing.Length; i++)
            {
                var u = i < battle.Allies.Length ? battle.Allies[i] : null;
                if (u == null) continue;
                BattleHudState.Collect(battle, i, _hudFlags);
                var charge = Mathf.Clamp01(u.Charge / 100f);
                var tapReady = u.Alive && u.Charge >= 100f;
                var slideReady = tapReady && u.SlideCd <= 0f;
                var driveReady = u.Alive && battle.Drive >= 100f;
                var port = PortAnchor(i);
                if (driveReady && (_driveWas == null || !_driveWas[i]))
                    VfxDriveReady.Play(_root, port);
                // Tutorial tip plates (inventory copy). Once each; skip during Fever tip chain.
                if (CanShowTutorialTip(battle))
                {
                    if (tapReady && !_tipSkillReadyShown && _tipChildsShown)
                    {
                        _tipSkillReadyShown = true;
                        // P0 t354: Mona's Tips + full Skill ready stem.
                        VfxTipPlate.Show(_root, BattleCueCopy.MonaTipsHeader, BattleCueCopy.TipSkillReadyMonaBody);
                    }
                    else if (slideReady && !_tipSlideShown)
                    {
                        _tipSlideShown = true;
                        VfxTipPlate.Show(_root, BattleCueCopy.TipSlideSkill, BattleCueCopy.TipSlideSkillBody);
                    }
                    else if (slideReady && _tipSlideShown && !_tipSlideMonaShown && !VfxTipPlate.AnyLive())
                    {
                        _tipSlideMonaShown = true;
                        // P0 t355: Mona's Tips + Slide Skills are powerful attacks!
                        VfxTipPlate.Show(_root, BattleCueCopy.MonaTipsHeader, BattleCueCopy.TipSlideMonaBody);
                    }
                    else if (slideReady && _tipSlideMonaShown && !_tipSlidePowerShown && !VfxTipPlate.AnyLive())
                    {
                        _tipSlidePowerShown = true;
                        // P0 t370: TIP! + Use Slide Skills for powerful attacks!
                        VfxTipPlate.Show(_root, BattleCueCopy.TipSlidePower, BattleCueCopy.TipSlidePowerBody);
                    }
                    else if (driveReady && !_tipDriveShown)
                    {
                        _tipDriveShown = true;
                        VfxTipPlate.Show(_root, BattleCueCopy.TipDriveSkill, BattleCueCopy.TipDriveSkillBody);
                    }
                    else if (driveReady && _tipDriveShown && !_tipDriveTimingShown)
                    {
                        _tipDriveTimingShown = true;
                        VfxTipPlate.Show(_root, BattleCueCopy.TipDriveTiming, BattleCueCopy.TipDriveTimingBody);
                    }
                    else if (driveReady && _tipDriveTimingShown && !_tipDriveTablesShown)
                    {
                        _tipDriveTablesShown = true;
                        _driveTotalLabelT = 5.20f;
                        VfxTipPlate.Show(_root, BattleCueCopy.TipDriveTables, BattleCueCopy.TipDriveTablesBody);
                        // P0 t388: "13% Skill Rate" secondary near Drive tip plate.
                        CombatFeel.NamePopStack(_root, new Vector2(0.50f, 0.12f),
                            "13% " + BattleCueCopy.SkillRate, VisualTokens.YellowValue);
                    }
                    else if (!_tipSpeedShown
                        && (_tipDriveTablesShown || _tipSlidePowerShown)
                        && !VfxTipPlate.AnyLive())
                    {
                        _tipSpeedShown = true;
                        // P0 t451: Mona's Tips + X2 SPEED body (X1 SPEED highlight on HUD).
                        VfxTipPlate.Show(_root, BattleCueCopy.MonaTipsHeader, BattleCueCopy.TipSpeedBody);
                    }
                }
                if (_tapWas != null) _tapWas[i] = tapReady;
                if (_slideWas != null) _slideWas[i] = slideReady;
                if (_driveWas != null) _driveWas[i] = driveReady;
                VfxStatusIcons.Draw(_root, port, StatusLabels(u), i);
                if (_portFlashT != null && _portFlashT[i] > 0f)
                    _portFlashT[i] -= Time.unscaledDeltaTime;
                if (_chargeRing[i] != null)
                {
                    _chargeRing[i].fillAmount = charge;
                    if (!u.Alive)
                        _chargeRing[i].color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                    else if (tapReady)
                        _chargeRing[i].color = VisualTokens.YellowConfirm;
                    else
                        _chargeRing[i].color = Color.Lerp(new Color(1f, 0.95f, 0.80f, 0.18f), VisualTokens.YellowValue, charge);
                    var pulse = tapReady || driveReady ? 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 6.4f) : 1f;
                    _chargeRing[i].transform.localScale = Vector3.one * pulse;
                }
                if (_portraitRim[i] != null && u.Def != null)
                {
                    var el = VisualTokens.Element(u.Def.Element);
                    Color rim;
                    if (!u.Alive) rim = new Color(0.25f, 0.25f, 0.25f, 0.7f);
                    else if (driveReady) rim = VisualTokens.DriveOrange;
                    else if (tapReady) rim = VisualTokens.YellowConfirm;
                    else
                    {
                        rim = Color.Lerp(VisualTokens.GoldMetal, el, 0.16f);
                        var shimmer = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.1f + i * 1.3f);
                        rim = Color.Lerp(rim, VisualTokens.GoldTitle, 0.10f * shimmer);
                    }
                    if (_portFlashT != null && _portFlashT[i] > 0f)
                        rim = Color.Lerp(rim, VisualTokens.TapWhite, Mathf.Clamp01(_portFlashT[i] / 0.28f));
                    _portraitRim[i].color = rim;
                }
                if (_tapFlash != null && _tapFlash[i] != null)
                {
                    var ping = _portFlashT != null ? Mathf.Clamp01(_portFlashT[i] / 0.28f) : 0f;
                    _tapFlash[i].color = ping > 0f ? new Color(1f, 0.95f, 0.75f, 0.55f * ping) : Color.clear;
                }
                if (_feverArc != null && _feverArc[i] != null)
                {
                    if (battle.FeverActive && u.Alive)
                    {
                        var rainbow = CombatFeel.FeverHue(Time.unscaledTime * 0.7f + i * 0.14f);
                        rainbow.a = ((int)CombatFeel.PixelBeat + i) % 2 == 0 ? 0.80f : 0.42f;
                        _feverArc[i].color = rainbow;
                        _feverArc[i].fillAmount = 0.26f + 0.06f * (((int)CombatFeel.PixelBeat + i) % 3);
                    }
                    else _feverArc[i].color = Color.clear;
                }
                if (_slidePip != null && _slidePip[i] != null)
                {
                    // P0 t446: green "+" when tap-ready. Hard r58: "+" over red SLIDE when slide-ready.
                    if (slideReady)
                    {
                        _slidePip[i].text = BattleCueCopy.SlideReadyPipStack;
                        _slidePip[i].color = Color.white;
                    }
                    else if (tapReady)
                    {
                        _slidePip[i].text = BattleCueCopy.PortraitReadyPlus;
                        var c = VisualTokens.SlideGreen;
                        c.a = 1f;
                        _slidePip[i].color = c;
                    }
                    else _slidePip[i].color = Color.clear;
                }
                if (_drivePip != null && _drivePip[i] != null)
                {
                    var c = VisualTokens.DriveOrange;
                    c.a = (driveReady && !battle.FeverActive) ? 1f : 0f;
                    _drivePip[i].color = c;
                }
                if (_readyTag != null && _readyTag[i] != null)
                {
                    if (!u.Alive)
                    {
                        _readyTag[i].text = "";
                    }
                    else if (QteOpen && battle.PendingDriveSlot == i)
                    {
                        // Primary P0 ~t365: portrait "N DRIVE TIME" during Drive window.
                        var left = Mathf.CeilToInt(Mathf.Max(0.01f, battle.QteRemaining));
                        _readyTag[i].text = BattleCueCopy.DriveTimeLine(left);
                        _readyTag[i].color = VisualTokens.DriveOrange;
                    }
                    else if (battle.FeverActive)
                    {
                        // Hard r67 SHOWTIME+Fever: cool portraits show N FEVER TIME.
                        // P0 t442/t452 ready portraits stay WEAKPOINT.
                        if (u.SlideCd > 0.05f)
                        {
                            var sec = Mathf.CeilToInt(u.SlideCd);
                            _readyTag[i].text = BattleCueCopy.FeverTimeLine(sec);
                            _readyTag[i].color = VisualTokens.FeverGold;
                        }
                        else
                        {
                            _readyTag[i].text = BattleCueCopy.WeakPointPortraitLine;
                            _readyTag[i].color = VisualTokens.Ember;
                        }
                    }
                    else if (hard
                        && (_showtimeT > 0.05f || VfxShowtime.AnyLive())
                        && battle.FeverGauge >= 85f
                        && u.SlideCd <= 0.05f)
                    {
                        // Hard r15: near-Fever SHOWTIME tray WEAKPOINT + SKILL RESERVE.
                        _readyTag[i].text = BattleCueCopy.WeakPointSkillReserveLine;
                        _readyTag[i].color = VisualTokens.Ember;
                    }
                    else if (driveReady)
                    {
                        _readyTag[i].text = BattleCueCopy.DriveReadyPortraitLine;
                        _readyTag[i].color = VisualTokens.DriveOrange;
                    }
                    else if (u.SlideCd > 0.05f)
                    {
                        // Mid-fight: COOL TIME (Hard r28/r56). Hard SHOWTIME: LEAD TIME (r50 FEVER~0).
                        // Hard SHOWTIME + banked Fever: CLICK TIME (r67 FEVER 40%).
                        // Ordinary SHOWTIME / Drive tip: SKILL TIME (P0 t365). SLIDE TIME inventory.
                        var sec = Mathf.CeilToInt(u.SlideCd);
                        var showtime = _showtimeT > 0.05f || VfxShowtime.AnyLive();
                        if (hard && showtime)
                            _readyTag[i].text = battle.FeverGauge >= 35f
                                ? BattleCueCopy.ClickTimeLine(sec)
                                : BattleCueCopy.LeadTimeLine(sec);
                        else if (showtime || _tipDriveShown)
                            _readyTag[i].text = BattleCueCopy.SkillTimeLine(sec);
                        else
                            _readyTag[i].text = BattleCueCopy.CoolTimeLine(sec);
                        _readyTag[i].color = VisualTokens.TextMuted;
                    }
                    else if (tapReady)
                    {
                        // P0 t446 / PVP5: ready is the green "+" pip, not TAP READY text.
                        _readyTag[i].text = "";
                    }
                    else _readyTag[i].text = "";
                }
                if (_readyDash != null && _readyDash[i] != null)
                {
                    var showDash = u.Alive && (tapReady || slideReady);
                    _readyDash[i].color = showDash
                        ? new Color(VisualTokens.YellowConfirm.r, VisualTokens.YellowConfirm.g, VisualTokens.YellowConfirm.b, 0.38f)
                        : Color.clear;
                    if (showDash)
                    {
                        var pulse = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 7.2f + i);
                        _readyDash[i].transform.localScale = Vector3.one * pulse;
                    }
                }
                if (_hpNum[i] != null)
                {
                    _hpNum[i].text = u.Hp.ToString();
                    _hpNum[i].color = !u.Alive
                        ? VisualTokens.TextMuted
                        : (u.Hp >= u.MaxHp ? VisualTokens.YellowValue : Color.white);
                }
                if (_hpBar != null && _hpBar[i] != null)
                {
                    var frac = u.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)u.Hp / u.MaxHp);
                    _hpBar[i].fillAmount = frac;
                    _hpBar[i].color = !u.Alive
                        ? VisualTokens.TextMuted
                        : (frac > 0.35f ? VisualTokens.YellowValue : VisualTokens.StarEvolved);
                }
                if (_portraitDim[i] != null)
                {
                    if (!u.Alive) _portraitDim[i].alpha = 0.35f;
                    else if (_warnT > 0f) _portraitDim[i].alpha = 0.40f;
                    else _portraitDim[i].alpha = 1f;
                }
                if (_nameTag[i] != null)
                    _nameTag[i].text = PortraitTrayNameText(u.Def != null ? u.Def.Id : null,
                        u.Def != null ? u.Def.Name : "");
                if (_lvTag[i] != null)
                {
                    _lvTag[i].text = PortraitMaxBadgeText(u.Def != null ? u.Def.Id : null);
                    _lvTag[i].color = !u.Alive ? VisualTokens.TextMuted : VisualTokens.YellowValue;
                }
                if (_costumeTag != null && _costumeTag[i] != null)
                {
                    var lv = 1;
                    if (u.Def != null && _host != null && _host.SaveData != null)
                        lv = _host.SaveData.GetUnit(u.Def.Id).Level;
                    var line = BattleCueCopy.PortraitInnerCostumeLine(
                        u.Def != null ? u.Def.Name : "", lv);
                    _costumeTag[i].text = line ?? "";
                    _costumeTag[i].color = !u.Alive
                        ? VisualTokens.TextMuted
                        : VisualTokens.TapWhite;
                }
            }
        }

        string PortraitLevelText(string unitId)
        {
            if (string.IsNullOrEmpty(unitId) || _host == null || _host.SaveData == null)
                return BattleCueCopy.PortraitLevelLine(1);
            return BattleCueCopy.PortraitLevelLine(_host.SaveData.GetUnit(unitId).Level);
        }

        string PortraitTrayNameText(string unitId, string unitName)
        {
            var lv = 1;
            if (!string.IsNullOrEmpty(unitId) && _host != null && _host.SaveData != null)
                lv = _host.SaveData.GetUnit(unitId).Level;
            return BattleCueCopy.PortraitTrayName(lv, ShortName(unitName));
        }

        string PortraitMaxBadgeText(string unitId)
        {
            if (string.IsNullOrEmpty(unitId) || _host == null || _host.SaveData == null)
                return BattleCueCopy.PortraitMaxBadge(1);
            return BattleCueCopy.PortraitMaxBadge(_host.SaveData.GetUnit(unitId).Level);
        }

        bool TutorialPresentationBlocked(BattleSim battle)
        {
            return battle == null || battle.Paused || battle.Outcome != BattleOutcome.InProgress
                || QteOpen || battle.PendingDriveSlot >= 0 || battle.HoldLeftSec > 0f
                || _cutHoldT > 0f || _showtimeT > 0f
                || (_pix != null && _pix.Showing)
                || VfxShowtime.AnyLive() || VfxJudge.AnyLive()
                || WavePreview.VisibleKind != WaveCueKind.None;
        }

        bool CanShowTutorialTip(BattleSim battle)
        {
            return battle != null && !battle.FeverActive && !VfxTipPlate.AnyLive()
                && !TutorialPresentationBlocked(battle);
        }

        void TickOverlays()
        {
            var battle = _host != null ? _host.Battle : null;
            var tutorialBlocked = TutorialPresentationBlocked(battle);
            VfxTipPlate.SetSuppressed(tutorialBlocked);
            var advanceTutorial = !tutorialBlocked && !battle.FeverActive;
            if (_feverCueQueued)
            {
                _feverCueDelay -= Time.unscaledDeltaTime;
                if (_feverCueDelay <= 0f)
                {
                    _feverCueQueued = false;
                    var live = _host != null ? _host.Battle : null;
                    if (live != null && live.FeverActive)
                        BeginFeverBanner();
                }
            }
            // P0 tip t58: Tap skill tip first, then Keep attacking (t60), Team HP (t345), Childs (t250).
            // An elapsed delay stays pending until the other plate has finished.
            if (advanceTutorial && !_tipTapShown)
            {
                _tipTapDelay = Mathf.Max(0f, _tipTapDelay - Time.unscaledDeltaTime);
                if (_tipTapDelay <= 0f
                    && !VfxTipPlate.AnyLive()
                    && (_host == null || _host.Battle == null || !_host.Battle.FeverActive))
                {
                    _tipTapShown = true;
                    _tipKeepDelay = 4.40f;
                    VfxTipPlate.Show(_root, BattleCueCopy.TipTapSkill, BattleCueCopy.TipTapSkillBody);
                }
            }
            if (advanceTutorial && _tipTapShown && !_tipKeepShown)
            {
                _tipKeepDelay = Mathf.Max(0f, _tipKeepDelay - Time.unscaledDeltaTime);
                if (_tipKeepDelay <= 0f
                    && !VfxTipPlate.AnyLive()
                    && (_host == null || _host.Battle == null || !_host.Battle.FeverActive))
                {
                    _tipKeepShown = true;
                    _tipTeamHpDelay = 4.40f;
                    VfxTipPlate.Show(_root, BattleCueCopy.TipKeepAttacking, "");
                }
            }
            if (advanceTutorial && _tipKeepShown && !_tipTeamHpShown)
            {
                _tipTeamHpDelay = Mathf.Max(0f, _tipTeamHpDelay - Time.unscaledDeltaTime);
                if (_tipTeamHpDelay <= 0f
                    && !VfxTipPlate.AnyLive()
                    && (_host == null || _host.Battle == null || !_host.Battle.FeverActive))
                {
                    _tipTeamHpShown = true;
                    _tipChildsDelay = 4.40f;
                    VfxTipPlate.Show(_root, BattleCueCopy.TeamHpTip, BattleCueCopy.TipTeamHpBody);
                }
            }
            // P0 cont34 t250: Childs tray tip after Team HP tip.
            if (advanceTutorial && _tipTeamHpShown && !_tipChildsShown)
            {
                _tipChildsDelay = Mathf.Max(0f, _tipChildsDelay - Time.unscaledDeltaTime);
                if (_tipChildsDelay <= 0f
                    && !VfxTipPlate.AnyLive()
                    && (_host == null || _host.Battle == null || !_host.Battle.FeverActive))
                {
                    _tipChildsShown = true;
                    _tipChildsMoreDelay = 4.40f;
                    VfxTipPlate.Show(_root, BattleCueCopy.MonaTipsHeader, BattleCueCopy.TipChildsTray);
                }
            }
            // P0 t344: more Childs tip after tray tip.
            if (advanceTutorial && _tipChildsShown && !_tipChildsMoreShown)
            {
                _tipChildsMoreDelay = Mathf.Max(0f, _tipChildsMoreDelay - Time.unscaledDeltaTime);
                if (_tipChildsMoreDelay <= 0f
                    && !VfxTipPlate.AnyLive()
                    && (_host == null || _host.Battle == null || !_host.Battle.FeverActive))
                {
                    _tipChildsMoreShown = true;
                    VfxTipPlate.Show(_root, BattleCueCopy.MonaTipsHeader, BattleCueCopy.TipChildsMoreBody);
                }
            }
            if (_driveTotalLabelT > 0f)
                _driveTotalLabelT -= Time.unscaledDeltaTime;
            if (_showtimeT > 0f)
                _showtimeT -= Time.unscaledDeltaTime;
            else if (VisibleStamp != BattleCueCopy.Kind.None)
            {
                VisibleStamp = BattleCueCopy.Kind.None;
                VisibleStampTitle = "";
            }
            if (_showtime != null) _showtime.color = Color.clear;

            if (_stampT > 0f)
            {
                _stampT -= Time.unscaledDeltaTime;
                var fade = Mathf.Clamp01(_stampT / 0.85f);
                if (_stamp != null)
                {
                    var c = _stamp.color;
                    c.a = fade;
                    _stamp.color = c;
                    _stamp.transform.localScale = Vector3.one * (1f + 0.22f * CombatFeel.Step(fade, 4));
                }
                if (_stampSub != null && !string.IsNullOrEmpty(_stampSub.text))
                {
                    var c = _stampSub.color;
                    c.a = fade;
                    _stampSub.color = c;
                }
            }
            else
            {
                if (_stamp != null)
                {
                    var c = _stamp.color;
                    c.a = 0f;
                    _stamp.color = c;
                }
                if (_stampSub != null)
                {
                    var c = _stampSub.color;
                    c.a = 0f;
                    _stampSub.color = c;
                }
            }

            if (_warnT > 0f)
                _warnT -= Time.unscaledDeltaTime;
        }

        void DrainCombatLog()
        {
            var battle = _host != null ? _host.Battle : null;
            if (battle == null || battle.Log == null) return;
            var log = battle.Log;
            while (_logCursor < log.Count)
            {
                var ft = log[_logCursor++];
                if (ft == null) continue;
                BattleFighter target = null;
                if (ft.Ally)
                {
                    if (_fieldAllies != null && ft.UnitSlot >= 0 && ft.UnitSlot < _fieldAllies.Length)
                        target = _fieldAllies[ft.UnitSlot];
                }
                else if (_fieldEnemies != null && ft.UnitSlot >= 0 && ft.UnitSlot < _fieldEnemies.Length)
                    target = _fieldEnemies[ft.UnitSlot];
                if (!string.IsNullOrEmpty(ft.Text) && ft.Text.StartsWith("FX "))
                {
                    var fxAnchor = target != null ? target.Anchor : new Vector2(0.5f, 0.58f);
                    if (_presentation != null)
                    {
                        _presentation.PlayCue(new PresentationCue(
                            PresentationCueKind.Buff, ft.UnitSlot, ft.Ally, ft.CasterSlot, ft.CasterAlly,
                            ft.Kind, 0, false, false, false, FxWord(ft.Text), Element.Dark, fxAnchor, fxAnchor));
                    }
                    else
                        VfxRouter.OnBuff(_fieldLayer != null ? _fieldLayer : _root, fxAnchor, FxWord(ft.Text));
                    continue;
                }
                if (target != null)
                {
                    if (_presentation == null) target.Hit();
                    FloatHop.Spawn(_fieldLayer != null ? _fieldLayer : _root, target.Anchor + new Vector2(0f, 0.08f),
                        (ft.Crit ? "!" : "") + ft.Text, ft.Crit, ft.Heal, ft.Kind, ft.Fever);
                    if (ft.Crit)
                        CombatFeel.NamePopStack(_fieldLayer != null ? _fieldLayer : _root, target.Anchor + new Vector2(0f, 0.12f),
                            "CRIT", VisualTokens.StarEvolved);
                    if (ft.Fever && ft.Crit && !ft.Heal)
                        CombatFeel.NamePopStack(_fieldLayer != null ? _fieldLayer : _root, target.Anchor + new Vector2(0.02f, 0.18f),
                            BattleCueCopy.WeakPoint, VisualTokens.Ember);
                    if (ft.CasterAlly && _portFlashT != null && ft.CasterSlot >= 0 && ft.CasterSlot < _portFlashT.Length)
                        _portFlashT[ft.CasterSlot] = ft.Kind == SkillType.Drive ? 0.40f : 0.28f;
                    int comboDmg;
                    if (ft.Fever && _pix != null && int.TryParse(StripPlus(ft.Text), out comboDmg))
                        _pix.AddCombo(comboDmg);
                    if (ft.Fever) CanvasShake.Punch(14f, 0.16f);
                    else if (ft.Kind == SkillType.Drive) CanvasShake.Punch(28f, 0.32f);
                    else if (ft.Kind == SkillType.Slide) CanvasShake.Punch(16f, 0.20f);
                    else if (ft.Crit) CanvasShake.Punch(14f, 0.18f);
                    else CanvasShake.Punch(9f, 0.14f);
                }
                BattleFighter caster = null;
                if (ft.CasterAlly)
                {
                    if (_fieldAllies != null && ft.CasterSlot >= 0 && ft.CasterSlot < _fieldAllies.Length)
                        caster = _fieldAllies[ft.CasterSlot];
                }
                else if (_fieldEnemies != null && ft.CasterSlot >= 0 && ft.CasterSlot < _fieldEnemies.Length)
                    caster = _fieldEnemies[ft.CasterSlot];
                int amt;
                int.TryParse(StripPlus(ft.Text), out amt);
                var from = caster != null ? caster.Anchor : (target != null ? target.Anchor : Vector2.zero);
                var to = target != null ? target.Anchor : from;
                var elem = caster != null ? caster.Elem : (target != null ? target.Elem : Element.Dark);
                var cue = new PresentationCue(
                    PresentationCueKind.Hit, ft.UnitSlot, ft.Ally, ft.CasterSlot, ft.CasterAlly,
                    ft.Kind, amt, ft.Crit, ft.Heal, ft.Fever, ft.Text, elem, from, to);
                if (_presentation != null) _presentation.PlayCue(cue);
                else if (caster != null) caster.PlayCast(ft.Kind, ft.Fever);
                if (target != null)
                {
                    if (caster != null)
                        AttackTrail.Fire(_fieldLayer != null ? _fieldLayer : _root, caster.Anchor, target.Anchor,
                            HitColor(ft, caster), ft.Kind, ft.Fever, ft.Heal);
                    if (_presentation == null)
                        VfxRouter.OnHit(_fieldLayer != null ? _fieldLayer : _root,
                            from, target.Anchor, ft.Kind, elem, amt, ft.Crit, ft.Heal, ft.Fever, ft.Text);
                    var dead = false;
                    if (ft.Ally && battle.Allies != null && ft.UnitSlot >= 0 && ft.UnitSlot < battle.Allies.Length
                        && battle.Allies[ft.UnitSlot] != null)
                        dead = !battle.Allies[ft.UnitSlot].Alive;
                    else if (!ft.Ally && battle.Enemies != null && ft.UnitSlot >= 0 && ft.UnitSlot < battle.Enemies.Count
                        && battle.Enemies[ft.UnitSlot] != null)
                        dead = !battle.Enemies[ft.UnitSlot].Alive;
                    if (dead)
                    {
                        var ko = new PresentationCue(
                            PresentationCueKind.Death, ft.UnitSlot, ft.Ally, ft.CasterSlot, ft.CasterAlly,
                            ft.Kind, amt, ft.Crit, false, ft.Fever, "", elem, from, to);
                        if (_presentation != null) _presentation.PlayCue(ko);
                        else VfxRouter.OnKo(target.transform, elem);
                    }
                }
            }
        }

        void CompleteTutorialSkill(SkillType skill)
        {
            if (skill != SkillType.Tap && skill != SkillType.Slide && skill != SkillType.Drive) return;
            if (!_tipTapShown)
            {
                _tipTapShown = true;
                _tipKeepDelay = 4.40f;
            }
            _tipSkillReadyShown = true;
            VfxTipPlate.HideIfShowing(BattleCueCopy.TipTapSkill, BattleCueCopy.TipTapSkillBody);
            VfxTipPlate.HideIfShowing(BattleCueCopy.MonaTipsHeader, BattleCueCopy.TipSkillReadyMonaBody);
            if (skill == SkillType.Slide)
            {
                _tipSlideShown = _tipSlideMonaShown = _tipSlidePowerShown = true;
                VfxTipPlate.HideIfShowing(BattleCueCopy.TipSlideSkill, BattleCueCopy.TipSlideSkillBody);
                VfxTipPlate.HideIfShowing(BattleCueCopy.MonaTipsHeader, BattleCueCopy.TipSlideMonaBody);
                VfxTipPlate.HideIfShowing(BattleCueCopy.TipSlidePower, BattleCueCopy.TipSlidePowerBody);
            }
            if (skill == SkillType.Drive)
            {
                _tipDriveShown = _tipDriveTimingShown = _tipDriveTablesShown = true;
                VfxTipPlate.HideIfShowing(BattleCueCopy.TipDriveSkill, BattleCueCopy.TipDriveSkillBody);
                VfxTipPlate.HideIfShowing(BattleCueCopy.TipDriveTiming, BattleCueCopy.TipDriveTimingBody);
                VfxTipPlate.HideIfShowing(BattleCueCopy.TipDriveTables, BattleCueCopy.TipDriveTablesBody);
            }
        }

        void DrainCasts()
        {
            var battle = _host != null ? _host.Battle : null;
            if (battle == null || battle.Casts == null) return;
            var log = battle.Casts;
            while (_castCursor < log.Count)
            {
                var fx = log[_castCursor++];
                if (fx == null) continue;
                if (fx.CasterAlly && !fx.Fever) CompleteTutorialSkill(fx.Type);
                BattleFighter caster = null;
                if (fx.CasterAlly)
                {
                    if (_fieldAllies != null && fx.CasterSlot >= 0 && fx.CasterSlot < _fieldAllies.Length)
                        caster = _fieldAllies[fx.CasterSlot];
                }
                else if (_fieldEnemies != null && fx.CasterSlot >= 0 && fx.CasterSlot < _fieldEnemies.Length)
                    caster = _fieldEnemies[fx.CasterSlot];

                var castAt = caster != null ? caster.Anchor : new Vector2(0.5f, 0.5f);
                var castElem = caster != null ? caster.Elem : Element.Dark;
                if (_presentation != null)
                {
                    _presentation.PlayCue(new PresentationCue(
                        PresentationCueKind.Cast, fx.CasterSlot, fx.CasterAlly, fx.CasterSlot, fx.CasterAlly,
                        fx.Type, 0, false, false, fx.Fever, fx.Name, castElem, castAt, castAt));
                }
                else
                    VfxRouter.OnCast(_fieldLayer != null ? _fieldLayer : _root, fx, castAt, castElem,
                        caster != null ? caster.UnitName : null);
                if (fx.Fever)
                {
                    CanvasShake.Punch(34f, 0.40f);
                    CombatFeel.ScreenTint(_fieldLayer != null ? _fieldLayer : _root, FeverPink, 0.08f, 0.22f);
                }
                if (fx.Fever && fx.Name == "FEVER")
                {
                    // Live word sits on VfxFeverOverlay (P0 t440). Do not park FEVER TIME on the HUD banner.
                    if (_skillBanner != null) _skillBanner.text = "";
                    CombatFeel.FeverRipple(_fieldLayer != null ? _fieldLayer : _root, new Vector2(0.5f, 0.56f));
                    continue;
                }
                var tag = SkillWord(fx.Type) + "  " + fx.Name;
                if (fx.Type == SkillType.Slide && fx.CasterAlly)
                {
                    // P0 t360 identity lives on VfxShowtime (IT'S SHOWTIME!!).
                    // Do not leave HUD 开演 / word-stamp behind for later Drive-ready.
                    if (_skillBanner != null) _skillBanner.text = "";
                    BeginShowtime(caster, BattleCueCopy.SlideShowtime, fx.Name, BattleCueCopy.SlideSkill, VisualTokens.StarEvolved, VfxShowtime.Duration, CombatCut.Slide, true);
                }
                else if (fx.Type == SkillType.Drive && !fx.CasterAlly)
                {
                    if (_skillBanner != null)
                    {
                        _skillBanner.text = tag;
                        _skillBanner.color = VisualTokens.StarEvolved;
                    }
                    _warnT = 0.95f;
                    ShowCue(BattleCueCopy.Kind.EnemyWarn, 0.90f, BattleCueCopy.EnemyWarnSub);
                }
                else if (fx.Type == SkillType.Drive && fx.CasterAlly)
                {
                    VfxShowtime.KillAll();
                    HideDriveSelect();
                    if (_skillBanner != null)
                    {
                        _skillBanner.text = tag;
                        _skillBanner.color = VisualTokens.DriveOrange;
                    }
                    CombatFeel.ScreenTint(_fieldLayer != null ? _fieldLayer : _root, VisualTokens.DriveOrange, 0.22f, 0.24f);
                    CombatFeel.DriveCrush(_fieldLayer != null ? _fieldLayer : _root, DriveHitAt());
                    if (_judgeFromQte) _judgeFromQte = false;
                    else
                    {
                        BeginShowtime(caster, BattleCueCopy.DriveReady, fx.Name, BattleCueCopy.DriveCast, VisualTokens.DriveOrange, 0.70f, CombatCut.Drive, true);
                        ShowCue(BattleCueCopy.Kind.DriveCast, 0.70f, fx.Name);
                    }
                }
                else if (fx.Type == SkillType.Tap && fx.CasterAlly && caster != null)
                {
                    var nameCol = Color.Lerp(VisualTokens.TapWhite, VisualTokens.Element(caster.Elem), 0.42f);
                    CombatFeel.NamePopStack(_fieldLayer != null ? _fieldLayer : _root,
                        caster.Anchor + new Vector2(0f, 0.12f), fx.Name, nameCol);
                    if (_skillBanner != null) _skillBanner.text = "";
                }
                else if (fx.Type == SkillType.Leader)
                {
                    // P0 t60: no LEADER / skill-name field stamp at fight start.
                    if (_skillBanner != null) _skillBanner.text = "";
                }
                else if (_skillBanner != null && fx.Type != SkillType.Auto)
                {
                    _skillBanner.text = tag;
                    _skillBanner.color = VisualTokens.Skill(fx.Type);
                }
            }
        }

        Vector2 DriveHitAt()
        {
            var b = _host != null ? _host.Battle : null;
            if (_fieldEnemies != null && b != null && b.Enemies != null)
            {
                for (int i = 0; i < _fieldEnemies.Length && i < b.Enemies.Count; i++)
                {
                    if (_fieldEnemies[i] != null && b.Enemies[i] != null && b.Enemies[i].Alive)
                        return _fieldEnemies[i].Anchor;
                }
            }
            return new Vector2(0.5f, 0.58f);
        }

        Color HitColor(FloatText ft, BattleFighter caster)
        {
            if (ft == null) return VisualTokens.TapWhite;
            if (ft.Heal) return new Color(0.45f, 1f, 0.55f);
            if (ft.Fever) return VisualTokens.FeverGold;
            if (ft.Kind != SkillType.Tap && ft.Kind != SkillType.Auto)
                return VisualTokens.Skill(ft.Kind);
            var battle = _host != null ? _host.Battle : null;
            if (battle == null || caster == null) return VisualTokens.TapWhite;
            if (caster.Ally && battle.Allies != null && caster.Slot >= 0 && caster.Slot < battle.Allies.Length
                && battle.Allies[caster.Slot] != null && battle.Allies[caster.Slot].Def != null)
                return VisualTokens.Element(battle.Allies[caster.Slot].Def.Element);
            if (!caster.Ally && battle.Enemies != null && caster.Slot >= 0 && caster.Slot < battle.Enemies.Count
                && battle.Enemies[caster.Slot] != null && battle.Enemies[caster.Slot].Def != null)
                return VisualTokens.Element(battle.Enemies[caster.Slot].Def.Element);
            return VisualTokens.TapWhite;
        }

        void TickCutHold(BattleSim battle)
        {
            if (battle != null && !battle.Paused && battle.HoldLeftSec > 0f)
                _cutHoldT = battle.HoldLeftSec;
            if (_cutHoldT <= 0f) return;
            if (battle != null && battle.Paused) return;
            if (battle != null && battle.HoldLeftSec > 0f) return;
            _cutHoldT -= Time.unscaledDeltaTime;
            if (_cutHoldT < 0f) _cutHoldT = 0f;
        }

        void BeginShowtime(BattleFighter caster, string title, string skill, string badge, Color accent, float life, CombatCut kind, bool hold)
        {
            VfxTipPlate.SetSuppressed(true);
            if (_pix != null && _pix.Showing) _pix.HideNow();
            var battle = _host != null ? _host.Battle : null;
            // Overlay lifetime only. Core owns the sim hold (C2); do not write HoldSim.
            if (hold)
            {
                if (battle != null && battle.HoldLeftSec > 0.01f)
                    _cutHoldT = battle.HoldLeftSec;
                else
                    _cutHoldT = Mathf.Max(0.35f, life);
            }
            else _cutHoldT = 0f;
        }

        static bool IsQteStamp(BattleCueCopy.Kind kind)
        {
            return kind == BattleCueCopy.Kind.QtePerfect
                || kind == BattleCueCopy.Kind.QteGreat
                || kind == BattleCueCopy.Kind.QteGood
                || kind == BattleCueCopy.Kind.QteBad;
        }

        static bool ExclusiveOverShowtime(BattleCueCopy.Kind kind)
        {
            return kind == BattleCueCopy.Kind.DriveSelect
                || kind == BattleCueCopy.Kind.DriveReady
                || kind == BattleCueCopy.Kind.DriveCast
                || kind == BattleCueCopy.Kind.FeverTime
                || IsQteStamp(kind);
        }

        void BeginFeverBanner()
        {
            VfxShowtime.KillAll();
            VfxJudge.KillAll();
            var battle = _host != null ? _host.Battle : null;
            var win = BattleSim.UnknownFeverWindowSec;
            if (battle != null && battle.FeverLeft > 0.05f)
                win = battle.FeverLeft;
            else if (battle != null && battle.Clocks != null && battle.Clocks.FeverWindowSec > 0.01f)
                win = battle.Clocks.FeverWindowSec;
            // P0 t440: rainbow arc + FEVER TIME + leftover. No 狂热时间 stamp, no edge slash lines.
            VfxFeverOverlay.Show(_root, win);
            if (_skillBanner != null) _skillBanner.text = "";
        }

        void ShowCue(BattleCueCopy.Kind kind, float life, string sub)
        {
            VfxTipPlate.SetSuppressed(true);
            if (ExclusiveOverShowtime(kind))
                VfxShowtime.KillAll();
            if (kind == BattleCueCopy.Kind.FeverTime)
                VfxJudge.KillAll();
            VisibleStamp = kind;
            VisibleStampTitle = BattleCueCopy.Title(kind);
            if (_skillBanner != null
                && (kind == BattleCueCopy.Kind.QtePerfect || kind == BattleCueCopy.Kind.QteGreat))
            {
                _skillBanner.text = VisibleStampTitle;
                _skillBanner.color = BattleCueCopy.StampTint(kind);
            }
            ShowStamp(VisibleStampTitle, BattleCueCopy.StampTint(kind), life, sub ?? "",
                BattleCueCopy.VfxChannelKey(kind));
        }

        void ShowStamp(string text, Color color, float life, string sub, string vfxKey)
        {
            _stampTint = color;
            _showtimeT = life;
            _stampT = 0f;
            if (_stamp != null)
            {
                var c = _stamp.color;
                c.a = 0f;
                _stamp.color = c;
                _stamp.text = text ?? "";
            }
            if (_stampSub != null)
            {
                var c = _stampSub.color;
                c.a = 0f;
                _stampSub.color = c;
                _stampSub.text = sub ?? "";
            }
            VfxWordStamp.Play(_root, vfxKey, sub, color, life);
        }

        void EnsureField()
        {
            var go = new GameObject("field", typeof(RectTransform));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _fieldLayer = go.transform;
            _built.Add(go);
        }

        void SpawnAllies()
        {
            var battle = _host != null ? _host.Battle : null;
            var slots = battle != null && battle.Allies != null ? battle.Allies.Length : 0;
            _fieldAllies = new BattleFighter[slots];
            if (battle == null || battle.Allies == null || _fieldLayer == null) return;
            for (int i = 0; i < slots; i++)
            {
                if (battle.Allies[i] == null) continue;
                var fx = BattleFighter.Create(_fieldLayer, battle.Allies[i], CharacterPresenter.AllyFieldAnchor(i), false);
                if (fx == null) continue;
                if (_built != null) _built.Add(fx.gameObject);
                _fieldAllies[i] = fx;
            }
        }

        void SpawnEnemies()
        {
            var battle = _host != null ? _host.Battle : null;
            if (_fieldEnemies != null)
            {
                for (int i = 0; i < _fieldEnemies.Length; i++)
                {
                    if (_fieldEnemies[i] != null)
                        Object.Destroy(_fieldEnemies[i].gameObject);
                }
            }
            if (battle == null || battle.Enemies == null || _fieldLayer == null)
            {
                _fieldEnemies = new BattleFighter[0];
                return;
            }
            var n = battle.Enemies.Count;
            _fieldEnemies = new BattleFighter[n];
            for (int i = 0; i < n; i++)
            {
                var u = battle.Enemies[i];
                if (u == null) continue;
                var fx = BattleFighter.Create(_fieldLayer, u, CharacterPresenter.EnemyFieldAnchor(i, n),
                    u.Def != null && u.Def.IsBoss);
                if (fx == null) continue;
                if (_built != null) _built.Add(fx.gameObject);
                var slot = i;
                fx.BindFocus(() => { if (_host != null) _host.TryFocusEnemy(slot); }); // GameRoot → Submit FocusEnemy
                _fieldEnemies[i] = fx;
            }
            _fieldWave = battle.WaveIndex;
            _presentation = CharacterPresenter.BindExisting(
                _fieldLayer != null ? _fieldLayer : _root, _fieldAllies, _fieldEnemies);
            // P0 t65 ordinary PHASE advance is stage name + PHASE n. t100 Loki
            // "THE MASTER OF DESIRE" is a different encounter — do not stamp it
            // on every IsBoss (VS wave-2 EBOSS was overlapping 10_wave).
        }

        static string AutoWord(AutoMode m)
        {
            // Primary GT: "> FULL AUTO" / SEMI. SPEED already EN; keep mode EN chrome.
            if (m == AutoMode.Full) return "> FULL AUTO";
            if (m == AutoMode.Semi) return "> SEMI AUTO";
            return "> MANUAL";
        }

        static int HudPhaseDuringSplash(BattleSim battle, int maxPhase)
        {
            if (battle == null) return 1;
            var phaseN = Mathf.Clamp(battle.WaveIndex + 1, 1, maxPhase);
            // P0 30fps PHASE 2 splash: HUD stays PHASE 1/3 until first-off.
            if (WavePreview.VisibleKind == WaveCueKind.WaveAdvance)
                phaseN = Mathf.Clamp(phaseN - 1, 1, maxPhase);
            return phaseN;
        }

        int MaxAllyLevel(BattleSim battle)
        {
            if (battle == null || battle.Allies == null) return Growth.MaxLevel;
            var max = 0;
            for (int i = 0; i < battle.Allies.Length; i++)
            {
                var u = battle.Allies[i];
                if (u == null || !u.Alive || u.Def == null) continue;
                var lv = 1;
                if (_host != null && _host.SaveData != null)
                    lv = _host.SaveData.GetUnit(u.Def.Id).Level;
                else if (u.Def.BattleLevel > 0)
                    lv = u.Def.BattleLevel;
                if (lv > max) max = lv;
            }
            return max > 0 ? max : Growth.MaxLevel;
        }

        void SetFieldSplashHidden(bool hidden)
        {
            if (_fieldAllies != null)
            {
                for (int i = 0; i < _fieldAllies.Length; i++)
                    if (_fieldAllies[i] != null) _fieldAllies[i].SetSplashHidden(hidden);
            }
            if (_fieldEnemies != null)
            {
                for (int i = 0; i < _fieldEnemies.Length; i++)
                    if (_fieldEnemies[i] != null) _fieldEnemies[i].SetSplashHidden(hidden);
            }
        }

        /// <summary>Primary GT PHASE n/m. Wave0+Wave1 ⇒ 2; Wave0 only ⇒ 1.</summary>
        static int StagePhaseCount(StageDef stage)
        {
            if (stage == null) return 2;
            var w1 = stage.Wave1 != null && stage.Wave1.Length > 0;
            return w1 ? 2 : 1;
        }

        static string SkillWord(SkillType t)
        {
            if (t == SkillType.Slide) return BattleCueCopy.SlideSkillEn;
            if (t == SkillType.Drive) return BattleCueCopy.DriveCast;
            if (t == SkillType.Auto) return "AUTO";
            if (t == SkillType.Leader) return "LEADER";
            return "TAP";
        }

        static string FxWord(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Buff";
            var id = raw.Trim();
            if (id.Length > 3 && id.StartsWith("FX ")) id = id.Substring(3);
            if (id == "shield") return "Barrier";
            if (id == "taunt") return "Taunt";
            if (id == "stun") return "Stun";
            if (id == "dot_flame") return "Poison";
            if (id == "atk_up" || id == "burst_atk") return "ATK ↑";
            if (id == "haste") return "Haste";
            if (id == "def_down") return "Debuff Blast";
            var b = BuffCatalog.FindId(id);
            if (b != null && !string.IsNullOrEmpty(b.Name)) return b.Name;
            return "Buff";
        }

        static string StripPlus(string text)
        {
            if (string.IsNullOrEmpty(text)) return "0";
            var s = text.Trim();
            if (s.Length > 0 && s[0] == '+') s = s.Substring(1);
            return s;
        }

        static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Length <= 4 ? name : name.Substring(0, 4);
        }

        static Vector2 PortAnchor(int i)
        {
            return new Vector2(PortLeft + i * PortSpread, PortY + PortArc * Mathf.Sin(i * Mathf.PI * 0.25f));
        }

        static string[] StatusLabels(UnitState u)
        {
            return StatusChipText.Labels(u);
        }

        Image Img(string name, Vector2 anchor, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            _built.Add(go);
            return img;
        }

        Text Label(string text, int size, Color color, Vector2 anchor, Vector2 dim, bool outline)
        {
            var go = new GameObject("label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
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

        Text Antique(string label, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = new Color(0.070f, 0.066f, 0.058f, 0.94f);
            go.GetComponent<Button>().onClick.AddListener(click);
            var wire = ChildImg(go.transform, "wire", Vector2.zero, Vector2.one, new Vector2(1.5f, 1.5f), new Vector2(-1.5f, -1.5f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.95f));
            UiSprites.Apply(wire, UiSprites.WireFrame());
            wire.raycastTarget = false;
            var hair = ChildImg(go.transform, "hair", Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.30f));
            UiSprites.Apply(hair, UiSprites.WireFrame());
            hair.raycastTarget = false;
            var tx = ChildLabel(go.transform, label, 15, VisualTokens.GoldTitle, new Vector2(0.5f, 0.5f), size);
            tx.fontStyle = FontStyle.Bold;
            var ol = tx.gameObject.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2.2f, -2.2f);
            var sh = tx.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.35f);
            sh.effectDistance = new Vector2(0f, 1.4f);
            _built.Add(go);
            return tx;
        }

        Image Stretch(string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            _built.Add(go);
            return img;
        }

        static Image ChildImg(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text ChildLabel(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 dim)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            var tx = go.GetComponent<Text>();
            tx.font = CharacterPresenter.UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.fontSize = size;
            tx.text = text;
            tx.raycastTarget = false;
            return tx;
        }
    }
}
