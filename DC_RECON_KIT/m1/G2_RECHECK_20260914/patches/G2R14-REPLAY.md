# G2R14-REPLAY — AutoFire→Submit + initial-header freeze

**Owner:** integrator (BattleSim.cs / GameRoot.cs).  
**Already landed:** `client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs` (this task).  
**Do not edit** `BattleSim.cs` / `GameRoot.cs` from the replay agent. No `G2Recheck*.cs`. No commit.

**Policy (REVIEW X05):** external-input replay. Auto Tap/Slide/Drive/Fever still `Submit` on the live run (observation). `BattleReplayer` does **not** re-inject `CommandSource.Auto`. Tick regenerates auto once. Do not mix half-replay half-regen. Keep seeded crits (`ForceNoCrit` stays false at the GameRoot entry).

**QA owns REGRESSIONS.md R01–R04** in `G2Recheck*.cs`. Existing `G2ReviewReplay*.cs` are not rewritten; they never `Freeze` and keep the legacy Speed=1/Manual guess, which is only safe for default-start tapes.

---

## 0. Why (double-fire)

Today:

- `AutoFireSkills` / `AutoFireDrive` call `TryTap` / `TrySlide` / `TryBeginDrive` / `ResolveDrive` and **bypass** `Submit`.
- `TickFever` already `Submit(FeverTap, Auto)` and writes `CommandLog`.
- `BattleReplayer` used to drain **all** accepted rows, including Auto Fever, while `TickFever` still fires Auto again → double-fire (second hit often `FeverThrottled`, sometimes a real extra cast).
- `SubmitReplay` rejects went only to static `Divergences`; `ReplayReport.Ok`/`Match` could stay true.

Landed replayer: skip `CommandSource.Auto`; any reject / unconsumed external command / `RulesVersion` or `DataIdentity` mismatch ⇒ `Match=false`. That already stops Fever double-fire **without** this patch. This patch is still required so auto Tap/Slide/Drive (and auto QTE resolve) go through the same command door as Fever.

---

## 1. `BattleSim.cs` — AutoFire and auto QTE resolve → `Submit`

**File:** `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`  
`TickFever` already submits Auto FeverTap (keep it). Do not wrap it again.

### Hunk A — auto Drive QTE resolve (`ReleaseBlocks`)

