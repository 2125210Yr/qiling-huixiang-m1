# G2R14-EFFECT — X03 Cast side + Silence both sides

**task_id:** `G2R14-EFFECT`  
**batch:** `G2R14`  
**baseline:** `2a00445c67dbb2ca9251d81428e3096b71c233fb` (HEAD art-only delta `05a0184` — keep combat as 2a00445)  
**Lease:** this file + `G2_RECHECK_20260914/fixtures/` only. Integrator applies hunks. **Do not** let this session edit `BattleSim.cs` / tests.  
**Compile/test this session:** `NOT_RUN` (no C# applied).  
**Evidence class:** ENGINEERING. Not GL / not original-game completeness.

Closes REVIEW X03 / REGRESSIONS E01 E02 E03 (core path). Does **not** close X04 import atomicity (G2R14-IMPORT).

---

## 0. Defects (source, 2a00445)

### D1 — Cast side is a kind whitelist

`BattleSim.Cast` (L915–929) after opcode settle:

```csharp
IEnumerable<UnitState> fxTargets = fx.Kind == EffectKind.AtkBuff
    || fx.Kind == EffectKind.DefBuff
    || fx.Kind == EffectKind.Shield
    || fx.Kind == EffectKind.ChargeHaste
    || fx.Kind == EffectKind.Taunt
    ? PickAllies(...)
    : PickFoes(...);
if (fx.Kind == EffectKind.Taunt)
    fxTargets = new[] { caster };
```

`ChargeSpeed` / `ChargeAmount` / `Barrier` are **implemented** (`EffectCapability` + `ChargeSpeedMul` / `charge.add` / `shield.apply`) but **not** on that list. A skill with `Target=AllAllies` and those `EffectId`s therefore calls `PickFoes`. `Select` treats `AllAllies` as “return the whole pool”, so **every living enemy** gets the effect.

`ApplyStatus(ally, fx)` never hits this branch — that is why existing ChargeSpeed tests stay green.

`EffectDef` has **no** `Target` field today (`Definitions.cs` L62–84). Side was smuggled through the kind list. Mixed builtins already depend on that: `C001_drive` is `Target=HighestAtkEnemies` + `burst_atk` (AtkBuff) so the whitelist applies the buff on the **ally** pool while damage uses the foe rule. A naive “always inherit `SkillDef.Target`” would buff enemies on those drives.

### D2 — Silence is ally-command only

`UnitState.SkillLocked => Has(Silence)` (L86–87). Comment: blocks Tap/Slide/Drive/FeverTap; auto continues.

`CanAcceptSkillInput` (L464–476) only indexes `Allies[slot]`. `Submit` Tap/Slide/DriveBegin and `UsePlayerSkill` use it. `TickUnit` ally auto does **not** (correct).

Enemy declared skills (`TickUnit` L642–650) when `Charge >= 100`: pick Tap/Slide, **zero Charge**, `Cast` — **no** `SkillLocked`. Same engineering rule is faction-split.

---

## 1. Proposed API (integrator contract)

Do **not** add another `EffectKind` list inside `Cast`. Side comes from declared target tokens.

### 1.1 `TargetSide` (`Enums.cs`, after `TargetRule`)

```csharp
/// <summary>
/// Pool for an effect. FromRule = derive from the resolved TargetRule.
/// Self=0 on TargetRule is a real picker, so this enum is the unset/override channel.
/// </summary>
public enum TargetSide
{
    FromRule = 0,
    Ally = 1,
    Foe = 2,
    Self = 3
}
```

### 1.2 `EffectDef` fields (`Definitions.cs`) — this is the missing “EffectDef.Target”

```csharp
/// <summary>
/// When true, <see cref="Target"/> is this effect's picker (side included via TargetSemantics).
/// When false, inherit <see cref="SkillDef.Target"/>. Required because TargetRule.Self == 0.
/// </summary>
public bool HasTarget;
public TargetRule Target;

/// <summary>
/// When not FromRule, force the pool even if the resolved TargetRule is named for the other side
/// (builtin mixed damage+buff rows: burst_atk / shield / haste on foe-named skills).
/// Cast must not infer this from EffectKind.
/// </summary>
public TargetSide Side;
```

Resolution (single function, both sides):

1. `rule = fx.HasTarget ? fx.Target : skill.Target`
2. `side = fx.Side != FromRule ? fx.Side : TargetSemantics.SideOf(rule)`
3. `Self` → caster only. `Ally` → `PickAllies(rule, skill.TargetCount, …)`. `Foe` → `PickFoes(…)`  
   Selection **names** (`HighestAtkEnemies` on an Ally side, `AllEnemies` == all of pool, etc.) stay as `Select` already implements.

E01/E02 need only `SkillDef.Target = AllAllies` and `fx.Side = FromRule` (and `HasTarget` optional). No kind branch.

### 1.3 `TargetSemantics` (new file, same assembly)

```csharp
public static class TargetSemantics
{
    public static TargetRule Rule(SkillDef skill, EffectDef fx);
    public static TargetSide Side(SkillDef skill, EffectDef fx);
    public static TargetSide SideOf(TargetRule rule);
    public static bool IsAllySide(TargetRule rule);
    public static bool IsFoeSide(TargetRule rule);
    public static bool IsDeclaredSkill(SkillType t);
}
```

`SideOf` **must** match `LeaderSkill.IsFoeRule` (private today):

| TargetRule | SideOf |
|---|---|
| Self | Self |
| LowestHpAlly, AllAllies, LowestHpRatioAlly | Ally |
| RandomEnemies, LowestHpEnemies, HighestAtkEnemies, AllEnemies, LowestHpRatioEnemies | Foe |

`IsDeclaredSkill`: `Tap`, `Slide`, `Drive`, `Fever` only. **Not** `Auto`, **not** `Leader`.

### 1.4 `CanAcceptSkillInput` — same gate, both sides

Keep `CanAcceptSkillInput(int slot, out CommandReject)` as the ally wrapper (`Submit` / HUD unchanged).

Add:

```csharp
public bool CanAcceptSkillInput(bool ally, int slot, out CommandReject reason);
UnitState UnitAt(bool ally, int slot); // null → SlotInvalid
```

Shared checks, in order: `NotInProgress`, `Paused`, `QtePending`, slot exists, `UnitDead`, `ActionLocked`, `Silenced`.

Enemy `TickUnit` **declared** Tap/Slide: call `CanAcceptSkillInput(false, u.Slot, out _)` **before** Charge dump / SlideCd / Cast. On reject: **return** (Charge stays; auto already ran this tick). Silence expiry then fires the ready skill (E03).

Auto-attack branch: no `SkillLocked` (both sides).

`TryFeverTap` already rejects `Silenced`; leave it (Fever is ally-only declared).

### 1.5 Overlay + playable gate

```csharp
public void OverlayEffect(string id, EffectDef fx);
EffectDef ResolveEffect(string id); // overlay then Catalog.TryEffect
```

`Cast` uses `ResolveEffect(skill.EffectId)` (not `Catalog.TryEffect`).

If `EffectId` is non-empty:

| Resolve | Action |
|---|---|
| missing | `NoteEvent("missing_effect", EffectId, …)` — **do not** apply, **do not** invent a status |
| `EffectCapability.Check(fx)` not `Ok` | `NoteEvent("unplayable_effect", opcode, …, (int)kind)` — **do not** `ApplyEffect` (no silent Dot/Reflect store). Damage/heal from `SettleSkill` already ran; do **not** fail the whole battle |
| `Ok` | `PickEffectTargets` → `ApplyEffect` |

Do **not** implement Dot/Reflect/Revive/etc. in this task. Do **not** throw `RejectUnplayable` from `Cast` (would nuke `C001_slide`+`dot_flame` mid-fight). G2R14-IMPORT owns inventory vs playable catalog.

`ApplyStatus` / `ApplyEffect` stay a direct test hook (existing lifecycle tests). Only the **skill-linked** Cast path is gated.

### 1.6 Builtin `Side` stamps (data, not Cast)

`Catalog.BuildBuiltin` sets `Side` on **existing** rows so mixed skills keep today’s whitelist **side** (selection still uses `SkillDef.Target`):

| id | Side | Why |
|---|---|---|
| atk_up, def_up, burst_atk, haste, shield | Ally | used on foe-named taps/drives (`C001_drive`, `C002_tap`, …) |
| taunt | Self | replaces `if (Kind==Taunt) caster` |
| def_down, stun, dot_flame | Foe | inherit would already be foe on those skills; stamp so JSON roundtrip is explicit |

New authored effects (E01 ChargeSpeed, …) stay `Side=FromRule` and follow `SkillDef.Target` / `HasTarget`.

---

## 2. Exact hunks

Line numbers = worktree at write time (BattleSim Cast ~L879, TickUnit ~L620). Re-anchor on `void Cast(` / `void TickUnit(` if the file moved.

### Hunk A — `Enums.cs` after `TargetRule`

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/Enums.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/Enums.cs
@@
         LowestHpRatioAlly = 7,
         LowestHpRatioEnemies = 8
     }
