# Destiny Child damage formulas (GameKee 52136)

These are **player-tested international-server formulas**, not official source. Tag content rows `source=gamekee:52136`.

## Tap / Fever (TS / FT)

```
[(extraAtk * 125 + skillDmg * 400) * elemCrit / (0.15 * def + 400) + truePierce]
  * extraDmgMul * feverMul
  + bonusHit * (crit ? 2 : 1)
  + enchantPlusCarta
```

- `feverMul` is **0.6** during Fever, else 1.
- Extra-damage multiplier floor is **0.1**.
- `truePierce = pierce * 0.6 + pierce * 0.4 * def / 20000`.

Worked example used by tests:

- extraAtk=1000, skillDmg=2707, def=2500, elemCrit=1.4
- core = (125000 + 1082800) * 1.4 / 775 = 1690920 / 775 = 2181.832 → **2182**

(The wiki post’s intermediate 1,232,800 was arithmetic slip; the structure of the formula is unchanged.)

Advantage crit uses elemCrit **2.4** → **3740** on the same sample.

## Slide (SS)

Same shape with `120` and denominator `0.2 * def + 400`. Agility term is still incompletely measured.

## Element × crit table

| | no crit | crit |
|---|---:|---:|
| advantage | 1.4 | 2.4 |
| neutral | 1.0 | 2.0 |
| disadvantage | 0.7 | 1.7 |

Fire > Wood > Water > Fire. Light ↔ Dark.

## Slice mapping (`DamageMath.Compute`)

Vertical-slice skills still store `atk * coef + flat`. That sum is fed in as `skillDmg` with `extraAtk = 0`. Fever hits keep using `coef * 0.60` rather than a second `feverMul`.

## Raid / World Boss extras (not in slice)

- Battlefield: +0.4 extraDmg when advantage (WB/Raid); 0 in Ragna Burst.
- Designer: −0.8 (or −0.7) extraDmg when not hitting weakness.
- Special: 0.8 / 0.9 when a light/dark unit hits a fire/water/wood boss (crit / no-crit).

## Ignition (red stats)

Red ATK `A` on TS:

```
extraAtk * 125 * (1 + 0.0015A) + baseAtk * 125 * 0.0015A
```

Red CRT `B` (on crit) and red AGL `C` (on weakness) add `0.003B + 0.002C` into extraDmgMul.

## Percent skills

```
(extraAtk + baseAtk) * 125 * percent * elemCrit / (0.15 * def + 400)
```

## Overlay

Same buff icon: Drive > Slide > Tap. Later same-tier overwrites earlier. Enchant and Soul Carta add after the main product and do not buff each other.
