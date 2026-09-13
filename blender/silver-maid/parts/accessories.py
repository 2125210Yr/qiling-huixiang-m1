"""Stool, headset, lacquer box, gold rifles, heels. Blender 5.2."""
from __future__ import annotations

import math

import bmesh
from mathutils import Euler, Matrix, Vector

_TAU = math.tau


def _mat(bpy, name, color, *, rough=0.3, metal=0.0, coat=0.0):
    hit = bpy.data.materials.get(name)
    if hit:
        return hit
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metal
    if coat and "Coat Weight" in bsdf.inputs:
        bsdf.inputs["Coat Weight"].default_value = coat
        if "Coat Roughness" in bsdf.inputs:
            bsdf.inputs["Coat Roughness"].default_value = 0.07
    return mat


def _xfm(loc, rot=(0.0, 0.0, 0.0), scale=(1.0, 1.0, 1.0)):
    return Matrix.LocRotScale(Vector(loc), Euler(rot), Vector(scale))


def _paint(bm, mi):
    for f in bm.faces:
        if not f.tag:
            f.material_index = mi
            f.tag = True


def _cyl(bm, r1, r2, depth, loc, rot=(0.0, 0.0, 0.0), segs=10, mi=0):
    if depth <= 1e-8:
        return
    bmesh.ops.create_cone(
        bm, cap_ends=True, cap_tris=False, segments=segs,
        radius1=r1, radius2=r2, depth=depth, matrix=_xfm(loc, rot),
    )
    _paint(bm, mi)


def _cube(bm, loc, xyz, rot=(0.0, 0.0, 0.0), mi=0):
    bmesh.ops.create_cube(bm, size=1.0, matrix=_xfm(loc, rot, xyz))
    _paint(bm, mi)


def _ball(bm, loc, r, div=1, mi=0):
    bmesh.ops.create_icosphere(bm, subdivisions=div, radius=r, matrix=_xfm(loc))
    _paint(bm, mi)


def _tube(bm, p0, p1, r, segs=8, mi=0, r2=None):
    a, b = Vector(p0), Vector(p1)
    vec = b - a
    length = vec.length
    if length < 1e-8:
        return
    mat = Matrix.LocRotScale((a + b) * 0.5, vec.normalized().to_track_quat("Z", "Y"), Vector((1, 1, 1)))
    bmesh.ops.create_cone(
        bm, cap_ends=True, cap_tris=False, segments=segs,
        radius1=r, radius2=r if r2 is None else r2, depth=length, matrix=mat,
    )
    _paint(bm, mi)


def _arc(bm, cent, ax, ay, r_maj, r_min, a0, a1, n, mi, segs=6):
    c, u, v = Vector(cent), Vector(ax), Vector(ay)
    pts = []
    for i in range(n + 1):
        t = a0 + (a1 - a0) * i / n
        pts.append(c + u * (math.cos(t) * r_maj) + v * (math.sin(t) * r_maj))
    for i in range(n):
        _tube(bm, pts[i], pts[i + 1], r_min, segs=segs, mi=mi)


def _torus(bm, loc, rot, R, r, n_maj, n_min, mi, angle=_TAU):
    start = _xfm(loc, rot) @ Matrix.Translation((R, 0.0, 0.0)) @ Matrix.Rotation(math.pi * 0.5, 4, "Y")
    ret = bmesh.ops.create_circle(bm, cap_ends=False, segments=n_min, radius=r, matrix=start)
    verts = list(ret["verts"])
    vset = set(verts)
    geom = verts[:]
    for e in bm.edges:
        if e.verts[0] in vset and e.verts[1] in vset:
            geom.append(e)
    axis = Euler(rot).to_matrix() @ Vector((0.0, 0.0, 1.0))
    full = abs(abs(angle) - _TAU) < 1e-3
    bmesh.ops.spin(
        bm, geom=geom, cent=Vector(loc), axis=axis,
        angle=angle, steps=n_maj, use_duplicate=False, use_merge=full,
    )
    _paint(bm, mi)


def _face(bm, verts):
    try:
        bm.faces.new(verts)
    except ValueError:
        pass


def _to_obj(bpy, bm, name, root, loc, rot, mats):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.update()
    me.shade_smooth()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.parent = root
    ob.location = loc
    ob.rotation_euler = rot
    for m in mats:
        me.materials.append(m)
    return ob


