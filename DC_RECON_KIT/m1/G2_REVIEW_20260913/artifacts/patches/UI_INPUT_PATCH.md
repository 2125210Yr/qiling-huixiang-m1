# UI_INPUT_PATCH — apply-ready (G2 路 A / N03 · R07 · Q01 · Q04)

**Owner:** integrator session B (`BattleHud.cs` / `GameRoot.cs` only; do not let other sessions overwrite).  
**Baseline read:** current worktree (not `ed7a9ba` text — `BattleSim` already has `QteRemaining` / `ResolveDriveChecked` / `TryFeverTap(int,out CommandReject)`).  
**Do not compile-edit `BattleSim.cs` here.** Code against `API_CONTRACT.md` §1–§3 names. `BattleSim.Commands.cs` is **missing** in this tree; `TickFever` already calls `Submit(...)`. Integrator must land `Submit` so these hunks compile.

**Apply order:** (1) `GameRoot` helper + `PortraitGesture` (2) `GameRoot.Tap/Slide/FireDrivePerfect/StartBattleAt/pause-speed-auto` (3) `BattleHud` QTE + portrait bind (4) `PauseBoardBind.RequestHostSpeed`.

**Design placeholders (not GL):** slide threshold `80` px (existing), gesture timeout `0.85` s unscaled, move-out = pointer leaves the portrait `RectTransform`.

---

## 0. Shared helper (new on `GameRoot`)

Add next to `Tap`/`Slide`. All player / smoke / HUD chrome commands go through this so `HitChainProbe` is driven only by `CommandResult.Accepted` (R07).

```csharp
public CommandResult SubmitInput(BattleCommand cmd)
{
    if (_battle == null)
        return new CommandResult { Accepted = false, Reason = CommandReject.NotInProgress, Seq = 0, Tick = 0 };
    var r = _battle.Submit(cmd);
    if (IsProbeKind(cmd.Kind))
    {
        if (r.Accepted) HitChainProbe.Input(ProbeName(cmd.Kind));
        else HitChainProbe.Cancel();
    }
    return r;
}

static bool IsProbeKind(BattleCommandKind k)
{
    return k == BattleCommandKind.Tap || k == BattleCommandKind.Slide
        || k == BattleCommandKind.DriveBegin || k == BattleCommandKind.FeverTap;
}

static string ProbeName(BattleCommandKind k)
{
    if (k == BattleCommandKind.Slide) return "slide";
    if (k == BattleCommandKind.DriveBegin) return "drive";
    if (k == BattleCommandKind.FeverTap) return "fever";
    return "tap";
}

/// <summary>Same classify as the portrait tray (R07). Fever wins, then Drive ready, then tap/slide.</summary>
public static BattleCommandKind ClassifyPortrait(BattleSim b, bool slide)
{
    if (b == null) return slide ? BattleCommandKind.Slide : BattleCommandKind.Tap;
    if (b.FeverActive) return BattleCommandKind.FeverTap;
    if (b.Drive >= 100f) return BattleCommandKind.DriveBegin;
    return slide ? BattleCommandKind.Slide : BattleCommandKind.Tap;
}
```

`Submit` must map `DriveResolve` → `ResolveDriveChecked` **once** and set `CommandResult.Accepted` iff `DriveResolveResult.Accepted`. Map `NoPending`→`NoQtePending`, `Paused`→`Paused`, `NotInProgress`→`NotInProgress`. Do **not** call `ResolveDriveChecked` from HUD *and* from `Submit` for the same press.

---

## 1. Portrait gesture state machine — `GameRoot.cs` `PortraitGesture`

**File:** `client/Assets/Scripts/Resonance.App/Core/GameRoot.cs`  
**Method:** replace the entire `PortraitGesture` class (currently L1505–1530).  
**N03:** one gesture → at most one `OnTap` **or** one `OnSlide`, never both; cancel emits nothing.

### Hunk A — replace class

