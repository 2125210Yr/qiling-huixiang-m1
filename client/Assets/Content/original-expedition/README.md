# Original Expedition content v0.1.0

This directory describes the synthetic content compiled from
`Assets/Scripts/Resonance.Battle/Expedition/ExpeditionContent.cs`.
There is no runtime network or original-game catalog dependency. The authoritative
numbers are the compiled definitions; editing this document does not change them.

The content version is `original-expedition-content-v0.1.0`, ruleset
`original-expedition-v01`. The runtime computes an invariant SHA-256 over all
character, skill, effect, stage, preset, relic, boss and clock definitions. Every
battle opening freezes those definitions, the party's actual HP, growth, seed and
relic IDs in its own deep copy. A saved checkpoint contains its resolved inputs.

## Numerical source

All new values are **DESIGN_CANDIDATE_V1**, proposed for this prototype from
ORIGINAL-EXPEDITION-MVP v0.1 sections 5–8. They are not measured original-game
values or a claim of completed balance. The running formula is the already
implemented `JP_LEGACY_EMPIRICAL` branch, explicitly adopted as the prototype's
mathematics. No new opcode or formula profile is invented.

| Fixed member | Original ID | Max HP | ATK | Primary charge | Main action |
|---|---|---:|---:|---:|---|
| 烬羽 | OE_POINT | 5200 | 700 | 7 s | Single hit, 1.6 ATK + 120 |
| 砚盾 | OE_GUARD | 6800 | 420 | 9 s | Team shield, 22% of each member Max HP for 8 s |
| 清弦 | OE_HEALER | 5600 | 360 | 8 s | Heal living team members, 15% Max HP + 80 |
| 逐锋 | OE_BLADE | 5000 | 720 | 6.5 s | Single hit, 1.5 ATK + 110 |
| 引灯 | OE_GUIDE | 5400 | 460 | 8 s | Team ATK +20% for 7 s |

The first-clear `sweep` preset changes only 烬羽's main action: up to two targets
at 0.8 ATK + 60 each. Its total coefficient and flat budget equal `single`;
against one target it is weaker. No persistent stat budget is added. Other
members and automatic single attacks still support the B relic family.

Enemy archetypes are 缄默卫士 (durable single attacker), 失音唱偶 (fragile single
attacker), and 执谱监场 (elite area attacker). N2 exposes two mutually exclusive
encounters: two durable enemies, or four fragile ones. N4 has one elite and two
fragile attendants. N5 provides four targets after the N4 amplifier opportunity.

The boss opens in slot 0 at 45,000 HP with two 7,000 HP masks in fixed slots 1/2.
Its frozen configuration declares first warning 9 s, cast duration 4 s, normal
attack interval 3 s, and warning intervals 10/8 s. Each living mask adds 0.5 to
the area-damage multiplier at resolution. Phase two starts at 50% HP. The
scheduler and exact relic resolution are supplied by the battle implementation;
static definitions alone are not evidence that those behaviors ran.

## Asset provenance and boundaries

All OE_* names and stat tables are new synthetic placeholder content. These
definitions contain no image/audio/model path and do not reuse C001… IDs. No
reference-game image was copied into this directory. The original-mode UI must
use identity-correct synthetic placeholders where art is absent. Existing
`Assets/Resources/Art/Characters/C*` assets are outside this content set and need
an explicit package exclusion or verified distribution provenance when building
the new standalone delivery.

## Validation

`tools/BattleSim.Tests/OriginalExpeditionContentTests.cs` checks the isolated
content, prerequisite reachability, five-slot HP inputs, deep copies, equivalent
preset budgets, serialized checkpoint inputs and numerical fingerprint changes.
Executed results belong in the integration evidence directory; this document
does not claim a test run, natural play, or gameplay balance result.
