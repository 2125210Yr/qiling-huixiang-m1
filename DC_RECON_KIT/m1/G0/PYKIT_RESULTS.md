# M1-G2-PYKIT — Python 参照器自洽

日期：2026-09-10  
仓库：`F:\天命之子`  
cwd：`F:\天命之子`  
命令：`python -m unittest discover -s DC_RECON_KIT/tools -p test_*.py -v`  
exit：`0`  
套件结果：**OK**（18 tests）

**只证明参照器自洽，不是原作一致。**  
未改 Unity。未跑 PlayMode / EditMode / `dotnet test`。未宣称还原分 / T27 / `GL_FINAL_VERIFIED`。失败才会记 BLOCKED；本轮套件未失败。

## 原始输出

```
test_advantage_critical_additive_example (test_candidate_formulas.CandidateTests.test_advantage_critical_additive_example) ... ok
test_fever_basic_repeat_exception (test_candidate_formulas.CandidateTests.test_fever_basic_repeat_exception) ... ok
test_fever_separate_channels (test_candidate_formulas.CandidateTests.test_fever_separate_channels) ... ok
test_kr_slide_floor (test_candidate_formulas.CandidateTests.test_kr_slide_floor) ... ok
test_kr_tap_floor_not_negative_at_high_defense (test_candidate_formulas.CandidateTests.test_kr_tap_floor_not_negative_at_high_defense) ... ok
test_neutral_critical (test_candidate_formulas.CandidateTests.test_neutral_critical) ... ok
test_outputs_are_not_silently_rounded (test_candidate_formulas.CandidateTests.test_outputs_are_not_silently_rounded) ... ok
test_piercing_domain_examples (test_candidate_formulas.CandidateTests.test_piercing_domain_examples) ... ok
test_piercing_rejects_extrapolation (test_candidate_formulas.CandidateTests.test_piercing_rejects_extrapolation) ... ok
test_region_secondary_attenuation (test_candidate_formulas.CandidateTests.test_region_secondary_attenuation) ... ok
test_reject_buffed_element_factor (test_candidate_formulas.CandidateTests.test_reject_buffed_element_factor) ... ok
test_reject_invalid_numbers (test_candidate_formulas.CandidateTests.test_reject_invalid_numbers) ... ok
test_reject_unknown_damage_floor (test_candidate_formulas.CandidateTests.test_reject_unknown_damage_floor) ... ok
test_reject_unverified_profile (test_candidate_formulas.CandidateTests.test_reject_unverified_profile) ... ok
test_slide_bounds_not_generic_percent_rng (test_candidate_formulas.CandidateTests.test_slide_bounds_not_generic_percent_rng) ... ok
test_speed_is_division_not_subtraction (test_candidate_formulas.CandidateTests.test_speed_is_division_not_subtraction) ... ok
test_tap_equipment_contribution (test_candidate_formulas.CandidateTests.test_tap_equipment_contribution) ... ok
test_tap_zero_defense (test_candidate_formulas.CandidateTests.test_tap_zero_defense) ... ok

----------------------------------------------------------------------
Ran 18 tests in 0.001s

OK
```