+
+    public enum TargetSide
+    {
+        FromRule = 0,
+        Ally = 1,
+        Foe = 2,
+        Self = 3
+    }
 
     public enum EffectKind
```

### Hunk B — `Definitions.cs` `EffectDef`

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/Definitions.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/Definitions.cs
@@
         public string Id;
         public string Opcode;
         public EffectKind Kind;
+        /// <summary>
+        /// When true, <see cref="Target"/> is this effect's picker.
+        /// When false, Cast inherits <see cref="SkillDef.Target"/>.
+        /// TargetRule.Self == 0, so absence cannot be encoded as default Target.
+        /// </summary>
+        public bool HasTarget;
+        public TargetRule Target;
+        /// <summary>FromRule = SideOf(resolved rule). Ally/Foe/Self force the pool (mixed builtins).</summary>
+        public TargetSide Side;
         public float Magnitude;
```

### Hunk C — new `client/Assets/Scripts/Resonance.Battle/Core/TargetSemantics.cs`

Create the file (Unity will pick it up in the Battle asmdef). Full contents:

```csharp
namespace Resonance.Battle
{
    /// <summary>
    /// Declared target side / declared-skill set. ENGINEERING — not GL.
    /// Cast must use this instead of an EffectKind whitelist.
    /// </summary>
    public static class TargetSemantics
    {
        public static TargetRule Rule(SkillDef skill, EffectDef fx)
        {
            if (fx != null && fx.HasTarget) return fx.Target;
            return skill != null ? skill.Target : TargetRule.Self;
        }

        public static TargetSide Side(SkillDef skill, EffectDef fx)
        {
            if (fx != null && fx.Side != TargetSide.FromRule) return fx.Side;
            return SideOf(Rule(skill, fx));
        }

        public static TargetSide SideOf(TargetRule rule)
        {
            switch (rule)
            {
                case TargetRule.Self:
                    return TargetSide.Self;
                case TargetRule.LowestHpAlly:
                case TargetRule.AllAllies:
                case TargetRule.LowestHpRatioAlly:
                    return TargetSide.Ally;
                default:
                    return TargetSide.Foe;
            }
        }

        public static bool IsAllySide(TargetRule rule)
        {
            var s = SideOf(rule);
            return s == TargetSide.Ally || s == TargetSide.Self;
        }

        public static bool IsFoeSide(TargetRule rule) => SideOf(rule) == TargetSide.Foe;

        /// <summary>Silence blocks these. Auto-attack and Leader are not in the set.</summary>
        public static bool IsDeclaredSkill(SkillType t)
        {
            return t == SkillType.Tap || t == SkillType.Slide
                || t == SkillType.Drive || t == SkillType.Fever;
        }
    }
}
```