```diff
--- a/client/Assets/Scripts/Resonance.App/Core/GameRoot.cs
+++ b/client/Assets/Scripts/Resonance.App/Core/GameRoot.cs
@@ -1505,26 +1505,118 @@
     public sealed class PortraitGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
     {
         public Action OnTap;
         public Action OnSlide;
-        Vector2 _start;
-        float _t;
-        bool _held;
+        public Action OnCancel;
+        public const float SlideThresholdPx = 80f;
+        public const float GestureTimeoutSec = 0.85f; // DesignPlaceholder
+
+        enum Phase { Idle, Down, Cancelled, Committed }
+        Vector2 _start;
+        float _downAt;
+        Phase _phase;
+        RectTransform _rt;
+
+        void Awake() { _rt = transform as RectTransform; }
+
+        void Update()
+        {
+            if (_phase != Phase.Down) return;
+            if (Time.unscaledTime - _downAt >= GestureTimeoutSec)
+                CancelGesture();
+        }
 
         public void OnPointerDown(PointerEventData e)
         {
             _start = e.position;
-            _t = Time.unscaledTime;
-            _held = true;
+            _downAt = Time.unscaledTime;
+            _phase = Phase.Down;
         }
 
-        public void OnDrag(PointerEventData e) { }
+        public void OnDrag(PointerEventData e)
+        {
+            if (_phase != Phase.Down) return;
+            if (Time.unscaledTime - _downAt >= GestureTimeoutSec) { CancelGesture(); return; }
+            if (!Inside(e)) CancelGesture();
+        }
+
+        public void OnPointerExit(PointerEventData e)
+        {
+            if (_phase != Phase.Down) return;
+            // Already past slide threshold: commit slide on exit. Else cancel (no tap).
+            if (e != null && (e.position.y - _start.y) > SlideThresholdPx)
+                Commit(OnSlide);
+            else
+                CancelGesture();
+        }
 
         public void OnPointerUp(PointerEventData e)
         {
-            if (!_held) return;
-            _held = false;
+            if (_phase != Phase.Down) return;
+            if (Time.unscaledTime - _downAt >= GestureTimeoutSec) { CancelGesture(); return; }
+            if (!Inside(e))
+            {
+                if (e != null && (e.position.y - _start.y) > SlideThresholdPx) Commit(OnSlide);
+                else CancelGesture();
+                return;
+            }
             var dy = e.position.y - _start.y;
-            if (dy > 80f) OnSlide?.Invoke();
-            else OnTap?.Invoke();
+            if (dy > SlideThresholdPx) Commit(OnSlide);
+            else Commit(OnTap);
+        }
+
+        void Commit(Action fire)
+        {
+            if (_phase != Phase.Down) return;
+            _phase = Phase.Committed;
+            if (fire != null) fire();
+        }
+
+        void CancelGesture()
+        {
+            if (_phase != Phase.Down) return;
+            _phase = Phase.Cancelled;
+            if (OnCancel != null) OnCancel();
+        }
+
+        bool Inside(PointerEventData e)
+        {
+            if (_rt == null || e == null) return false;
+            return RectTransformUtility.RectangleContainsScreenPoint(_rt, e.position, e.pressEventCamera);
         }
     }
```

Also change the class declaration to implement `IPointerExitHandler`:

```diff
-    public sealed class PortraitGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
+    public sealed class PortraitGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerExitHandler
```

`OnCancel` is for HUD bookkeeping only (no `Submit`). Integrator: `EventSystem` + `GraphicRaycaster` already exist (`EnsureEventSystem` / canvas). Portrait root `Image` must stay `raycastTarget = true` (it is today).

---

## 2. HUD portrait bind + one command — `BattleHud.cs`

**File:** `client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs`  
**Methods:** `BuildPortraits` bind (L633–639), replace `OnPortraitTap` (L699–708).

### Hunk B — bind (no direct `TrySlide`)

```diff
--- a/client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs
+++ b/client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs
@@ -633,10 +633,9 @@
                 var g = go.GetComponent<PortraitGesture>();
-                g.OnTap = () => OnPortraitTap(slot);
-                g.OnSlide = () =>
-                {
-                    if (QteOpen) return;
-                    if (_host != null && _host.Battle != null) _host.Battle.TrySlide(slot);
-                };
+                g.OnTap = () => SubmitPortrait(slot, slide: false);
+                g.OnSlide = () => SubmitPortrait(slot, slide: true);
+                g.OnCancel = null;
```

