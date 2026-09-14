# UI_STATE_LIFECYCLE_AUDIT — U01 RepeatedBattleEntry_ReleasesUiState

**Read-only.** No production edits. Line numbers are the worktree at write time (2026-09-13).  
**Teardown owner today:** `GameRoot.Show` → `ClearUi` (`GameRoot.cs` L388–395, L1421–1432). There is **no** `BattleHud.Dispose`.

`ClearUi` does three things only: `EmeraldLobby.ReleaseFrom` (Inochi unparent), `DestroyImmediate` **every canvas child**, `_built.Clear()`, `_hud = null`. It does **not** reset statics, canvas-hosted components, or `DontDestroyOnLoad` hosts.

---

## 1. Inventory — created per battle entry vs released

| Object / subscription / static | Created | Added to `_built`? | `Show(*)` / `ClearUi` | Rematch `StartVsBattle` / `RepeatCurrentBattle` | Home (`Show(Home)` / Pause Home) |
|---|---|---|---|---|---|
| `BattleHud` instance (`_hud`) | `DrawBattle` L1200–1201 `new BattleHud` + `Build` | n/a (C# object) | `_hud = null` L1431. No Dispose. | New HUD after `StartBattleAt` → `Show(Battle)` → `ClearUi` | `_hud = null`. `_battle` **not** nulled |
| Canvas HUD roots (`arch*`, dock, portraits `pN`, `driveSelect`, `field`, antique buttons) | `BattleHud.Build*` / `Img`/`Label`/`Antique` add to `_built` | yes | `DestroyImmediate` all canvas children | destroyed then rebuilt | destroyed |
| `CharacterPresenter.StageArena` mosaic/wash/discs + `Vignette` (`vB/vT/vL/vR`) | `Build` L164 → `StageBackdrop` L510–529, `Vignette` L788–794 | wash/discs yes; **vignette Parts are not tracked** | still canvas children → destroyed | ok | ok |
| `PixelCombatFx` (`pixelFx`) | `PixelCombatFx.Ensure` `BattleHud.cs` L166–167 | yes | destroyed; static `_dotsSpr` kept | new Ensure | destroyed |
| `BattleFighter` allies | `SpawnAllies` L1941–1954 | yes | destroyed | destroyed | destroyed |
| `BattleFighter` enemies | `SpawnEnemies` L1957–1986 | yes | destroyed | destroyed | destroyed |
| Enemy `focusHit` `Button.onClick` | `BindFocus` `BattleFighter.cs` L220–243 | via fighter GO | destroyed with fighter | **wave change** uses `Object.Destroy` (deferred) L1965 — one-frame overlap + stale `_built` refs | n/a |
| `PortraitGesture` + `Image` raycast | `BuildPortraits` L558, L633–639 | via `pN` | destroyed (listeners die with GO) | rebuilt | destroyed |
| Antique `Button.onClick` (PAUSE / SPEED / AUTO / hidden ESCAPE/Repeat) | `BuildTopBar` L268–302 | via antique GO | destroyed | rebuilt | destroyed |
| `EventTrigger` | **none** on battle HUD | — | — | — | — |
| `ICharacterPresentation` / `CharacterPresentationAdapter` | `BindExisting` L192–193 and again `SpawnEnemies` L1988–1989 | no (plain C#) | GC when `_hud` dropped | new adapter; old arrays hold destroyed fighters until GC | GC |
| `CharacterPresenter` static sprites/font | first UI use | n/a | **process lifetime** (OK) | kept | kept |
| `VfxShowtime` instances | `VfxShowtime.Play` (`VfxRouter` L179, casts) | no | canvas child → destroyed. `KillAll` L48–56 uses **`Destroy` not Immediate** | one-frame leftover; `AnyLive()` can stay true | destroyed next `ClearUi` |
| `VfxGoodButton._live` + `vfxGood` | `OpenQte` → `Show` L764 | no | `OnDestroy` L92–96 clears `_live` if this==_live | Hide then destroy | destroy |
| `VfxGoodButton` sparks | `OnClick` / `ReleaseSparks` L243–254 **reparents to canvas** | no | `ClearUi` gets them if still children | mid-battle Hide leaves sparks ~0.28s | leftover until expire or `ClearUi` |
| `VfxFeverOverlay._live` / `Active` | `BeginFeverBanner` L1885 | no | `OnDestroy` L74–80 clears. Home from **pause during Fever** skips HUD `feverEnd` (`BattleHud` L1127–1133) | `Active` cleared if GO destroyed | static `Active` false on destroy; combo see below |
| `VfxTipPlate._live` | `RefreshPortraits` / fever tips | no | `OnDestroy` clears `_live` | may Hide on fever end only | destroy |
| `VfxJudge` | `ShowJudge` | no | canvas child / `KillAll` | leftover frame if `Destroy` | destroy |
| `VfxDriveReady` | first Drive-ready edge L1208–1209 | no | canvas child | `Play` `DestroyImmediate`s siblings L40–45 | destroy |
| `VfxStatusIcons` `statusIcons{slot}` | `RefreshPortraits` L1267 every tick | no (canvas child) | destroyed | rebuilt | destroyed |
| Field status icons | `BattleFighter.Apply` L308 `DrawField` | on fighter | destroyed with fighter | destroyed | destroyed |
| `VfxShield` hex | **only** `VfxRouter.OnBuff` (not chips) | no | self-`Destroy` at 0.35s; also canvas teardown | see §4 | see §4 |
| `VfxRouter._combo` / `_comboDmg` | fever hits L95–99 | n/a | **not reset** unless `EndFever` L103–110 | Home mid-fever: **stale combo** | stale until next `EndFever` |
| `HitChainProbe` static `Lines` / `_inAt` | `Input` on GameRoot.Tap/Slide today | n/a | **not reset** on `Show` | smoke `Reset` only (`VerticalSliceSmokeRuntime` L89). Rematch **appends** | persists |
| `WavePreview.VisibleKind` / `VisibleTitle` | `WaveCueBoard` play/Hide L635–636, L704–705 | n/a | **`Hide()` not called on DestroyImmediate** | **stale WaveAdvance** can hide field / block Drive select | stale |
| `WavePreviewCue` / `WaveCueBoard` | `WaveCueHost.LateUpdate` L738–746 → `Observe` L151–177 | **no** | canvas child → destroyed. Host is **not** under canvas | recreated next Battle LateUpdate | not recreated (`CurrentScreen != Battle`) |
| `WaveCueHost` GO | `RuntimeInitialize` L728–735 `DontDestroyOnLoad` | n/a | **never released** (singleton OK) | kept | kept |
| `WaveCueBoard._phaseHold` → `HoldSim` | splash L682–686 | n/a | destroy **without** `Hide` L692–708 leaves `HoldSim` on **old** `BattleSim` | new `BattleSim` in `StartBattleAt` — OK | abandoned sim GC |
| `PauseBoard` + `PauseBoardBind` | `ToggleBattlePause` L205 `PauseBoard.Draw` (`built: null`) L22–81 | **no** | canvas child named `PauseBoard` → destroyed | `RepeatCurrentBattle` L213–214 `DestroyImmediate` then `StartBattleAt` | Home callback `Show(Home)` → `ClearUi` |
| `ResultBoard` | `DrawResult` L1214 | no | destroyed on next `Show` | rematch `StartBattleAt` / `onNext` | Home button |
| `VfxStageClear` + `_onDone` | `DrawResult` L1216 | no | `OnDestroy` L101–104 does **not** invoke callback | rematch mid-splash: callback lost (board already cleared) | same |
| `VfxLevelUpDelay` | `ResultBoard.Reveal` L46–47 → `PlayClearModalDelayed` L57–63 **`AddComponent` on `root.parent` = Canvas** | n/a | **`ClearUi` does not strip Canvas components** | **fires LEVEL UP on next battle** | can fire on Home if rematch/home before 2.5s |
| `VfxLevelUp` / `VfxLevelUpMark` modal | delay `Update` L267–271 or `Play` | child of canvas | child destroyed if already spawned | delay-not-fired is the leak | same |
| `CanvasShake.Live` | `BuildCanvas` L1461; `OnEnable` `CombatFeel.cs` L15 | on **Canvas** | **survives `ClearUi`**. `Punch` `_t/_mag` can continue into Result/Home | shake into next battle | shake on lobby |
| `GameRoot` / `EventSystem` | `DontDestroyOnLoad` L63, L1468 | n/a | keep | keep | keep |
| `GameRoot._battle` | `StartBattleAt` L1182 | n/a | **kept after Home/Result** | replaced | stale sim until next start (FeverOn/QteOpen still readable: QteOpen false without HUD) |
| `GameRoot._simAcc` | L1189 | n/a | leftover until next `StartBattleAt` | reset | leftover |
| Coroutines | **none** in `BattleHud` / `GameRoot` battle path | — | — | — | — |
| Debug `StartCoroutine` | smoke / capture / cue strip only | n/a | their own hosts DDoL | n/a | n/a |
| `IceParticles` world cam/RT | Inspect C001 only (`GameRoot` L841) | yes | `OnDestroy` L278–288 `Destroy` world/cam/RT | n/a (not battle) | inspect `ClearUi` OK |
| Home `CharacterPresenter.DrawStage` / `EmeraldLobby` | `DrawHome` | yes / ReleaseFrom | ReleaseFrom then wipe children | n/a | rebuild |

---

## 2. Transition matrix (what actually runs)

### `Show(ScreenId)` (`GameRoot.cs` L388–395)

Always `ClearUi()` first. Battle → Result/Home/Stage destroys HUD GOs. Does not call `VfxGoodButton.Hide`, `VfxShowtime.KillAll`, `VfxFeverOverlay.Hide`, `WaveCueBoard.Hide`, `HitChainProbe.Reset`, `VfxLevelUp.SkipLive`.

### Rematch

- **Pause Repeat:** `RepeatCurrentBattle` L209–216: `_battle.Paused = false`, destroy `PauseBoard` by name, `StartBattleAt(_activeStage)` → new `BattleSim` → `Show(Battle)` → `ClearUi` → `DrawBattle`.
- **Result NEXT/RETRY:** `DrawResult` L1209–1213 `StartBattleAt` (same).
- **`StartVsBattle`:** L101–104 `StartBattleAt(0)` (same Show path).

Duplicate HUD: **not** created. `DrawBattle` is only reached after `ClearUi`. Risk is leftover **statics / Canvas components**, not a second `BattleHud`.

### Home

- Pause Home: `Show(ScreenId.Home)` L205.
- Result Home: `Show(ScreenId.Home)` L1214.
- Mid-Fever Home: HUD `feverEnd` (`BattleHud` L1127–1133) **does not run** → `VfxRouter` combo + any Hide() skips.

---

## 3. Concrete leaks / overlap (ranked) + proposed fix

### L1 — `VfxLevelUpDelay` on the Canvas (Result rematch / Home)

- **Where:** `ResultBoard.Reveal` L46–47 passes `root.parent` (the Canvas). `VfxLevelUp.PlayClearModalDelayed` L60–62 `parent.gameObject.AddComponent<VfxLevelUpDelay>()`.
- **Risk:** `ClearUi` only destroys **children**. After CLEAR Reveal, rematch or Home within `ClearHoldSec` (2.50s) still has an armed delay on the Canvas. `Update` L267–271 then `PlayClearModal` on the **new** battle or Home.
- **U01:** LEVEL UP dim (`ResultBoard` L78–80 raycastTarget false, but modal covers the field) over a live fight.
- **Fix:** Arm the delay on `ResultBoard` root (destroyed with the board), **or** `ClearUi` `Destroy(GetComponent<VfxLevelUpDelay>())` + `VfxLevelUp.SkipLive()`. Prefer both.

### L2 — `WavePreview` statics survive `DestroyImmediate`

- **Where:** `WaveCueBoard.Hide` L704–705 is the only clearer of `VisibleKind`/`VisibleTitle`. No `OnDestroy`. `ClearUi` L1427–1428 destroys `WavePreviewCue` mid-PHASE without `Hide`.
- **Risk:** Next battle `RefreshDriveSelect` L507–508 treats `WaveAdvance` as exclusive; `SetFieldSplashHidden` L1184 hides standees; `HudPhaseDuringSplash` L2008–2009 shows the previous phase number.
- **Fix:** `WaveCueBoard.OnDestroy` → `Hide()`. Also `ClearUi` / `StartBattleAt` set `VisibleKind = None`.

### L3 — QTE coin + sparks after battle end / Hide

- **Where:** `OpenQte` L764–768; `VfxGoodButton.Hide` L48–51 / `End` L150–163 queues `LateUpdate` deactivate; `ReleaseSparks` L243–254 reparents `Spark` to the canvas.
- **Risk:** `Show(Result)` `DestroyImmediate` usually kills the coin (`OnDestroy` clears `_live`). If `Hide()` ran first, inactive `_live` can be **reparented** by `Ensure` L55–60 onto a later canvas rebuild if `ClearUi` missed a detached instance (Ensure also `GetComponentInChildren(true)` L63). Sparks after Hide live ~0.28s on the canvas (`Spark` L471+) and can sit on Result/Home until they expire — or on the next battle if rematch is immediate and `ClearUi` already ran before reparent (order: Hide LateUpdate after `ClearUi` is OK; Hide then `ReleaseSparks` **before** `ClearUi` is OK; Hide LateUpdate **after** Result draw leaves sparks on Result).
- **Fix:** `ClearUi` call `VfxGoodButton.Hide()` then destroy leftover `vfxGood` / `Spark`. `ReleaseSparks` should `Destroy` sparks, not reparent. `OpenQte`/`FinishQte` already Hide; add Hide in `ClearUi`.

### L4 — PauseBoard vs Result / next battle (mostly safe, two holes)

- **Where:** `PauseBoard.Draw` L22–25 `OverlayDraw.Group(..., null, "PauseBoard")` — **new GO every Draw**, not in `_built`. `ToggleBattlePause` L199–205 Find+destroy only when resuming.
- **Safe path:** settle while paused cannot happen (`BattleSim.Tick` L354 returns if `Paused`). `Show(Result)` still `ClearUi`s the overlay.
- **Hole A:** `PauseBoard.Draw` does not reuse/find existing. Two `Draw` calls without destroy → **two** full-screen modal dims (duplicate `Find` resumes only the first).
- **Hole B:** `RepeatCurrentBattle` L213 Find+destroy then `StartBattleAt` → `ClearUi`. OK. If `Find` fails (rename), overlay still dies in `ClearUi`.
- **Fix:** `Draw` first `Find("PauseBoard")` and destroy; `ClearUi` also `Find`+destroy before wipe. Block `ToggleBattlePause` when `Outcome != InProgress`.

### L5 — `CanvasShake` continues across screens

- **Where:** component on Canvas (`GameRoot` L1461). `Punch` `CombatFeel.cs` L22–27. `ClearUi` does not zero `_t/_mag`.
- **Risk:** Drive/QTE punch still running when Result/Home appears; rematch inherits leftover shake.
- **Fix:** `ClearUi` / `Show` call a `CanvasShake.Stop()` that zeros `_t/_mag` and `localPosition`.

### L6 — Fever statics on Home-from-pause

- **Where:** `VfxRouter._combo` L12–13; `EndFever` only from HUD feverEnd L1133. `VfxFeverOverlay.Hide` L1129 only on that path.
- **Risk:** GO destroy clears `_live`/`Active`. Combo integers persist → next Fever `VfxComboBanner.Show` L99 continues the old count.
- **Fix:** `ClearUi` / `StartBattleAt` call `VfxRouter.EndFever(null)` (null-safe today L107) and `VfxFeverOverlay.Hide()`.

### L7 — `HitChainProbe` lines persist

- **Where:** `HitChainProbe.cs` L17–22 `Reset` only from smoke L89. `Input` L24–27.
- **Risk:** rematch / Home / Result reports append previous battle’s `in→num` lines.
- **Fix:** `HitChainProbe.Reset()` in `StartBattleAt` and `ClearUi`.

### L8 — `VfxShowtime.KillAll` is deferred `Destroy`

- **Where:** `VfxShowtime.cs` L48–56. HUD calls KillAll on QTE/Fever L748, L1112, L1765, L1876.
- **Risk:** `AnyLive()` L37–45 can still be true the same frame → Drive-select / tray copy (`RefreshDriveSelect` L507, portraits L1368) think SHOWTIME is up after it was killed.
- **Fix:** `DestroyImmediate` in `KillAll`, or `AnyLive` ignore `destroy`ing instances.

### L9 — Wave enemy `Destroy` vs new fighters (one frame)

- **Where:** `SpawnEnemies` L1960–1966 `Object.Destroy` (not Immediate), then create new fighters L1973–1985. `_built` still lists dead GOs.
- **Risk:** two enemy rows for one frame; old `focusHit` can still click (`TryFocusEnemy` on the old slot).
- **Fix:** `DestroyImmediate` old enemies; or remove from `_built` and disable `Button` before spawn.

### L10 — QTE layer conceptually after end (object teardown OK, logic not)

- **Where:** `GameRoot.Update` L242–252 `Settled` → `Show(Result)` → `ClearUi`. Coin GO dies.
- **Risk:** not a leftover **layer** if `ClearUi` runs. Residual is **late callback**: `VfxGoodButton` `OnClick` L174–191 nulls `_onPressed` after invoke; if click is in-flight the same frame as `Settled`, `FinishQte` still `ResolveDrive` + `ShowJudge` (`BattleHud` L774–783) on a battle that may already be `NotInProgress` — Q04 (judge/damage). After the input patch, `ResolveDriveChecked` / `Submit` reject and skip judge.
- **Fix:** apply `UI_INPUT_PATCH.md` Hunk I–J; `ClearUi` Hide coin first.

### L11 — Duplicate HUD (not observed)

- `DrawBattle` only from `Show(Battle)` after `ClearUi`. `RepeatCurrentBattle` / `StartVsBattle` go through `StartBattleAt` → `Show`. **No second HUD** unless someone calls `DrawBattle` without `Show` (no such caller).

### L12 — `_battle` kept after Home

- `Show(Home)` does not null `_battle`. `FeverOn`/`FeverSeen` (`GameRoot` L22–23) still true. Harmless for HUD (null). Confuses any debug that reads `GameRoot.Battle` on Home.
- **Fix:** optional `_battle = null` when leaving Battle/Result if rematch data is copied (`_activeStage` / `_resultTitle` already copied).

---

## 4. Ally-field `VfxShield` / `Barrier` hex — persist? per chip refresh?

**Does not spawn per status-chip refresh.**

| Caller | File:line | Spawns `VfxShield`? |
|---|---|---|
| `VfxRouter.OnBuff` | `VfxRouter.cs` L118–134 | **Yes** if label contains 格挡/护盾/屏障/`Barrier`/`Shield` |
| `VfxRouter.PlayCue` Buff | L46–47 | yes, via `OnBuff` |
| `BattleHud.DrainCombatLog` `FX ` lines | `BattleHud.cs` L1624–1634 | presentation `Buff` or `OnBuff(FxWord)` |
| `BattleHud.FxWord` | L2062–2067 | `"shield"` → `"Barrier"` → OnBuff hex |
| `VfxStatusIcons.Draw` | `BattleHud.RefreshPortraits` L1267 every tick | **No** — text chips only |
| `VfxStatusIcons.DrawField` | `BattleFighter.Apply` L308 every `Apply` | **No** |
| `StatusChipText` | `Barrier` for `EffectKind.Shield`/`Barrier` | label string only |

**Lifetime:** `VfxShield.Play` L23–37 **always `new GameObject`**. `Update` L67–116 uses `Time.unscaledDeltaTime` (runs while paused). `Destroy(gameObject)` when `_age >= Life` (`Life = 0.35f` L11, L116). No static cache. No disable path that would freeze `_age`.

**Can it persist past 0.35s?** Only if `Update` does not run (inactive GO). `Play` does not deactivate. Parent `ClearUi` destroys early. **No** stay-on-field hex bound to `StatusInst` duration (8s catalog shield is chips only).

**Can chip refresh spawn hexes?** **No.** Refreshing Barrier chips does not call `OnBuff`.

**Root cause of stacked hexes:** each combat-log `FX shield` / Buff cue calls `OnBuff` once (`_logCursor` advances L1612–1614). Multiple applies = multiple 0.35s flashes, not a leak. Home/`ClearUi` mid-flash destroys the GO.

**Proposed (only if design wants a persistent Barrier mesh):** bind one hex to the fighter while `UnitState.Shield > 0`, and keep `VfxShield.Play` as the apply flash. Do **not** call `Play` from `VfxStatusIcons`.

---

## 5. EventTrigger / coroutines (explicit)

- **EventTrigger:** none under battle HUD. Input is `PortraitGesture` (`IPointerDown/Up/Drag`) and `Button.onClick` (antiques, `focusHit`, Pause/Result chrome).
- **Coroutines:** none on `BattleHud` / `GameRoot` battle. Result LEVEL UP is `VfxLevelUpDelay.Update`, not `StartCoroutine`. Wave host is `LateUpdate`. Smoke/debug coroutines are out of U01 battle scope.

---

## 6. Suggested `ClearUi` teardown (for the GameRoot owner)

Apply with the input patch if touching `GameRoot.ClearUi` anyway:

```csharp
void ClearUi()
{
    VfxGoodButton.Hide();
    VfxShowtime.KillAll();
    VfxJudge.KillAll();
    VfxFeverOverlay.Hide();
    VfxTipPlate.Hide();
    VfxRouter.EndFever(null);
    VfxLevelUp.SkipLive();
    var delay = _canvas != null ? _canvas.GetComponent<VfxLevelUpDelay>() : null;
    if (delay != null) DestroyImmediate(delay);
    WavePreview.VisibleKind = WaveCueKind.None;
    WavePreview.VisibleTitle = "";
    HitChainProbe.Reset();
    if (CanvasShake.Live != null) { /* Stop() */ }
    // existing EmeraldLobby.ReleaseFrom + child DestroyImmediate + _built.Clear + _hud = null
}
```

`WavePreview.VisibleKind` setter is `internal` — same assembly (`Resonance.App`), legal from `GameRoot`.

---

## 7. U01 assertion map (for QA)

| After N rematch/Home cycles | Must be false / zero |
|---|---|
| `FindObjectsByType<BattleHud>` | n/a (not a Behaviour) — count `p0` portraits == party size, not `N * party` |
| `Find("PauseBoard")` on Result/Home | null |
| `Find("vfxGood")` active on Result/Home | null / inactive |
| `WavePreview.VisibleKind` off Battle | `None` |
| `Canvas.GetComponent<VfxLevelUpDelay>()` | null |
| `VfxFeverOverlay.Active` off Battle | false |
| `VfxRouter` combo (if exposed) | 0 |
| `HitChainProbe` lines after Reset | empty |
| Ally `vfxShield` count | 0 after 0.35s with no new FX log |