Optional same-PR: `LeaderSkill.IsFoeRule` → `return skill != null && TargetSemantics.IsFoeSide(skill.Target);` so the two classifiers cannot drift.

### Hunk D — `BattleSim.cs` overlay + resolve

After `OverlaySkill` / `ResolveSkill` (L292–305):

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@
         Dictionary<string, SkillDef> _skillOverlay;
+        Dictionary<string, EffectDef> _effectOverlay;
         SkillDef _execSkill;
@@
         public void OverlaySkill(string id, SkillDef skill)
         {
             if (string.IsNullOrEmpty(id) || skill == null) return;
             if (_skillOverlay == null) _skillOverlay = new Dictionary<string, SkillDef>();
             _skillOverlay[id] = skill;
         }
+
+        public void OverlayEffect(string id, EffectDef fx)
+        {
+            if (string.IsNullOrEmpty(id) || fx == null) return;
+            if (_effectOverlay == null) _effectOverlay = new Dictionary<string, EffectDef>();
+            _effectOverlay[id] = fx;
+        }
 
         SkillDef ResolveSkill(string id)
         {
             if (_skillOverlay != null && !string.IsNullOrEmpty(id)
                 && _skillOverlay.TryGetValue(id, out var over) && over != null)
                 return over;
             return Catalog.TrySkill(id);
         }