### Hunk C — replace `OnPortraitTap` with `SubmitPortrait`

```diff
--- a/client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs
+++ b/client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs
@@ -699,13 +699,32 @@
-        void OnPortraitTap(int slot)
+        void SubmitPortrait(int slot, bool slide)
         {
             var b = _host != null ? _host.Battle : null;
             if (b == null || QteOpen) return;
-            if (b.TryPortraitTap(slot) && b.PendingDriveSlot == slot)
-            {
-                if (b.Auto == AutoMode.Full) FinishQte(DriveTiming.Great);
-                else OpenQte();
-            }
+            var kind = GameRoot.ClassifyPortrait(b, slide);
+            var r = _host.SubmitInput(new BattleCommand
+            {
+                Kind = kind,
+                Slot = slot,
+                Source = CommandSource.Player
+            });
+            if (!r.Accepted) return;
+            if (kind == BattleCommandKind.DriveBegin
+                && b.PendingDriveSlot == slot
+                && b.Auto != AutoMode.Full)
+                OpenQte();
         }
```

**Do not** call `TryPortraitTap` / `TryTap` / `TrySlide` / `TryFeverTap` / `TryBeginDrive` from the tray after this hunk.

---

## 3. GameRoot.Tap / Slide / FireDrivePerfect + smoke (R07)

**File:** `GameRoot.cs`  
**Methods:** `Tap` L126–133, `Slide` L135–142, `FireDrivePerfect` L162–166.

### Hunk D

```diff
--- a/client/Assets/Scripts/Resonance.App/Core/GameRoot.cs
+++ b/client/Assets/Scripts/Resonance.App/Core/GameRoot.cs
@@ -126,21 +126,18 @@
         public bool Tap(int slot)
         {
-            if (_battle == null) return false;
-            HitChainProbe.Input("tap");
-            var ok = _battle.TryTap(slot);
-            if (!ok) HitChainProbe.Cancel();
-            return ok;
+            if (_battle == null) return false;
+            var kind = ClassifyPortrait(_battle, slide: false);
+            return SubmitInput(new BattleCommand
+            {
+                Kind = kind,
+                Slot = slot,
+                Source = CommandSource.Fixture
+            }).Accepted;
         }
 
         public bool Slide(int slot)
         {
-            if (_battle == null) return false;
-            HitChainProbe.Input("slide");
-            var ok = _battle.TrySlide(slot);
-            if (!ok) HitChainProbe.Cancel();
-            return ok;
+            if (_battle == null) return false;
+            var kind = ClassifyPortrait(_battle, slide: true);
+            return SubmitInput(new BattleCommand
+            {
+                Kind = kind,
+                Slot = slot,
+                Source = CommandSource.Fixture
+            }).Accepted;
         }
```

```diff
         public bool FireDrivePerfect()
         {
-            if (_hud != null) return _hud.FirePerfect();
-            return SliceDriveSequence.TryFirePerfect(_battle);
+            if (_battle == null) return false;
+            if (_battle.PendingDriveSlot < 0)
+            {
+                var n = _battle.Allies != null ? _battle.Allies.Length : 0;
+                for (int i = 0; i < n; i++)
+                {
+                    var begin = SubmitInput(new BattleCommand
+                    {
+                        Kind = BattleCommandKind.DriveBegin,
+                        Slot = i,
+                        Source = CommandSource.Fixture
+                    });
+                    if (begin.Accepted) break;
+                }
+            }
+            if (_hud != null && _hud.QteOpen)
+                return _hud.FirePerfect();
+            if (_battle.PendingDriveSlot < 0) return false;
+            return SubmitInput(new BattleCommand
+            {
+                Kind = BattleCommandKind.DriveResolve,
+                Slot = _battle.PendingDriveSlot,
+                Timing = DriveTiming.Perfect,
+                Source = CommandSource.Fixture
+            }).Accepted;
         }
```

