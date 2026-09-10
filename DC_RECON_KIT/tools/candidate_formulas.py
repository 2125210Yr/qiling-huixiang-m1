"""Limited historical Destiny Child empirical candidates, NOT a production battle engine.

Python >=3.10, standard library only. Source IDs refer to ../sources.json.
Only unbuffed ordinary branches and the stated nonnegative input domain are supported.
All outputs are unrounded, before unknown final modifiers. No RNG distribution is assumed.
No profile here represents the final KR/JP/GL release. Failure is preferable to silent fallback.
"""
from __future__ import annotations
from dataclasses import dataclass
from enum import Enum
import math
from typing import Union

Number = Union[int, float]

class Profile(str, Enum):
    JP_LEGACY_EMPIRICAL = "JP_LEGACY_EMPIRICAL"
    KR_LEGACY_REPORTED = "KR_LEGACY_REPORTED"

@dataclass(frozen=True)
class Bounds:
    minimum: float
    maximum: float

@dataclass(frozen=True)
class FeverParts:
    base: float
    piercing: float
    extra: float

    @property
    def total(self) -> float:
        return self.base + self.piercing + self.extra


def _finite(name: str, value: Number, *, maximum: float | None = None) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise TypeError(f"{name} must be a real int/float, not bool or a string")
    try:
        out = float(value)
    except OverflowError as exc:
        raise ValueError(f"{name} is too large") from exc
    if not math.isfinite(out) or out < 0:
        raise ValueError(f"{name} must be finite and nonnegative")
    if maximum is not None and out > maximum:
        raise ValueError(f"{name} exceeds the supported research domain ({maximum})")
    return out


def _profile(profile: Profile | str) -> Profile:
    try:
        return Profile(profile)
    except (TypeError, ValueError) as exc:
        raise ValueError("Explicit historical JP/KR profile required; no final-version fallback") from exc


def _factor(element: Number, critical: bool) -> float:
    e = _finite("element", element)
    if e not in (0.7, 1.0, 1.4):
        raise ValueError("Only ordinary historical element factors 0.7, 1, 1.4 are supported")
    if not isinstance(critical, bool):
        raise TypeError("critical must be bool; buffed critical damage is outside this model")
    # Normalize the source's worked example: advantage+crit is 2.4, not 2.8.
    return e + int(critical)


def tap_base(*, skill_display: Number, equipment_attack: Number, defense: Number,
             profile: Profile | str, element: Number = 1.0, critical: bool = False) -> float:
    """S05/S07: ordinary Tap candidate only, before piercing/extra/final reduction."""
    p = _profile(profile)
    s = _finite("skill_display", skill_display)
    ae = _finite("equipment_attack", equipment_attack)
    d = _finite("defense", defense, maximum=20000)
    value = (400*s + 125*ae) / (400 + 0.15*d) * _factor(element, critical)
    if p is Profile.KR_LEGACY_REPORTED:
        value *= max(0.6, 1-0.00008*d)
    return _finite("computed tap", value)


def slide_bounds(*, skill_display: Number, equipment_attack: Number, defense: Number,
                 naked_agility: Number, equipment_agility: Number,
                 profile: Profile | str, element: Number = 1.0,
                 critical: bool = False) -> Bounds:
    """S06/S07: ordinary Slide support bounds, NOT a validated random distribution.

    The source did not separately establish every elemental/critical branch for Slide.
    Those arguments are explicit inherited candidate assumptions, not verified observations.
    Negative lower bounds are rejected; an original damage floor is not invented here.
    """
    p = _profile(profile)
    s = _finite("skill_display", skill_display)
    ae = _finite("equipment_attack", equipment_attack)
    d = _finite("defense", defense, maximum=20000)
    gn = _finite("naked_agility", naked_agility)
    ge = _finite("equipment_agility", equipment_agility)
    factor = _factor(element, critical) / (400+0.2*d)
    if p is Profile.KR_LEGACY_REPORTED:
        factor *= max(0.7, 1-0.00005*d)
    lo = (400*s+120*ae-40*gn)*factor
    hi = (400*s+120*ae+40*ge)*factor
    if lo < 0:
        raise ValueError("Candidate yielded a negative lower bound; unknown original floor cannot be assumed")
    return Bounds(_finite("computed minimum",lo), _finite("computed maximum",hi))


def piercing_unmodified(*, nominal: Number, defense: Number) -> float:
    """S08: historical approximate JP piercing contribution on DEF [0,20000].

    Not full-nominal true damage, not DEF subtraction, and not a general all-skills rule.
    """
    n = _finite("nominal",nominal)
    d = _finite("defense",defense,maximum=20000)
    return _finite("computed piercing",n*(0.6+0.4*d/20000))


def fever_components(*, resolved_noncritical_base: Number, resolved_piercing: Number,
                     extra_flat: Number, basic_repeat_branch: bool = False) -> FeverParts:
    """S09/S10: channel handling before target-side final modifiers.

    No buff/resistance/crit/rounding pipeline is supplied. The caller must resolve the
    correct ordinary or basic-repeat branch first; this helper cannot identify a skill.
    """
    base = _finite("resolved_noncritical_base",resolved_noncritical_base)
    pierce = _finite("resolved_piercing",resolved_piercing)
    extra = _finite("extra_flat",extra_flat)
    if not isinstance(basic_repeat_branch,bool):
        raise TypeError("basic_repeat_branch must be bool")
    return FeverParts(base*(1.0 if basic_repeat_branch else 0.6),pierce*0.6,extra)


def charge_time_speed_only(*, base_seconds: Number, rate_bonus: Number) -> float:
    """S11: speed-only candidate, NOT instant gauge add, charge-amount or cooldown reduction."""
    t = _finite("base_seconds",base_seconds)
    bonus = _finite("rate_bonus",rate_bonus)
    if t == 0:
        raise ValueError("base_seconds must be positive")
    return t/(1+bonus)