def _stool(bpy, root, metal):
    bm = bmesh.new()
    s, h, r = 0.16, 0.72, 0.007
    corners = [(x, y, z) for z in (0.0, -h) for y in (-s, s) for x in (-s, s)]
    for c in corners:
        _ball(bm, c, r * 1.2, div=1, mi=0)
    edges = ((0, 1), (1, 3), (3, 2), (2, 0), (4, 5), (5, 7), (7, 6), (6, 4), (0, 4), (1, 5), (2, 6), (3, 7))
    for i, j in edges:
        _tube(bm, corners[i], corners[j], r, segs=8, mi=0)
    return _to_obj(bpy, bm, "maid_stool", root, (0.0, 0.0, 0.0), (0.0, 0.0, 0.0), [metal])


def _headset(bpy, root, metal, gold):
    bm = bmesh.new()
    ry = math.pi * 0.5
    for sx in (-1.0, 1.0):
        p = (0.096 * sx, 0.018, -0.004)
        _cyl(bm, 0.027, 0.029, 0.016, p, (0.0, ry, 0.0), segs=16, mi=0)
        inner = (0.096 * sx - 0.011 * sx, 0.018, -0.004)
        _cyl(bm, 0.024, 0.021, 0.009, inner, (0.0, ry, 0.0), segs=16, mi=0)
        _torus(bm, inner, (0.0, ry, 0.0), 0.021, 0.0068, 14, 8, 0)
        _tube(bm, (0.090 * sx, 0.016, 0.012), (0.086 * sx, 0.012, 0.072), 0.004, segs=6, mi=0)
        _ball(bm, (0.088 * sx, 0.014, 0.040), 0.006, div=1, mi=1)
    _torus(bm, (0.0, 0.012, -0.006), (ry, 0.0, 0.0), 0.118, 0.0075, 18, 8, 0, angle=math.pi)
    bmesh.ops.create_icosphere(
        bm, subdivisions=2, radius=1.0,
        matrix=_xfm((0.0, 0.010, 0.116), (0.0, 0.0, 0.0), (0.026, 0.012, 0.010)),
    )
    _paint(bm, 0)
    boom = ((0.100, -0.016, -0.010), (0.082, -0.052, -0.032), (0.052, -0.092, -0.060), (0.026, -0.108, -0.074))
    for i in range(3):
        _tube(bm, boom[i], boom[i + 1], 0.0032, segs=8, mi=0)
    _ball(bm, boom[-1], 0.0075, div=2, mi=0)
    _cyl(bm, 0.0065, 0.0055, 0.011, boom[-1], (1.15, 0.15, 0.35), segs=8, mi=0)
    return _to_obj(bpy, bm, "maid_headset", root, (0.02, -0.03, 0.68), (0.0, 0.0, 0.0), [metal, gold])