+
+        EffectDef ResolveEffect(string id)
+        {
+            if (_effectOverlay != null && !string.IsNullOrEmpty(id)
+                && _effectOverlay.TryGetValue(id, out var over) && over != null)
+                return over;
+            return Catalog.TryEffect(id);
+        }
```

### Hunk E — `CanAcceptSkillInput` both sides (replace L464–476)

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@
         public bool CanAcceptSkillInput(int slot, out CommandReject reason)
         {
-            reason = CommandReject.None;
-            if (Outcome != BattleOutcome.InProgress) { reason = CommandReject.NotInProgress; return false; }
-            if (Paused) { reason = CommandReject.Paused; return false; }
-            if (PendingDriveSlot >= 0) { reason = CommandReject.QtePending; return false; }
-            if (slot < 0 || slot >= Allies.Length || Allies[slot] == null) { reason = CommandReject.SlotInvalid; return false; }
-            var u = Allies[slot];
-            if (!u.Alive) { reason = CommandReject.UnitDead; return false; }
-            if (u.ActionLocked) { reason = CommandReject.ActionLocked; return false; }
-            if (u.SkillLocked) { reason = CommandReject.Silenced; return false; }
-            return true;
+            return CanAcceptSkillInput(true, slot, out reason);
+        }
+
+        public bool CanAcceptSkillInput(bool ally, int slot, out CommandReject reason)
+        {
+            reason = CommandReject.None;
+            if (Outcome != BattleOutcome.InProgress) { reason = CommandReject.NotInProgress; return false; }
+            if (Paused) { reason = CommandReject.Paused; return false; }
+            if (PendingDriveSlot >= 0) { reason = CommandReject.QtePending; return false; }
+            var u = UnitAt(ally, slot);
+            if (u == null) { reason = CommandReject.SlotInvalid; return false; }
+            if (!u.Alive) { reason = CommandReject.UnitDead; return false; }
+            if (u.ActionLocked) { reason = CommandReject.ActionLocked; return false; }
+            if (u.SkillLocked) { reason = CommandReject.Silenced; return false; }
+            return true;
+        }
+
+        UnitState UnitAt(bool ally, int slot)
+        {
+            if (ally)
+            {
+                if (slot < 0 || Allies == null || slot >= Allies.Length) return null;
+                return Allies[slot];
+            }
+            if (slot < 0 || Enemies == null || slot >= Enemies.Count) return null;
+            return Enemies[slot];
         }
```

### Hunk F — `TickUnit` enemy declared skills (replace L642–650)

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@
             if (!ally && u.Charge >= 100f)
             {
+                // Same declared-skill gate as ally Submit. Silence keeps Charge; auto already ran.
+                if (!CanAcceptSkillInput(false, u.Slot, out _))
+                    return;
                 var wantSlide = _rng.NextDouble() >= 0.65 && u.SlideCd <= 0f;
                 var skill = ResolveSkill(wantSlide ? u.Def.SlideSkillId : u.Def.TapSkillId);
-                u.Charge = 0f;
-                if (wantSlide) u.SlideCd = SlideCdDurationSec;
-                if (skill != null)
-                    Cast(u, false, skill, 1f);
+                if (skill == null) return;
+                u.Charge = 0f;
+                if (wantSlide) u.SlideCd = SlideCdDurationSec;
+                Cast(u, false, skill, 1f);
             }