Current (approx. L433–436):

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@ -433,7 +433,7 @@
             if (PendingDriveSlot >= 0)
             {
                 if (Auto == AutoMode.Full)
-                    ResolveDrive(DriveTiming.Great);
+                    Submit(BattleCommand.DriveResolve(DriveTiming.Great, CommandSource.Auto));
                 else
                 {
```

`Submit` → `ResolveDriveChecked` once. `Player` Full-auto still gets `AutoOwnsInput`; `Auto` source does not.

### Hunk B — `AutoFireDrive`

Current (approx. L465–475):

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@ -465,13 +465,13 @@
         void AutoFireDrive()
         {
             if (Auto != AutoMode.Full || Drive < 100f) return;
             for (int i = 0; i < Allies.Length; i++)
             {
-                if (!TryBeginDrive(i)) continue;
+                var begin = Submit(BattleCommand.DriveBegin(i, CommandSource.Auto));
+                if (!begin.Accepted) continue;
                 if (PendingDriveSlot >= 0)
-                    ResolveDrive(DriveTiming.Great);
+                    Submit(BattleCommand.DriveResolve(DriveTiming.Great, CommandSource.Auto));
                 break;
             }
         }
```

Rejected slots stay in `CommandLog` (observation). Replay skips them (`Source=Auto`).

### Hunk C — `AutoFireSkills`

Current (approx. L477–486):

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@ -477,12 +477,13 @@
         void AutoFireSkills()
         {
             if (Auto == AutoMode.Manual) return;
             for (int i = 0; i < Allies.Length; i++)
             {
                 if (Allies[i] == null || !Allies[i].Alive || Allies[i].Charge < 100f) continue;
-                if (NextAutoType(i) == SkillType.Slide && Allies[i].SlideCd <= 0f) TrySlide(i);
-                else TryTap(i);
+                if (NextAutoType(i) == SkillType.Slide && Allies[i].SlideCd <= 0f)
+                    Submit(BattleCommand.Slide(i, CommandSource.Auto));
+                else
+                    Submit(BattleCommand.Tap(i, CommandSource.Auto));
             }
         }
```

Do **not** disable `AutoFire*` during replay. Regeneration is the replay path.

---

## 2. `BattleSim.Commands.cs` — complete fingerprint + optional header field

**File:** `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.Commands.cs`

`BattleContentIdentity` (R04) already lives in `BattleReplay.cs`. Point `ContentFingerprint` at it so `RunHeader` `data=` is no longer the partial skill/effect hash (missing character HP/ATK, stage, FlatPower, Target, Trigger/PeriodSec).

### Hunk D — `ContentFingerprint`

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.Commands.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.Commands.cs
@@ -94,39 +94,11 @@
             return sb.ToString();
         }
 
-        /// <summary>Cheap content identity for logs: counts + stable hash of skill/effect ids and numeric fields.</summary>
+        /// <summary>REGRESSIONS.md R04 — full catalog identity (HP/ATK, stage, FlatPower/Target, Trigger/PeriodSec).</summary>
         public static string ContentFingerprint()
         {
-            var chars = Catalog.Characters;
-            var skills = Catalog.Skills;
-            var effects = Catalog.Effects;
-            unchecked
-            {
-                int h = 17;
-                if (skills != null)
-                    foreach (var kv in skills)
-                    {
-                        var s = kv.Value;
-                        h = h * 31 + Fnv(kv.Key);
-                        if (s == null) continue;
-                        h = h * 31 + Fnv(s.Opcode);
-                        h = h * 31 + s.AtkCoef.GetHashCode();
-                        h = h * 31 + s.HitCount;
-                        h = h * 31 + s.DriveGain;
-                    }
-                if (effects != null)
-                    foreach (var kv in effects)
-                    {
-                        var e = kv.Value;
-                        h = h * 31 + Fnv(kv.Key);
-                        if (e == null) continue;
-                        h = h * 31 + (int)e.Kind;
-                        h = h * 31 + e.Magnitude.GetHashCode();
-                        h = h * 31 + e.DurationSec.GetHashCode();
-                    }
-                return "builtin-c" + (chars != null ? chars.Count : 0) + "-s" + (skills != null ? skills.Count : 0)
-                    + "-e" + (effects != null ? effects.Count : 0) + "-" + (h & 0x7fffffff).ToString("x8");
-            }
+            return "g2id1-" + BattleContentIdentity.Fingerprint();
         }
```

`Fnv` may remain unused; delete it in the same edit if the compiler warns. `RunHeader` `data=` value **will change**. `G2ReviewRngReplayTests` only asserts seed + `RulesVersion` inside `RunHeader()`, not the old `builtin-c` prefix.

### Hunk E — optional `InitialHeader` on the partial (not BattleSim.cs)

Add next to `CommandLog`:

```csharp
        /// <summary>REGRESSIONS.md R02. Set by <see cref="FreezeInitialHeader"/> after Speed/Auto/Profile/Clocks.</summary>
        public BattleInitialHeader InitialHeader { get; set; }

        public BattleInitialHeader FreezeInitialHeader(string[] partyIds = null, string stageId = null)
        {
            InitialHeader = BattleInitialHeader.Freeze(this, partyIds, stageId);
            return InitialHeader;
        }
```

`BattleInitialHeader.Freeze` is first-call-wins and also writes this property when present. Do **not** freeze inside the `BattleSim` constructor: object initializers set Speed/Auto/Profile **after** the ctor.

`RunHeader()` still prints **current** Auto/Speed. That is a live log line, not the frozen opening header. Capture reads `BattleInitialHeader`, not `RunHeader` Auto/Speed.

---

## 3. `GameRoot.cs` — freeze after configure, before first Tick

**File:** `client/Assets/Scripts/Resonance.App/Core/GameRoot.cs`  
**Method:** `StartBattleAt` (approx. L1185–1194).  
`using Resonance.Battle;` is already present.

### Hunk F

```diff
--- a/client/Assets/Scripts/Resonance.App/Core/GameRoot.cs
+++ b/client/Assets/Scripts/Resonance.App/Core/GameRoot.cs
@@ -1185,12 +1185,14 @@
             _battle = new BattleSim(_save.PartyIds, _save.LeaderSlot, _save.LastSeed, stage, _save.ProgressForParty(), mods)
             {
                 Speed = _save.Speed,
                 Auto = _save.Auto,
                 // R04: seeded RNG is already reproducible; ForceNoCrit stays a fixture-only switch.
                 ForceNoCrit = false,
                 Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
             };
+            // R02: freeze real opening seed/party/growth/gear/leader/clocks/profile/auto/speed/data identity.
+            BattleInitialHeader.Freeze(_battle, _save.PartyIds, stage != null ? stage.Id : "");
             _simAcc = 0f;
             Show(ScreenId.Battle);
```

Equivalent: `_battle.FreezeInitialHeader(_save.PartyIds, stage != null ? stage.Id : "");` after hunk E.

Any other `new BattleSim(...) { Speed=..., Auto=... }` playable entry (debug / natural-play factory) must Freeze the same way **after** those assignments and **before** the first `Tick`. Tests that start at Speed=3 / Auto=Full and later `SetSpeed(2)` / `SetAuto(Manual)` must Freeze or Capture will still guess 1/Manual (legacy fallback for `G2ReviewReplay*`).

Do not set `ForceNoCrit = true` here.

---

## 4. Apply order

1. `BattleReplay.cs` (already in tree).
2. Hunks D + E (`BattleSim.Commands.cs`).
3. Hunks A–C (`BattleSim.cs`).
4. Hunk F (`GameRoot.cs`).

After merge: auto actions appear on `CommandLog` with `Source=Auto`; `BattleReplayer.IsExternalInput` leaves them off the inject list; Tick regenerates them once.

---

## 5. Existing tests (do not rewrite G2Review*; do not edit G2Recheck*)

| File | Status vs this landing |
|---|---|
| `G2ReviewReplayTests` / `G2ReviewReplayComponentTests` | Manual Player tapes. Infer fallback Speed=1. Isolated run **9/9 pass**. Not R01–R04. |
| `G2RecheckReplayContractTests.R01` | Should go green once Auto Fever is on the tape (already) and replayer skips `CommandSource.Auto`. Call `Freeze` after `FeverFactory` clock edits if `DataIdentity` mismatches. |
| `G2RecheckReplayContractTests.R02` | **Will stay red until the test (or GameRoot) calls `BattleInitialHeader.Freeze(sim)` after `Speed=3`/`Auto=Full` and before the first `Tick`.** Capture cannot recover 3/Full from a later `SetSpeed(2)` / `SetAuto(Manual)`. Infer fallback is still 1/Manual. Do not “fix” Infer. |
| `G2RecheckReplayContractTests.R03` | Compatible: injected Player `FeverTap` is external, reject is copied into `ReplayReport.CommandDiff` / `Diff` (`seq=…;tick=…;reject=…`), `Match=false`. |
| `G2RecheckReplayContractTests.R04` | `BattleContentIdentity.Fingerprint()` already includes HP / FlatPower / Trigger / PeriodSec / stage. `BattleSim.ContentFingerprint()` still the old partial hash until hunk D. |

G2Recheck compile is currently blocked by ambiguous `NewJp` (`using static` both fixture classes) — QA/integrator, not this task.

---

## 6. Residual

- Enemy AI Tap/Slide in `TickUnit` still bypasses `Submit` (out of X05 / player-auto scope).
- `RunHeader()` current Auto/Speed vs frozen header: live log vs tape header. Do not “fix” by rewriting `RunHeader` from the end state.
- Natural-play / other `new BattleSim` sites besides `StartBattleAt` need the same Freeze call when they become evidence producers.
