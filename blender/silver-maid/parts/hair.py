"""Destiny-Child silver high-ponytail. Graphic ribbon masses, fabric C-hook tips."""
from math import sin, cos, pi
from mathutils import Vector

PONY = (0.02, -0.04, 0.74)  # MaidRoot local; world (0.02, -0.04, 1.46)


def _bsdf(mat):
    return next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")


def _material(bpy, name, color):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    if mat.node_tree is None:
        mat.use_nodes = True
    mat.diffuse_color = (color[0], color[1], color[2], 1.0)
    mat.roughness = 0.22
    n = _bsdf(mat)
    n.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
    n.inputs["Roughness"].default_value = 0.22
    n.inputs["Specular IOR Level"].default_value = 0.5
    n.inputs["Coat Weight"].default_value = 0.15
    return mat


def _curve(bpy, name, depth, parent, loc, extrude=0.0):
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "3D"
    cu.fill_mode = "FULL"
    cu.bevel_depth = depth
    cu.bevel_resolution = 4
    cu.resolution_u = 16
    cu.render_resolution_u = 20
    cu.use_fill_caps = True
    cu.twist_mode = "MINIMUM"
    cu.extrude = extrude
    ob = bpy.data.objects.new(name, cu)
    bpy.context.scene.collection.objects.link(ob)
    ob.parent = parent
    ob.location = loc
    return ob, cu


def _radii(n, root_r, mid_r, tip_r):
    out = []
    last = max(n - 1, 1)
    for i in range(n):
        t = i / last
        if t < 0.16:
            u = t / 0.16
            r = root_r + (mid_r - root_r) * (u * u * (3.0 - 2.0 * u))
        elif t < 0.62:
            r = mid_r
        else:
            u = (t - 0.62) / 0.38
            r = mid_r + (tip_r - mid_r) * (u * u * u)
        out.append(r)
    return out


def _strand(cu, pts, radii, h=0.34):
    vs = [Vector(p) for p in pts]
    n = len(vs)
    if n < 2:
        return
    sp = cu.splines.new("BEZIER")
    sp.use_cyclic_u = False
    sp.resolution_u = 12
    sp.use_smooth = True
    sp.bezier_points.add(n - 1)
    for i, bp in enumerate(sp.bezier_points):
        bp.co = vs[i]
        bp.tilt = 0.0
        bp.radius = radii[i] if i < len(radii) else 1.0
        prev = vs[i - 1] if i else vs[i]
        nxt = vs[i + 1] if i < n - 1 else vs[i]
        tang = nxt - prev
        if tang.length < 1e-8:
            tang = Vector((0.0, 0.0, 1.0))
        else:
            tang.normalize()
        hl = (vs[i] - prev).length if i else (nxt - vs[i]).length
        hr = (nxt - vs[i]).length if i < n - 1 else (vs[i] - prev).length
        bp.handle_left_type = "FREE"
        bp.handle_right_type = "FREE"
        bp.handle_left = bp.co - tang * max(hl * h, 0.012)
        bp.handle_right = bp.co + tang * max(hr * h, 0.012)


def _bundle(cu, spine, rad, count, width, depth, h, seed=0.0):
    """Flat overlapping ribbon: roots stay bunched, tips taper together."""
    vs = [Vector(p) for p in spine]
    npts = len(vs)
    for j in range(count):
        a = (j / max(count - 1, 1) - 0.5) if count > 1 else 0.0
        pts = []
        rs = []
        for i, p in enumerate(vs):
            t = i / max(npts - 1, 1)
            s = 0.04 + 0.96 * t
            if t > 0.68:
                u = (t - 0.68) / 0.32
                s = (0.04 + 0.96 * 0.68) * (1.0 - 0.82 * u * u)
            tg = (vs[i + 1] - vs[i]) if i < npts - 1 else (vs[i] - vs[i - 1])
            side = Vector((tg.x, 0.0, tg.z)).cross(Vector((0.0, 1.0, 0.0)))
            if side.length < 1e-7:
                side = Vector((1.0, 0.0, 0.0))
            else:
                side.normalize()
            k = seed + j * 1.73 + i * 0.41
            pts.append((
                p.x + side.x * a * width * s + 0.014 * s * sin(k),
                p.y + ((j % 2) - 0.5) * depth * s + 0.012 * s * cos(k * 0.85),
                p.z + side.z * a * width * s + 0.010 * s * sin(k * 1.27),
            ))
            rs.append(rad[i] * (1.0 - abs(a) * 0.10))
        _strand(cu, pts, rs, h=h)


def _to_mesh(bpy, ob, levels=1):
    vl = bpy.context.view_layer
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    for o in vl.objects:
        o.select_set(False)
    ob.select_set(True)
    vl.objects.active = ob
    bpy.ops.object.convert(target="MESH")
    ob.data.shade_smooth()
    md = ob.modifiers.new("subsurf", "SUBSURF")
    md.levels = levels
    md.render_levels = max(levels, 2)
    return ob