def _box(bpy, root, lacquer, gold):
    bm = bmesh.new()
    w, d, h = 0.18, 0.12, 0.10
    hw, hd, hh = w * 0.5, d * 0.5, h * 0.5
    _cube(bm, (0.0, 0.0, 0.0), (w - 0.004, d - 0.004, h - 0.004), mi=0)
    _cube(bm, (0.0, 0.0, hh - 0.006), (w - 0.012, d - 0.012, 0.018), mi=0)
    for z in (-hh, hh):
        for y in (-hd, hd):
            for x in (-hw, hw):
                _cube(bm, (x, y, z), (0.018, 0.018, 0.018), mi=1)
    corners = [(x, y, z) for z in (-hh, hh) for y in (-hd, hd) for x in (-hw, hw)]
    for i, j in ((0, 1), (1, 3), (3, 2), (2, 0), (4, 5), (5, 7), (7, 6), (6, 4), (0, 4), (1, 5), (2, 6), (3, 7)):
        _tube(bm, corners[i], corners[j], 0.0032, segs=5, mi=1)
    for y in (-hd, hd):
        _cube(bm, (0.0, y, 0.016), (w + 0.002, 0.005, 0.016), mi=1)
    for x in (-hw, hw):
        _cube(bm, (x, 0.0, 0.016), (0.005, d + 0.002, 0.016), mi=1)
    for sx, sy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        _cyl(bm, 0.008, 0.006, 0.012, (sx * 0.068, sy * 0.042, -hh - 0.002), segs=8, mi=1)
        _ball(bm, (sx * 0.068, sy * 0.042, -hh - 0.010), 0.007, div=1, mi=1)
    _tube(bm, (-0.048, 0.0, hh), (-0.048, 0.0, hh + 0.016), 0.0055, segs=8, mi=1)
    _tube(bm, (0.048, 0.0, hh), (0.048, 0.0, hh + 0.016), 0.0055, segs=8, mi=1)
    _torus(bm, (0.0, 0.0, hh + 0.016), (math.pi * 0.5, 0.0, 0.0), 0.048, 0.0080, 16, 8, 1, angle=math.pi)
    _cube(bm, (0.0, -hd, 0.004), (0.070, 0.005, 0.034), mi=1)
    _cyl(bm, 0.014, 0.014, 0.007, (0.0, -hd - 0.001, 0.006), (math.pi * 0.5, 0.0, 0.0), segs=16, mi=1)
    _cyl(bm, 0.006, 0.006, 0.009, (0.0, -hd - 0.004, 0.006), (math.pi * 0.5, 0.0, 0.0), segs=12, mi=1)
    # front filigree: nested gold scrolls so the box reads as the still's ornate chest
    for sx in (-1.0, 1.0):
        _arc(bm, (sx * 0.028, -hd - 0.001, 0.004), (1.0, 0.0, 0.0), (0.0, 0.0, 1.0), 0.028, 0.0022, 0.35, math.pi - 0.35, 8, 1)
        _arc(bm, (sx * 0.022, -hd - 0.001, 0.004), (1.0, 0.0, 0.0), (0.0, 0.0, 1.0), 0.016, 0.0018, 0.4, math.pi - 0.4, 6, 1)
        _ball(bm, (sx * 0.048, -hd - 0.001, 0.022), 0.0045, div=1, mi=1)
        _ball(bm, (sx * 0.048, -hd - 0.001, -0.014), 0.0045, div=1, mi=1)
    _torus(bm, (0.0, -hd - 0.001, 0.004), (math.pi * 0.5, 0.0, 0.0), 0.012, 0.0020, 12, 6, 1)
    _cube(bm, (0.0, -hd, 0.024), (0.096, 0.005, 0.007), mi=1)
    _cube(bm, (0.0, -hd, -0.020), (0.096, 0.005, 0.007), mi=1)
    for sx in (-1.0, 1.0):
        _arc(bm, (sx * 0.038, -hd, 0.004), (0.0, 0.0, 1.0), (1.0, 0.0, 0.0), 0.016, 0.003, 0.2, math.pi - 0.2, 6, 1)
        _ball(bm, (sx * 0.058, -hd, 0.016), 0.006, div=1, mi=1)
        _ball(bm, (sx * 0.058, -hd, -0.012), 0.006, div=1, mi=1)
    return _to_obj(bpy, bm, "maid_box", root, (0.12, -0.18, 0.33), (0.08, 0.10, 0.32), [lacquer, gold])


def _rifle(bpy, root, gold, name, loc, rot):
    bm = bmesh.new()
    _tube(bm, (0.010, 0.0, 0.002), (0.046, 0.0, 0.002), 0.0023, segs=6, mi=0)
    _cyl(bm, 0.0034, 0.0030, 0.007, (0.050, 0.0, 0.002), (0.0, math.pi * 0.5, 0.0), segs=6, mi=0)
    _cube(bm, (0.000, 0.0, 0.001), (0.024, 0.008, 0.010), mi=0)
    _cube(bm, (-0.024, 0.0, -0.001), (0.026, 0.007, 0.012), (0.0, 0.18, 0.0), mi=0)
    _cube(bm, (0.004, 0.0, -0.010), (0.011, 0.005, 0.014), mi=0)
    _cube(bm, (0.010, 0.0, 0.008), (0.008, 0.003, 0.006), mi=0)
    _ball(bm, (-0.006, 0.0, -0.004), 0.003, div=1, mi=0)
    _arc(bm, (-0.002, 0.0, -0.006), (1.0, 0.0, 0.0), (0.0, 0.0, 1.0), 0.008, 0.0015, math.pi, _TAU, 5, 0, segs=4)
    return _to_obj(bpy, bm, name, root, loc, rot, [gold])


def _ring(bm, y, z0, z1, hw, n, sx):
    cz = 0.5 * (z0 + z1)
    rz = max(0.5 * (z1 - z0), 0.0012)
    vs = []
    for i in range(n):
        a = _TAU * i / n
        vs.append(bm.verts.new((hw * math.cos(a) * sx, y, cz + rz * math.sin(a))))
    return vs


def _bridge(bm, a, b):
    n = len(a)
    for i in range(n):
        j = (i + 1) % n
        _face(bm, (a[i], a[j], b[j], b[i]))


