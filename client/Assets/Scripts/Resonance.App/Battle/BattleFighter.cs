using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public sealed class BattleFighter : MonoBehaviour
    {
        public int Slot;
        public bool Ally;
        public Element Elem;
        public string UnitName;

        Image _hp;
        Image _charge;
        Image _flash;
        Image _focusMark;
        Image _pixel;
        Sprite _pixA;
        Sprite _pixB;
        Text _hpNum;
        Text _slideTag;
        CanvasGroup _group;
        RectTransform _body;
        Vector2 _rest;
        int _lastHp = -1;
        float _flashT;
        float _flashLife = 0.22f;
        float _lungeT;
        float _lungeDur = 0.22f;
        float _lungeDist = 96f;
        float _hitT;
        float _face = 1f;
        Color _flashColor = Color.white;
        bool _dead;
        float _deadT;
        bool _splashHidden;
        SkillType _castKind;

        public static BattleFighter Create(Transform parent, UnitState unit, Vector2 anchor, bool boss)
        {
            if (parent == null || unit == null) return null;
            var height = unit.Ally ? 340f : (boss ? 480f : 280f);
            var go = new GameObject(unit.Ally ? "ally" : "foe", typeof(RectTransform), typeof(CanvasGroup), typeof(BattleFighter));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) { Object.Destroy(go); return null; }
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(height * 0.70f, height + 48f);
            rt.anchoredPosition = Vector2.zero;

            var plat = new GameObject("plat", typeof(RectTransform), typeof(Image));
            plat.transform.SetParent(go.transform, false);
            var prt = plat.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.10f);
            prt.sizeDelta = new Vector2(height * 0.72f, height * 0.22f);
            var pimg = plat.GetComponent<Image>();
            UiSprites.Apply(pimg, UiSprites.Circle());
            pimg.color = new Color(0.10f, 0.09f, 0.09f, 0.40f);
            pimg.raycastTarget = false;
            var rim = new GameObject("rim", typeof(RectTransform), typeof(Image));
            rim.transform.SetParent(plat.transform, false);
            var rrt = rim.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = new Vector2(-2f, -2f);
            rrt.offsetMax = new Vector2(2f, 2f);
            var rimg = rim.GetComponent<Image>();
            UiSprites.Apply(rimg, UiSprites.Circle());
            var el = VisualTokens.Element(unit.Def != null ? unit.Def.Element : Element.Dark);
            rimg.color = new Color(el.r, el.g, el.b, 0.55f);
            rimg.raycastTarget = false;
            rim.transform.SetAsFirstSibling();

            var body = new GameObject("pixel", typeof(RectTransform), typeof(Image));
            body.transform.SetParent(go.transform, false);
            var brt = body.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.58f);
            brt.sizeDelta = new Vector2(height * 0.78f, height);
            brt.anchoredPosition = Vector2.zero;
            var bimg = body.GetComponent<Image>();
            var stand = unit.Def != null ? CharacterArt.Body(unit.Def.Id) : null;
            if (stand == null && unit.Def != null) stand = CharacterArt.Face(unit.Def.Id);
            if (stand == null) stand = PixelStandIn.Get(unit.Def, "", 0);
            if (stand == null) stand = UiSprites.Pixel();
            bimg.sprite = stand;
            bimg.preserveAspect = true;
            bimg.raycastTarget = false;

            var fx = go.GetComponent<BattleFighter>();
            if (fx == null) { Object.Destroy(go); return null; }
            fx.Slot = unit.Slot;
            fx.Ally = unit.Ally;
            fx.Elem = unit.Def != null ? unit.Def.Element : Element.Dark;
            fx.UnitName = unit.Def != null ? unit.Def.Name : "";
            fx._group = go.GetComponent<CanvasGroup>();
            if (fx._group != null) fx._group.alpha = 1f;
            fx._body = brt;
            fx._pixel = bimg;
            fx._pixA = stand;
            fx._pixB = stand;
            fx._rest = Vector2.zero;
            fx._face = unit.Ally ? 1f : -1f;
            fx._hp = Bar(go.transform, "hp", new Vector2(0.5f, 0.08f), new Vector2(height * 0.52f, 10f),
                unit.Ally ? VisualTokens.YellowValue : VisualTokens.StarEvolved);
            if (!unit.Ally)
            {
                // Primary GT: small skull under enemy HP bar (P0 bloops / Old Skull).
                var skull = new GameObject("skull", typeof(RectTransform), typeof(Image));
                skull.transform.SetParent(go.transform, false);
                var srt = skull.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(0.22f, 0.04f);
                srt.sizeDelta = new Vector2(boss ? 18f : 14f, boss ? 18f : 14f);
                var simg = skull.GetComponent<Image>();
                UiSprites.Apply(simg, UiSprites.Spark());
                simg.color = new Color(0.92f, 0.90f, 0.88f, 0.88f);
                simg.raycastTarget = false;
                // Primary Robin ~t48: green SLIDE charge under foe HP.
                fx._charge = Bar(go.transform, "ch", new Vector2(0.5f, 0.02f), new Vector2(height * 0.52f, 6f), VisualTokens.SlideGreen);
                fx._slideTag = CharacterPresenter.Label(go.transform, BattleCueCopy.SlidePipEn, 11, VisualTokens.SlideGreen,
                    new Vector2(0.72f, 0.02f), new Vector2(72f, 18f), true);
                if (fx._slideTag != null) fx._slideTag.color = Color.clear;
                // Robin ~t68 ND: "CORE" pip under boss HP (ordinary bloops keep skull only).
                if (boss)
                {
                    var core = CharacterPresenter.Label(go.transform, BattleCueCopy.EnemyCore, 10, VisualTokens.GoldMetal,
                        new Vector2(0.42f, 0.005f), new Vector2(64f, 16f), true);
                    if (core != null)
                    {
                        var col = core.color;
                        col.a = 0.92f;
                        core.color = col;
                    }
                }
            }
            if (unit.Ally)
                fx._charge = Bar(go.transform, "ch", new Vector2(0.5f, 0.03f), new Vector2(height * 0.52f, 5f), VisualTokens.Ember);
            fx._flash = FlashPlate(go.transform);
            fx._hpNum = CharacterPresenter.Label(go.transform, unit.Hp.ToString(), unit.Ally || boss ? 22 : 18, VisualTokens.YellowValue,
                new Vector2(0.5f, -0.01f), new Vector2(height * 0.90f, 28f), true);
            {
                var unitName = unit.Def != null ? unit.Def.Name : "";
                var unitLv = unit.Def != null && unit.Def.BattleLevel > 0 ? unit.Def.BattleLevel : 1;
                // Foe: bloops Small/N Name; titled ND Title/N Name (r25/r35); skulls Lv.; else N Name.
                // Ally: "Hero"/"N Name" (Robin ~t57).
                var plate = unit.Ally
                    ? (BattleCueCopy.AllyRoleHero + "\n" + unitLv + " " + unitName)
                    : BattleCueCopy.FoePlateLine(unitLv, unitName);
                var tallPlate = unit.Ally || plate.IndexOf('\n') >= 0;
                var nm = CharacterPresenter.Label(go.transform, plate, boss ? 26 : (unit.Ally ? 16 : 20), Color.white,
                    new Vector2(0.5f, unit.Ally ? 0.18f : 0.175f), new Vector2(height * 0.98f, tallPlate ? 44f : 36f), true);
                if (nm != null && tallPlate)
                {
                    nm.horizontalOverflow = HorizontalWrapMode.Wrap;
                    nm.verticalOverflow = VerticalWrapMode.Overflow;
                    nm.alignment = TextAnchor.LowerCenter;
                    nm.lineSpacing = 0.85f;
                }
                if (nm != null)
                {
                    var ol2 = nm.gameObject.AddComponent<Outline>();
                    ol2.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    ol2.effectDistance = new Vector2(1f, -1f);
                }
                if (!unit.Ally)
                {
                    // P0 t70: overhead "The Hanged" on skull hangers (not foot plate).
                    var banner = BattleCueCopy.FoeBannerForName(unitName);
                    if (!string.IsNullOrEmpty(banner))
                    {
                        var ban = CharacterPresenter.Label(go.transform, banner, boss ? 14 : 12,
                            VisualTokens.TapWhite, new Vector2(0.5f, 0.92f), new Vector2(height * 0.85f, 22f), true);
                        if (ban != null)
                        {
                            ban.color = new Color(0.92f, 0.92f, 0.94f, 0.92f);
                            var bol = ban.gameObject.AddComponent<Outline>();
                            bol.effectColor = new Color(0f, 0f, 0f, 0.85f);
                            bol.effectDistance = new Vector2(1f, -1f);
                        }
                    }
                }
                // Element / role pip left of name (water drop / sun / ally leaf-role).
                var elPip = new GameObject("elPip", typeof(RectTransform), typeof(Image));
                elPip.transform.SetParent(go.transform, false);
                var ert = elPip.GetComponent<RectTransform>();
                ert.anchorMin = ert.anchorMax = new Vector2(unit.Ally ? 0.14f : 0.18f, unit.Ally ? 0.16f : 0.175f);
                ert.sizeDelta = new Vector2(boss ? 22f : 16f, boss ? 22f : 16f);
                var eimg = elPip.GetComponent<Image>();
                UiSprites.Apply(eimg, UiSprites.Circle());
                if (unit.Ally)
                    eimg.color = new Color(VisualTokens.SlideGreen.r, VisualTokens.SlideGreen.g, VisualTokens.SlideGreen.b, 0.95f);
                else
                {
                    eimg.color = new Color(el.r, el.g, el.b, 0.95f);
                }
                eimg.raycastTarget = false;
                if (!unit.Ally)
                {
                    var ul = new GameObject("nameul", typeof(RectTransform), typeof(Image));
                    ul.transform.SetParent(go.transform, false);
                    var urt = ul.GetComponent<RectTransform>();
                    urt.anchorMin = urt.anchorMax = new Vector2(0.5f, 0.148f);
                    urt.sizeDelta = new Vector2(height * (boss ? 0.44f : 0.34f), boss ? 4f : 3f);
                    var uimg = ul.GetComponent<Image>();
                    if (uimg != null)
                    {
                        UiSprites.Apply(uimg, boss ? UiSprites.Slash() : UiSprites.Dashed());
                        uimg.color = new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.85f);
                        uimg.raycastTarget = false;
                    }
                }
            }
            if (boss)
                CharacterPresenter.Label(go.transform, "BOSS", 16, VisualTokens.GoldSelect,
                    new Vector2(0.5f, 0.235f), new Vector2(100f, 24f), true);
            fx.Apply(unit, true);
            return fx;
        }

        public void BindFocus(System.Action onClick)
        {
            if (Ally || onClick == null) return;
            var hit = transform.Find("focusHit");
            if (hit == null)
            {
                var go = new GameObject("focusHit", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.01f);
                img.raycastTarget = true;
                hit = go.transform;
            }
            var btn = hit.GetComponent<Button>();
            if (btn == null) btn = hit.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick());
        }

        public void SetFocused(bool on)
        {
            if (Ally) return;
            if (_focusMark == null)
            {
                var go = new GameObject("focusMark", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.10f);
                rt.anchorMax = new Vector2(0.5f, 0.10f);
                rt.sizeDelta = new Vector2(88f, 18f);
                rt.anchoredPosition = Vector2.zero;
                _focusMark = go.GetComponent<Image>();
                UiSprites.Apply(_focusMark, UiSprites.Round());
                _focusMark.raycastTarget = false;
            }
            _focusMark.color = on
                ? new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.95f)
                : Color.clear;
            _focusMark.enabled = on;
        }

        public void Apply(UnitState u, bool instant = false)
        {
            if (u == null || _hp == null) return;
            var ratio = u.MaxHp <= 0 ? 0f : (float)u.Hp / u.MaxHp;
            _hp.fillAmount = ratio;
            if (_hpNum != null) _hpNum.text = u.Hp.ToString();
            if (_charge != null)
            {
                _charge.fillAmount = Mathf.Clamp01(u.Charge / 100f);
                if (!Ally)
                {
                    // Robin mid-fight: foe SLIDE gauge (green) or COOL when SlideCd ticking.
                    _charge.color = VisualTokens.SlideGreen;
                    if (_slideTag != null)
                    {
                        // Robin ~t48: foe can show COOL + SLIDE together under HP.
                        if (u.SlideCd > 0.05f && u.Charge > 8f)
                        {
                            _slideTag.text = "COOL  SLIDE";
                            _slideTag.color = VisualTokens.SlideGreen;
                        }
                        else if (u.SlideCd > 0.05f)
                        {
                            _slideTag.text = "COOL";
                            _slideTag.color = new Color(0.45f, 0.72f, 1f, 0.95f);
                        }
                        else if (u.Charge > 8f)
                        {
                            _slideTag.text = BattleCueCopy.SlidePipEn;
                            _slideTag.color = VisualTokens.SlideGreen;
                        }
                        else
                        {
                            _slideTag.text = BattleCueCopy.SlidePipEn;
                            _slideTag.color = Color.clear;
                        }
                    }
                }
                else
                    _charge.color = u.Charge >= 100f ? VisualTokens.YellowConfirm : VisualTokens.Ember;
            }
            VfxStatusIcons.DrawField(transform, StatusChipText.Labels(u), (Ally ? 0 : 20) + Slot);
            if (_lastHp >= 0 && u.Hp < _lastHp && !instant) Hit();
            _lastHp = u.Hp;
            if (!u.Alive)
            {
                if (!_dead) _deadT = 0.55f;
                _dead = true;
            }
            else if (_dead)
            {
                _dead = false;
                _deadT = 0f;
                if (_group != null && !_splashHidden) _group.alpha = 1f;
                if (_body != null) _body.localEulerAngles = Vector3.zero;
            }
        }

        /// <summary>
        /// P0 PHASE splash (t64–t66): field standees gone; portraits stay.
        /// Presentation hide only. Not T28.
        /// </summary>
        public void SetSplashHidden(bool hidden)
        {
            _splashHidden = hidden;
            if (_group == null) return;
            if (hidden) _group.alpha = 0f;
            else if (!_dead) _group.alpha = 1f;
        }

        public void PlayCue(PresentationCue cue)
        {
            if (cue.Kind == PresentationCueKind.Cast)
                PlayCast(cue.Channel, cue.Fever);
            else if (cue.Kind == PresentationCueKind.Hit)
                Hit();
        }

        public void Hit()
        {
            PlayFlash(new Color(1f, 0.82f, 0.78f), 0.22f);
            _hitT = 0.18f;
        }

        public void PlayCast(SkillType type, bool fever)
        {
            _castKind = type;
            var c = fever ? VisualTokens.FeverGold : VisualTokens.Skill(type);
            var life = fever ? 0.50f : type == SkillType.Drive ? 0.50f : type == SkillType.Slide ? 0.38f
                : type == SkillType.Tap ? 0.32f : 0.20f;
            PlayFlash(c, life);
            if (fever || type == SkillType.Drive)
            {
                _lungeDur = 0.44f;
                _lungeDist = 118f;
            }
            else if (type == SkillType.Slide)
            {
                _lungeDur = 0.34f;
                _lungeDist = 88f;
            }
            else if (type == SkillType.Tap)
            {
                _lungeDur = 0.28f;
                _lungeDist = 72f;
            }
            else
            {
                _lungeDur = 0.18f;
                _lungeDist = 36f;
            }
            _lungeT = _lungeDur;
        }

        void PlayFlash(Color c, float life)
        {
            _flashColor = c;
            _flashLife = Mathf.Max(0.08f, life);
            _flashT = _flashLife;
            if (_flash != null)
            {
                _flash.transform.SetAsLastSibling();
                _flash.color = new Color(c.r, c.g, c.b, 0.92f);
            }
        }

        public void Lunge()
        {
            _lungeT = 0.22f;
        }

        public Vector2 Anchor
        {
            get
            {
                var rt = transform as RectTransform;
                return rt != null ? rt.anchorMin : Vector2.zero;
            }
        }

        void LateUpdate()
        {
            if (_splashHidden)
            {
                if (_group != null) _group.alpha = 0f;
                return;
            }
            if (_flashT > 0f && _flash != null)
            {
                _flashT -= Time.unscaledDeltaTime;
                var a = Mathf.Clamp01(_flashT / _flashLife) * 0.90f;
                _flash.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, a);
            }
            if (_body == null) return;
            var phase = Slot * 1.17f;
            var breathY = Mathf.Sin(Time.unscaledTime * 3.1f + phase) * 7f;
            var breathS = 1f + Mathf.Sin(Time.unscaledTime * 2.5f + phase) * 0.035f;
            if (_dead)
            {
                _deadT -= Time.unscaledDeltaTime;
                var fall = 1f - Mathf.Clamp01(_deadT / 0.55f);
                if (_group != null) _group.alpha = Mathf.Lerp(1f, 0.28f, fall);
                _body.anchoredPosition = _rest + new Vector2(_face * 18f * fall, -36f * fall);
                _body.localEulerAngles = new Vector3(0f, 0f, _face * -22f * fall);
                _body.localScale = new Vector3(_face * Mathf.Lerp(1f, 0.82f, fall), Mathf.Lerp(1f, 0.72f, fall), 1f);
                return;
            }
            if (_pixel != null && _pixA != null && _pixB != null)
                _pixel.sprite = (_lungeT > 0f || Mathf.Repeat(Time.unscaledTime + Slot * 0.17f, 0.64f) < 0.32f) ? _pixB : _pixA;
            if (_lungeT > 0f)
            {
                _lungeT -= Time.unscaledDeltaTime;
                var u = 1f - Mathf.Clamp01(_lungeT / _lungeDur);
                var wind = u < 0.28f ? u / 0.28f : 1f - (u - 0.28f) / 0.72f;
                var dir = Ally ? _lungeDist : -_lungeDist;
                var dip = _castKind == SkillType.Tap || _castKind == SkillType.Auto ? 0.88f : 0.72f;
                var pop = _castKind == SkillType.Drive ? 1.28f : _castKind == SkillType.Slide ? 1.16f : 1.08f;
                var squash = u < 0.30f ? Mathf.Lerp(1f, dip, u / 0.30f) : Mathf.Lerp(dip, pop, (u - 0.30f) / 0.70f);
                var tilt = _castKind == SkillType.Slide ? -16f : _castKind == SkillType.Drive ? -10f : -4f;
                _body.anchoredPosition = _rest + new Vector2(0f, dir * Mathf.Clamp01(wind));
                _body.localScale = new Vector3(_face * squash, (2f - squash) * 0.92f, 1f);
                _body.localEulerAngles = new Vector3(0f, 0f, _face * tilt * wind);
                return;
            }
            if (_hitT > 0f)
            {
                _hitT -= Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(_hitT / 0.18f);
                _body.anchoredPosition = _rest + new Vector2(_face * -36f * k, breathY * 0.3f - 14f * k);
                _body.localScale = new Vector3(_face * (1f + 0.18f * k), 1f - 0.14f * k, 1f);
                return;
            }
            _body.anchoredPosition = _rest + new Vector2(0f, breathY);
            _body.localScale = new Vector3(_face * breathS, 2f - breathS, 1f);
            _body.localEulerAngles = Vector3.zero;
        }

        static Image Bar(Transform parent, string name, Vector2 anchor, Vector2 size, Color fill)
        {
            if (parent == null) return null;
            var bg = new GameObject(name + "bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(parent, false);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = anchor;
            brt.sizeDelta = size + new Vector2(2f, 2f);
            var bimg = bg.GetComponent<Image>();
            if (bimg != null)
            {
                UiSprites.Apply(bimg, UiSprites.Pixel());
                bimg.color = new Color(0.02f, 0.02f, 0.03f, 0.70f);
                bimg.raycastTarget = false;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(bg.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(1f, 1f);
            rt.offsetMax = new Vector2(-1f, -1f);
            var img = go.GetComponent<Image>();
            if (img == null) return null;
            UiSprites.Apply(img, UiSprites.Pixel());
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            img.color = fill;
            img.raycastTarget = false;

            var wire = new GameObject(name + "wire", typeof(RectTransform), typeof(Image));
            wire.transform.SetParent(bg.transform, false);
            var wrt = wire.GetComponent<RectTransform>();
            wrt.anchorMin = Vector2.zero;
            wrt.anchorMax = Vector2.one;
            wrt.offsetMin = Vector2.zero;
            wrt.offsetMax = Vector2.zero;
            var wimg = wire.GetComponent<Image>();
            if (wimg != null)
            {
                UiSprites.Apply(wimg, UiSprites.WireFrame());
                wimg.color = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.9f);
                wimg.raycastTarget = false;
            }
            return img;
        }

        static Image FlashPlate(Transform parent)
        {
            if (parent == null) return null;
            var go = new GameObject("flash", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.2f);
            rt.anchorMax = new Vector2(0.85f, 0.92f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (img == null) return null;
            UiSprites.Apply(img, UiSprites.Soft());
            img.color = Color.clear;
            img.raycastTarget = false;
            return img;
        }
    }

    public sealed class FloatHop : MonoBehaviour
    {
        Text _tx;
        float _life = 0.85f;
        float _max = 0.85f;
        Vector2 _from;
        float _pop = 1.35f;

        public static void Spawn(Transform parent, Vector2 anchor, string text, bool crit, bool heal, SkillType kind, bool fever)
        {
            if (parent == null) return;
            var go = new GameObject("dmg", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(FloatHop));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(320f, 80f);
            rt.anchoredPosition = Vector2.zero;
            var tx = go.GetComponent<Text>();
            if (tx == null) { Object.Destroy(go); return; }
            tx.font = CharacterPresenter.UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            if (fever || kind == SkillType.Drive) tx.fontSize = 56;
            else if (kind == SkillType.Slide || crit) tx.fontSize = 48;
            else tx.fontSize = 36;
            tx.text = text;
            if (heal) tx.color = new Color(0.45f, 1f, 0.55f);
            else if (fever) tx.color = VisualTokens.FeverGold;
            else if (kind == SkillType.Drive) tx.color = VisualTokens.DriveOrange;
            else if (kind == SkillType.Slide) tx.color = VisualTokens.SlideGreen;
            else if (kind == SkillType.Auto) tx.color = VisualTokens.AutoGrey;
            else tx.color = VisualTokens.TapWhite;
            var ol = go.GetComponent<Outline>();
            if (ol != null)
            {
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(3f, -3f);
            }
            var hop = go.GetComponent<FloatHop>();
            if (hop == null) return;
            hop._tx = tx;
            hop._from = new Vector2(Random.Range(-18f, 18f), 0f);
            hop._pop = fever ? 1.62f : kind == SkillType.Drive || crit ? 1.50f : kind == SkillType.Slide ? 1.36f : 1.18f;
            hop._life = fever || kind == SkillType.Drive ? 1.08f : kind == SkillType.Slide ? 0.92f : 0.72f;
            hop._max = hop._life;
        }

        void Update()
        {
            _life -= Time.unscaledDeltaTime;
            var u = 1f - Mathf.Clamp01(_life / Mathf.Max(0.01f, _max));
            var rt = transform as RectTransform;
            if (rt != null) rt.anchoredPosition = _from + new Vector2(_from.x * 0.15f, 110f * u);
            var punch = Mathf.Lerp(_pop, 0.88f, u);
            transform.localScale = Vector3.one * punch;
            if (_tx != null)
            {
                var c = _tx.color;
                c.a = 1f - u * u;
                _tx.color = c;
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }

    public sealed class AttackTrail : MonoBehaviour
    {
        RectTransform _bolt;
        Vector2 _from;
        Vector2 _to;
        Image _beam;
        Image _boltImg;
        Color _color;
        float _life = 0.28f;
        float _age;
        bool _hit;
        bool _critLike;
        SkillType _kind;
        bool _fever;

        public static void Fire(Transform parent, Vector2 from, Vector2 to, Color color, SkillType kind, bool fever, bool heal)
        {
            if (parent == null) return;
            if (heal)
            {
                CombatFeel.HealBurst(parent, to, color);
                CombatFeel.Impact(parent, to, color, false, false, SkillType.Tap);
                return;
            }
            // Tap: punch/impact only — no Slash ribbon (P0 isolated tap ≠ Slide slash).
            if (!fever && kind == SkillType.Tap)
            {
                CombatFeel.Impact(parent, to, color, false, false, kind);
                return;
            }
            CombatFeel.Slash(parent, from, to, color, kind, fever);
            if (!fever && kind == SkillType.Auto)
            {
                CombatFeel.Impact(parent, to, color, false, false, kind);
                return;
            }
            if (fever && (kind == SkillType.Tap || kind == SkillType.Auto))
                return;
            var go = new GameObject("trail", typeof(RectTransform), typeof(AttackTrail));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            var canvas = parent as RectTransform;
            var cw = canvas != null ? Mathf.Max(360f, canvas.rect.width) : 756f;
            var ch = canvas != null ? Mathf.Max(640f, canvas.rect.height) : 1344f;
            var mid = Vector2.Lerp(from, to, 0.45f);
            var a = new Vector2(from.x * cw, from.y * ch);
            var b = new Vector2(to.x * cw, to.y * ch);
            var delta = b - a;
            var dist = Mathf.Max(80f, delta.magnitude * 0.70f);
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var thick = 18f;
            if (fever) thick = 28f;
            else if (kind == SkillType.Drive) thick = 56f;
            else if (kind == SkillType.Slide) thick = 38f;
            else if (kind == SkillType.Tap) thick = 32f;
            else if (kind == SkillType.Auto) thick = 18f;

            var beamGo = new GameObject("beam", typeof(RectTransform), typeof(Image));
            beamGo.transform.SetParent(go.transform, false);
            var brt = beamGo.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = mid;
            brt.sizeDelta = new Vector2(dist, thick);
            brt.anchoredPosition = Vector2.zero;
            brt.localEulerAngles = new Vector3(0f, 0f, ang);
            var bimg = beamGo.GetComponent<Image>();
            if (bimg != null)
            {
                UiSprites.Apply(bimg, UiSprites.Slash());
                bimg.color = new Color(color.r, color.g, color.b, 0.88f);
                bimg.raycastTarget = false;
            }

            var boltGo = new GameObject("bolt", typeof(RectTransform), typeof(Image));
            boltGo.transform.SetParent(go.transform, false);
            var ort = boltGo.GetComponent<RectTransform>();
            ort.anchorMin = ort.anchorMax = from;
            var bolt = fever || kind == SkillType.Drive ? 72f : kind == SkillType.Slide ? 56f : kind == SkillType.Tap ? 48f : 32f;
            ort.sizeDelta = new Vector2(bolt, bolt);
            ort.anchoredPosition = Vector2.zero;
            var oimg = boltGo.GetComponent<Image>();
            if (oimg != null)
            {
                UiSprites.Apply(oimg, UiSprites.Soft());
                oimg.color = color;
                oimg.raycastTarget = false;
            }

            var fx = go.GetComponent<AttackTrail>();
            if (fx == null) return;
            fx._beam = bimg;
            fx._bolt = ort;
            fx._boltImg = oimg;
            fx._from = from;
            fx._to = to;
            fx._color = color;
            fx._critLike = fever || kind == SkillType.Drive;
            fx._kind = kind;
            fx._fever = fever;
            fx._life = kind == SkillType.Drive || fever ? 0.38f : kind == SkillType.Slide ? 0.30f : 0.18f;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / Mathf.Max(0.08f, _life * 0.70f));
            u = u * u * (3f - 2f * u);
            if (_bolt != null)
            {
                _bolt.anchorMin = _bolt.anchorMax = Vector2.Lerp(_from, _to, u);
                if (_boltImg != null)
                    _bolt.localScale = Vector3.one * (1.15f - u * 0.25f);
            }
            if (_beam != null)
            {
                var c = _beam.color;
                c.a = 0.88f * (1f - Mathf.Clamp01(_age / _life));
                _beam.color = c;
            }
            if (!_hit && u >= 0.92f)
            {
                _hit = true;
                CombatFeel.Impact(transform.parent, _to, _color, _critLike, _fever, _kind);
            }
            if (_age >= _life) Destroy(gameObject);
        }
    }
}