`VerticalSliceSmokeRuntime` already calls `g.Tap` / `g.Slide` / `g.FireDrivePerfect` — those inherit Fixture. **Residual (not this patch):** `SliceDriveSequence.TryFillDrive` / `TryFirePerfect` and smoke `b.TryBeginDrive` still bypass `Submit` (`SliceDriveSequence.cs` L42, L56–62; `VerticalSliceSmokeRuntime.cs` ~L636, L802). Integrator or smoke owner should route those later; do not expand this patch into smoke files unless session A lease allows.

---

## 4. `StartBattleAt` — drop `Deterministic = true` (R04 / NEXT_GOAL §4.D)

**File:** `GameRoot.cs` **method** `StartBattleAt` L1182–1188.

```diff
             _battle = new BattleSim(_save.PartyIds, _save.LeaderSlot, _save.LastSeed, stage, _save.ProgressForParty(), mods)
             {
                 Speed = _save.Speed,
                 Auto = _save.Auto,
-                Deterministic = true,
                 Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
             };
```

Do not set `ForceNoCrit` here. Seed remains `_save.LastSeed`.

Also in the same method, after `_simAcc = 0f`, reset probe lines so rematch does not append old hit-chain:

```diff
             _simAcc = 0f;
+            HitChainProbe.Reset();
             Show(ScreenId.Battle);
```

---

## 5. QTE single clock (Q01) + late callback (Q04) — `BattleHud.cs`

**Methods:** field `_qteT` L80; `Tick` L199–216; `FirePerfect` L218–240; `TickQte` L710–728; `OpenOrAutoQte` L730–737; `OpenQte` L739–772; `FinishQte` L774–784; countdown in `RefreshPortraits` L1344–1348.

### Hunk E — fields

```diff
         float _showtimeT;
         float _stampT;
         Color _stampTint = VisualTokens.YellowConfirm;
-        float _qteT;
+        int _qteSeenResolveTick = -1;
```

### Hunk F — `Tick` (keep pause Refresh; QTE still readable when paused for countdown, but do not resolve)

Current `Tick` already skips `TickQte` when `Paused`. Keep that. `RefreshPortraits` will read `QteRemaining` without `_qteT`.

### Hunk G — `FirePerfect`

```diff
         public bool FirePerfect()
         {
             if (_host == null) return false;
             var b = _host.Battle;
             if (b == null) return false;
-            var casts = b.Casts != null ? b.Casts.Count : 0;
             if (b.PendingDriveSlot < 0)
             {
                 var n = b.Allies != null ? b.Allies.Length : 0;
                 for (int i = 0; i < n; i++)
-                    if (b.TryBeginDrive(i)) break;
+                {
+                    var begin = _host.SubmitInput(new BattleCommand
+                    {
+                        Kind = BattleCommandKind.DriveBegin,
+                        Slot = i,
+                        Source = CommandSource.Fixture
+                    });
+                    if (begin.Accepted) break;
+                }
             }
             if (b.PendingDriveSlot >= 0)
             {
                 FinishQte(DriveTiming.Perfect, CommandSource.Fixture);
                 return QteOpen == false && b.LastDriveResolveTick == _qteSeenResolveTick;
             }
-            if (b.Casts == null) return false;
-            for (int i = casts; i < b.Casts.Count; i++)
-                if (b.Casts[i] != null && b.Casts[i].CasterAlly && b.Casts[i].Type == SkillType.Drive)
-                    return true;
-            return false;
+            return false;
         }
```

Simpler accepted shape if the integrator prefers:

```csharp
public bool FirePerfect()
{
    if (_host == null || _host.Battle == null) return false;
    var b = _host.Battle;
    if (b.PendingDriveSlot < 0)
    {
        var n = b.Allies != null ? b.Allies.Length : 0;
        for (int i = 0; i < n; i++)
        {
            if (_host.SubmitInput(new BattleCommand
            {
                Kind = BattleCommandKind.DriveBegin,
                Slot = i,
                Source = CommandSource.Fixture
            }).Accepted) break;
        }
    }
    if (b.PendingDriveSlot < 0) return false;
    var before = b.LastDriveResolveTick;
    FinishQte(DriveTiming.Perfect, CommandSource.Fixture);
    return b.LastDriveResolveTick != before && b.LastDriveResolveTick >= 0;
}
```

