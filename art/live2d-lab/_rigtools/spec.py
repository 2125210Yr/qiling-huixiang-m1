"""Parse the rig's four source-of-truth files into one model.

A Live2D rig in this lab is described by four files that must agree with each
other, and nothing currently checks that they do:

    rig/parameters.md          parameter table  (Id, range, default, driver)
    rig/physics_groups.json    physics spec     (Cubism physics3 shape + comments)
    psd_cut_plan.json          layer plan       (34 layers, pivots, underpaint)
    <name>.pose3.json          part groups      (Ids referenced by name)

Every function here is read-only.  `validate.py` is what reports on them.
"""
from __future__ import annotations

import json
import re
from dataclasses import dataclass, field
from pathlib import Path

# ---------------------------------------------------------------- parameters

# | `ParamAngleX` | 头水平转 | -30 .. 30 | 0 | 3 | 追踪 | 正 = 转向画面右… |
# | `ParamHairSideL` / `R` | 侧发根 | … |          <- shorthand for two ids
_ROW = re.compile(r"^\|(?P<idcell>[^|]+)\|(?P<rest>.*)$")
_ANY_PARAM = re.compile(r"`(Param[A-Za-z0-9_]+)`")
_TAIL_TOKEN = re.compile(r"`([A-Z][A-Za-z0-9_]*)`")
# a range cell looks like  -30 .. 30  /  0 .. 2  /  -1 .. 1
_RANGE = re.compile(r"(-?\d+(?:\.\d+)?)\s*\.\.\s*(-?\d+(?:\.\d+)?)")


def expand_id_cell(cell: str) -> list[str]:
    """`ParamHairSideL` / `R`  ->  ParamHairSideL, ParamHairSideR

    The table uses a shorthand where a trailing bare token replaces the tail of
    the preceding id: `ParamHairSideLSub` / `RSub` -> ...RSub.
    """
    ids: list[str] = []
    stem: str | None = None
    for frag in cell.split("/"):
        m = _ANY_PARAM.search(frag)
        if m:
            stem = m.group(1)
            ids.append(stem)
            continue
        t = _TAIL_TOKEN.search(frag)
        if t and stem:
            suf = t.group(1)
            ids.append(stem[: len(stem) - len(suf)] + suf if len(suf) <= len(stem)
                       else stem + suf)
    return ids


@dataclass
class Param:
    id: str
    label: str
    lo: float | None
    hi: float | None
    default: float | None
    driver: str
    note: str
    line: int

    @property
    def drivable_by_physics(self) -> bool:
        return "物理输出" in self.driver

    @property
    def hand_keyed(self) -> bool:
        d = self.driver
        return ("手 K" in d) or ("姿势" in d) or ("演出" in d)


def parse_parameters_md(path: Path) -> dict[str, Param]:
    """Pull the parameter table out of parameters.md.

    Only table rows whose first cell is a `Param*` id count; prose and the
    other tables in the file are ignored.
    """
    out: dict[str, Param] = {}
    for i, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        m = _ROW.match(raw.strip())
        if not m:
            continue
        ids = expand_id_cell(m.group("idcell"))
        if not ids:
            continue
        cells = [c.strip() for c in m.group("rest").split("|")]
        label = cells[0] if len(cells) > 0 else ""
        rng = cells[1] if len(cells) > 1 else ""
        default = cells[2] if len(cells) > 2 else ""
        driver = cells[4] if len(cells) > 4 else ""
        note = cells[5] if len(cells) > 5 else ""

        rm = _RANGE.search(rng)
        lo = float(rm.group(1)) if rm else None
        hi = float(rm.group(2)) if rm else None
        try:
            dv = float(re.sub(r"[^\d.\-]", "", default)) if default else None
        except ValueError:
            dv = None
        for pid in ids:
            out[pid] = Param(pid, label, lo, hi, dv, driver, note, i)
    return out


# ------------------------------------------------------------------- physics

@dataclass
class PhysicsGroup:
    id: str
    name: str
    comment: str
    inputs: list[dict]
    outputs: list[dict]
    vertices: list[dict]
    norm_position: dict | None
    norm_angle: dict | None
    order: int

    def input_ids(self) -> list[str]:
        return [i["Source"]["Id"] for i in self.inputs]

    def output_ids(self) -> list[str]:
        return [o["Destination"]["Id"] for o in self.outputs]


def parse_physics(path: Path) -> tuple[dict, list[PhysicsGroup]]:
    d = json.loads(path.read_text(encoding="utf-8"))
    names = {e["Id"]: e["Name"] for e in d["Meta"].get("PhysicsDictionary", [])}
    groups = []
    for i, s in enumerate(d["PhysicsSettings"]):
        groups.append(PhysicsGroup(
            id=s["Id"],
            name=names.get(s["Id"], s["Id"]),
            comment=s.get("Comment", ""),
            inputs=s.get("Input", []),
            outputs=s.get("Output", []),
            vertices=s.get("Vertices", []),
            norm_position=s.get("Normalization", {}).get("Position"),
            norm_angle=s.get("Normalization", {}).get("Angle"),
            order=i,
        ))
    return d, groups


# -------------------------------------------------------------- layer plan

def parse_cut_plan(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def parse_pose3(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


# ------------------------------------------------------------- convention

def layer_to_part(layer: str) -> str:
    """Cubism ArtMesh naming for a cut-plan layer.

    The lab's own files use `PartHairBackL`-style ids (see the smoke pack's
    layer-manifest and elevator_red.pose3.json), so the mapping is mechanical:
    snake_case -> PascalCase with a `Part` prefix.
    """
    return "Part" + "".join(w[:1].upper() + w[1:] for w in layer.split("_"))


@dataclass
class Rig:
    root: Path
    name: str
    params: dict[str, Param] = field(default_factory=dict)
    physics_raw: dict = field(default_factory=dict)
    physics: list[PhysicsGroup] = field(default_factory=list)
    cut_plan: dict = field(default_factory=dict)
    pose3: dict = field(default_factory=dict)

    @classmethod
    def load(cls, model_dir: Path, name: str) -> "Rig":
        # sibling files use the underscored stem (elevator_red.*) while the
        # folder is hyphenated (elevator-red), so try both spellings
        stems = [name, name.replace("-", "_"), name.replace("_", "-")]

        r = cls(root=model_dir, name=name)
        pm = model_dir / "rig" / "parameters.md"
        if pm.exists():
            r.params = parse_parameters_md(pm)

        # prefer the authored spec; fall back to whatever was built
        spec = model_dir / "rig" / "physics_groups.json"
        built = next((model_dir / f"{s}.physics3.json" for s in stems
                      if (model_dir / f"{s}.physics3.json").exists()), None)
        r.physics_spec_src = spec if spec.exists() else None
        r.physics_built_src = built
        src = r.physics_spec_src or built
        if src:
            r.physics_raw, r.physics = parse_physics(src)

        cp = model_dir / "psd_cut_plan.json"
        if cp.exists():
            r.cut_plan = parse_cut_plan(cp)
        for s in stems:
            p3 = model_dir / f"{s}.pose3.json"
            if p3.exists():
                r.pose3 = parse_pose3(p3)
                break
        return r
