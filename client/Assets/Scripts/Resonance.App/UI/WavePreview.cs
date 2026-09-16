using System;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public enum WaveCueKind
    {
        None = 0,
        WaveEnter,
        WaveAdvance,
        AllyDown,
        EnemyDown
    }

    public readonly struct WaveCue
    {
        public readonly WaveCueKind Kind;
        public readonly string Title;
        public readonly string Sub;
        public readonly Color Tint;

        public WaveCue(WaveCueKind kind, string title, string sub, Color tint)
        {
            Kind = kind;
            Title = title ?? "";
            Sub = sub ?? "";
            Tint = tint;
        }

        public bool Visible => Kind != WaveCueKind.None && !string.IsNullOrEmpty(Title);
    }

    /// <summary>
    /// 关卡波次预览 + 战斗换波/死亡提示（M1-G2-WAVE-UI）。
    /// 预览：宽暗马赛克卡 + 五敌片。金题印章、斜切金痕、半调网点衬底、内圈细金线、
    /// 四角六角铆钉。确认胶囊在卡下、躲开六页签。Boss 衔用 EN <c>BOSS</c>。
    /// 提示：只读 <see cref="BattleEventLog"/> 的 <c>wave</c> / <c>death</c>。
    /// 换波 ≠ 开战 / 开演 / 狂热时间，≠ Ragna <c>READY TO RUMBLE?</c>，≠ 20 人 WB 条。
    /// 布局 <see cref="LayoutStatus"/>。不是 T27。不宣称验收。
    /// </summary>
    public static class WavePreview
    {
        public const int SlotCount = 5;
        public const string LayoutStatus = "PRIMARY_PARTIAL_HANDOFF";
        public const string WaveEnter = "WAVE START";
        public const string WaveAdvance = "PHASE";
        public const string AllyDown = "DOWN";
        /// <summary>Kept for exclusivity checks. P0 t64–t90 shows no KO word on kill/wave-clear.</summary>
        public const string EnemyDown = "KO";
        public const string CueRootName = "WavePreviewCue";

        const float PanelY = 0.32f;
        const float ConfirmY = 0.13f;

        public static string VisibleTitle { get; internal set; }
        public static WaveCueKind VisibleKind { get; internal set; }

        /// <summary>
        /// Drive select / SHOWTIME / QTE owns the plate. Kill live 击破/倒下 and
        /// keep them queued so 06d is not double-stamped. Engineering exclusivity,
        /// not GT fidelity.
        /// </summary>
        public static void SuppressDeathCues()
        {
            var boards = UnityEngine.Object.FindObjectsByType<WaveCueBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < boards.Length; i++)
            {
                if (boards[i] != null) boards[i].SuppressDeathForExclusive();
            }
        }

        public static bool CueCopyDistinct
        {
            get
            {
                return WaveAdvance != BattleCueCopy.BattleStart
                    && WaveAdvance != BattleCueCopy.SlideShowtime
                    && WaveAdvance != BattleCueCopy.FeverTime
                    && WaveEnter != BattleCueCopy.BattleStart
                    && WaveEnter != WaveAdvance
                    && AllyDown != EnemyDown
                    && AllyDown != WaveAdvance
                    && AllyDown != BattleCueCopy.BattleStart
                    && EnemyDown != BattleCueCopy.FeverTime;
            }
        }

        public static void Draw(Transform parent, string[] enemyIds, Action onFight)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, null, "WavePreview");

            var panelGo = UiChrome.Panel(root, null, new Vector2(0.5f, PanelY), new Vector2(1040f, 360f));
            var panel = panelGo.transform;
            GoldWire(panel, new Vector2(1012f, 332f));

            OverlayDraw.Label(panel, null, "Enemies", 22, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.90f), new Vector2(320f, 36f), false, true, 2f, true);
            OverlayDraw.Pic(panel, null, "stampL", new Vector2(0.385f, 0.90f), new Vector2(26f, 26f),
                VisualTokens.GoldTitle, UiSprites.Stamp(2));
            OverlayDraw.Pic(panel, null, "stampR", new Vector2(0.615f, 0.90f), new Vector2(26f, 26f),
                VisualTokens.GoldTitle, UiSprites.Stamp(2));
            var slTl = OverlayDraw.Pic(panel, null, "slashL", new Vector2(0.44f, 0.838f), new Vector2(80f, 18f),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.80f),
                UiSprites.Slash());
            if (slTl != null) slTl.rectTransform.localEulerAngles = new Vector3(0f, 0f, 10f);
            var slTr = OverlayDraw.Pic(panel, null, "slashR", new Vector2(0.56f, 0.838f), new Vector2(80f, 18f),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.80f),
                UiSprites.Slash());
            if (slTr != null) slTr.rectTransform.localEulerAngles = new Vector3(0f, 0f, -10f);
            OverlayDraw.DashLine(panel, null, new Vector2(0.5f, 0.80f), 920f);

            var ht = OverlayDraw.Pic(panel, null, "halftone", new Vector2(0.5f, 0.42f), new Vector2(980f, 230f),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.10f),
                UiSprites.Halftone());
            if (ht != null)
            {
                ht.type = Image.Type.Tiled;
                ht.pixelsPerUnitMultiplier = 0.5f;
            }
            CornerBolts(panel);

            for (int i = 0; i < SlotCount; i++)
            {
                var id = enemyIds != null && i < enemyIds.Length ? enemyIds[i] : null;
                Chip(panel, new Vector2(0.14f + i * 0.18f, 0.42f), TryChar(id));
            }

            if (onFight != null)
            {
                var glow = OverlayDraw.Pic(root, null, "ctaGlow", new Vector2(0.5f, ConfirmY),
                    new Vector2(460f, 140f),
                    new Color(VisualTokens.YellowConfirm.r, VisualTokens.YellowConfirm.g, VisualTokens.YellowConfirm.b, 0.28f),
                    UiSprites.Soft());
                if (glow != null) glow.raycastTarget = false;
                var slL = OverlayDraw.Pic(root, null, "ctaSlashL", new Vector2(0.235f, ConfirmY), new Vector2(110f, 26f),
                    new Color(VisualTokens.YellowConfirm.r, VisualTokens.YellowConfirm.g, VisualTokens.YellowConfirm.b, 0.75f),
                    UiSprites.Slash());
                if (slL != null) slL.rectTransform.localEulerAngles = new Vector3(0f, 0f, 16f);
                var slR = OverlayDraw.Pic(root, null, "ctaSlashR", new Vector2(0.765f, ConfirmY), new Vector2(110f, 26f),
                    new Color(VisualTokens.YellowConfirm.r, VisualTokens.YellowConfirm.g, VisualTokens.YellowConfirm.b, 0.75f),
                    UiSprites.Slash());
                if (slR != null) slR.rectTransform.localEulerAngles = new Vector3(0f, 0f, -16f);
                var cta = UiChrome.Confirm(root, null, "FIGHT", new Vector2(0.5f, ConfirmY), onFight);
                if (cta != null) cta.transform.localScale = new Vector3(1.16f, 1.16f, 1f);
            }
        }

        public static WaveCueBoard Observe(Transform parent, BattleSim battle)
        {
            if (parent == null) return null;
            var board = FindBoard(parent);
            if (board == null)
            {
                var go = new GameObject(CueRootName, typeof(RectTransform), typeof(CanvasGroup), typeof(WaveCueBoard));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                }
                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                    cg.alpha = 0f;
                }
                board = go.GetComponent<WaveCueBoard>();
                board.Build();
            }
            board.Bind(battle);
            return board;
        }

        public static bool IsWaveEvent(BattleEvent ev)
        {
            return ev != null && ev.Kind == "wave";
        }

        public static bool IsDeathEvent(BattleEvent ev)
        {
            return ev != null && ev.Kind == "death";
        }

        public static bool TryRead(BattleEvent ev, BattleSim battle, out WaveCue cue)
        {
            cue = default;
            if (ev == null) return false;
            if (IsWaveEvent(ev))
            {
                var advance = ev.Opcode == "advance" || ev.Amount > 0;
                var kind = advance ? WaveCueKind.WaveAdvance : WaveCueKind.WaveEnter;
                // Primary P0 t65 / t510: large PHASE n + stage name; heart crest. No 击破.
                var phaseN = (ev.Amount < 0 ? 0 : ev.Amount) + 1;
                var phase = "PHASE " + phaseN;
                var stage = StageLabel();
                // GT: PHASE is the hero word; stage name sits above as secondary.
                var title = advance ? phase : WaveEnter;
                var sub = advance ? stage : WaveSub(ev.Amount);
                cue = new WaveCue(kind, title, sub, StampTint(kind));
                return cue.Visible;
            }
            if (IsDeathEvent(ev))
            {
                // P0 1fps 60–90s: last-wave clear → PHASE splash; mid-fight kills are
                // damage + cut-in only. Do not invent 击破. Ally down still engineering.
                if (!ev.TargetAlly) return false;
                var kind = WaveCueKind.AllyDown;
                cue = new WaveCue(kind, AllyDown, DeathSub(battle, ev), StampTint(kind));
                return cue.Visible;
            }
            return false;
        }

        public static Color StampTint(WaveCueKind kind)
        {
            switch (kind)
            {
                case WaveCueKind.WaveEnter:
                case WaveCueKind.WaveAdvance:
                    return VisualTokens.GoldSelect;
                case WaveCueKind.AllyDown:
                    return VisualTokens.StarEvolved;
                case WaveCueKind.EnemyDown:
                    return VisualTokens.OrangeCancel;
                default:
                    return VisualTokens.TextMuted;
            }
        }

        static string WaveSub(int waveIndex)
        {
            var n = waveIndex < 0 ? 0 : waveIndex;
            return "WAVE " + (n + 1);
        }

        static string StageLabel()
        {
            var g = GameRoot.Live;
            if (g == null || g.SaveData == null) return "";
            var hard = g.SaveData.UseHard;
            var table = Catalog.Chapter(hard);
            var idx = g.ActiveStageIndex;
            if (table == null || idx < 0 || idx >= table.Length || table[idx] == null) return "";
            var raw = table[idx].Name ?? "";
            var sp = raw.LastIndexOf(' ');
            var st = sp >= 0 && sp + 1 < raw.Length ? raw.Substring(sp + 1) : raw;
            if (hard)
            {
                if (st.EndsWith("困难"))
                    st = st.Substring(0, st.Length - 2).TrimEnd() + " (Hard)";
                else if (!st.EndsWith("(Hard)"))
                    st = st + " (Hard)";
            }
            return st;
        }

        static string DeathSub(BattleSim battle, BattleEvent ev)
        {
            var name = UnitName(battle, ev);
            if (!string.IsNullOrEmpty(name)) return name;
            return ev != null && ev.TargetAlly ? "ALLY" : "ENEMY";
        }

        static string UnitName(BattleSim battle, BattleEvent ev)
        {
            if (battle == null || ev == null) return "";
            if (ev.TargetAlly)
            {
                if (battle.Allies != null && ev.TargetSlot >= 0 && ev.TargetSlot < battle.Allies.Length)
                {
                    var u = battle.Allies[ev.TargetSlot];
                    if (u != null && u.Def != null) return u.Def.Name ?? "";
                }
                return "";
            }
            if (battle.Enemies != null && ev.TargetSlot >= 0 && ev.TargetSlot < battle.Enemies.Count)
            {
                var u = battle.Enemies[ev.TargetSlot];
                if (u != null && u.Def != null) return u.Def.Name ?? "";
            }
            return "";
        }

        static WaveCueBoard FindBoard(Transform parent)
        {
            if (parent == null) return null;
            var owned = parent.GetComponent<WaveCueBoard>();
            if (owned != null) return owned;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child == null) continue;
                if (child.name == CueRootName)
                    return child.GetComponent<WaveCueBoard>();
            }
            return parent.GetComponentInChildren<WaveCueBoard>(true);
        }

        static void CornerBolts(Transform panel)
        {
            if (panel == null) return;
            OverlayDraw.Pic(panel, null, "bolt", new Vector2(0.030f, 0.930f), new Vector2(18f, 18f),
                VisualTokens.GoldMetal, UiSprites.Hex());
            OverlayDraw.Pic(panel, null, "bolt", new Vector2(0.970f, 0.930f), new Vector2(18f, 18f),
                VisualTokens.GoldMetal, UiSprites.Hex());
            OverlayDraw.Pic(panel, null, "bolt", new Vector2(0.030f, 0.070f), new Vector2(18f, 18f),
                VisualTokens.GoldMetal, UiSprites.Hex());
            OverlayDraw.Pic(panel, null, "bolt", new Vector2(0.970f, 0.070f), new Vector2(18f, 18f),
                VisualTokens.GoldMetal, UiSprites.Hex());
        }

        static void Chip(Transform parent, Vector2 anchor, CharacterDef def)
        {
            if (parent == null) return;
            var size = new Vector2(144f, 188f);
            var rim = VisualTokens.SlotRim;
            if (def != null)
                rim = def.IsBoss ? VisualTokens.GoldSelect : VisualTokens.Element(def.Element);
            var go = OverlayDraw.Pic(parent, null, def != null ? def.Id : "empty",
                anchor, size, rim, UiSprites.Round());
            if (go != null) go.raycastTarget = false;
            var host = go != null ? go.transform : parent;
            var well = OverlayDraw.Pic(host, null, "well", new Vector2(0.5f, 0.5f),
                size - new Vector2(10f, 10f), VisualTokens.SlotWell, UiSprites.Round());
            if (well != null) well.raycastTarget = false;

            if (def == null)
            {
                var ghost = VisualTokens.SlotRim;
                ghost.a = 0.55f;
                var mark = OverlayDraw.Pic(host, null, "ghost", new Vector2(0.5f, 0.58f),
                    new Vector2(44f, 44f), ghost, UiSprites.Hex());
                if (mark != null) mark.raycastTarget = false;
                return;
            }

            PixelBody(host, def, new Vector2(0.5f, 0.56f), new Vector2(124f, 160f));

            var nbar = OverlayDraw.Pic(host, null, "nbar", new Vector2(0.5f, 0.10f),
                new Vector2(size.x - 12f, 28f), new Color(0f, 0f, 0f, 0.72f), UiSprites.Round());
            if (nbar != null) nbar.raycastTarget = false;
            OverlayDraw.Label(host, null, def.Name ?? "", 14, VisualTokens.TextPrimary,
                new Vector2(0.5f, 0.10f), new Vector2(132f, 26f), false, true, 2f, true);

            var el = OverlayDraw.Pic(host, null, "el", new Vector2(0.16f, 0.88f),
                new Vector2(22f, 22f), VisualTokens.Element(def.Element), UiSprites.Circle());
            if (el != null) el.raycastTarget = false;
            var roleRim = OverlayDraw.Pic(host, null, "roleRim", new Vector2(0.84f, 0.88f),
                new Vector2(26f, 26f), VisualTokens.Element(def.Element), UiSprites.Circle());
            if (roleRim != null) roleRim.raycastTarget = false;
            var role = OverlayDraw.Pic(host, null, "role", new Vector2(0.84f, 0.88f),
                new Vector2(22f, 22f), VisualTokens.RoleDisc, UiSprites.Circle());
            if (role != null) role.raycastTarget = false;
            OverlayDraw.Label(host, null, CharacterPresenter.RoleMark(def.Role), 12, VisualTokens.TextPrimary,
                new Vector2(0.84f, 0.88f), new Vector2(26f, 24f), false, true, 2f, true);

            if (!def.IsBoss) return;
            var bossSlash = OverlayDraw.Pic(host, null, "bossSlash", new Vector2(0.5f, 0.918f), new Vector2(100f, 24f),
                new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.38f),
                UiSprites.Slash());
            if (bossSlash != null) bossSlash.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);
            OverlayDraw.Label(host, null, "BOSS", 12, VisualTokens.GoldSelect,
                new Vector2(0.5f, 0.92f), new Vector2(72f, 22f), false, true, 2f, true);
        }

        static void PixelBody(Transform parent, CharacterDef def, Vector2 anchor, Vector2 size)
        {
            if (parent == null || def == null) return;
            var go = new GameObject("pixel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            var spr = CharacterArt.Face(def.Id);
            if (spr == null) spr = CharacterArt.Body(def.Id);
            img.sprite = spr != null ? spr : PixelStandIn.Get(def, "", 0);
            img.color = Color.white;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        static CharacterDef TryChar(string id)
        {
            if (string.IsNullOrEmpty(id) || Catalog.Characters == null) return null;
            CharacterDef def;
            return Catalog.Characters.TryGetValue(id, out def) ? def : null;
        }

        static void GoldWire(Transform panel, Vector2 size)
        {
            if (panel == null) return;
            var img = OverlayDraw.Pic(panel, null, "wire", new Vector2(0.5f, 0.5f), size,
                new Color(0.22f, 0.18f, 0.16f, 1f), UiSprites.FloorMosaic());
            if (img == null) return;
            img.raycastTarget = false;
            var wash = OverlayDraw.Pic(panel, null, "wash", new Vector2(0.5f, 0.5f), size,
                new Color(0.05f, 0.04f, 0.04f, 0.42f), UiSprites.Round());
            if (wash != null) wash.raycastTarget = false;
            var ol = img.gameObject.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(1f, -1f);
            var wire = OverlayDraw.Pic(panel, null, "wireIn", new Vector2(0.5f, 0.5f), size - new Vector2(14f, 14f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.85f),
                UiSprites.WireFrame());
            if (wire != null) wire.raycastTarget = false;
        }
    }

    /// <summary>
    /// Transient 换波 / 倒下 / 击破 stamp. Engineering layout only.
    /// </summary>
    public sealed class WaveCueBoard : MonoBehaviour
    {
        // PhaseLifeSec: P0 30fps PHASE 2 first-on to first-off. AllyDown unmeasured.
        // Not T27 / T28. Not Ragna phase.
        const float CueY = 0.58f;
        const float LifeSec = 1.20f;
        const float PhaseLifeSec = 2.00f;
        const float SlamSec = 0.14f;

        int _probeId;
        CanvasGroup _group;
        Image _tone;
        Image _slash;
        Image _slashIn;
        Image _stampL;
        Image _stampR;
        Image _crest;
        Text _title;
        Text _sub;
        BattleSim _battle;
        int _cursor;
        float _age;
        float _life;
        WaveCueKind _kind;
        float _tilt;
        WaveCue _held;

        public WaveCueKind Kind => _kind;
        public string Title => _title != null ? _title.text : "";
        public bool Showing => _life > 0f && _group != null && _group.alpha > 0.01f;

        public void SuppressDeathForExclusive()
        {
            if (IsDeath(_kind) && _life > 0f)
                Hide();
            if (IsDeath(_held.Kind))
                _held = default;
        }

        public void Build()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            _tone = OverlayDraw.Pic(transform, null, "cueTone", new Vector2(0.5f, CueY),
                new Vector2(720f, 160f), Color.clear, UiSprites.Halftone());
            if (_tone != null)
            {
                _tone.type = Image.Type.Tiled;
                _tone.pixelsPerUnitMultiplier = 0.55f;
                _tone.raycastTarget = false;
            }

            _slash = OverlayDraw.Pic(transform, null, "cueSlash", new Vector2(0.5f, CueY),
                new Vector2(860f, 110f), Color.clear, UiSprites.Slash());
            if (_slash != null) _slash.raycastTarget = false;

            _slashIn = OverlayDraw.Pic(transform, null, "cueSlashIn", new Vector2(0.5f, CueY),
                new Vector2(720f, 42f), Color.clear, UiSprites.Slash());
            if (_slashIn != null) _slashIn.raycastTarget = false;

            _stampL = OverlayDraw.Pic(transform, null, "cueStampL", new Vector2(0.30f, CueY),
                new Vector2(28f, 28f), Color.clear, UiSprites.Stamp(2));
            if (_stampL != null) _stampL.raycastTarget = false;
            _stampR = OverlayDraw.Pic(transform, null, "cueStampR", new Vector2(0.70f, CueY),
                new Vector2(28f, 28f), Color.clear, UiSprites.Stamp(2));
            if (_stampR != null) _stampR.raycastTarget = false;
            // Primary P0 t65: large heart crest above PHASE splash.
            _crest = OverlayDraw.Pic(transform, null, "cueCrest", new Vector2(0.5f, CueY + 0.10f),
                new Vector2(72f, 72f), Color.clear, UiSprites.Heart());
            if (_crest != null) _crest.raycastTarget = false;

            _title = OverlayDraw.Label(transform, null, "", 52, VisualTokens.TextPrimary,
                new Vector2(0.5f, CueY), new Vector2(520f, 72f), false, true, 3.5f, true);
            if (_title != null)
            {
                _title.gameObject.name = "换波死亡题";
                _title.raycastTarget = false;
            }
            _sub = OverlayDraw.Label(transform, null, "", 22, VisualTokens.TextSecondary,
                new Vector2(0.5f, CueY - 0.055f), new Vector2(480f, 36f), false, true, 2f, false);
            if (_sub != null)
            {
                _sub.gameObject.name = "换波死亡副";
                _sub.raycastTarget = false;
            }
            Hide();
        }

        public void Bind(BattleSim battle)
        {
            if (!ReferenceEquals(_battle, battle))
            {
                _battle = battle;
                _cursor = 0;
            }
            transform.SetAsLastSibling();
            Drain();
        }

        void Update()
        {
            // DriveSelect / SHOWTIME / QTE already queues new 击破/倒下.
            // Also kill a stamp that started before the plate opened (06d residual).
            if (_life > 0f && IsDeath(_kind) && ExclusiveBusy())
            {
                Hide();
                return;
            }
            if (_life <= 0f && _held.Visible && !ExclusiveBusy())
            {
                var held = _held;
                _held = default;
                Play(held);
            }
            if (_life <= 0f) return;
            _age += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(_age / _life);
            var a = u < 0.10f ? u / 0.10f
                : u < 0.72f ? 1f
                : 1f - (u - 0.72f) / 0.28f;
            a = Mathf.Clamp01(a);
            if (_group != null) _group.alpha = a;

            var slam = 1f - (1f - Mathf.Clamp01(_age / SlamSec)) * (1f - Mathf.Clamp01(_age / SlamSec));
            var scale = Mathf.Lerp(1.28f, 1f, slam);
            if (_slash != null) _slash.rectTransform.localScale = new Vector3(scale, 1.05f, 1f);
            if (_title != null) _title.rectTransform.localScale = Vector3.one * scale;

            if (_age >= _life) Hide();
        }

        void Drain()
        {
            if (_battle == null || _battle.Events == null) return;
            var list = _battle.Events.Events;
            if (list == null) return;
            if (_cursor > list.Count) _cursor = 0;

            var hasWave = false;
            var hasAlly = false;
            var hasEnemy = false;
            var wave = default(WaveCue);
            var ally = default(WaveCue);
            var enemy = default(WaveCue);

            while (_cursor < list.Count)
            {
                var ev = list[_cursor++];
                WaveCue cue;
                if (!WavePreview.TryRead(ev, _battle, out cue) || !cue.Visible) continue;
                if (cue.Kind == WaveCueKind.WaveAdvance)
                {
                    wave = cue;
                    hasWave = true;
                }
                else if (cue.Kind == WaveCueKind.AllyDown)
                {
                    ally = cue;
                    hasAlly = true;
                }
                else if (cue.Kind == WaveCueKind.EnemyDown)
                {
                    enemy = cue;
                    hasEnemy = true;
                }
            }

            if (hasWave) Play(wave);
            else if (hasAlly) MaybePlayDeath(ally);
            else if (hasEnemy) MaybePlayDeath(enemy);
        }

        static bool ExclusiveBusy()
        {
            if (VfxShowtime.AnyLive() || VfxJudge.AnyLive()) return true;
            var g = GameRoot.Live;
            return g != null && g.DriveSelectVisible;
        }

        static bool IsDeath(WaveCueKind kind)
        {
            return kind == WaveCueKind.AllyDown || kind == WaveCueKind.EnemyDown;
        }

        void MaybePlayDeath(WaveCue cue)
        {
            if (!cue.Visible) return;
            if (ExclusiveBusy())
            {
                _held = cue;
                return;
            }
            Play(cue);
        }

        void Play(WaveCue cue)
        {
            if (!cue.Visible) return;
            _kind = cue.Kind;
            _life = cue.Kind == WaveCueKind.WaveAdvance ? PhaseLifeSec : LifeSec;
            if (cue.Kind == WaveCueKind.WaveAdvance && _battle != null && _battle.HoldLeftSec > 0.01f)
                _life = _battle.HoldLeftSec;
            _age = 0f;
            _tilt = cue.Kind == WaveCueKind.AllyDown ? 12f
                : cue.Kind == WaveCueKind.EnemyDown ? -4f
                : -8f;

            if (cue.Kind == WaveCueKind.WaveAdvance || cue.Kind == WaveCueKind.WaveEnter)
                VfxFeverOverlay.Hide();

            WavePreview.VisibleKind = cue.Kind;
            WavePreview.VisibleTitle = cue.Title;

            if (_title != null)
            {
                _title.text = cue.Title;
                _title.color = VisualTokens.TextPrimary;
                // PHASE splash: hero word centered; stage name rides above (P0 t65).
                var titleY = CueY;
                _title.rectTransform.anchorMin = _title.rectTransform.anchorMax = new Vector2(0.5f, titleY);
            }
            if (_sub != null)
            {
                _sub.text = cue.Sub ?? "";
                _sub.color = cue.Kind == WaveCueKind.WaveAdvance ? VisualTokens.FeverGold : cue.Tint;
                var subY = cue.Kind == WaveCueKind.WaveAdvance ? CueY + 0.07f : CueY - 0.055f;
                _sub.rectTransform.anchorMin = _sub.rectTransform.anchorMax = new Vector2(0.5f, subY);
            }

            var advance = cue.Kind == WaveCueKind.WaveAdvance;
            // P0 t64–t66: empty red field + heart + PHASE n + stage name. No slash plate.
            Paint(_slash, cue.Tint, advance ? 0f : 0.92f);
            Paint(_slashIn, Color.white, advance ? 0f : 0.55f);
            Paint(_tone, cue.Tint, advance ? 0f : 0.10f);

            var wave = cue.Kind == WaveCueKind.WaveAdvance || cue.Kind == WaveCueKind.WaveEnter;
            Paint(_stampL, cue.Tint, !advance && wave ? 0.95f : 0f);
            Paint(_stampR, cue.Tint, !advance && wave ? 0.95f : 0f);
            // Heart crest on PHASE advance only (P0 t65). Large; not T27.
            if (_crest != null)
            {
                var sz = advance ? 260f : 72f;
                _crest.rectTransform.sizeDelta = new Vector2(sz, sz);
                var crestY = advance ? CueY + 0.04f : CueY + 0.10f;
                _crest.rectTransform.anchorMin = _crest.rectTransform.anchorMax = new Vector2(0.5f, crestY);
            }
            Paint(_crest, VisualTokens.FeverGold, advance ? 1f : 0f);

            Tilt(_slash, _tilt);
            Tilt(_slashIn, _tilt);
            if (_title != null) _title.rectTransform.localEulerAngles = new Vector3(0f, 0f, _tilt * 0.35f);

            if (_group != null) _group.alpha = 0f;
            gameObject.SetActive(true);
            if (cue.Kind == WaveCueKind.WaveAdvance)
            {
                // Overlay only. Core owns the PHASE sim hold from LoadWave (C2).
                _probeId++;
                CueTimingProbe.On("phase", _probeId);
            }
        }

        void Hide()
        {
            if (_kind == WaveCueKind.WaveAdvance && _probeId != 0)
                CueTimingProbe.Off("phase", _probeId);
            _life = 0f;
            _age = 0f;
            _kind = WaveCueKind.None;
            WavePreview.VisibleKind = WaveCueKind.None;
            WavePreview.VisibleTitle = "";
            if (_group != null) _group.alpha = 0f;
            if (_title != null) _title.text = "";
            if (_sub != null) _sub.text = "";
        }

        static void Paint(Image img, Color tint, float a)
        {
            if (img == null) return;
            img.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(a));
            img.enabled = a > 0.01f;
        }

        static void Tilt(Image img, float deg)
        {
            if (img == null) return;
            img.rectTransform.localEulerAngles = new Vector3(0f, 0f, deg);
        }
    }

    [DefaultExecutionOrder(1100)]
    sealed class WaveCueHost : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<WaveCueHost>() != null) return;
            var go = new GameObject("WavePreviewCueHost");
            DontDestroyOnLoad(go);
            go.AddComponent<WaveCueHost>();
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            var g = GameRoot.Live;
            if (g == null || g.Battle == null || g.CurrentScreen != "Battle")
                return;
            var canvas = g.GetComponentInChildren<Canvas>();
            if (canvas == null) return;
            WavePreview.Observe(canvas.transform, g.Battle);
        }
    }
}