### Hunk H — `TickQte` / `OpenOrAutoQte` (HUD must not timeout or auto-resolve)

```diff
         void TickQte(BattleSim battle)
         {
             if (battle == null || battle.Paused) return;
-            _qteT += Time.unscaledDeltaTime;
             TickOverlays();
             if (battle.PendingDriveSlot < 0)
             {
-                QteOpen = false;
-                HideGood();
-                return;
-            }
-            if (battle.Auto == AutoMode.Full)
-            {
-                FinishQte(DriveTiming.Great);
-                return;
-            }
-            if (_qteT >= BattleSim.DriveQteTimeoutSec)
-                FinishQte(DriveTiming.Good);
+                var tick = battle.LastDriveResolveTick;
+                var timing = battle.LastDriveTiming;
+                QteOpen = false;
+                HideGood();
+                if (tick >= 0 && tick != _qteSeenResolveTick)
+                {
+                    _qteSeenResolveTick = tick;
+                    ShowJudge(timing);
+                }
+                return;
+            }
+            // Core owns timeout / Auto Full resolve (ReleaseBlocks). HUD only displays QteRemaining.
         }
 
         void OpenOrAutoQte(BattleSim battle)
         {
             if (battle == null) return;
-            if (battle.Auto == AutoMode.Full)
-                FinishQte(DriveTiming.Great);
-            else
-                OpenQte();
+            if (battle.Auto == AutoMode.Full) return;
+            OpenQte();
         }
```

### Hunk I — `OpenQte` (delete `_qteT = 0`; coin guards pending)

```diff
         void OpenQte()
         {
             var b = _host != null ? _host.Battle : null;
             if (b == null || b.PendingDriveSlot < 0) return;
-            if (b.Auto == AutoMode.Full)
-            {
-                FinishQte(DriveTiming.Great);
-                return;
-            }
+            if (b.Auto == AutoMode.Full) return;
             VfxShowtime.KillAll();
             HideDriveSelect();
             QteOpen = true;
-            _qteT = 0f;
+            _qteSeenResolveTick = b.LastDriveResolveTick;
             // ... existing caster / BeginShowtime unchanged ...
             VfxGoodButton.Show(_root, hit =>
             {
                 if (!QteOpen) return;
+                var live = _host != null ? _host.Battle : null;
+                if (live == null || live.PendingDriveSlot < 0) return;
+                if (live.Paused || live.Outcome != BattleOutcome.InProgress) return;
                 FinishQte(hit ? DriveTiming.Perfect : DriveTiming.Good, CommandSource.Player);
             });
```

### Hunk J — `FinishQte` (ResolveDriveChecked / Submit once; no judge on reject)

```diff
-        void FinishQte(DriveTiming timing)
+        void FinishQte(DriveTiming timing, CommandSource source)
         {
             var b = _host != null ? _host.Battle : null;
             QteOpen = false;
             HideGood();
             if (_pix != null && _pix.Showing) _pix.HideNow();
             if (b == null) return;
-            b.ResolveDrive(timing);
-            _judgeFromQte = true;
-            ShowJudge(timing);
+            var slot = b.PendingDriveSlot;
+            CommandResult submitted;
+            if (_host != null)
+            {
+                submitted = _host.SubmitInput(new BattleCommand
+                {
+                    Kind = BattleCommandKind.DriveResolve,
+                    Slot = slot,
+                    Timing = timing,
+                    Source = source
+                });
+            }
+            else
+            {
+                var checkedResult = b.ResolveDriveChecked(timing);
+                submitted = new CommandResult
+                {
+                    Accepted = checkedResult == DriveResolveResult.Accepted,
+                    Reason = checkedResult == DriveResolveResult.Paused ? CommandReject.Paused
+                        : checkedResult == DriveResolveResult.NotInProgress ? CommandReject.NotInProgress
+                        : checkedResult == DriveResolveResult.NoPending ? CommandReject.NoQtePending
+                        : CommandReject.None
+                };
+            }
+            if (!submitted.Accepted) return;
+            _qteSeenResolveTick = b.LastDriveResolveTick;
+            _judgeFromQte = true;
+            ShowJudge(b.LastDriveTiming);
         }
```