def _heel(bpy, root, metal, gold, name, loc, rot, sx):
    bm = bmesh.new()
    n = 12
    vamp = (
        (-0.122, 0.000, 0.005, 0.0028),
        (-0.102, 0.000, 0.022, 0.018),
        (-0.078, 0.001, 0.036, 0.030),
        (-0.048, 0.004, 0.042, 0.036),
        (-0.018, 0.008, 0.038, 0.038),
        (0.006, 0.012, 0.028, 0.036),
    )
    rings = [_ring(bm, y, z0, z1, hw, n, sx) for y, z0, z1, hw in vamp]
    for a, b in zip(rings, rings[1:]):
        _bridge(bm, a, b)
    _face(bm, rings[0])
    _paint(bm, 0)
    stations = (
        (-0.118, 0.004, 0.0036),
        (-0.085, 0.006, 0.026),
        (-0.045, 0.010, 0.036),
        (-0.005, 0.016, 0.038),
        (0.030, 0.036, 0.028),
        (0.058, 0.058, 0.022),
        (0.088, 0.080, 0.017),
    )
    na = 5
    top, bot = [], []
    for y, z, hw in stations:
        tv, bv = [], []
        for i in range(na):
            x = ((i / (na - 1)) * 2.0 - 1.0) * hw * sx
            tv.append(bm.verts.new((x, y, z)))
            bv.append(bm.verts.new((x * 0.96, y, z - 0.006)))
        top.append(tv)
        bot.append(bv)
    ns = len(stations)
    for k in range(ns - 1):
        for i in range(na - 1):
            _face(bm, (top[k][i], top[k][i + 1], top[k + 1][i + 1], top[k + 1][i]))
            _face(bm, (bot[k][i], bot[k + 1][i], bot[k + 1][i + 1], bot[k][i + 1]))
        _face(bm, (top[k][0], top[k + 1][0], bot[k + 1][0], bot[k][0]))
        _face(bm, (top[k][-1], bot[k][-1], bot[k + 1][-1], top[k + 1][-1]))
    for i in range(na - 1):
        _face(bm, (top[0][i], bot[0][i], bot[0][i + 1], top[0][i + 1]))
        _face(bm, (top[-1][i], top[-1][i + 1], bot[-1][i + 1], bot[-1][i]))
    _paint(bm, 0)
    ins = []
    for y, z, hw in stations[1:-1]:
        row = []
        for i in range(na):
            x = ((i / (na - 1)) * 2.0 - 1.0) * hw * 0.58 * sx
            row.append(bm.verts.new((x, y, z + 0.0018)))
        ins.append(row)
    for k in range(len(ins) - 1):
        for i in range(na - 1):
            _face(bm, (ins[k][i], ins[k][i + 1], ins[k + 1][i + 1], ins[k + 1][i]))
    _paint(bm, 1)
    _cyl(bm, 0.011, 0.010, 0.018, (0.0, 0.082, 0.072), (math.pi * 0.5, 0.0, 0.0), segs=8, mi=0)
    _tube(bm, (0.0, 0.084, 0.074), (0.0, 0.087, 0.038), 0.0062, segs=8, mi=0, r2=0.0038)
    _tube(bm, (0.0, 0.087, 0.038), (0.0, 0.090, 0.002), 0.0038, segs=8, mi=0, r2=0.0026)
    _ball(bm, (0.0, 0.090, 0.002), 0.0030, div=1, mi=0)
    return _to_obj(bpy, bm, name, root, loc, rot, [metal, gold])


def build(bpy, root):
    """Create accessories, parent to root, return dict name->object."""
    metal = _mat(bpy, "maid_metal_black", (0.03, 0.03, 0.035), rough=0.16, metal=0.96, coat=0.45)
    gold = _mat(bpy, "maid_gold", (0.72, 0.55, 0.18), rough=0.24, metal=1.0, coat=0.35)
    lacquer = _mat(bpy, "maid_box_lacquer", (0.012, 0.010, 0.009), rough=0.08, metal=0.18, coat=1.0)
    out = {}
    out["maid_stool"] = _stool(bpy, root, metal)
    out["maid_headset"] = _headset(bpy, root, metal, gold)
    out["maid_box"] = _box(bpy, root, lacquer, gold)
    out["maid_rifle_ornament_a"] = _rifle(
        bpy, root, gold, "maid_rifle_ornament_a", (0.11, -0.11, 0.155), (0.20, 0.55, -0.45),
    )
    out["maid_rifle_ornament_b"] = _rifle(
        bpy, root, gold, "maid_rifle_ornament_b", (-0.11, -0.11, 0.155), (0.20, -0.55, 3.55),
    )
    # Right foot is -X (facing -Y). Dropped left shoe at given floor point.
    out["maid_heel_R"] = _heel(
        bpy, root, metal, gold, "maid_heel_R",
        (-0.09, -0.20, -0.70), (0.55, 0.10, 0.25), 1.0,
    )
    out["maid_heel_L_dropped"] = _heel(
        bpy, root, metal, gold, "maid_heel_L_dropped",
        (0.05, -0.18, -0.70), (0.95, 1.15, -0.55), -1.0,
    )
    return out