```

Do **not** add `SkillLocked` to the auto-attack block (L628–640).

### Hunk G — `Cast` effect apply (replace L915–929)

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@
             Casts.Add(new CastFx { ... });
 
-            var fx = Catalog.TryEffect(skill.EffectId);
-            if (fx != null)
-            {
-                IEnumerable<UnitState> fxTargets = fx.Kind == EffectKind.AtkBuff
-                    || fx.Kind == EffectKind.DefBuff
-                    || fx.Kind == EffectKind.Shield
-                    || fx.Kind == EffectKind.ChargeHaste
-                    || fx.Kind == EffectKind.Taunt
-                    ? PickAllies(fx.Kind == EffectKind.Taunt ? TargetRule.Self : skill.Target, skill.TargetCount, casterAlly, caster)
-                    : PickFoes(casterAlly, skill.Target, skill.TargetCount, caster);
-                if (fx.Kind == EffectKind.Taunt)
-                    fxTargets = new[] { caster };
-                foreach (var t in fxTargets)
-                    ApplyEffect(t, fx);
-            }
+            ApplyLinkedEffect(skill, caster, casterAlly);
         }
```

Add these methods next to `Cast` (private):

```csharp
        void ApplyLinkedEffect(SkillDef skill, UnitState caster, bool casterAlly)
        {
            if (skill == null || string.IsNullOrEmpty(skill.EffectId)) return;
            var fx = ResolveEffect(skill.EffectId);
            if (fx == null)
            {
                NoteEvent("missing_effect", skill.EffectId, caster, null, 0, skill.Type);
                return;
            }
            var verdict = EffectCapability.Check(fx);
            if (!verdict.Ok)
            {
                NoteEvent("unplayable_effect", fx.Opcode ?? "", caster, null, (int)fx.Kind, skill.Type);
                return;
            }
            foreach (var t in PickEffectTargets(skill, fx, caster, casterAlly))
                ApplyEffect(t, fx);
        }

        IEnumerable<UnitState> PickEffectTargets(SkillDef skill, EffectDef fx, UnitState caster, bool casterAlly)
        {
            var side = TargetSemantics.Side(skill, fx);
            if (side == TargetSide.Self)
            {
                if (caster != null && caster.Alive) return new[] { caster };
                return new UnitState[0];
            }
            var rule = TargetSemantics.Rule(skill, fx);
            var count = skill != null ? skill.TargetCount : 1;
            if (side == TargetSide.Ally)
                return PickAllies(rule, count, casterAlly, caster);
            return PickFoes(casterAlly, rule, count, caster);
        }
```

