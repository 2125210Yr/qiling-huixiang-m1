"""Synthetic arithmetic/contract tests only. NOT real-game validation fixtures."""
import unittest
from candidate_formulas import (Profile, tap_base, slide_bounds, piercing_unmodified,
                                fever_components, charge_time_speed_only)
JP=Profile.JP_LEGACY_EMPIRICAL
KR=Profile.KR_LEGACY_REPORTED

class CandidateTests(unittest.TestCase):
    def test_tap_zero_defense(self):
        self.assertAlmostEqual(tap_base(skill_display=1000,equipment_attack=0,defense=0,profile=JP),1000)
    def test_tap_equipment_contribution(self):
        self.assertAlmostEqual(tap_base(skill_display=1000,equipment_attack=400,defense=0,profile=JP),1125)
    def test_advantage_critical_additive_example(self):
        self.assertAlmostEqual(tap_base(skill_display=1000,equipment_attack=0,defense=0,profile=JP,element=1.4,critical=True),2400)
    def test_neutral_critical(self):
        self.assertAlmostEqual(tap_base(skill_display=1000,equipment_attack=0,defense=0,profile=JP,critical=True),2000)
    def test_region_secondary_attenuation(self):
        kw=dict(skill_display=1000,equipment_attack=0,defense=5000)
        self.assertAlmostEqual(tap_base(**kw,profile=JP),400000/1150)
        self.assertAlmostEqual(tap_base(**kw,profile=KR),400000/1150*.6)
    def test_kr_tap_floor_not_negative_at_high_defense(self):
        kw=dict(skill_display=1000,equipment_attack=0,defense=20000)
        self.assertAlmostEqual(tap_base(**kw,profile=KR)/tap_base(**kw,profile=JP),.6)
    def test_slide_bounds_not_generic_percent_rng(self):
        b=slide_bounds(skill_display=1000,equipment_attack=400,defense=0,naked_agility=1000,equipment_agility=200,profile=JP)
        self.assertAlmostEqual(b.minimum,1020)
        self.assertAlmostEqual(b.maximum,1140)
    def test_kr_slide_floor(self):
        kw=dict(skill_display=1000,equipment_attack=400,defense=6000,naked_agility=1000,equipment_agility=200)
        jp=slide_bounds(**kw,profile=JP); kr=slide_bounds(**kw,profile=KR)
        self.assertAlmostEqual(kr.minimum,jp.minimum*.7)
        self.assertAlmostEqual(kr.maximum,jp.maximum*.7)
    def test_piercing_domain_examples(self):
        for defense, expected in [(0,600),(10000,800),(20000,1000)]:
            with self.subTest(defense=defense):
                self.assertAlmostEqual(piercing_unmodified(nominal=1000,defense=defense),expected)
    def test_piercing_rejects_extrapolation(self):
        with self.assertRaises(ValueError): piercing_unmodified(nominal=1000,defense=20001)
    def test_fever_separate_channels(self):
        p=fever_components(resolved_noncritical_base=1000,resolved_piercing=100,extra_flat=50)
        self.assertAlmostEqual(p.base,600);self.assertAlmostEqual(p.piercing,60)
        self.assertAlmostEqual(p.extra,50);self.assertAlmostEqual(p.total,710)
    def test_fever_basic_repeat_exception(self):
        p=fever_components(resolved_noncritical_base=1000,resolved_piercing=100,extra_flat=50,basic_repeat_branch=True)
        self.assertAlmostEqual(p.total,1110)
    def test_speed_is_division_not_subtraction(self):
        self.assertAlmostEqual(charge_time_speed_only(base_seconds=14,rate_bonus=.5),14/1.5)
        self.assertNotEqual(charge_time_speed_only(base_seconds=14,rate_bonus=.5),7)
    def test_reject_unverified_profile(self):
        for profile in [None,'GL_FINAL_VERIFIED','JP_FINAL_VERIFIED']:
            with self.subTest(profile=profile),self.assertRaises(ValueError):
                tap_base(skill_display=1,equipment_attack=0,defense=0,profile=profile)
    def test_reject_invalid_numbers(self):
        for value in [float('nan'),float('inf'),-1,True,'1000']:
            with self.subTest(value=value),self.assertRaises((TypeError,ValueError)):
                tap_base(skill_display=value,equipment_attack=0,defense=0,profile=JP)
    def test_reject_unknown_damage_floor(self):
        with self.assertRaises(ValueError):
            slide_bounds(skill_display=1,equipment_attack=0,defense=0,naked_agility=1000,equipment_agility=0,profile=JP)
    def test_reject_buffed_element_factor(self):
        with self.assertRaises(ValueError):
            tap_base(skill_display=1000,equipment_attack=0,defense=0,profile=JP,element=2)
    def test_outputs_are_not_silently_rounded(self):
        v=tap_base(skill_display=1000,equipment_attack=0,defense=1318,profile=JP)
        self.assertNotEqual(v,int(v))

if __name__=='__main__': unittest.main(verbosity=2)
