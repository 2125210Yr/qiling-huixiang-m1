"""Black high-collar maid dress + tights. Sitting, stool top at local Z=0."""

import bmesh
import math

from mathutils import Euler, Matrix, Vector

YAW = 0.0  # MaidRoot already yaws +18°; extra dress yaw double-rotates.


def _sock(pr, name, value):
    if name in pr.inputs:
        try:
            pr.inputs[name].default_value = value
        except Exception:
            pass


def _mat(bpy, name, color, rough, metallic=0.0, extra=None):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (color[0], color[1], color[2], 1.0)
    pr = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    _sock(pr, "Base Color", (color[0], color[1], color[2], 1.0))
    _sock(pr, "Roughness", rough)
    _sock(pr, "Metallic", metallic)
    if extra:
        for key, val in extra.items():
            _sock(pr, key, val)
    return mat


def _stitch(bm, rings, wrap=False):
    n, segs = len(rings), len(rings[0])
    last = n if wrap else n - 1
    for k in range(last):
        a, b = rings[k], rings[(k + 1) % n]
        for i in range(segs):
            j = (i + 1) % segs
            try:
                bm.faces.new((a[i], a[j], b[j], b[i]))
            except ValueError:
                pass


def _drape(x, y, z):
    """Pancake the hem onto the stool (local z=0 / world Z=0.72); front spills off the edge."""
    if z >= 0.10:
        return x, y, z
    t = min(1.0, (0.10 - z) / 0.12)
    z = z * (1.0 - 0.78 * t) + 0.004 * t
    x *= 1.0 + 0.12 * t
    y *= 1.0 + 0.03 * t
    if y < 0.0:
        z += 0.05 * y * t
    else:
        z = max(z, 0.003)
    return x, y, z


def _add_lathe(bm, profile, segs=44, drape=False):
    rings = []
    for item in profile:
        r, z = item[0], item[1]
        wn = item[2] if len(item) > 2 else 0
        wa = item[3] if len(item) > 3 else 0.0
        ring = []
        for i in range(segs):
            a = (i / segs) * math.tau
            rr = r + wa * math.sin(wn * a) + 0.3 * wa * math.sin(2.0 * wn * a + 0.8)
            zz = z + 0.4 * wa * math.cos(wn * a + 0.5)
            x, y = rr * math.cos(a), rr * math.sin(a)
            if drape:
                x, y, zz = _drape(x, y, zz)
            ring.append(bm.verts.new((x, y, zz)))
        rings.append(ring)
    _stitch(bm, rings, wrap=False)


def _add_torus(bm, R, r, z=0.0, segs_R=40, segs_r=8, wave_n=0, wave_a=0.0, matrix=None, drape=False):
    if matrix is None:
        matrix = Matrix.Translation((0.0, 0.0, z))
    rings = []
    for i in range(segs_R):
        u = (i / segs_R) * math.tau
        Rm = R + wave_a * math.sin(wave_n * u)
        ring = []
        for j in range(segs_r):
            v = (j / segs_r) * math.tau
            x = (Rm + r * math.cos(v)) * math.cos(u)
            y = (Rm + r * math.cos(v)) * math.sin(u)
            zz = r * math.sin(v) + 0.35 * wave_a * math.cos(wave_n * u)
            p = matrix @ Vector((x, y, zz))
            if drape:
                p.x, p.y, p.z = _drape(p.x, p.y, p.z)
            ring.append(bm.verts.new(p))
        rings.append(ring)
    _stitch(bm, rings, wrap=True)


