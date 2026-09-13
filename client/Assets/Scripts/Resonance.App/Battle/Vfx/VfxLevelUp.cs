using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Account / character level-up cue.
    /// GT P0 CLEAR aftermath (t470): crest + <c>LEVEL UP!</c>, orange <c>Level up!</c>,
    /// large <c>Level n ▶ m</c>, stamina refill / max+ lines, yellow Confirm.
    /// Character debug path (onLevel) keeps a shorter chip; CLEAR uses the modal stub
    /// with — placeholders until account EXP / stamina formulas land.
    /// </summary>
    public static class VfxLevelUp
    {
        public static void Play(Transform parent, int level)
        {
            if (parent == null) return;
            var from = Mathf.Max(1, level - 1);
            Spawn(parent, from.ToString(), level.ToString(), modal: false);
        }

        /// <summary>
        /// P0 t396 is CLEAR; t470 is LEVEL UP. Do not stack the modal on the
        /// same frame as Reveal. Delay is engineering so CLEAR is readable —
        /// not the tutorial ~74s gap, not T28.
        /// </summary>
        public const float ClearHoldSec = 2.50f;

        public static bool AnyLive()
        {
            var live = Object.FindObjectsByType<VfxLevelUpMark>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return live != null && live.Length > 0;
        }

        public static void SkipLive()
        {
            var live = Object.FindObjectsByType<VfxLevelUpMark>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (live != null)
            {
                for (int i = 0; i < live.Length; i++)
                {
                    if (live[i] != null) Object.Destroy(live[i].gameObject);
                }
            }
            var delays = Object.FindObjectsByType<VfxLevelUpDelay>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (delays == null) return;
            for (int i = 0; i < delays.Length; i++)
            {
                if (delays[i] != null) Object.Destroy(delays[i]);
            }
        }

        /// <summary>CLEAR aftermath account modal — stubs until EXP/stamina formulas known.</summary>
        public static void PlayClearModal(Transform parent)
            => Spawn(parent, BattleCueCopy.LevelUpFromStub, BattleCueCopy.LevelUpToStub, modal: true);

        public static void PlayClearModalDelayed(Transform parent, float delaySec)
        {
            if (parent == null) return;
            var host = parent.GetComponent<VfxLevelUpDelay>();
            if (host == null) host = parent.gameObject.AddComponent<VfxLevelUpDelay>();
            host.Arm(parent, delaySec);
        }

        static void Spawn(Transform parent, string fromLv, string toLv, bool modal)
        {
            var go = new GameObject("VfxLevelUp");
            go.transform.SetParent(parent, false);
            go.AddComponent<VfxLevelUpMark>();
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (modal)
            {
                var dim = go.AddComponent<Image>();
                dim.color = new Color(0f, 0f, 0f, 0.62f);
                dim.raycastTarget = false;
            }

            var card = new GameObject("Card");
            card.transform.SetParent(go.transform, false);
            var crt = card.AddComponent<RectTransform>();
            if (modal)
            {
                crt.anchorMin = new Vector2(0.5f, 0.52f);
                crt.anchorMax = new Vector2(0.5f, 0.52f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.sizeDelta = new Vector2(460f, 340f);
            }
            else
            {
                crt.anchorMin = new Vector2(0.5f, 0.62f);
                crt.anchorMax = new Vector2(0.5f, 0.62f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.sizeDelta = new Vector2(280f, 96f);
            }
            var bg = card.AddComponent<Image>();
            bg.color = modal
                ? new Color(0.08f, 0.07f, 0.10f, 0.94f)
                : new Color(0.12f, 0.10f, 0.18f, 0.92f);
            bg.raycastTarget = false;

            var edge = new GameObject("Edge");
            edge.transform.SetParent(card.transform, false);
            var ert = edge.AddComponent<RectTransform>();
            ert.anchorMin = Vector2.zero;
            ert.anchorMax = Vector2.one;
            ert.offsetMin = new Vector2(2f, 2f);
            ert.offsetMax = new Vector2(-2f, -2f);
            var eimg = edge.AddComponent<Image>();
            eimg.color = new Color(1f, 0.72f, 0.22f, modal ? 0.70f : 0.35f);
            eimg.raycastTarget = false;

            if (modal)
            {
                // Crest stub (P0 t470 wings/star/laurel → gold star pip).
                var crest = new GameObject("Crest");
                crest.transform.SetParent(card.transform, false);
                var crestRt = crest.AddComponent<RectTransform>();
                crestRt.anchorMin = crestRt.anchorMax = new Vector2(0.5f, 0.92f);
                crestRt.pivot = new Vector2(0.5f, 0.5f);
                crestRt.sizeDelta = new Vector2(72f, 72f);
                var crestImg = crest.AddComponent<Image>();
                crestImg.sprite = UiSprites.Star();
                crestImg.color = new Color(1f, 0.86f, 0.30f, 1f);
                crestImg.raycastTarget = false;
            }

            var stamp = MkText(card.transform, "Stamp", BattleCueCopy.LevelUpEn,
                modal ? 40 : 22, FontStyle.Bold, TextAnchor.UpperCenter,
                new Color(1f, 0.86f, 0.28f, 1f));
            var srt = stamp.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.06f, modal ? 0.74f : 0.52f);
            srt.anchorMax = new Vector2(0.94f, modal ? 0.90f : 0.96f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            if (modal)
            {
                var sub = MkText(card.transform, "SubEn", BattleCueCopy.LevelUpSubEn,
                    18, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Color(1f, 0.55f, 0.18f, 1f));
                var subRt = sub.GetComponent<RectTransform>();
                subRt.anchorMin = new Vector2(0.1f, 0.66f);
                subRt.anchorMax = new Vector2(0.9f, 0.76f);
                subRt.offsetMin = Vector2.zero;
                subRt.offsetMax = Vector2.zero;
            }
            else
            {
                var cn = MkText(card.transform, "Cn", BattleCueCopy.LevelUp,
                    18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                var crt2 = cn.GetComponent<RectTransform>();
                crt2.anchorMin = new Vector2(0.06f, 0.08f);
                crt2.anchorMax = new Vector2(0.94f, 0.52f);
                crt2.offsetMin = Vector2.zero;
                crt2.offsetMax = Vector2.zero;
            }

            // P0 t470: single line "Level n ▶ m" (not bare n > m under separate Level caps).
            var arrow = MkText(card.transform, "Arrow",
                modal
                    ? BattleCueCopy.LevelUpArrowPrefixed(fromLv, toLv)
                    : BattleCueCopy.LevelUpArrow(fromLv, toLv),
                modal ? 36 : 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.92f, 0.35f, 1f));
            var art = arrow.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.08f, modal ? 0.42f : 0.02f);
            art.anchorMax = new Vector2(0.92f, modal ? 0.62f : 0.40f);
            art.offsetMin = Vector2.zero;
            art.offsetMax = Vector2.zero;

            if (modal)
            {
                var stam = MkText(card.transform, "StaminaRefill",
                    BattleCueCopy.LevelUpStaminaRefill,
                    15, FontStyle.Normal, TextAnchor.MiddleCenter,
                    new Color(0.92f, 0.93f, 0.96f, 0.95f));
                var str = stam.GetComponent<RectTransform>();
                str.anchorMin = new Vector2(0.08f, 0.30f);
                str.anchorMax = new Vector2(0.92f, 0.42f);
                str.offsetMin = Vector2.zero;
                str.offsetMax = Vector2.zero;

                var stam2 = MkText(card.transform, "StaminaMax",
                    BattleCueCopy.LevelUpStaminaMaxUp,
                    15, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Color(1f, 0.55f, 0.18f, 1f));
                var str2 = stam2.GetComponent<RectTransform>();
                str2.anchorMin = new Vector2(0.08f, 0.18f);
                str2.anchorMax = new Vector2(0.92f, 0.30f);
                str2.offsetMin = Vector2.zero;
                str2.offsetMax = Vector2.zero;

                var btn = new GameObject("ConfirmBtn");
                btn.transform.SetParent(go.transform, false);
                var brt = btn.AddComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.22f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = new Vector2(280f, 56f);
                var bimg = btn.AddComponent<Image>();
                bimg.sprite = UiSprites.Pill();
                bimg.color = new Color(1f, 0.78f, 0.22f, 1f);
                bimg.raycastTarget = false;

                var confirm = MkText(btn.transform, "Confirm", BattleCueCopy.LevelUpConfirmEn,
                    22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
                var cfrt = confirm.GetComponent<RectTransform>();
                cfrt.anchorMin = Vector2.zero;
                cfrt.anchorMax = Vector2.one;
                cfrt.offsetMin = Vector2.zero;
                cfrt.offsetMax = Vector2.zero;
            }

            var mark = go.GetComponent<VfxLevelUpMark>();
            if (mark != null) mark.DieAt = Time.unscaledTime + (modal ? 3.0f : 1.15f);
        }

        static Text MkText(Transform parent, string name, string msg, int size, FontStyle style,
            TextAnchor align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<Text>();
            t.font = CharacterPresenter.UiFont();
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = msg ?? "";
            return t;
        }
    }

    sealed class VfxLevelUpMark : MonoBehaviour
    {
        public float DieAt;

        void Update()
        {
            if (DieAt > 0f && Time.unscaledTime >= DieAt)
                Destroy(gameObject);
        }
    }

    sealed class VfxLevelUpDelay : MonoBehaviour
    {
        Transform _parent;
        float _at;
        bool _armed;

        public void Arm(Transform parent, float delaySec)
        {
            _parent = parent;
            _at = Time.unscaledTime + Mathf.Max(0.05f, delaySec);
            _armed = true;
        }

        void Update()
        {
            if (!_armed || Time.unscaledTime < _at) return;
            _armed = false;
            VfxLevelUp.PlayClearModal(_parent != null ? _parent : transform);
            Destroy(this);
        }
    }
}
