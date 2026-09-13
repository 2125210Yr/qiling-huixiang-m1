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
                var skillName = cue.Label;
                if (string.IsNullOrEmpty(skillName) && cue.Channel == SkillType.Drive && caster != null)
                    skillName = BattleCueCopy.DriveSkillStubForName(caster.UnitName);
                OnHit(parent, from, to, cue.Channel, cue.Elem, cue.Amount, cue.Crit, cue.Heal, cue.Fever, skillName);
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
                var casterName = caster != null ? caster.UnitName : null;
                if (string.IsNullOrEmpty(fx.Name) && cue.Channel == SkillType.Drive)
                    fx.Name = BattleCueCopy.DriveSkillStubForName(casterName);
                if (string.IsNullOrEmpty(fx.Name) && cue.Channel == SkillType.Slide)
                    fx.Name = BattleCueCopy.SlideSkillStubForName(casterName);
                OnCast(parent, fx, caster != null ? caster.Anchor : cue.From, cue.Elem, casterName);
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
            Element elem, int amount, bool crit, bool heal, bool fever, string skillName = null)
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
                    // Auto keeps bead fly-in. Tap = punch only (no Slash streak projectile).
                    if (kind == SkillType.Auto)
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
                VfxDriveCrush.Play(parent, to, color, crit, skillName);
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
            if (Has(label, "增益") || Has(label, "已标记") || Has(label, "Buff")) return;
            if (label == VfxBuffFloat.Regen || Has(label, "Regen") || Has(label, "再生")) VfxRegen.Play(parent, anchor);
            else if (Has(label, "沉默") || Has(label, "Silence")) VfxSilence.Play(parent, anchor);
            else if (Has(label, "无敌") || Has(label, "Immortal")) VfxInvuln.Play(parent, anchor);
            else if (Has(label, "嘲讽") || Has(label, "Taunt")) VfxTaunt.Play(parent, anchor);
            else if (Has(label, "睡眠") || Has(label, "Sleep")) VfxSleep.Play(parent, anchor);
            else if (Has(label, "净化") || Has(label, "Cleanse")) VfxCleanse.Play(parent, anchor);
            else if (Has(label, "毒") || Has(label, "Poison") || Has(label, "DoT") || Has(label, "Burn")) VfxPoison.Play(parent, anchor);
            else if (Has(label, "流血") || Has(label, "Bleed")) VfxBleed.Play(parent, anchor);
            else if (Has(label, "眩晕") || Has(label, "晕眩") || Has(label, "Stun")) VfxStun.Play(parent, anchor);
            else if (Has(label, "格挡") || Has(label, "护盾") || Has(label, "屏障")
                || Has(label, "Barrier") || Has(label, "Shield"))
                VfxShield.Play(parent, anchor, VisualTokens.GoldMetal);
            else if (Has(label, "反击") || Has(label, "反射") || Has(label, "Reflect") || Has(label, "Counter"))
                VfxCounter.Play(parent, anchor, anchor, VisualTokens.StarEvolved);
        }

        static bool Has(string label, string word)
        {
            return label != null && word != null && label.IndexOf(word, System.StringComparison.Ordinal) >= 0;
        }

        public static void OnCast(Transform parent, CastFx fx, Vector2 caster, Element elem)
            => OnCast(parent, fx, caster, elem, null);

        public static void OnCast(Transform parent, CastFx fx, Vector2 caster, Element elem, string casterName)
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
            EmitCastIdentity(parent, fx, casterName, caster);
        }

        static void EmitCastIdentity(Transform parent, CastFx fx, string casterName, Vector2 caster)
        {
            if (fx.Type == SkillType.Slide && fx.CasterAlly && !VfxFeverOverlay.Active)
            {
                var slideName = fx.Name;
                if (string.IsNullOrEmpty(slideName))
                    slideName = BattleCueCopy.SlideSkillStubForName(casterName);
                var rank = BattleCueCopy.SlideRankFromContent(fx.SlideRank, fx.SlideSkillLv, fx.SlideSkillLvMax);
                VfxShowtime.Play(parent, null, slideName, rank);
            }
            else if (fx.Type == SkillType.Drive && fx.CasterAlly && !VfxFeverOverlay.Active)
            {
                var driveName = fx.Name;
                if (string.IsNullOrEmpty(driveName))
                    driveName = BattleCueCopy.DriveSkillStubForName(casterName);
                var pix = PixelCombatFx.Ensure(parent);
                if (pix != null) pix.PlayDrive(null, driveName, 0.70f, null);
            }
            else if (fx.Type == SkillType.Drive && !fx.CasterAlly)
                VfxWarning.Play(parent, fx.Name);
            else if ((fx.Type == SkillType.Auto || fx.Type == SkillType.Tap) && fx.CasterAlly)
            {
                var autoName = fx.Name;
                if (string.IsNullOrEmpty(autoName))
                    autoName = BattleCueCopy.AutoSkillStubForName(casterName);
                if (fx.Type == SkillType.Auto)
                {
                    if (string.IsNullOrEmpty(autoName))
                    {
                        // Hard r36: AUTO SKILL when skill stub unknown.
                        CombatFeel.NamePopStack(parent, caster + new Vector2(0f, 0.12f),
                            BattleCueCopy.AutoSkillPortraitEn, VisualTokens.YellowConfirm);
                    }
                    else
                    {
                        // P0 t500: Auto chip + skill name (Eclipse / Dark Water / …).
                        CombatFeel.NamePopStack(parent, caster + new Vector2(0f, 0.13f),
                            BattleCueCopy.AutoCastBadge, VisualTokens.YellowConfirm);
                        CombatFeel.NamePopStack(parent, caster + new Vector2(0f, 0.10f), autoName, Color.white);
                    }
                }
                else if (!string.IsNullOrEmpty(autoName))
                    CombatFeel.NamePopStack(parent, caster + new Vector2(0f, 0.10f), autoName, Color.white);
            }
            // P0 t60 ordinary fight: no LEADER field splash. VfxLeaderBurst was invented.
        }

        public static void OnFeverBanner(Transform parent)
        {
            if (parent == null) return;
            VfxShowtime.KillAll();
            if (VfxJudge.AnyLive())
                return;
            VfxJudge.KillAll();
            VfxFeverOverlay.Show(parent, BattleSim.UnknownFeverWindowSec);
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