def _add_tube(bm, p0, p1, r0, r1, segs=12, caps=True):
    p0, p1 = Vector(p0), Vector(p1)
    axis = p1 - p0
    length = axis.length
    if length < 1e-7:
        return
    z = axis / length
    tmp = Vector((0.0, 0.0, 1.0)) if abs(z.z) < 0.93 else Vector((1.0, 0.0, 0.0))
    x = tmp.cross(z)
    x.normalize()
    y = z.cross(x)
    rings = []
    for s in range(4):
        t = s / 3.0
        c = p0.lerp(p1, t)
        rad = r0 * (1.0 - t) + r1 * t
        ring = []
        for i in range(segs):
            a = (i / segs) * math.tau
            ring.append(bm.verts.new(c + rad * (math.cos(a) * x + math.sin(a) * y)))
        rings.append(ring)
    _stitch(bm, rings, wrap=False)
    if caps:
        for ring, center, flip in ((rings[0], p0, True), (rings[-1], p1, False)):
            cv = bm.verts.new(center)
            n = len(ring)
            for i in range(n):
                j = (i + 1) % n
                tri = (cv, ring[j], ring[i]) if flip else (cv, ring[i], ring[j])
                try:
                    bm.faces.new(tri)
                except ValueError:
                    pass


def _add_sphere(bm, loc, radii, u=14, v=8, rot=None):
    if isinstance(radii, (int, float)):
        radii = (radii, radii, radii)
    mat = Matrix.Translation(Vector(loc))
    if rot is not None:
        mat = mat @ Euler(rot, "XYZ").to_matrix().to_4x4()
    mat = mat @ Matrix.Diagonal((radii[0], radii[1], radii[2], 1.0))
    rings = []
    for vi in range(1, v):
        pol = math.pi * vi / v
        sp, cp = math.sin(pol), math.cos(pol)
        ring = []
        for ui in range(u):
            az = (ui / u) * math.tau
            ring.append(bm.verts.new(mat @ Vector((sp * math.cos(az), sp * math.sin(az), cp))))
        rings.append(ring)
    pn = bm.verts.new(mat @ Vector((0.0, 0.0, 1.0)))
    ps = bm.verts.new(mat @ Vector((0.0, 0.0, -1.0)))
    _stitch(bm, rings, wrap=False)
    for i in range(u):
        j = (i + 1) % u
        try:
            bm.faces.new((pn, rings[0][i], rings[0][j]))
            bm.faces.new((ps, rings[-1][j], rings[-1][i]))
        except ValueError:
            pass


def _add_box(bm, loc, size, rot_z=0.0, rot_x=0.0):
    mat = (
        Matrix.Translation(Vector(loc))
        @ Matrix.Rotation(rot_z, 4, "Z")
        @ Matrix.Rotation(rot_x, 4, "X")
        @ Matrix.Diagonal((size[0], size[1], size[2], 1.0))
    )
    bmesh.ops.create_cube(bm, size=1.0, matrix=mat)


def _finish(bpy, root, name, bm, mat, sub=1, solid=0.0):
    try:
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    except Exception:
        pass
    yaw = Matrix.Rotation(YAW, 4, "Z")
    for v in bm.verts:
        v.co = yaw @ v.co
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.update()
    if hasattr(me, "shade_smooth"):
        me.shade_smooth()
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = root
    if mat is not None:
        me.materials.append(mat)
    if solid:
        md = obj.modifiers.new("solidify", "SOLIDIFY")
        md.thickness = solid
        md.offset = 1.0
    if sub:
        md = obj.modifiers.new("subsurf", "SUBSURF")
        md.levels = sub
        md.render_levels = min(sub + 1, 3)
    return obj


def _polar(radius, ang_front, z):
    a = -math.pi * 0.5 + ang_front
    return (radius * math.cos(a), radius * math.sin(a), z)


def _bell(z0, z1, r0, r1, steps=9, waves=0, amp=0.0):
    prof = []
    for i in range(steps):
        t = i / (steps - 1)
        u = t * t * (3.0 - 2.0 * t)
        w = amp * max(0.0, (t - 0.68) / 0.32) if t > 0.68 else 0.0
        wn = waves if w > 0.0 else 0
        prof.append((r0 + (r1 - r0) * u, z0 + (z1 - z0) * t, wn, w))
    return prof


def _sculpt_chest(bm):
    """Flatten the bust into a closed high-neck plate (no cleavage)."""
    for v in bm.verts:
        x, y, z = v.co.x, v.co.y, v.co.z
        if 0.27 < z < 0.44 and y < 0.0:
            w = math.sin((z - 0.27) / 0.17 * math.pi)
            v.co.y *= 1.0 - 0.22 * w
            v.co.y -= 0.012 * w
            if abs(x) < 0.04:
                v.co.y -= 0.01 * w


