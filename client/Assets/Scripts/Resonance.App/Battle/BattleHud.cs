using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public sealed class BattleHud
    {
        public bool QteOpen { get; private set; }

        readonly GameRoot _host;
        readonly Transform _root;
        readonly List<GameObject> _built;

        Image _archHp;
        Text _archNum;
        Text _archMeta;
        Text _speedLabel;
        Text _autoLabel;
        Text _pauseLabel;
        Image _partyHp;
        Text _partyHpLabel;
        Image _driveBar;
        Text _driveLabel;
        Image _feverBar;
        Text _feverLabel;
        Text _hintLabel;
        Image _feverFlash;
        CanvasGroup _goodGroup;
        Image _showtime;
        Image _warnFlash;
        Text _stamp;
        Text _skillBanner;
        Image _goodBtn;
        Text _goodLabel;
        BattleFighter[] _fieldAllies;
        BattleFighter[] _fieldEnemies;
        Transform _fieldLayer;
        Image[] _chargeRing;
        Image[] _hpFill;
        Text[] _readyTag;
        Text[] _nameTag;
        Image[] _wing;
        CanvasGroup[] _portraitDim;
        int _logCursor;
        int _castCursor;
        int _fieldWave = -1;
        float _showtimeT;
        float _stampT;
        float _qteT;
        float _warnT;
        float _feverBurstT;
        bool _feverWas;
        bool _judgeFromQte;

        static readonly Color FeverPink = new Color(0.92f, 0.42f, 0.78f, 1f);

        public BattleHud(GameRoot host, Transform root, List<GameObject> built)
        {
            _host = host;
            _root = root;
            _built = built;
        }

        public void Build(int stageIndex)
        {
            var battle = _host.Battle;
            CharacterPresenter.StageArena(_root, _built, stageIndex);
            EnsureField();
            SpawnAllies();
            SpawnEnemies();

            _feverFlash = Img("fever", new Vector2(0.5f, 0.58f), new Vector2(1400, 1400), Color.clear);
            UiSprites.Apply(_feverFlash, UiSprites.Soft());
            _warnFlash = Img("warn", new Vector2(0.5f, 0.55f), new Vector2(1400, 1600), Color.clear);
            _showtime = Img("show", new Vector2(0.5f, 0.62f), new Vector2(980, 260), Color.clear);
            UiSprites.Apply(_showtime, UiSprites.Slash());

            _skillBanner = Label("", 26, VisualTokens.TapWhite, new Vector2(0.5f, 0.86f), new Vector2(900, 56), true);
            _stamp = Label("", 64, VisualTokens.SlideGreen, new Vector2(0.5f, 0.58f), new Vector2(980, 160), true);
            var sc = _stamp.color;
            sc.a = 0f;
            _stamp.color = sc;

            BuildTopBar();
            BuildBottomBar();
            BuildPortraits();
            HideGood();
            Refresh();
        }

        public void Tick()
        {
            var battle = _host.Battle;
            if (battle == null) return;
            if (QteOpen)
            {
                _qteT += Time.unscaledDeltaTime;
                PulseGood();
                TickOverlays();
                return;
            }
            if (battle.PendingDriveSlot >= 0)
                OpenQte();
            Refresh();
        }

        public bool FirePerfect()
        {
            var b = _host.Battle;
            if (b == null) return false;
            if (b.PendingDriveSlot < 0)
            {
                for (int i = 0; i < 5; i++)
                    if (b.TryBeginDrive(i)) break;
            }
            if (b.PendingDriveSlot < 0) return false;
            FinishQte(DriveTiming.Perfect);
            return true;
        }

        void BuildTopBar()
        {
            _speedLabel = Pill("×1", new Vector2(0.12f, 0.955f), new Vector2(160, 64), () => _host.ToggleBattleSpeed());
            _autoLabel = Pill("手动", new Vector2(0.88f, 0.955f), new Vector2(200, 64), () => _host.CycleBattleAuto());

            var archBg = Img("archBg", new Vector2(0.5f, 0.945f), new Vector2(640, 56), new Color(0.08f, 0.02f, 0.02f, 0.92f));
            UiSprites.Apply(archBg, UiSprites.Circle());
            _archHp = Img("arch", new Vector2(0.5f, 0.945f), new Vector2(620, 42), VisualTokens.StarEvolved);
            UiSprites.Apply(_archHp, UiSprites.Circle());
            _archHp.type = Image.Type.Filled;
            _archHp.fillMethod = Image.FillMethod.Horizontal;
            _archHp.fillAmount = 1f;
            _archNum = Label("", 20, Color.white, new Vector2(0.5f, 0.948f), new Vector2(600, 28), true);
            _archMeta = Label("", 16, VisualTokens.TextSecondary, new Vector2(0.5f, 0.912f), new Vector2(720, 28), true);
            _pauseLabel = Ghost("暂停", new Vector2(0.5f, 0.878f), () => _host.ToggleBattlePause());
        }

        void BuildBottomBar()
        {
            var phpBg = Img("phpBg", new Vector2(0.5f, 0.228f), new Vector2(640, 14), new Color(0f, 0f, 0f, 0.75f));
            phpBg.raycastTarget = false;
            _partyHp = Img("php", new Vector2(0.5f, 0.228f), new Vector2(624, 8), new Color(0.35f, 0.82f, 0.42f));
            UiSprites.Apply(_partyHp, UiSprites.Round());
            _partyHp.type = Image.Type.Filled;
            _partyHp.fillMethod = Image.FillMethod.Horizontal;
            _partyHpLabel = Label("HP 100%", 14, new Color(0.55f, 0.95f, 0.62f), new Vector2(0.5f, 0.242f), new Vector2(240, 22), true);

            var dBg = Img("dBg", new Vector2(0.5f, 0.205f), new Vector2(560, 12), new Color(0f, 0f, 0f, 0.7f));
            dBg.raycastTarget = false;
            _driveBar = Img("drive", new Vector2(0.5f, 0.205f), new Vector2(544, 8), VisualTokens.Ember);
            UiSprites.Apply(_driveBar, UiSprites.Round());
            _driveBar.type = Image.Type.Filled;
            _driveBar.fillMethod = Image.FillMethod.Horizontal;
            _driveLabel = Label("驱动  0%", 14, VisualTokens.DriveOrange, new Vector2(0.5f, 0.219f), new Vector2(400, 22), true);

            var fBg = Img("fBg", new Vector2(0.5f, 0.182f), new Vector2(480, 18), new Color(0f, 0f, 0f, 0.72f));
            UiSprites.Apply(fBg, UiSprites.Round());
            _feverBar = Img("feverBar", new Vector2(0.5f, 0.182f), new Vector2(464, 10), VisualTokens.FeverGold);
            UiSprites.Apply(_feverBar, UiSprites.Round());
            _feverBar.type = Image.Type.Filled;
            _feverBar.fillMethod = Image.FillMethod.Horizontal;
            _feverLabel = Label("Fever  0%", 14, VisualTokens.YellowValue, new Vector2(0.5f, 0.196f), new Vector2(400, 22), true);
            _hintLabel = Label("点按 头像   上滑 技能   驱动满 GOOD   粉闪 Fever", 15, VisualTokens.TextPrimary,
                new Vector2(0.5f, 0.018f), new Vector2(1040, 32), true);
        }

        void BuildPortraits()
        {
            var battle = _host.Battle;
            _chargeRing = new Image[5];
            _hpFill = new Image[5];
            _readyTag = new Text[5];
            _nameTag = new Text[5];
            _wing = new Image[5];
            _portraitDim = new CanvasGroup[5];
            for (int i = 0; i < 5; i++)
            {
                var slot = i;
                var x = 0.10f + i * 0.20f;
                var y = 0.108f + 0.018f * Mathf.Sin(i * 0.785f);
                var def = battle.Allies[i].Def;
                var go = new GameObject("p" + i, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(PortraitGesture));
                go.transform.SetParent(_root, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(x, y);
                rt.sizeDelta = new Vector2(148, 148);
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, UiSprites.Circle());
                img.color = new Color(0.08f, 0.07f, 0.07f, 0.94f);
                _portraitDim[i] = go.GetComponent<CanvasGroup>();

                var ring = ChildImg(go.transform, "ring", Vector2.zero, Vector2.one, new Vector2(-14f, -14f), new Vector2(14f, 14f), Color.white);
                UiSprites.Apply(ring, UiSprites.Circle());
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillOrigin = (int)Image.Origin360.Top;
                ring.fillClockwise = true;
                ring.fillAmount = 0f;
                ring.raycastTarget = false;
                ring.transform.SetAsFirstSibling();
                _chargeRing[i] = ring;

                CharacterPresenter.Draw(go.transform, def, new Vector2(0.5f, 0.56f), 132f, "", false);

                var g = go.GetComponent<PortraitGesture>();
                g.OnTap = () => OnPortraitTap(slot);
                g.OnSlide = () =>
                {
                    if (QteOpen) return;
                    if (_host.Battle != null) _host.Battle.TrySlide(slot);
                };

                _readyTag[i] = ChildLabel(go.transform, "", 13, VisualTokens.YellowConfirm, new Vector2(0.5f, 1.05f), new Vector2(148, 22));
                var readyOl = _readyTag[i].gameObject.AddComponent<Outline>();
                readyOl.effectColor = Color.black;
                readyOl.effectDistance = new Vector2(2f, -2f);
                _nameTag[i] = ChildLabel(go.transform, ShortName(def.Name), 15, Color.white, new Vector2(0.5f, 0.08f), new Vector2(140, 22));

                var hpbg = ChildImg(go.transform, "hpbg", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.7f));
                hpbg.rectTransform.sizeDelta = new Vector2(108, 8);
                hpbg.rectTransform.anchoredPosition = new Vector2(0f, -10f);
                UiSprites.Apply(hpbg, UiSprites.Round());
                var fill = ChildImg(hpbg.transform, "fill", Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f), VisualTokens.YellowValue);
                UiSprites.Apply(fill, UiSprites.Round());
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillAmount = 1f;
                fill.raycastTarget = false;
                _hpFill[i] = fill;

                var wing = ChildImg(go.transform, "wing", new Vector2(0.5f, 1.08f), new Vector2(0.5f, 1.08f), Vector2.zero, Vector2.zero, VisualTokens.DriveOrange);
                wing.rectTransform.sizeDelta = new Vector2(22, 14);
                UiSprites.Apply(wing, UiSprites.Soft());
                wing.raycastTarget = false;
                wing.enabled = false;
                _wing[i] = wing;
                _built.Add(go);
            }

            var goodGo = new GameObject("good", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            goodGo.transform.SetParent(_root, false);
            var grt = goodGo.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.29f);
            grt.sizeDelta = new Vector2(196, 196);
            _goodBtn = goodGo.GetComponent<Image>();
            UiSprites.Apply(_goodBtn, UiSprites.Circle());
            _goodBtn.color = Opaque(VisualTokens.Ember);
            _goodBtn.raycastTarget = true;
            _goodGroup = goodGo.GetComponent<CanvasGroup>();
            _goodGroup.alpha = 1f;
            _goodGroup.interactable = true;
            _goodGroup.blocksRaycasts = true;
            goodGo.GetComponent<Button>().onClick.AddListener(OnGoodClick);
            _goodLabel = ChildLabel(goodGo.transform, "GOOD", 32, VisualTokens.TextOnYellow, new Vector2(0.5f, 0.56f), new Vector2(180, 72));
            ChildLabel(goodGo.transform, "点按", 16, VisualTokens.TextOnYellow, new Vector2(0.5f, 0.28f), new Vector2(160, 32));
            _built.Add(goodGo);
        }

        void OnPortraitTap(int slot)
        {
            var b = _host.Battle;
            if (b == null || QteOpen) return;
            if (b.TryPortraitTap(slot) && b.PendingDriveSlot == slot)
                OpenQte();
        }

        void OnGoodClick()
        {
            if (!QteOpen) return;
            var t = Mathf.PingPong(_qteT, 1.2f);
            DriveTiming timing;
            if (t > 0.52f && t < 0.68f) timing = DriveTiming.Perfect;
            else if (t > 0.42f && t < 0.78f) timing = DriveTiming.Great;
            else if (t > 0.28f && t < 0.92f) timing = DriveTiming.Good;
            else timing = DriveTiming.Bad;
            FinishQte(timing);
        }

        void OpenQte()
        {
            var b = _host.Battle;
            if (b == null || b.PendingDriveSlot < 0) return;
            QteOpen = true;
            _qteT = 0f;
            if (_goodBtn != null)
            {
                _goodBtn.gameObject.SetActive(true);
                _goodBtn.enabled = true;
                _goodBtn.raycastTarget = true;
                _goodBtn.color = Opaque(VisualTokens.Ember);
                _goodBtn.transform.localScale = Vector3.one;
                _goodBtn.transform.SetAsLastSibling();
            }
            if (_goodGroup != null)
            {
                _goodGroup.alpha = 1f;
                _goodGroup.interactable = true;
                _goodGroup.blocksRaycasts = true;
            }
            if (_goodLabel != null)
            {
                _goodLabel.text = "GOOD";
                _goodLabel.color = Opaque(VisualTokens.TextOnYellow);
            }
            ShowStamp("点按 GOOD", VisualTokens.DriveOrange, 0.85f);
        }

        void FinishQte(DriveTiming timing)
        {
            var b = _host.Battle;
            QteOpen = false;
            HideGood();
            if (b == null) return;
            b.ResolveDrive(timing);
            _judgeFromQte = true;
            ShowJudge(timing);
        }

        void HideGood()
        {
            if (_goodBtn != null)
            {
                _goodBtn.gameObject.SetActive(false);
                _goodBtn.transform.localScale = Vector3.one;
            }
        }

        void PulseGood()
        {
            if (_goodBtn == null || !_goodBtn.gameObject.activeSelf) return;
            if (_goodGroup != null) _goodGroup.alpha = 1f;
            var t = Mathf.PingPong(_qteT, 1.2f);
            var glow = Mathf.Clamp01(1f - Mathf.Abs(t - 0.60f) / 0.40f);
            _goodBtn.transform.localScale = Vector3.one * (1f + 0.32f * glow);
            _goodBtn.color = Opaque(Color.Lerp(VisualTokens.Ember, VisualTokens.YellowConfirm, glow));
            if (_goodLabel != null) _goodLabel.color = Opaque(VisualTokens.TextOnYellow);
        }

        void ShowJudge(DriveTiming timing)
        {
            switch (timing)
            {
                case DriveTiming.Perfect:
                    ShowStamp("完美  150%", new Color(0.78f, 0.55f, 1f), 1.0f);
                    break;
                case DriveTiming.Great:
                    ShowStamp("优秀", VisualTokens.SlideGreen, 0.85f);
                    break;
                case DriveTiming.Good:
                    ShowStamp("好", VisualTokens.YellowValue, 0.7f);
                    break;
                default:
                    ShowStamp("偏了", VisualTokens.TextMuted, 0.6f);
                    break;
            }
        }

        void Refresh()
        {
            var battle = _host.Battle;
            if (battle == null) return;
            if (_fieldWave != battle.WaveIndex) SpawnEnemies();
            PulseGood();

            int hpNow = 0, hpMax = 0, phpNow = 0, phpMax = 0;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                hpNow += battle.Enemies[i].Hp;
                hpMax += battle.Enemies[i].MaxHp;
            }
            for (int i = 0; i < 5; i++)
            {
                phpNow += battle.Allies[i].Hp;
                phpMax += battle.Allies[i].MaxHp;
            }
            if (_archHp != null)
                _archHp.fillAmount = hpMax <= 0 ? 0f : (float)hpNow / hpMax;
            if (_archNum != null)
                _archNum.text = hpNow + " / " + hpMax;
            if (_archMeta != null)
            {
                var pct = hpMax <= 0 ? 0 : Mathf.RoundToInt(100f * hpNow / hpMax);
                var table = Catalog.Chapter(_host.SaveData.UseHard);
                var stName = "";
                var idx = _host.ActiveStageIndex;
                if (table != null && idx >= 0 && idx < table.Length && table[idx] != null)
                {
                    var raw = table[idx].Name ?? "";
                    var sp = raw.LastIndexOf(' ');
                    stName = sp >= 0 && sp + 1 < raw.Length ? raw.Substring(sp + 1) : raw;
                }
                _archMeta.text = stName + "  第" + (battle.WaveIndex + 1) + "/2波  " + pct + "%  "
                    + Mathf.CeilToInt(battle.TimeLeft).ToString("00") + "s";
            }
            if (_partyHp != null)
                _partyHp.fillAmount = phpMax <= 0 ? 0f : (float)phpNow / phpMax;
            if (_partyHpLabel != null)
                _partyHpLabel.text = "HP  " + (phpMax <= 0 ? 0 : Mathf.RoundToInt(100f * phpNow / phpMax)) + "%";
            if (_driveBar != null)
            {
                _driveBar.fillAmount = Mathf.Clamp01(battle.Drive / 100f);
                _driveBar.color = battle.Drive >= 100f ? VisualTokens.YellowConfirm : VisualTokens.Ember;
            }
            if (_driveLabel != null)
            {
                if (battle.Drive >= 100f)
                {
                    _driveLabel.text = "驱动 满  点头像";
                    _driveLabel.color = VisualTokens.YellowConfirm;
                }
                else
                {
                    _driveLabel.text = "驱动  " + Mathf.RoundToInt(battle.Drive) + "%";
                    _driveLabel.color = VisualTokens.DriveOrange;
                }
            }
            if (_feverBar != null && _feverLabel != null)
            {
                if (battle.FeverActive)
                {
                    _feverBar.fillAmount = battle.FeverLeft <= 0f ? 0f : Mathf.Clamp01(battle.FeverLeft / 7f);
                    _feverBar.color = Color.Lerp(FeverPink, VisualTokens.FeverGold, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f));
                    _feverLabel.text = "Fever  狂热";
                    _feverLabel.color = FeverPink;
                }
                else
                {
                    _feverBar.fillAmount = Mathf.Clamp01(battle.FeverGauge / 100f);
                    _feverBar.color = VisualTokens.FeverGold;
                    _feverLabel.text = "Fever  " + Mathf.RoundToInt(battle.FeverGauge) + "%";
                    _feverLabel.color = VisualTokens.YellowValue;
                }
            }
            var feverStart = battle.FeverActive && !_feverWas;
            if (feverStart) _feverBurstT = 0.90f;
            _feverWas = battle.FeverActive;
            if (_feverBurstT > 0f) _feverBurstT -= Time.unscaledDeltaTime;
            if (_feverFlash != null)
            {
                if (battle.FeverActive)
                {
                    var k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7.2f);
                    var burst = Mathf.Clamp01(_feverBurstT / 0.90f);
                    _feverFlash.color = new Color(1f, 0.55f, 0.85f, 0.18f + 0.20f * k + 0.28f * burst);
                }
                else _feverFlash.color = Color.clear;
            }
            if (_speedLabel != null) _speedLabel.text = battle.Speed == 2 ? "×2" : "×1";
            if (_autoLabel != null) _autoLabel.text = AutoWord(_host.SaveData.Auto);
            if (_pauseLabel != null) _pauseLabel.text = battle.Paused ? "继续" : "暂停";

            if (_fieldAllies != null)
            {
                for (int i = 0; i < _fieldAllies.Length; i++)
                {
                    if (_fieldAllies[i] == null) continue;
                    var pulse = battle.FeverActive ? 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 8f + i) : 1f;
                    _fieldAllies[i].transform.localScale = Vector3.one * pulse;
                    _fieldAllies[i].Apply(battle.Allies[i]);
                }
            }
            if (_fieldEnemies != null)
            {
                for (int i = 0; i < _fieldEnemies.Length && i < battle.Enemies.Count; i++)
                    if (_fieldEnemies[i] != null) _fieldEnemies[i].Apply(battle.Enemies[i]);
            }

            TickOverlays();
            DrainCombatLog();
            DrainCasts();
            RefreshPortraits();
            if (feverStart) ShowStamp("Fever", FeverPink, 1.20f);
        }

        void RefreshPortraits()
        {
            var battle = _host.Battle;
            if (battle == null || _chargeRing == null) return;
            for (int i = 0; i < 5; i++)
            {
                var u = battle.Allies[i];
                var charge = Mathf.Clamp01(u.Charge / 100f);
                if (_chargeRing[i] != null)
                {
                    _chargeRing[i].fillAmount = charge;
                    if (!u.Alive)
                        _chargeRing[i].color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                    else if (battle.Drive >= 100f)
                        _chargeRing[i].color = VisualTokens.DriveOrange;
                    else if (u.Charge >= 100f)
                        _chargeRing[i].color = VisualTokens.YellowConfirm;
                    else
                        _chargeRing[i].color = Color.Lerp(new Color(1f, 1f, 1f, 0.18f), VisualTokens.SlideGreen, charge);
                }
                if (_hpFill[i] != null)
                    _hpFill[i].fillAmount = u.MaxHp <= 0 ? 0f : (float)u.Hp / u.MaxHp;
                if (_portraitDim[i] != null)
                    _portraitDim[i].alpha = u.Alive ? 1f : 0.35f;
                var driveReady = u.Alive && battle.Drive >= 100f;
                if (_wing[i] != null) _wing[i].enabled = driveReady;
                if (_readyTag[i] != null)
                {
                    if (!u.Alive)
                    {
                        _readyTag[i].text = "";
                    }
                    else if (driveReady)
                    {
                        _readyTag[i].text = u.Charge >= 100f ? "驱动 / 上滑" : "驱动";
                        _readyTag[i].color = VisualTokens.DriveOrange;
                    }
                    else if (u.Charge >= 100f)
                    {
                        _readyTag[i].text = "点按 / 上滑";
                        _readyTag[i].color = VisualTokens.YellowConfirm;
                    }
                    else
                    {
                        _readyTag[i].text = "蓄力";
                        _readyTag[i].color = VisualTokens.TextMuted;
                    }
                }
                if (_nameTag[i] != null)
                    _nameTag[i].text = ShortName(u.Def.Name) + "  LV" + Mathf.Max(1, _host.SaveData.GetUnit(u.Def.Id).Level);
            }
        }

        void TickOverlays()
        {
            if (_showtimeT > 0f)
            {
                _showtimeT -= Time.unscaledDeltaTime;
                if (_showtime != null)
                {
                    var glow = _stamp != null ? _stamp.color : VisualTokens.SlideGreen;
                    _showtime.color = new Color(glow.r * 0.55f, glow.g * 0.12f, glow.b * 0.12f, 0.42f * Mathf.Clamp01(_showtimeT / 1.10f));
                }
            }
            else if (_showtime != null) _showtime.color = Color.clear;

            if (_stampT > 0f)
            {
                _stampT -= Time.unscaledDeltaTime;
                if (_stamp != null)
                {
                    var c = _stamp.color;
                    c.a = Mathf.Clamp01(_stampT / 0.85f);
                    _stamp.color = c;
                    _stamp.transform.localScale = Vector3.one * (1f + 0.22f * Mathf.Clamp01(_stampT / 0.85f));
                }
            }
            else if (_stamp != null)
            {
                var c = _stamp.color;
                c.a = 0f;
                _stamp.color = c;
            }

            if (_warnT > 0f)
            {
                _warnT -= Time.unscaledDeltaTime;
                if (_warnFlash != null)
                    _warnFlash.color = new Color(0.55f, 0.05f, 0.05f, 0.45f * Mathf.Clamp01(_warnT / 0.8f));
            }
            else if (_warnFlash != null) _warnFlash.color = Color.clear;
        }

        void DrainCombatLog()
        {
            var battle = _host.Battle;
            var log = battle.Log;
            while (_logCursor < log.Count)
            {
                var ft = log[_logCursor++];
                BattleFighter target = null;
                if (ft.Ally)
                {
                    if (_fieldAllies != null && ft.UnitSlot >= 0 && ft.UnitSlot < _fieldAllies.Length)
                        target = _fieldAllies[ft.UnitSlot];
                }
                else if (_fieldEnemies != null && ft.UnitSlot >= 0 && ft.UnitSlot < _fieldEnemies.Length)
                    target = _fieldEnemies[ft.UnitSlot];
                if (target != null)
                {
                    target.Hit();
                    FloatHop.Spawn(_fieldLayer != null ? _fieldLayer : _root, target.Anchor + new Vector2(0f, 0.08f),
                        (ft.Crit ? "!" : "") + ft.Text, ft.Crit, ft.Heal, ft.Kind, ft.Fever);
                    if (ft.Fever) CanvasShake.Punch(26f, 0.32f);
                    else if (ft.Kind == SkillType.Drive) CanvasShake.Punch(18f, 0.24f);
                    else if (ft.Crit) CanvasShake.Punch(11f, 0.16f);
                    else CanvasShake.Punch(4.5f, 0.10f);
                }
                BattleFighter caster = null;
                if (ft.CasterAlly)
                {
                    if (_fieldAllies != null && ft.CasterSlot >= 0 && ft.CasterSlot < _fieldAllies.Length)
                        caster = _fieldAllies[ft.CasterSlot];
                }
                else if (_fieldEnemies != null && ft.CasterSlot >= 0 && ft.CasterSlot < _fieldEnemies.Length)
                    caster = _fieldEnemies[ft.CasterSlot];
                if (caster != null)
                {
                    caster.PlayCast(ft.Kind, ft.Fever);
                    if (target != null)
                    {
                        var col = ft.Heal
                            ? new Color(0.45f, 1f, 0.55f)
                            : (ft.Fever ? VisualTokens.FeverGold : VisualTokens.Skill(ft.Kind));
                        AttackTrail.Fire(_fieldLayer != null ? _fieldLayer : _root, caster.Anchor, target.Anchor, col, ft.Kind, ft.Fever, ft.Heal);
                    }
                }
            }
        }

        void DrainCasts()
        {
            var battle = _host.Battle;
            var log = battle.Casts;
            while (_castCursor < log.Count)
            {
                var fx = log[_castCursor++];
                if (fx.Fever)
                {
                    CanvasShake.Punch(34f, 0.40f);
                    continue;
                }
                if (_skillBanner != null && fx.Type != SkillType.Tap)
                {
                    _skillBanner.text = VisualTokens.SkillTag(fx.Type) + "  " + fx.Name;
                    _skillBanner.color = VisualTokens.Skill(fx.Type);
                }
                if (fx.Type == SkillType.Slide && fx.CasterAlly)
                    ShowStamp("开演", VisualTokens.StarEvolved, 1.10f);
                else if (fx.Type == SkillType.Drive && fx.CasterAlly)
                {
                    if (_judgeFromQte) _judgeFromQte = false;
                    else ShowStamp("优秀", VisualTokens.SlideGreen, 0.70f);
                }
                else if (fx.Type == SkillType.Drive && !fx.CasterAlly)
                {
                    _warnT = 0.80f;
                    ShowStamp("警告", VisualTokens.StarEvolved, 0.80f);
                }
            }
        }

        void ShowStamp(string text, Color color, float life)
        {
            _showtimeT = life;
            _stampT = life;
            if (_stamp != null)
            {
                _stamp.text = text;
                _stamp.color = color;
            }
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
            var battle = _host.Battle;
            _fieldAllies = new BattleFighter[5];
            for (int i = 0; i < 5; i++)
            {
                var fx = BattleFighter.Create(_fieldLayer, battle.Allies[i], CharacterPresenter.AllyFieldAnchor(i), false);
                _built.Add(fx.gameObject);
                _fieldAllies[i] = fx;
            }
        }

        void SpawnEnemies()
        {
            var battle = _host.Battle;
            if (_fieldEnemies != null)
            {
                for (int i = 0; i < _fieldEnemies.Length; i++)
                {
                    if (_fieldEnemies[i] != null)
                        Object.Destroy(_fieldEnemies[i].gameObject);
                }
            }
            var n = battle.Enemies.Count;
            _fieldEnemies = new BattleFighter[n];
            for (int i = 0; i < n; i++)
            {
                var u = battle.Enemies[i];
                var fx = BattleFighter.Create(_fieldLayer, u, CharacterPresenter.EnemyFieldAnchor(i, n), u.Def.IsBoss);
                _built.Add(fx.gameObject);
                _fieldEnemies[i] = fx;
            }
            _fieldWave = battle.WaveIndex;
        }

        static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        static string AutoWord(AutoMode m)
        {
            if (m == AutoMode.Full) return "全自动";
            if (m == AutoMode.Semi) return "半自动";
            return "手动";
        }

        static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Length <= 4 ? name : name.Substring(0, 4);
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

        Text Pill(string label, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Pill());
            img.color = VisualTokens.YellowConfirm;
            go.GetComponent<Button>().onClick.AddListener(click);
            var tx = ChildLabel(go.transform, label, 22, VisualTokens.TextOnYellow, new Vector2(0.5f, 0.5f), size);
            _built.Add(go);
            return tx;
        }

        Text Ghost(string label, Vector2 anchor, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(140, 48);
            go.GetComponent<Image>().color = Color.clear;
            go.GetComponent<Button>().onClick.AddListener(click);
            var tx = ChildLabel(go.transform, label, 20, VisualTokens.TextPrimary, new Vector2(0.5f, 0.5f), new Vector2(140, 48));
            var ol = tx.gameObject.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(2, -2);
            _built.Add(go);
            return tx;
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