def _on_head(hc, rr, az, el):
    return hc + Vector((rr * sin(el) * sin(az), -rr * sin(el) * cos(az), rr * cos(el)))


def build(bpy, root):
    """Create graphic silver hair. Parent to MaidRoot. Return name->object."""
    mat = _material(bpy, "maid_hair", (0.78, 0.82, 0.88))
    hi = _material(bpy, "maid_hair_hi", (0.90, 0.93, 0.97))

    o_root, c_root = _curve(bpy, "maid_hair_root", 0.0045, root, PONY, 0.003)
    o_l, c_l = _curve(bpy, "maid_hair_mass_L", 0.0018, root, PONY, 0.016)
    o_r, c_r = _curve(bpy, "maid_hair_mass_R", 0.0016, root, PONY, 0.014)
    o_b, c_b = _curve(bpy, "maid_bangs", 0.0022, root, PONY, 0.006)
    o_hi, c_hi = _curve(bpy, "maid_hair_hi", 0.0014, root, PONY, 0.008)
    for ob, m in ((o_root, mat), (o_l, mat), (o_r, mat), (o_b, mat), (o_hi, hi)):
        ob.data.materials.append(m)

    # maid_head is (0.01, -0.03, 0.655) r~(0.086, 0.100, 0.112) in MaidRoot space.
    hc = Vector((-0.01, 0.01, -0.085))
    rr = 0.100

    # Scalp cap near the crown only (front fringe is bangs).
    for i in range(12):
        az = 0.85 + i * (2.0 * pi - 1.70) / 11.0
        el = 0.30 + 0.14 * (0.5 + 0.5 * cos(i * 0.9))
        pts = []
        for k, e in enumerate((el, el * 0.50, el * 0.18, 0.04)):
            p = _on_head(hc, rr, az, e)
            if k == 3:
                p = Vector((0.010 * sin(az), 0.014, 0.045))
            pts.append(p)
        _strand(c_root, pts, (1.35, 1.22, 1.10, 0.95), h=0.28)

    # Tight upward ponytail column
    for j in range(7):
        a = j * 0.90
        dx, dy = 0.009 * cos(a), 0.009 * sin(a)
        _strand(
            c_root,
            (
                (dx, dy, 0.00),
                (dx * 1.2, dy * 1.2 + 0.016, 0.09),
                (dx * 1.6 - 0.015, dy * 1.6 + 0.032, 0.18),
                (dx * 1.8 - 0.04, dy * 1.8 + 0.045, 0.27),
            ),
            (1.45, 1.30, 1.10, 0.88),
            h=0.28,
        )

    # --- left mass: huge hooked ribbons, viewer's left / -X, back +Y ---
    l_up = (
        (0.00, 0.02, 0.00),
        (-0.05, 0.10, 0.20),
        (-0.18, 0.20, 0.42),
        (-0.40, 0.28, 0.50),
        (-0.64, 0.26, 0.40),
        (-0.82, 0.18, 0.20),
        (-0.90, 0.08, 0.00),
        (-0.84, 0.00, -0.16),
        (-0.66, -0.06, -0.24),
        (-0.48, -0.08, -0.16),
    )
    l_outer = (
        (0.00, 0.04, 0.04),
        (-0.08, 0.16, 0.24),
        (-0.26, 0.26, 0.40),
        (-0.50, 0.30, 0.34),
        (-0.72, 0.28, 0.12),
        (-0.90, 0.22, -0.14),
        (-1.02, 0.14, -0.42),
        (-1.12, 0.06, -0.70),
        (-1.16, -0.02, -0.94),
        (-1.10, -0.10, -1.12),
        (-0.94, -0.16, -1.20),
        (-0.74, -0.14, -1.12),
        (-0.58, -0.08, -0.96),
        (-0.48, -0.02, -0.80),
    )
    l_fill = (
        (0.00, 0.06, 0.02),
        (-0.10, 0.16, 0.24),
        (-0.30, 0.24, 0.30),
        (-0.52, 0.22, 0.12),
        (-0.70, 0.18, -0.14),
        (-0.82, 0.12, -0.42),
        (-0.90, 0.06, -0.70),
        (-0.88, -0.02, -0.94),
        (-0.74, -0.08, -1.06),
        (-0.56, -0.06, -0.96),
        (-0.44, 0.00, -0.80),
    )
    l_inner = (
        (0.00, 0.01, 0.00),
        (-0.10, 0.10, 0.16),
        (-0.28, 0.16, 0.20),
        (-0.44, 0.14, 0.04),
        (-0.54, 0.06, -0.18),
        (-0.58, -0.02, -0.40),
        (-0.50, -0.10, -0.56),
        (-0.36, -0.14, -0.60),
        (-0.26, -0.10, -0.46),
        (-0.22, -0.04, -0.30),
    )
    l_trail = (
        (-0.06, 0.08, 0.08),
        (-0.22, 0.14, 0.04),
        (-0.42, 0.10, -0.16),
        (-0.58, 0.02, -0.40),
        (-0.68, -0.06, -0.66),
        (-0.72, -0.14, -0.88),
        (-0.64, -0.20, -1.04),
        (-0.46, -0.16, -1.08),
        (-0.32, -0.08, -0.94),
        (-0.26, -0.02, -0.78),
    )
    _bundle(c_l, l_up, _radii(len(l_up), 0.95, 1.15, 0.18), 12, 0.16, 0.034, 0.38, 0.2)
    _bundle(c_l, l_outer, _radii(len(l_outer), 1.00, 1.20, 0.16), 12, 0.16, 0.036, 0.36, 1.1)
    _bundle(c_l, l_fill, _radii(len(l_fill), 1.00, 1.18, 0.16), 11, 0.14, 0.032, 0.34, 2.0)
    _bundle(c_l, l_inner, _radii(len(l_inner), 0.85, 1.05, 0.16), 10, 0.12, 0.028, 0.37, 3.4)
    _bundle(c_l, l_trail, _radii(len(l_trail), 0.80, 1.00, 0.14), 10, 0.12, 0.028, 0.36, 4.2)

    # --- right mass: smaller trailing C-hooks, +X ---
    r_main = (
        (0.00, 0.00, 0.02),
        (0.10, 0.06, 0.16),
        (0.26, 0.12, 0.18),
        (0.44, 0.14, 0.02),
        (0.58, 0.10, -0.22),
        (0.70, 0.04, -0.48),
        (0.78, -0.02, -0.72),
        (0.76, -0.08, -0.92),
        (0.62, -0.14, -1.02),
        (0.44, -0.12, -0.96),
        (0.32, -0.06, -0.80),
        (0.26, 0.00, -0.64),
    )
    r_back = (
        (0.02, 0.04, 0.00),
        (0.14, 0.14, 0.10),
        (0.30, 0.20, 0.04),
        (0.46, 0.18, -0.16),
        (0.56, 0.12, -0.40),
        (0.60, 0.04, -0.64),
        (0.54, -0.04, -0.82),
        (0.40, -0.08, -0.90),
        (0.28, -0.04, -0.78),
        (0.22, 0.02, -0.62),
    )
    r_low = (
        (0.04, 0.02, -0.02),
        (0.16, 0.08, 0.06),
        (0.34, 0.08, -0.08),
        (0.50, 0.04, -0.32),
        (0.60, -0.02, -0.56),
        (0.62, -0.10, -0.74),
        (0.52, -0.16, -0.84),
        (0.36, -0.12, -0.76),
        (0.26, -0.06, -0.62),
    )
    _bundle(c_r, r_main, _radii(len(r_main), 0.90, 1.10, 0.16), 12, 0.14, 0.032, 0.36, 0.5)
    _bundle(c_r, r_back, _radii(len(r_back), 0.80, 1.00, 0.14), 10, 0.12, 0.030, 0.35, 1.6)
    _bundle(c_r, r_low, _radii(len(r_low), 0.75, 0.95, 0.14), 10, 0.12, 0.026, 0.36, 2.7)

    # --- front bangs covering the forehead + temples ---
    for row, az0, n in ((0.0, -0.42, 9), (0.04, -0.38, 8), (0.08, -0.34, 7)):
        for i in range(n):
            az = az0 + i * 0.105
            el0 = 0.62 + 0.05 * sin((i + row * 8) * 1.7)
            extra = 0.10 * ((i + int(row * 10)) % 2)
            st = _on_head(hc, rr + 0.002, az, el0)
            p1 = _on_head(hc, rr + 0.010, az * 0.94, el0 + 0.34)
            p2 = Vector((
                p1.x * 0.94,
                p1.y - 0.012 - extra * 0.05,
                p1.z - 0.022 - extra * 0.02,
            ))
            _strand(c_b, (st, p1, p2), (1.10, 0.82, 0.28), h=0.24)
    for az, el0, el1 in ((-0.88, 0.84, 1.26), (0.90, 0.84, 1.22)):
        st = _on_head(hc, rr + 0.003, az, el0)
        p1 = _on_head(hc, rr + 0.005, az * 0.97, (el0 + el1) * 0.5)
        p2 = _on_head(hc, rr + 0.004, az * 0.93, el1)
        _strand(c_b, (st, p1, p2), (0.92, 0.70, 0.28), h=0.25)

    for pts, abc, hh in (
        (l_up, (0.62, 0.78, 0.20), 0.38),
        (l_outer[1:], (0.58, 0.74, 0.18), 0.36),
        (r_main, (0.55, 0.70, 0.18), 0.36),
        (l_trail, (0.50, 0.66, 0.16), 0.36),
    ):
        shifted = [(p[0], p[1] - 0.025, p[2] + 0.018) for p in pts]
        _strand(c_hi, shifted, _radii(len(shifted), *abc), h=hh)

    for ob in (o_root, o_l, o_r, o_b, o_hi):
        _to_mesh(bpy, ob, levels=1)

    return {
        "maid_hair_root": o_root,
        "maid_hair_mass_L": o_l,
        "maid_hair_mass_R": o_r,
        "maid_bangs": o_b,
        "maid_hair_hi": o_hi,
    }