def build(bpy, root):
    """Create objects, parent to `root` (Empty named MaidRoot). Return dict name->object."""
    cloth = _mat(
        bpy,
        "maid_black",
        (0.02, 0.02, 0.025),
        0.35,
        extra={
            "Specular IOR Level": 0.45,
            "Sheen Weight": 0.32,
            "Sheen Roughness": 0.42,
            "Sheen Tint": (0.06, 0.06, 0.07, 1.0),
        },
    )
    leather = _mat(
        bpy,
        "maid_leather",
        (0.02, 0.02, 0.025),
        0.22,
        extra={
            "Specular IOR Level": 0.55,
            "Coat Weight": 0.12,
            "Coat Roughness": 0.20,
            "Coat Tint": (1.0, 1.0, 1.0, 1.0),
        },
    )
    gold = _mat(
        bpy,
        "maid_gold",
        (0.72, 0.55, 0.18),
        0.25,
        metallic=1.0,
        extra={"Coat Weight": 0.18, "Coat Roughness": 0.12, "Specular IOR Level": 1.0},
    )

    out = {}

    bm = bmesh.new()
    _add_lathe(
        bm,
        [
            (0.100, 0.392),
            (0.072, 0.418),
            (0.056, 0.445),
            (0.050, 0.472),
            (0.049, 0.500),
            (0.053, 0.520),
            (0.050, 0.528),
        ],
        segs=36,
    )
    _add_torus(bm, 0.053, 0.008, z=0.524, segs_R=28, segs_r=8)
    _add_torus(bm, 0.051, 0.006, z=0.500, segs_R=24, segs_r=6)
    out["maid_collar"] = _finish(bpy, root, "maid_collar", bm, leather, sub=1, solid=0.003)

    bm = bmesh.new()
    _add_lathe(
        bm,
        [
            (0.128, 0.172),
            (0.114, 0.192),
            (0.106, 0.212),
            (0.112, 0.245),
            (0.122, 0.288),
            (0.130, 0.328),
            (0.132, 0.358),
            (0.126, 0.388),
            (0.110, 0.414),
            (0.092, 0.434),
            (0.070, 0.450),
        ],
        segs=40,
    )
    _sculpt_chest(bm)
    _add_sphere(bm, (0.150, 0.000, 0.446), (0.080, 0.076, 0.066), u=16, v=10)
    _add_sphere(bm, (-0.150, 0.000, 0.446), (0.080, 0.076, 0.066), u=16, v=10)
    out["maid_bodice"] = _finish(bpy, root, "maid_bodice", bm, leather, sub=1, solid=0.004)

    bm = bmesh.new()
    el_r, wr_r = (0.412, -0.047, 0.319), (0.501, -0.270, 0.248)
    el_l, wr_l = (-0.397, -0.088, 0.314), (-0.166, -0.181, 0.290)
    _add_sphere(bm, (0.168, 0.000, 0.430), (0.078, 0.070, 0.068), u=16, v=10)
    _add_sphere(bm, (-0.168, 0.000, 0.430), (0.078, 0.070, 0.068), u=16, v=10)
    _add_tube(bm, (0.185, 0.008, 0.438), el_r, 0.042, 0.032, segs=14)
    _add_tube(bm, el_r, wr_r, 0.032, 0.024, segs=14)
    _add_tube(bm, (-0.185, 0.008, 0.438), el_l, 0.042, 0.032, segs=14)
    _add_tube(bm, el_l, wr_l, 0.032, 0.024, segs=14)
    for wr, el in ((wr_r, el_r), (wr_l, el_l)):
        d = (Vector(wr) - Vector(el)).normalized()
        _add_tube(bm, wr, Vector(wr) + d * 0.028, 0.029, 0.029, segs=10)
    out["maid_sleeves"] = _finish(bpy, root, "maid_sleeves", bm, leather, sub=1, solid=0.002)

    bm = bmesh.new()
    _add_lathe(
        bm,
        [
            (0.108, 0.186),
            (0.122, 0.190),
            (0.126, 0.206),
            (0.124, 0.222),
            (0.110, 0.226),
        ],
        segs=36,
    )
    out["maid_belt"] = _finish(bpy, root, "maid_belt", bm, leather, sub=1, solid=0.002)

    bm = bmesh.new()
    for sx in (-0.040, 0.040):
        _add_box(bm, (sx, -0.118, 0.342), (0.016, 0.006, 0.155))
        _add_box(bm, (sx * 1.65, -0.150, 0.122), (0.012, 0.005, 0.100), rot_x=-0.55)
    out["maid_straps"] = _finish(bpy, root, "maid_straps", bm, leather, sub=0, solid=0.0)

    bm = bmesh.new()
    hardware = (
        (0.128, 0.00, 0.206, 0.022, 0.028),
        (0.128, -0.36, 0.206, 0.020, 0.026),
        (0.128, 0.36, 0.206, 0.020, 0.026),
        (0.122, -0.32, 0.368, 0.016, 0.022),
        (0.122, 0.32, 0.368, 0.016, 0.022),
        (0.175, -0.42, 0.095, 0.016, 0.022),
        (0.175, 0.42, 0.095, 0.016, 0.022),
    )
    for radius, ang, z, w, h in hardware:
        loc = _polar(radius, ang, z)
        az = -math.pi * 0.5 + ang
        _add_box(bm, loc, (0.008, w * 1.35, h * 1.25), rot_z=az)
        ring = (
            Matrix.Translation(Vector(loc))
            @ Matrix.Rotation(az, 4, "Z")
            @ Matrix.Rotation(math.pi * 0.5, 4, "Y")
        )
        _add_torus(bm, 0.010, 0.0022, segs_R=10, segs_r=5, matrix=ring)
    out["maid_buckles"] = _finish(bpy, root, "maid_buckles", bm, gold, sub=0, solid=0.0)

    layers = (
        ("maid_skirt_inner", 0.048, -0.004, 0.232, 0.246, 40, 0.018, 0.012),
        ("maid_skirt_mid", 0.092, 0.040, 0.202, 0.236, 36, 0.020, 0.011),
        ("maid_skirt_outer", 0.180, 0.084, 0.118, 0.196, 42, 0.026, 0.012),
    )
    for name, z0, z1, r0, r1, waves, amp, tr in layers:
        bm = bmesh.new()
        _add_lathe(bm, _bell(z0, z1, r0, r1, waves=waves, amp=amp), segs=48, drape=True)
        _add_torus(bm, r1 + 0.004, tr, z=z1 + 0.004, segs_R=48, segs_r=8, wave_n=waves, wave_a=amp, drape=True)
        out[name] = _finish(bpy, root, name, bm, cloth, sub=1, solid=0.003)

    bm = bmesh.new()
    _add_lathe(bm, [(0.108, 0.015), (0.132, 0.045), (0.128, 0.095), (0.100, 0.125)], segs=28)
    legs = (
        ((0.095, 0.015, 0.088), (0.125, -0.155, -0.215), (0.112, -0.055, -0.515), (0.112, -0.175, -0.545), (0.42, 0.0, 0.05)),
        ((-0.095, 0.015, 0.088), (-0.115, -0.125, -0.235), (-0.100, -0.095, -0.535), (-0.100, -0.210, -0.572), (0.62, 0.0, -0.07)),
    )
    for hip, knee, ank, toe, frot in legs:
        _add_tube(bm, hip, knee, 0.058, 0.046, segs=14, caps=False)
        _add_tube(bm, knee, ank, 0.045, 0.028, segs=14, caps=False)
        _add_sphere(bm, knee, 0.047, u=12, v=8)
        mid = (Vector(ank) + Vector(toe)) * 0.5
        _add_sphere(bm, mid, (0.028, 0.070, 0.022), rot=frot)
        _add_sphere(bm, hip, 0.052, u=12, v=8)
        _add_sphere(bm, ank, 0.027, u=10, v=6)
    out["maid_tights"] = _finish(bpy, root, "maid_tights", bm, cloth, sub=1, solid=0.0)

    return out
