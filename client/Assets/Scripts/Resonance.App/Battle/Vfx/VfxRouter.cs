using Resonance.Battle;
using UnityEngine;

namespace Resonance.App
{
    /// <summary>
    /// Event dispatch for stacked VFX. Cast/Hit/Fever/Judge follow the combat log.
    /// Overlays never wait to apply damage.
    /// </summary>
    public static class VfxRouter
    {
        static int _combo;
        static int _comboDmg;

        public static void PlayCue(PresentationCue cue, Transform parent, BattleFighter caster, BattleFighter target)
        {
            if (parent == null) return;
            if (cue.Kind == PresentationCueKind.Hit)
            {
                var from = caster != null ? caster.Anchor : cue.From;
                var to = target != null ? target.Anchor : cue.To;
                OnHit(parent, from, to, cue.Channel, cue.Elem, cue.Amount, cue.Crit, cue.Heal, cue.Fever);
            }
            else if (cue.Kind == PresentationCueKind.Cast)
            {
                var fx = new CastFx
                {
                    CasterSlot = cue.CasterSlot,
                    CasterAlly = cue.CasterAlly,
                    Type = cue.Channel,
                    Name = cue.Label,
                    Fever = cue.Fever
                };
                OnCast(parent, fx, caster != null ? caster.Anchor : cue.From, cue.Elem);
            }
            else if (cue.Kind == PresentationCueKind.Death && target != null)
                OnKo(target.transform, cue.Elem);
            else if (cue.Kind == PresentationCueKind.Buff)
                OnBuff(parent, target != null ? target.Anchor : cue.To, cue.Label);
            else if (cue.Kind == PresentationCueKind.Fever)
                OnFeverBanner(parent);
            else if (cue.Kind == PresentationCueKind.Judge)
                VfxJudge.Play(parent, cue.Label, cue.Amount > 0 ? cue.Amount / 100f : 1f, cue.Fever ? 100f : 0f);
        }

        public static void OnHit(Transform parent, Vector2 from, Vector2 to, SkillType kind,
            Element elem, int amount, bool crit, bool heal, bool fever)
        {
            if (parent == null) return;
            var color = VisualTokens.Element(elem);
            VfxHitFlash.Play(parent, to, color, fever);
            if (heal)
            {
                VfxHeal.Play(parent, to, amount);
                VfxRegen.Play(parent, to);
                VfxBuffFloat.Play(parent, to, VfxBuffFloat.Regen, VfxBuffFloat.ColorOf(VfxBuffFloat.Regen));
            }
            else
            {
                var weak = fever && crit;
                VfxDamagePopup.Spawn(parent, to, Mathf.Max(0, amount), weak, crit, elem);
                VfxKnockback.Play(parent, from, to, color, kind == SkillType.Slide || kind == SkillType.Drive);
                if (weak) VfxWeakPoint.Play(parent, to, amount);
                else if (crit) VfxCritical.Play(parent, to, amount);
            }
            if (!fever) PlayElem(parent, to, elem, false);
            if (kind == SkillType.Tap || kind == SkillType.Auto)
            {
                if (!fever)
                {
                    VfxProjectile.Play(parent, from, to, color, null);
                    VfxTapSkill.Play(parent, from, to, color);
                }
            }
            else if (kind == SkillType.Slide)
            {
                VfxAfterimage.Play(parent, from, to, color, 4);
                VfxSlideSlash.Play(parent, from, to, color, fever);
            }
            else if (kind == SkillType.Drive)
            {
                VfxAfterimage.Play(parent, from, to, color, 5);
                VfxDriveCrush.Play(parent, to, color, crit);
            }
            if (fever && !heal && amount > 0)
            {
                _combo++;
                _comboDmg += amount;
                VfxComboBanner.Show(parent, _combo, _comboDmg);
            }
        }

        public static void EndFever(Transform parent)
        {
            _combo = 0;
            _comboDmg = 0;
            if (parent == null) return;
            var live = parent.GetComponentInChildren<VfxComboBanner>(true);
            if (live != null) live.Hide();
        }

        public static void OnKo(Transform fighter, Element elem)
        {
            if (fighter == null) return;
            VfxKo.Play(fighter, VisualTokens.Element(elem));
        }

