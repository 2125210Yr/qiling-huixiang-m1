using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public static class RunBattleFactory
    {
        public static int[] GetPartyMaxHp(string presetId) => ExpeditionContent.GetPartyMaxHp(presetId);

        public static ExpeditionBattleInput CreateInput(string nodeId, string presetId, int seed,
            string[] relicIds, int[] openingHp)
        {
            ExpeditionContent.ValidatePreset(presetId);
            var maxHp = ExpeditionContent.GetPartyMaxHp(presetId);
            if (openingHp != null && openingHp.Length != maxHp.Length)
                throw new ArgumentException("Opening HP must contain exactly five slots.", nameof(openingHp));
            var hp = openingHp == null ? maxHp : (int[])openingHp.Clone();
            for (int i = 0; i < hp.Length; i++)
                if (hp[i] < 0 || hp[i] > maxHp[i]) throw new ArgumentException("Opening HP outside frozen character range at slot " + i, nameof(openingHp));
            var party = ExpeditionContent.PartyIds;
            var growth = new UnitProgress[party.Length];
            for (int i = 0; i < growth.Length; i++) growth[i] = new UnitProgress { Id = party[i] };
            var input = new ExpeditionBattleInput { ContentVersion = ExpeditionContent.Version,
                ContentHash = ExpeditionContent.ContentHash, RulesetId = ExpeditionContent.RulesetId,
                RunId = "", EncounterId = "", AttemptId = "", Seed = seed, PresetId = presetId,
                Stage = ExpeditionContent.CreateStage(nodeId), Characters = ExpeditionContent.CreateCharacters(),
                Skills = ExpeditionContent.CreateSkills(presetId), Effects = ExpeditionContent.CreateEffects(),
                PartyIds = party, OpeningHp = hp, Growth = growth, Mods = new BattleMods(),
                Clocks = ExpeditionContent.CreateClocks(), Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                RelicIds = relicIds == null ? new string[0] : (string[])relicIds.Clone(),
                RelicParameters = new ExpeditionRelicParameters(), IsBoss = nodeId == "N7",
                Boss = nodeId == "N7" ? new BossEncounterDef() : null,
                Elite = nodeId == "N4" ? new EliteEncounterDef() : null };
            Validate(input);
            return input;
        }

        public static BattleSim Create(ExpeditionBattleInput input)
        {
            Validate(input);
            return new BattleSim(input);
        }

        public static void Validate(ExpeditionBattleInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.SchemaVersion != 1 || input.ModeId != "original-expedition"
                || input.ContentVersion != ExpeditionContent.Version || input.ContentHash != ExpeditionContent.ContentHash
                || input.RulesetId != ExpeditionContent.RulesetId)
                throw new ArgumentException("Unsupported original-expedition content or rules version.", nameof(input));
            ExpeditionContent.ValidatePreset(input.PresetId);
            if (input.Stage == null || input.Characters == null || input.Skills == null || input.Effects == null
                || input.Mods == null || input.Clocks == null || input.RelicParameters == null)
                throw new ArgumentException("Incomplete original-expedition frozen input.", nameof(input));
            ExpeditionContent.CreateStage(input.Stage.Id); // reject unrelated stage IDs without mutating input
            if (input.PartyIds == null || input.PartyIds.Length != 5 || input.OpeningHp == null || input.OpeningHp.Length != 5
                || input.Growth == null || input.Growth.Length != 5)
                throw new ArgumentException("Original expedition requires five frozen party slots.", nameof(input));
            if (input.EnableDrive || input.EnableFever || input.Profile != FormulaProfile.JP_LEGACY_EMPIRICAL)
                throw new ArgumentException("Unsupported original-expedition combat policy.", nameof(input));
            var characters = new Dictionary<string, CharacterDef>(StringComparer.Ordinal);
            foreach (var c in input.Characters)
            {
                if (c == null || string.IsNullOrEmpty(c.Id) || characters.ContainsKey(c.Id) || c.Hp <= 0
                    || c.Hp > 1000000 || c.Atk < 0 || c.Atk > 100000 || c.Def < 0 || c.Def > 20000
                    || !Finite(c.ChargeTimeSec) || c.ChargeTimeSec <= 0f)
                    throw new ArgumentException("Invalid frozen character definition.", nameof(input));
                characters.Add(c.Id, c);
            }
            if (!InRange(input.Stage.TimeLimitSec, 1f, 3600f) || !InRange(input.Stage.EnemyHpMul, 0.01f, 10f)
                || !InRange(input.Stage.EnemyAtkMul, 0.01f, 10f) || !InRange(input.Stage.EnemyDefMul, 0.01f, 10f)
                || input.Stage.Wave0 == null || input.Stage.Wave0.Length < 1 || input.Stage.Wave0.Length > 5
                || input.Stage.Wave1 == null || input.Stage.Wave1.Length != 0)
                throw new ArgumentException("Invalid original encounter wave or numerical domain.", nameof(input));
            foreach (var id in input.Stage.Wave0)
                if (id == null || !characters.ContainsKey(id) || !characters[id].IsEnemy)
                    throw new ArgumentException("Missing enemy definition: " + id, nameof(input));
            var effects = new Dictionary<string, EffectDef>(StringComparer.Ordinal);
            foreach (var e in input.Effects)
            {
                if (e == null || string.IsNullOrEmpty(e.Id) || effects.ContainsKey(e.Id) || !EffectCapability.Check(e).Ok
                    || !InRange(e.Magnitude, 0f, 100f) || !InRange(e.DurationSec, 0f, 3600f))
                    throw new ArgumentException("Invalid or unsupported original effect.", nameof(input));
                effects.Add(e.Id, e);
            }
            var skills = new Dictionary<string, SkillDef>(StringComparer.Ordinal);
            foreach (var s in input.Skills)
            {
                if (s == null || string.IsNullOrEmpty(s.Id) || skills.ContainsKey(s.Id)
                    || !EffectCapability.CheckSkill(s, effects).Ok || s.HitCount < 1 || s.HitCount > 10
                    || s.TargetCount < 1 || s.TargetCount > 5 || !InRange(s.AtkCoef, 0f, 20f)
                    || s.FlatPower < 0 || s.FlatPower > 100000 || !InRange(s.HealCoef, 0f, 20f)
                    || s.FlatHeal < 0 || s.FlatHeal > 100000 || !InRange(s.HealMaxHpFrac, 0f, 1f)
                    || !InRange(s.PercentAtk, 0f, 20f) || !InRange(s.SkillFlat, 0f, 100000f))
                    throw new ArgumentException("Invalid or unsupported original skill.", nameof(input));
                skills.Add(s.Id, s);
            }
            foreach (var c in input.Characters)
                if (string.IsNullOrEmpty(c.AutoSkillId) || !skills.ContainsKey(c.AutoSkillId)
                    || string.IsNullOrEmpty(c.TapSkillId) || !skills.ContainsKey(c.TapSkillId)
                    || string.IsNullOrEmpty(c.SlideSkillId) || !skills.ContainsKey(c.SlideSkillId))
                    throw new ArgumentException("Missing frozen character skill.", nameof(input));
            if (!InRange(input.Mods.FoodAtkMul, 0f, 10f) || !InRange(input.Mods.CartaMul, 0f, 10f)
                || !InRange(input.Clocks.SlideCdSec, 0f, 120f) || !InRange(input.Clocks.SlideShowtimeHoldSec, 0f, 10f)
                || !InRange(input.Clocks.HoldTimeoutSec, 0.01f, 120f))
                throw new ArgumentException("Invalid original battle modifiers or clock policy.", nameof(input));
            ValidateRelicParameters(input.RelicParameters);
            var fixedParty = ExpeditionContent.PartyIds;
            for (int i = 0; i < 5; i++)
            {
                CharacterDef c;
                if (input.PartyIds[i] != fixedParty[i] || !characters.TryGetValue(input.PartyIds[i], out c)
                    || input.Growth[i] == null || input.Growth[i].Id != input.PartyIds[i])
                    throw new ArgumentException("Invalid fixed party identity.", nameof(input));
                var max = Growth.Apply(c, input.Growth[i]).Hp;
                if (input.OpeningHp[i] < 0 || input.OpeningHp[i] > max)
                    throw new ArgumentException("Opening HP outside character range.", nameof(input));
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in input.RelicIds ?? new string[0])
                if (ExpeditionContent.FindRelic(id) == null || !seen.Add(id))
                    throw new ArgumentException("Unknown or duplicate relic: " + id, nameof(input));
            foreach (var id in seen)
            {
                var other = new HashSet<string>(seen, StringComparer.Ordinal); other.Remove(id);
                if (!ExpeditionContent.FindRelic(id).IsEligible(other))
                    throw new ArgumentException("Missing relic prerequisite: " + id, nameof(input));
            }
            if (input.IsBoss != (input.Stage.Id == "N7") || input.IsBoss != (input.Boss != null))
                throw new ArgumentException("Boss identity does not match encounter.", nameof(input));
            if (input.Boss != null)
            {
                var b = input.Boss;
                if (b.Version != "white-conductor-v1" || b.BossSlot != 0 || b.MaskSlots == null
                    || b.MaskSlots.Length != 2 || b.MaskSlots[0] != 1 || b.MaskSlots[1] != 2
                    || b.MaskCharacterId != "OE_MASK" || b.AutoSkillId != "OE_BOSS_auto" || b.AreaSkillId != "OE_BOSS_echo"
                    || input.Stage.Wave0.Length != 3 || input.Stage.Wave0[0] != "OE_BOSS"
                    || input.Stage.Wave0[1] != "OE_MASK" || input.Stage.Wave0[2] != "OE_MASK"
                    || !characters["OE_BOSS"].IsBoss || characters["OE_MASK"].IsBoss
                    || characters["OE_BOSS"].AutoSkillId != b.AutoSkillId || !InRange(b.FirstIntentSec, 0.01f, 120f)
                    || !InRange(b.CastDurationSec, 0.01f, 120f) || !InRange(b.PhaseOneIntervalSec, 0.01f, 120f)
                    || !InRange(b.PhaseTwoIntervalSec, 0.01f, 120f) || !InRange(b.AutoIntervalSec, 0.01f, 120f)
                    || !InRange(b.PhaseTwoHpFraction, 0.01f, 0.99f) || !InRange(b.DamagePerLivingMask, 0f, 10f))
                    throw new ArgumentException("Invalid frozen boss configuration.", nameof(input));
                ValidateEncounterSkills(skills, b.AutoSkillId, b.AreaSkillId);
            }
            if ((input.Stage.Id == "N4") != (input.Elite != null))
                throw new ArgumentException("Elite identity does not match encounter.", nameof(input));
            if (input.Elite != null)
            {
                var e = input.Elite;
                if (e.Version != "prompter-v1" || e.CasterSlot != 0 || input.Stage.Wave0[0] != "OE_PROMPTER"
                    || characters["OE_PROMPTER"].IsBoss || e.AutoSkillId != "OE_PROMPTER_auto"
                    || e.AreaSkillId != "OE_PROMPTER_tap" || characters["OE_PROMPTER"].AutoSkillId != e.AutoSkillId
                    || characters["OE_PROMPTER"].TapSkillId != e.AreaSkillId
                    || !InRange(e.FirstIntentSec, 0.01f, 120f) || !InRange(e.CastDurationSec, 0.01f, 120f)
                    || !InRange(e.IntervalSec, 0.01f, 120f) || !InRange(e.AutoIntervalSec, 0.01f, 120f))
                    throw new ArgumentException("Invalid frozen elite configuration.", nameof(input));
                ValidateEncounterSkills(skills, e.AutoSkillId, e.AreaSkillId);
            }
        }

        static void ValidateEncounterSkills(Dictionary<string, SkillDef> skills, string autoId, string areaId)
        {
            SkillDef auto, area;
            if (!skills.TryGetValue(autoId, out auto) || !skills.TryGetValue(areaId, out area))
                throw new ArgumentException("Missing scripted encounter skill.", "input");
            var singleFoe = auto.Target == TargetRule.RandomEnemies || auto.Target == TargetRule.LowestHpEnemies
                || auto.Target == TargetRule.HighestAtkEnemies || auto.Target == TargetRule.LowestHpRatioEnemies;
            if (auto.Type != SkillType.Auto || auto.Opcode != EffectOpcodes.DmgAuto || !singleFoe || auto.TargetCount != 1
                || area.Type != SkillType.Tap || area.Opcode != EffectOpcodes.DmgTap
                || area.Target != TargetRule.AllEnemies || area.TargetCount != 5
                || !DirectDamage(auto) || !DirectDamage(area))
                throw new ArgumentException("Scripted encounter requires single-target Auto and whole-party area damage.", "input");
        }

        static bool DirectDamage(SkillDef skill) => skill.HealCoef == 0f && skill.FlatHeal == 0 && skill.HealMaxHpFrac == 0f
            && (skill.AtkCoef > 0f || skill.FlatPower > 0 || skill.PercentAtk > 0f);

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool InRange(float value, float min, float max) => Finite(value) && value >= min && value <= max;

        static void ValidateRelicParameters(ExpeditionRelicParameters p)
        {
            if (!InRange(p.BarrierThresholdFraction, 0.01f, 1f) || p.BarrierStoredThresholds < 1 || p.BarrierStoredThresholds > 3
                || !InRange(p.ShieldBonus, 0f, 2f) || !InRange(p.ShockRatio, 0f, 10f) || !InRange(p.ShockSplashRatio, 0f, 10f)
                || p.ShockSplashTargets < 0 || p.ShockSplashTargets > 2 || p.OverloadThresholds < 1 || p.OverloadThresholds > 3
                || !InRange(p.OverloadRatio, 0f, 10f) || !InRange(p.ScatterRatio, 0f, 10f) || !InRange(p.ImprovedScatterRatio, 0f, 10f)
                || p.ScatterTargets < 0 || p.ScatterTargets > 2 || p.ImprovedScatterTargets < 0 || p.ImprovedScatterTargets > 2
                || !InRange(p.KillEchoRatio, 0f, 10f) || !InRange(p.RelayCharge, 0f, 100f) || !InRange(p.ImprovedRelayCharge, 0f, 100f)
                || p.HarmonyDistinctActors < 1 || p.HarmonyDistinctActors > 5 || !InRange(p.HarmonyCharge, 0f, 100f)
                || !InRange(p.ForteDamageBonus, 0f, 10f))
                throw new ArgumentException("Invalid or unbounded frozen relic parameters.", nameof(p));
        }
    }
}