### Hunk H — `Catalog.cs` builtin Side + `Fx` optional arg

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Content/Catalog.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Content/Catalog.cs
@@
             var effects = new Dictionary<string, EffectDef>
             {
-                ["dot_flame"] = Fx("dot_flame", EffectKind.Dot, 0.18f, 8f, 1, 2, "dot"),
-                ["def_down"] = Fx("def_down", EffectKind.DefDebuff, 0.20f, 10f, 1, 2, "def"),
-                ["atk_up"] = Fx("atk_up", EffectKind.AtkBuff, 0.18f, 12f, 1, 2, "atk"),
-                ["def_up"] = Fx("def_up", EffectKind.DefBuff, 0.18f, 12f, 1, 2, "def"),
-                ["shield"] = Fx("shield", EffectKind.Shield, 0.22f, 8f, 1, 1, "shield"),
-                ["taunt"] = Fx("taunt", EffectKind.Taunt, 1f, 6f, 1, 2, "taunt"),
-                ["haste"] = Fx("haste", EffectKind.ChargeHaste, 0.25f, 8f, 1, 1, "haste"),
-                ["burst_atk"] = Fx("burst_atk", EffectKind.AtkBuff, 0.35f, 10f, 1, 3, "atk"),
-                ["stun"] = Fx("stun", EffectKind.Stun, 1f, 3f, 1, 2, "stun")
+                ["dot_flame"] = Fx("dot_flame", EffectKind.Dot, 0.18f, 8f, 1, 2, "dot", TargetSide.Foe),
+                ["def_down"] = Fx("def_down", EffectKind.DefDebuff, 0.20f, 10f, 1, 2, "def", TargetSide.Foe),
+                ["atk_up"] = Fx("atk_up", EffectKind.AtkBuff, 0.18f, 12f, 1, 2, "atk", TargetSide.Ally),
+                ["def_up"] = Fx("def_up", EffectKind.DefBuff, 0.18f, 12f, 1, 2, "def", TargetSide.Ally),
+                ["shield"] = Fx("shield", EffectKind.Shield, 0.22f, 8f, 1, 1, "shield", TargetSide.Ally),
+                ["taunt"] = Fx("taunt", EffectKind.Taunt, 1f, 6f, 1, 2, "taunt", TargetSide.Self),
+                ["haste"] = Fx("haste", EffectKind.ChargeHaste, 0.25f, 8f, 1, 1, "haste", TargetSide.Ally),
+                ["burst_atk"] = Fx("burst_atk", EffectKind.AtkBuff, 0.35f, 10f, 1, 3, "atk", TargetSide.Ally),
+                ["stun"] = Fx("stun", EffectKind.Stun, 1f, 3f, 1, 2, "stun", TargetSide.Foe)
             };
@@
         static EffectDef Fx(string id, EffectKind kind, float mag, float dur, int stack, int tier, string group)
+            => Fx(id, kind, mag, dur, stack, tier, group, TargetSide.FromRule);
+
+        static EffectDef Fx(string id, EffectKind kind, float mag, float dur, int stack, int tier, string group, TargetSide side)
         {
             return new EffectDef
             {
                 Id = id,
                 Opcode = EffectOpcodes.ForKind(kind),
                 Kind = kind,
                 Magnitude = mag,
                 DurationSec = dur,
                 MaxStack = stack,
                 SourceTier = tier,
-                Group = group
+                Group = group,
+                Side = side
             };
         }
```

Do **not** put ChargeSpeed/ChargeAmount/Barrier into builtin unless QA asks — fixtures overlay them.

### Hunk I — `CatalogJson.cs` (merge with G2R14-IMPORT; do not drop)

`ReadEffect` / `OverlayEffect` / `CloneEffect` / `Serialize` must persist `hasTarget`, `target`, `side`.

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Content/CatalogJson.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Content/CatalogJson.cs
@@ ReadEffect
                 Group = Str(o, "group")
             };
+            if (HasKey(o, "target") || HasKey(o, "hasTarget"))
+            {
+                fx.HasTarget = true;
+                fx.Target = (TargetRule)Int(o, "target");
+            }
+            if (HasNum(o, "side")) fx.Side = (TargetSide)Int(o, "side");
             ApplyLifecycleJson(fx, o);
@@ OverlayEffect
             if (HasText(o, "group")) dst.Group = Str(o, "group");
+            if (HasKey(o, "target") || HasKey(o, "hasTarget"))
+            {
+                dst.HasTarget = true;
+                if (HasNum(o, "target")) dst.Target = (TargetRule)Int(o, "target");
+            }
+            if (HasNum(o, "side")) dst.Side = (TargetSide)Int(o, "side");
             ApplyLifecycleJson(dst, o);
@@ CloneEffect
                 Group = e.Group
             };
+            copy.HasTarget = e.HasTarget;
+            copy.Target = e.Target;
+            copy.Side = e.Side;
             EffectCapability.CopyLifecycle(e, copy);
@@ Serialize effects object
                 sb.Append(",\"group\":\"").Append(Esc(e.Group)).Append("\"");
+                if (e.HasTarget)
+                    sb.Append(",\"hasTarget\":true,\"target\":").Append((int)e.Target);
+                if (e.Side != TargetSide.FromRule)
+                    sb.Append(",\"side\":").Append((int)e.Side);
```