        public static void OnBuff(Transform parent, Vector2 anchor, string label)
        {
            if (parent == null || string.IsNullOrEmpty(label)) return;
            VfxBuffFloat.Play(parent, anchor, label, VfxBuffFloat.ColorOf(label));
            if (Has(label, "增益") || Has(label, "已标记")) return;
            if (label == VfxBuffFloat.Regen) VfxRegen.Play(parent, anchor);
            else if (Has(label, "沉默")) VfxSilence.Play(parent, anchor);
            else if (Has(label, "无敌")) VfxInvuln.Play(parent, anchor);
            else if (Has(label, "嘲讽")) VfxTaunt.Play(parent, anchor);
            else if (Has(label, "睡眠")) VfxSleep.Play(parent, anchor);
            else if (Has(label, "净化")) VfxCleanse.Play(parent, anchor);
            else if (Has(label, "毒")) VfxPoison.Play(parent, anchor);
            else if (Has(label, "流血")) VfxBleed.Play(parent, anchor);
            else if (Has(label, "眩晕")) VfxStun.Play(parent, anchor);
            else if (Has(label, "格挡") || Has(label, "护盾")) VfxShield.Play(parent, anchor, VisualTokens.GoldMetal);
            else if (Has(label, "反击")) VfxCounter.Play(parent, anchor, anchor, VisualTokens.StarEvolved);
        }

        static bool Has(string label, string word)
        {
            return label != null && word != null && label.IndexOf(word, System.StringComparison.Ordinal) >= 0;
        }

        public static void OnCast(Transform parent, CastFx fx, Vector2 caster, Element elem)
        {
            if (parent == null || fx == null) return;
            if (IsFeverCast(fx))
            {
                OnFeverBanner(parent);
                return;
            }

            var color = VisualTokens.Element(elem);
            if (fx.Fever) color = Color.Lerp(color, VisualTokens.FeverGold, 0.62f);
            else if (fx.Type == SkillType.Drive) color = Color.Lerp(color, VisualTokens.DriveOrange, 0.55f);
            else if (fx.Type == SkillType.Slide) color = Color.Lerp(color, VisualTokens.StarEvolved, 0.32f);
            else if (fx.Type == SkillType.Leader) color = Color.Lerp(color, VisualTokens.GoldTitle, 0.40f);
            else color = Color.Lerp(color, VisualTokens.TapWhite, 0.28f);

            if (fx.Type != SkillType.Auto)
                VfxSkillCast.Play(parent, caster, color, fx.Type, fx.Fever);

            // Screen identity follows the cast event. Damage is already in the log;
            // these overlays never gate Resolve / opcode execution.
            if (fx.Type == SkillType.Slide && fx.CasterAlly && !VfxFeverOverlay.Active)
                VfxShowtime.Play(parent, null, fx.Name);
            else if (fx.Type == SkillType.Drive && fx.CasterAlly && !VfxFeverOverlay.Active)
            {
                var pix = PixelCombatFx.Ensure(parent);
                if (pix != null) pix.PlayDrive(null, fx.Name, 0.70f, null);
            }
            else if (fx.Type == SkillType.Drive && !fx.CasterAlly)
                VfxWarning.Play(parent, fx.Name);
            if (fx.Type == SkillType.Leader)
                VfxLeaderBurst.Play(parent, fx.Name);
        }

        public static void OnFeverBanner(Transform parent)
        {
            if (parent == null) return;
            VfxShowtime.KillAll();
            VfxJudge.KillAll();
            VfxFeverOverlay.Show(parent, 7f);
            CombatFeel.FeverRipple(parent, new Vector2(0.5f, 0.56f));
        }

        static bool IsFeverCast(CastFx fx)
        {
            if (fx == null) return false;
            if (fx.Type == SkillType.Fever) return true;
            return string.Equals(fx.Name, "FEVER", System.StringComparison.Ordinal);
        }

        static void PlayElem(Transform parent, Vector2 to, Element elem, bool fever)
        {
            switch (elem)
            {
                case Element.Fire: VfxElemFire.Play(parent, to, fever); break;
                case Element.Water: VfxElemWater.Play(parent, to, fever); break;
                case Element.Wood: VfxElemWood.Play(parent, to, fever); break;
                case Element.Light: VfxElemLight.Play(parent, to, fever); break;
                case Element.Dark: VfxElemDark.Play(parent, to, fever); break;
            }
        }
    }
}
