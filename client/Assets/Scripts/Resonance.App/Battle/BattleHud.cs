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
        Text _archMeta;
        Text _timerLabel;
        Text _speedLabel;
        Text _autoLabel;
        Text _pauseLabel;
        Image _partyHp;
        Text _partyHpLabel;
        Image _driveBar;
        Text _driveLabel;
        Image _feverBar;
        Text _feverLabel;
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
        Text[] _hpNum;
        Text[] _nameTag;
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
        bool _bossIntroPlayed;
        float _showtimeT;
        float _stampT;
        Color _stampTint = VisualTokens.YellowConfirm;
        float _qteT;
        float _warnT;
        float _feverBurstT;
        bool _feverWas;
        bool _judgeFromQte;
        float _cutHoldT;
        float _feverCueDelay;
        bool _feverCueQueued;
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
            _bossIntroPlayed = false;
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
                    if (b.TryBeginDrive(i)) break;
            }
            if (b.PendingDriveSlot >= 0)
            {
                FinishQte(DriveTiming.Perfect);
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

            _pauseLabel = Antique("暂停", new Vector2(0.09f, 0.950f), new Vector2(96, 34), () => _host.ToggleBattlePause());
            _speedLabel = Antique("×1", new Vector2(0.09f, 0.910f), new Vector2(96, 32), () => _host.ToggleBattleSpeed());
            _autoLabel = Antique("手动", new Vector2(0.91f, 0.950f), new Vector2(148, 34), () => _host.CycleBattleAuto());

            _archMeta = Label("", 16, VisualTokens.GoldTitle, new Vector2(0.5f, 0.964f), new Vector2(560, 28), true);
            AntiqueType(_archMeta);
            _archNum = Label("", 18, VisualTokens.TapWhite, new Vector2(0.5f, 0.910f), new Vector2(220, 26), true);
            AntiqueType(_archNum);
            _timerLabel = Label("00:00", 18, Color.white, new Vector2(0.5f, 0.884f), new Vector2(220, 26), true);
            AntiqueType(_timerLabel);

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
            _partyHpLabel = Label("生命 100%", 12, PartyGreen, new Vector2(0.14f, 0.070f), new Vector2(160, 20), true);
            if (_partyHpLabel != null) _partyHpLabel.alignment = TextAnchor.MiddleLeft;

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
            _driveLabel = Label("驱动  0%", 11, VisualTokens.YellowValue, new Vector2(0.5f, 0.0485f), new Vector2(240, 16), true);

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
            rt.anchorMin = new Vector2(0.08f, 0.40f);
            rt.anchorMax = new Vector2(0.92f, 0.52f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var plate = go.GetComponent<Image>();
            UiSprites.Apply(plate, UiSprites.Pixel());
            plate.color = new Color(0.04f, 0.03f, 0.02f, 0.82f);
            plate.raycastTarget = false;
            _driveSelect = go.GetComponent<CanvasGroup>();
            _driveSelect.blocksRaycasts = false;
            _driveSelect.interactable = false;
            _driveSelect.alpha = 0f;

            var wire = ChildImg(go.transform, "wire", Vector2.zero, Vector2.one,
                new Vector2(2f, 2f), new Vector2(-2f, -2f),
                new Color(VisualTokens.DriveOrange.r, VisualTokens.DriveOrange.g, VisualTokens.DriveOrange.b, 0.88f));
            UiSprites.Apply(wire, UiSprites.WireFrame());
            wire.raycastTarget = false;

            _driveSelectTitle = ChildLabel(go.transform, BattleCueCopy.DriveSelect, 36, VisualTokens.DriveOrange,
                new Vector2(0.5f, 0.62f), new Vector2(720f, 48f));
            _driveSelectTitle.fontStyle = FontStyle.Bold;
            var tOl = _driveSelectTitle.gameObject.AddComponent<Outline>();
            tOl.effectColor = Color.black;
            tOl.effectDistance = new Vector2(2.4f, -2.4f);

            _driveSelectHint = ChildLabel(go.transform, "点按一名角色释放驱动", 20, VisualTokens.YellowValue,
                new Vector2(0.5f, 0.28f), new Vector2(780f, 32f));
            var hOl = _driveSelectHint.gameObject.AddComponent<Outline>();
            hOl.effectColor = Color.black;
            hOl.effectDistance = new Vector2(1.6f, -1.6f);

            go.SetActive(false);
            _built.Add(go);
        }

        void RefreshDriveSelect(BattleSim battle)
        {
            var show = battle != null
                && battle.Outcome == BattleOutcome.InProgress
                && battle.Drive >= 100f
                && battle.PendingDriveSlot < 0
                && !QteOpen;
            DriveSelectVisible = show;
            if (_driveSelect == null) return;
            if (show)
            {
                VfxShowtime.KillAll();
                if (_driveSelectTitle != null)
                    _driveSelectTitle.text = BattleCueCopy.DriveSelect;
                _driveSelect.gameObject.SetActive(true);
                _driveSelect.alpha = 1f;
            }
            else
                HideDriveSelect();
        }

        void HideDriveSelect()
        {
            DriveSelectVisible = false;
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
            _hpNum = new Text[n];
            _nameTag = new Text[n];
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

                var flash = ChildImg(go.transform, "ping", Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f), Color.clear);
                UiSprites.Apply(flash, UiSprites.Soft());
                _tapFlash[i] = flash;

                var g = go.GetComponent<PortraitGesture>();
                g.OnTap = () => OnPortraitTap(slot);
                g.OnSlide = () =>
                {
                    if (QteOpen) return;
                    if (_host != null && _host.Battle != null) _host.Battle.TrySlide(slot);
                };

                _slidePip[i] = ChildLabel(go.transform, BattleCueCopy.SlideSkill, 11, VisualTokens.SlideGreen, new Vector2(0.82f, 0.92f), new Vector2(56, 22));
                var sOl = _slidePip[i].gameObject.AddComponent<Outline>();
                sOl.effectColor = Color.black;
                sOl.effectDistance = new Vector2(1f, -1f);
                _slidePip[i].color = Color.clear;

                _drivePip[i] = ChildLabel(go.transform, BattleCueCopy.DriveCast, 11, VisualTokens.DriveOrange, new Vector2(0.18f, 0.92f), new Vector2(56, 22));
                var dOl = _drivePip[i].gameObject.AddComponent<Outline>();
                dOl.effectColor = Color.black;
                dOl.effectDistance = new Vector2(1f, -1f);
                _drivePip[i].color = Color.clear;

                _readyTag[i] = ChildLabel(go.transform, "", 14, VisualTokens.YellowConfirm, new Vector2(0.5f, 1.14f), new Vector2(PortSize, 24));
                var rOl = _readyTag[i].gameObject.AddComponent<Outline>();
                rOl.effectColor = Color.black;
                rOl.effectDistance = new Vector2(2f, -2f);

                _hpNum[i] = ChildLabel(go.transform, "0", 20, Color.white, new Vector2(0.5f, -0.06f), new Vector2(PortSize, 26));
                var hpOl = _hpNum[i].gameObject.AddComponent<Outline>();
                hpOl.effectColor = Color.black;
                hpOl.effectDistance = new Vector2(2f, -2f);
                _nameTag[i] = ChildLabel(go.transform, ShortName(def != null ? def.Name : ""), 14, Color.white, new Vector2(0.5f, -0.24f), new Vector2(PortSize, 22));
                _nameTag[i].fontStyle = FontStyle.Bold;
                var nameOl = _nameTag[i].gameObject.AddComponent<Outline>();
                nameOl.effectColor = Color.black;
                nameOl.effectDistance = new Vector2(2.4f, -2.4f);
                _built.Add(go);
            }
        }

        void OnPortraitTap(int slot)
        {
            var b = _host != null ? _host.Battle : null;
            if (b == null || QteOpen) return;
            if (b.TryPortraitTap(slot) && b.PendingDriveSlot == slot)
            {
                if (b.Auto == AutoMode.Full) FinishQte(DriveTiming.Great);
                else OpenQte();
            }
        }

        void TickQte(BattleSim battle)
        {
            if (battle == null || battle.Paused) return;
            _qteT += Time.unscaledDeltaTime;
            TickOverlays();
            if (battle.PendingDriveSlot < 0)
            {
                QteOpen = false;
                HideGood();
                return;
            }
            if (battle.Auto == AutoMode.Full)
            {
                FinishQte(DriveTiming.Great);
                return;
            }
            if (_qteT >= BattleSim.DriveQteTimeoutSec)
                FinishQte(DriveTiming.Good);
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
            VfxGoodButton.Show(_root, hit =>
            {
                if (!QteOpen) return;
                FinishQte(hit ? DriveTiming.Perfect : DriveTiming.Good);
            });
            CombatFeel.ScreenTint(_fieldLayer != null ? _fieldLayer : _root, VisualTokens.DriveOrange, 0.18f, 0.20f);
            CanvasShake.Punch(18f, 0.18f);
            ShowCue(BattleCueCopy.Kind.DriveReady, 0.70f, "点 好");
        }

        void FinishQte(DriveTiming timing)
        {
            var b = _host != null ? _host.Battle : null;
            QteOpen = false;
            HideGood();
            if (_pix != null && _pix.Showing) _pix.HideNow();
            if (b == null) return;
            b.ResolveDrive(timing);
            _judgeFromQte = true;
            ShowJudge(timing);
        }

        void HideGood()
        {
            VfxGoodButton.Hide();
        }

        static float FeverPct(BattleSim b)
        {
            if (b == null) return 0f;
            if (b.FeverActive) return 100f;
            return Mathf.Clamp(b.FeverGauge, 0f, 100f);
        }

        void ShowJudge(DriveTiming timing)
        {
            var b = _host != null ? _host.Battle : null;
            var feverLine = b != null
                ? BattleCueCopy.FeverLine(b.FeverActive, Mathf.RoundToInt(b.FeverGauge))
                : "";
            switch (timing)
            {
                case DriveTiming.Perfect:
                    ShowCue(BattleCueCopy.Kind.QtePerfect, 1.20f, BattleCueCopy.QtePerfectSub + "   " + feverLine);
                    CombatFeel.ScreenTint(_fieldLayer != null ? _fieldLayer : _root, Color.white, 0.55f, 0.10f);
                    VfxJudge.Play(_root, "perfect", 1.5f, FeverPct(b));
                    break;
                case DriveTiming.Great:
                    ShowCue(BattleCueCopy.Kind.QteGreat, 0.90f, feverLine);
                    VfxJudge.Play(_root, "great", 1.2f, FeverPct(b));
                    break;
                case DriveTiming.Good:
                    ShowCue(BattleCueCopy.Kind.QteGood, 0.70f, feverLine);
                    VfxJudge.Play(_root, "good", 1f, FeverPct(b));
                    break;
                default:
                    ShowCue(BattleCueCopy.Kind.QteBad, 0.60f, feverLine);
                    break;
            }
        }

        void Refresh()
        {
            if (_host == null) return;
            var battle = _host.Battle;
            if (battle == null) return;
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
            if (_archNum != null)
                _archNum.text = pct + "%";
            if (_archMeta != null)
            {
                var table = Catalog.Chapter(_host.SaveData != null && _host.SaveData.UseHard);
                var stName = "";
                var idx = _host.ActiveStageIndex;
                if (table != null && idx >= 0 && idx < table.Length && table[idx] != null)
                {
                    var raw = table[idx].Name ?? "";
                    var sp = raw.LastIndexOf(' ');
                    stName = sp >= 0 && sp + 1 < raw.Length ? raw.Substring(sp + 1) : raw;
                }
                _archMeta.text = stName;
            }
            VfxPhaseBar.Draw(_root, battle.WaveIndex + 1, 2);
            VfxDpsPanel.Draw(_root, battle.Stats);
            if (_timerLabel != null)
            {
                var sec = Mathf.Max(0, Mathf.CeilToInt(battle.TimeLeft));
                _timerLabel.text = (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00");
            }
            if (_enemyPips != null && battle.Enemies != null)
            {
                var maxCh = 0f;
                for (int i = 0; i < battle.Enemies.Count; i++)
                    if (battle.Enemies[i] != null && battle.Enemies[i].Alive)
                        maxCh = Mathf.Max(maxCh, battle.Enemies[i].Charge);
                for (int i = 0; i < _enemyPips.Length; i++)
                {
                    if (_enemyPips[i] == null) continue;
                    var lit = maxCh >= (i + 1) * (100f / _enemyPips.Length);
                    _enemyPips[i].color = lit ? VisualTokens.StarEvolved : new Color(0.26f, 0.19f, 0.08f, 0.75f);
                }
            }
            if (_partyHp != null)
                _partyHp.fillAmount = phpMax <= 0 ? 0f : (float)phpNow / phpMax;
            if (_partyHpLabel != null)
                _partyHpLabel.text = "生命  " + (phpMax <= 0 ? 0 : Mathf.RoundToInt(100f * phpNow / phpMax)) + "%";
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
                if (battle.Drive >= 100f)
                {
                    _driveLabel.text = "驱动满 · 点按";
                    _driveLabel.color = VisualTokens.YellowConfirm;
                }
                else
                {
                    _driveLabel.text = "驱动  " + Mathf.RoundToInt(battle.Drive) + "%";
                    _driveLabel.color = VisualTokens.YellowValue;
                }
            }
            FeverBannerOn = battle.FeverActive;
            if (_feverBar != null && _feverLabel != null)
            {
                if (battle.FeverActive)
                {
                    _feverBar.fillAmount = battle.FeverLeft <= 0f ? 0f : Mathf.Clamp01(battle.FeverLeft / 7f);
                    var rainbow = CombatFeel.FeverHue(Time.unscaledTime * 0.55f);
                    _feverBar.color = rainbow;
                    _feverLabel.text = BattleCueCopy.FeverTime;
                    _feverLabel.color = rainbow;
                }
                else
                {
                    _feverBar.fillAmount = Mathf.Clamp01(battle.FeverGauge / 100f);
                    _feverBar.color = FeverPink;
                    _feverLabel.text = BattleCueCopy.FeverLine(false, Mathf.RoundToInt(battle.FeverGauge));
                    _feverLabel.color = FeverPink;
                }
            }
            var feverStart = battle.FeverActive && !_feverWas;
            var feverEnd = !battle.FeverActive && _feverWas;
            if (feverStart)
            {
                VfxShowtime.KillAll();
                _feverBurstT = 0.90f;
                if (_pix != null) _pix.SetFever(true);
                if (IsQteStamp(VisibleStamp) && _stampT > 0.12f)
                {
                    _feverCueQueued = true;
                    _feverCueDelay = _stampT + 0.06f;
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
                VfxRouter.EndFever(_root);
            }
            if (_feverBurstT > 0f) _feverBurstT -= Time.unscaledDeltaTime;
            if (_feverFlash != null) _feverFlash.color = Color.clear;
            if (_speedLabel != null) _speedLabel.text = battle.Speed == 2 ? "×2" : "×1";
            if (_autoLabel != null) _autoLabel.text = AutoWord(_host.SaveData != null ? _host.SaveData.Auto : AutoMode.Manual);
            if (_pauseLabel != null) _pauseLabel.text = battle.Paused ? "继续" : "暂停";

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
                for (int i = 0; i < _fieldEnemies.Length && i < battle.Enemies.Count; i++)
                    if (_fieldEnemies[i] != null) _fieldEnemies[i].Apply(battle.Enemies[i]);
            }

            TickOverlays();
            DrainCombatLog();
            DrainCasts();
            RefreshPortraits();
            RefreshDriveSelect(battle);
        }

        void RefreshPortraits()
        {
            var battle = _host != null ? _host.Battle : null;
            if (battle == null || battle.Allies == null || _chargeRing == null) return;
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
                if (tapReady && (_tapWas == null || !_tapWas[i]))
                    VfxTapReady.Play(_root, port);
                if (slideReady && (_slideWas == null || !_slideWas[i]))
                    VfxSlideReady.Play(_root, port);
                if (driveReady && (_driveWas == null || !_driveWas[i]))
                    VfxDriveReady.Play(_root, port);
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
                    var c = VisualTokens.SlideGreen;
                    c.a = tapReady ? 1f : 0f;
                    _slidePip[i].color = c;
                }
                if (_drivePip != null && _drivePip[i] != null)
                {
                    var c = VisualTokens.DriveOrange;
                    c.a = driveReady ? 1f : 0f;
                    _drivePip[i].color = c;
                }
                if (_readyTag != null && _readyTag[i] != null)
                {
                    if (!u.Alive)
                    {
                        _readyTag[i].text = "";
                    }
                    else if (driveReady)
                    {
                        _readyTag[i].text = "驱动";
                        _readyTag[i].color = VisualTokens.DriveOrange;
                    }
                    else if (tapReady)
                    {
                        _readyTag[i].text = "点按";
                        _readyTag[i].color = VisualTokens.YellowConfirm;
                    }
                    else _readyTag[i].text = "";
                }
                if (_hpNum[i] != null)
                {
                    _hpNum[i].text = u.Hp.ToString();
                    _hpNum[i].color = !u.Alive
                        ? VisualTokens.TextMuted
                        : (u.Hp >= u.MaxHp ? VisualTokens.YellowValue : Color.white);
                }
                if (_portraitDim[i] != null)
                {
                    if (!u.Alive) _portraitDim[i].alpha = 0.35f;
                    else if (_warnT > 0f) _portraitDim[i].alpha = 0.40f;
                    else _portraitDim[i].alpha = 1f;
                }
                if (_nameTag[i] != null)
                    _nameTag[i].text = u.Def != null ? ShortName(u.Def.Name) : "";
            }
        }

        void TickOverlays()
        {
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
                            "暴击", VisualTokens.StarEvolved);
                    if (ft.Fever && ft.Crit && !ft.Heal)
                        CombatFeel.NamePopStack(_fieldLayer != null ? _fieldLayer : _root, target.Anchor + new Vector2(0.02f, 0.18f),
                            "弱点", VisualTokens.Ember);
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
                            from, target.Anchor, ft.Kind, elem, amt, ft.Crit, ft.Heal, ft.Fever);
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

        void DrainCasts()
        {
            var battle = _host != null ? _host.Battle : null;
            if (battle == null || battle.Casts == null) return;
            var log = battle.Casts;
            while (_castCursor < log.Count)
            {
                var fx = log[_castCursor++];
                if (fx == null) continue;
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
                    VfxRouter.OnCast(_fieldLayer != null ? _fieldLayer : _root, fx, castAt, castElem);
                if (fx.Fever)
                {
                    CanvasShake.Punch(34f, 0.40f);
                    CombatFeel.ScreenTint(_fieldLayer != null ? _fieldLayer : _root, FeverPink, 0.08f, 0.22f);
                }
                if (fx.Fever && fx.Name == "FEVER")
                {
                    if (_skillBanner != null)
                    {
                        _skillBanner.text = BattleCueCopy.FeverTime;
                        _skillBanner.color = VisualTokens.FeverGold;
                    }
                    CombatFeel.FeverRipple(_fieldLayer != null ? _fieldLayer : _root, new Vector2(0.5f, 0.56f));
                    continue;
                }
                var tag = SkillWord(fx.Type) + "  " + fx.Name;
                if (fx.Type == SkillType.Slide && fx.CasterAlly)
                {
                    if (_skillBanner != null)
                    {
                        _skillBanner.text = BattleCueCopy.SlideBanner(fx.Name);
                        _skillBanner.color = VisualTokens.SlideGreen;
                    }
                    BeginShowtime(caster, BattleCueCopy.SlideShowtime, fx.Name, BattleCueCopy.SlideSkill, VisualTokens.StarEvolved, 1.15f, CombatCut.Slide, true);
                    ShowCue(BattleCueCopy.Kind.SlideShowtime, 1.10f, BattleCueCopy.SlideSkill + "  " + fx.Name);
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
            if (_cutHoldT <= 0f) return;
            if (battle != null && battle.Paused) return;
            _cutHoldT -= Time.unscaledDeltaTime;
            if (_cutHoldT > 0f) return;
            _cutHoldT = 0f;
            if (battle != null) battle.HoldSim = false;
        }

        void BeginShowtime(BattleFighter caster, string title, string skill, string badge, Color accent, float life, CombatCut kind, bool hold)
        {
            if (_pix != null && _pix.Showing) _pix.HideNow();
            var battle = _host != null ? _host.Battle : null;
            if (hold && battle != null && battle.Auto == AutoMode.Manual)
            {
                battle.HoldSim = true;
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
            VfxFeverOverlay.Show(_root, 7f);
            VfxSpeedLines.Show(_root, 7f);
            ShowCue(BattleCueCopy.Kind.FeverTime, 1.20f, "");
        }

        void ShowCue(BattleCueCopy.Kind kind, float life, string sub)
        {
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
                _fieldEnemies[i] = fx;
            }
            _fieldWave = battle.WaveIndex;
            _presentation = CharacterPresenter.BindExisting(
                _fieldLayer != null ? _fieldLayer : _root, _fieldAllies, _fieldEnemies);
            if (!_bossIntroPlayed)
            {
                for (int i = 0; i < n; i++)
                {
                    var u = battle.Enemies[i];
                    if (u != null && u.Def != null && u.Def.IsBoss)
                    {
                        VfxBossIntro.Play(_root, u.Def.Name);
                        _bossIntroPlayed = true;
                        break;
                    }
                }
            }
        }

        static string AutoWord(AutoMode m)
        {
            if (m == AutoMode.Full) return "全自动";
            if (m == AutoMode.Semi) return "半自动";
            return "手动";
        }

        static string SkillWord(SkillType t)
        {
            if (t == SkillType.Slide) return BattleCueCopy.SlideSkill;
            if (t == SkillType.Drive) return BattleCueCopy.DriveCast;
            if (t == SkillType.Auto) return "普攻";
            if (t == SkillType.Leader) return "队长";
            return "点按";
        }

        static string FxWord(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "增益";
            var id = raw.Trim();
            if (id.Length > 3 && id.StartsWith("FX ")) id = id.Substring(3);
            if (id == "shield") return "护盾";
            if (id == "taunt") return "嘲讽";
            if (id == "stun") return "眩晕";
            if (id == "dot_flame") return "毒";
            if (id == "atk_up" || id == "burst_atk" || id == "haste") return "增益";
            if (id == "def_down") return "减益爆破";
            var b = BuffCatalog.FindId(id);
            if (b != null && !string.IsNullOrEmpty(b.Name)) return b.Name;
            return "增益";
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