`HasTarget` is set when JSON has `target` **or** `hasTarget` so a fragment with `"target":2` is explicit.

---

## 3. Fixtures (E01 / E02)

Text + JSON under `G2_RECHECK_20260914/fixtures/`. All **DESIGN_PLACEHOLDER** — not original-game numbers.

| ID | EffectId | Opcode | Kind | Skill.Target |
|---|---|---|---|---|
| E01 | `g2r14_charge_speed` | `charge.rate` | ChargeSpeed (23) | AllAllies (2) |
| E02a | `g2r14_charge_amount` | `charge.add` | ChargeAmount (22) | AllAllies |
| E02b | `g2r14_barrier` | `shield.apply` | Barrier (25) | AllAllies |

Wire **only** via `OverlayEffect` + `OverlaySkill` on the caster’s `TapSkillId`, then `Submit(Tap)`. Never `ApplyStatus` for the pass assertion.

Default party `C001,C007,C010,C003,C005`: overlay `C005_tap` (or whichever slot you Submit). Zero `AtkCoef`/`FlatPower` so the skill is effect-only (heals already use `PickAllies(skill.Target)` and are out of scope).

Assert: every living ally changes; every living enemy does **not**. Parameter (`Magnitude`, `TargetCount`) respected. `Check(fx).Ok` is true (supported kinds).

E03 (QA owns the test): `ApplyStatus` Silence on an enemy, `Charge=100`, `Tick` — no enemy Tap/Slide `cast` event; Auto `cast` still allowed; after Silence expires, declared skill fires without re-charge if Charge was kept.

---

## 4. Residual risks

1. **`dot_flame` on Cast** — `status.apply`+Dot is `Registered_NotImplemented`. After this patch, `C001_slide` etc. still deal damage but **stop storing** a Dot status (`unplayable_effect`). Tests that asserted a leftover Dot instance from the **skill** path will fail; `ApplyStatus` tests will not. IMPORT should move that row out of playable content. Do not implement a Dot tick here.

2. **Mixed builtins without Hunk H** — if Cast is patched and Catalog `Side` stamps are skipped, `C001_drive` / `C002_tap` / `C016_tap` / taunt-on-foe-target skills apply buffs/shields/taunt to **enemies**. Hunk H is not optional.

3. **`C001_drive` selection** — Ally + `HighestAtkEnemies` + `TargetCount=3` still means “3 highest-ATK **allies**” (`Select` ignores the foe token once the pool is chosen). Do not “fix” that by stamping `HasTarget`+`AllAllies` on `burst_atk` (would buff the whole party).

4. **QtePending on the shared gate** — today `ReleaseBlocks` skips `TickUnit` while QTE is waiting, so enemies never see `QtePending`. If later ticks run during QTE, enemy Tap/Slide will wait. Acceptable; do not add a second enemy-only gate.

5. **Missing `TapSkillId` after the Charge-keep change** — old code dumped Charge even when `ResolveSkill` was null. New code keeps Charge. Edge-only.

6. **`LeaderSkill.IsFoeRule` duplication** — if not pointed at `TargetSemantics`, a later `TargetRule` can split leader vs Cast.

7. **Fingerprint / replay** — `ContentFingerprint` still ignores `Target` / `Side` / `HasTarget` (X05 / G2R14-REPLAY). E01 overlays will not show up in that hash.

8. **This session** — hunks not applied; E01/E02/E03 `NOT_RUN`. Integrator + G2R14-QA execute.

---

## 5. Apply order

1. Hunk A–C (types + `TargetSemantics`) — compiles without BattleSim.  
2. Hunk D–G (`BattleSim`) — X03 behavior.  
3. Hunk H (`Catalog` Side stamps) — **same BattleSim commit** or mixed builtins regress.  
4. Hunk I with IMPORT.  
5. QA: `fixtures/E01_E02.md` + REGRESSIONS E01/E02/E03.

**Out of scope:** full original-game effect list; import atomicity; AutoFire→Submit (REPLAY); QTE double-debit; Measured alias.