**Integrator rule:** `Submit(DriveResolve)` must call `ResolveDriveChecked` internally. Then delete the `else` branch. **Never** call both `Submit` and `ResolveDriveChecked` for one press.

If `Submit` is not landed yet, apply the `else` path only (still satisfies Q04) and add a `TODO` that CommandLog will miss DriveResolve until `Submit` exists.

### Hunk K — countdown label uses `QteRemaining`

```diff
                     else if (QteOpen && battle.PendingDriveSlot == i)
                     {
                         // Primary P0 ~t365: portrait "N DRIVE TIME" during Drive window.
-                        var left = Mathf.CeilToInt(Mathf.Max(0.01f, BattleSim.DriveQteTimeoutSec - _qteT));
+                        var left = Mathf.CeilToInt(Mathf.Max(0.01f, battle.QteRemaining));
                         _readyTag[i].text = BattleCueCopy.DriveTimeLine(left);
                         _readyTag[i].color = VisualTokens.DriveOrange;
                     }
```

Grep `_qteT` after apply — must be **zero** hits.

---

## 6. Pause / speed / auto → `Submit` (CommandLog)

**File:** `GameRoot.cs`  
**Methods:** `EnsureAutoOn` L148–154, `EnsureSpeed2` L156–160, `CycleBattleAuto` L168–174, `SetBattleSpeed` L184–192, `ToggleBattlePause` L194–206.

### Hunk L — pause

```diff
         public void ToggleBattlePause()
         {
             if (_battle == null || _screen != ScreenId.Battle) return;
             if (_battle.Paused)
             {
-                _battle.Paused = false;
+                var r = SubmitInput(new BattleCommand { Kind = BattleCommandKind.Resume, Source = CommandSource.Player });
+                if (!r.Accepted) return;
                 var overlay = Root().Find("PauseBoard");
                 if (overlay != null) DestroyImmediate(overlay.gameObject);
                 return;
             }
-            _battle.Paused = true;
+            var pause = SubmitInput(new BattleCommand { Kind = BattleCommandKind.Pause, Source = CommandSource.Player });
+            if (!pause.Accepted) return;
             PauseBoard.Draw(Root(), ToggleBattlePause, () => Show(ScreenId.Home), RepeatCurrentBattle);
         }
```

`RepeatCurrentBattle` may keep `_battle.Paused = false` on the **abandoned** sim (it is replaced immediately). Optional: `SubmitInput(Resume)` before destroy; not required.

### Hunk M — speed / auto

```diff
         public void CycleBattleAuto()
         {
             var n = ((int)_save.Auto + 1) % 3;
-            _save.Auto = (AutoMode)n;
-            if (_battle != null) _battle.Auto = _save.Auto;
-            Persist();
+            if (_battle != null)
+            {
+                var r = SubmitInput(new BattleCommand
+                {
+                    Kind = BattleCommandKind.SetAuto,
+                    Value = n,
+                    Source = CommandSource.Player
+                });
+                if (!r.Accepted) return;
+            }
+            _save.Auto = (AutoMode)n;
+            Persist();
         }
 
         public void SetBattleSpeed(int speed)
         {
-            if (_battle == null) return;
             if (speed < 1) speed = 1;
             if (speed > 3) speed = 3;
-            _battle.Speed = speed;
+            if (_battle != null)
+            {
+                var r = SubmitInput(new BattleCommand
+                {
+                    Kind = BattleCommandKind.SetSpeed,
+                    Value = speed,
+                    Source = CommandSource.Player
+                });
+                if (!r.Accepted) return;
+            }
             _save.Speed = speed;
             Persist();
         }
```

`EnsureAutoOn` / `EnsureSpeed2` (smoke): same `Submit` with `Source = CommandSource.Fixture`.

```diff
         public void EnsureAutoOn()
         {
             if (_save.Auto == AutoMode.Full) return;
-            _save.Auto = AutoMode.Full;
-            if (_battle != null) _battle.Auto = AutoMode.Full;
-            Persist();
+            if (_battle != null)
+            {
+                var r = SubmitInput(new BattleCommand
+                {
+                    Kind = BattleCommandKind.SetAuto,
+                    Value = (int)AutoMode.Full,
+                    Source = CommandSource.Fixture
+                });
+                if (!r.Accepted) return;
+            }
+            _save.Auto = AutoMode.Full;
+            Persist();
         }
```

`EnsureSpeed2` already calls `SetBattleSpeed(2)` — after Hunk M it logs. For smoke, if `SetBattleSpeed` always uses `Player`, add an optional `CommandSource source` argument defaulting to `Player`, and pass `Fixture` from `EnsureSpeed2`.

**File:** `PauseBoardBind.cs` **method** `RequestHostSpeed` L49–56 — no change if it keeps calling `GameRoot.SetBattleSpeed`. After Hunk M that is enough.

**HUD antiques** (`BattleHud.BuildTopBar` L268–271) already call `_host.ToggleBattlePause` / `ToggleBattleSpeed` / `CycleBattleAuto`. No extra HUD bind once GameRoot submits.

**Residual:** `GameRoot.DrawSettings` `onSpeed` still writes `_battle.Speed` directly (L643–647). Not a battle-chrome button; fix in a follow-up so Settings also `Submit(SetSpeed)`.

---

## 7. `Submit` contract the integrator must implement (Commands.cs)

These hunks need members from `API_CONTRACT` §1 that are **referenced but not defined** in the current tree:

| Kind | `Slot` | `Timing` | `Value` | Success path |
|---|---|---|---|---|
| `Tap` | ally | unused | unused | existing `TryTap` / `UsePlayerSkill` after `CanAcceptSkillInput` |
| `Slide` | ally | unused | unused | `TrySlide`; reject `SlideOnCooldown` |
| `DriveBegin` | ally | unused | unused | `TryBeginDrive` (must **not** auto-`ResolveDrive` when `Auto==Full` if the command source is `Player` — Full Auto uses `CommandSource.Auto`) |
| `DriveResolve` | pending | judge | unused | `ResolveDriveChecked` once |
| `FeverTap` | ally | unused | unused | `TryFeverTap(slot, out reason)` |
| `Pause` / `Resume` | unused | unused | unused | set `Paused` |
| `SetSpeed` | unused | unused | `1..3` | `Speed = Value` |
| `SetAuto` | unused | unused | `(int)AutoMode` | `Auto = (AutoMode)Value` |

`Auto == Full` ⇒ reject `Player` `Tap`/`Slide`/`DriveBegin`/`FeverTap` with `AutoOwnsInput`. Pause / speed / auto stay accepted.

`TryBeginDrive` today (`BattleSim.cs` L569–570) still `ResolveDrive(Great)` when `Auto==Full`. After command landing, that auto-resolve should be `Submit(DriveResolve, Source=Auto)` inside the sim tick, not a side effect of a Player `DriveBegin`. HUD hunks already stop calling `FinishQte(Great)` on Full Auto.

---

## 8. Apply checklist

- [ ] `_qteT` gone; countdown = `battle.QteRemaining`
- [ ] Portrait Down/Drag/Up/Exit: one of Tap, Slide, or cancel
- [ ] Fever → `FeverTap`; Drive≥100 → `DriveBegin`; else Tap/Slide
- [ ] `HitChainProbe.Input` only if `Accepted`
- [ ] `StartBattleAt` has no `Deterministic = true`
- [ ] Coin callback: `QteOpen && PendingDriveSlot>=0 && !Paused && InProgress`
- [ ] `FinishQte` no `ShowJudge` unless Accepted
- [ ] Core timeout: HUD shows `LastDriveTiming` once via `LastDriveResolveTick`
- [ ] Pause / SPEED / AUTO antiques + PauseBoard chips land in `CommandLog`
- [ ] No production edits outside the leased files except `PauseBoardBind` if needed (same pause overlay; already goes through `SetBattleSpeed`)
