"""Anime face features in MaidRoot space. Joined into maid_body by join_figure."""
from __future__ import annotations

import math

import bmesh
from mathutils import Matrix, Vector

# Head from body.py: (0.01, -0.03, 0.655). Face looks -Y.
_HEAD = Vector((0.01, -0.03, 0.655))
_FACE_N = Vector((0.0, -1.0, 0.0))


def _bsdf(mat):
    return next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")


def _sock(pr, name, value):
    if name in pr.inputs:
        try:
            pr.inputs[name].default_value = value
        except Exception:
            pass


def _mat(bpy, name, color, rough=0.35, metal=0.0, extra=None):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (color[0], color[1], color[2], 1.0)
    pr = _bsdf(mat)
    _sock(pr, "Base Color", (color[0], color[1], color[2], 1.0))
    _sock(pr, "Roughness", rough)
    _sock(pr, "Metallic", metal)
    if extra:
        for k, v in extra.items():
            _sock(pr, k, v)
    return mat


def _to_obj(bpy, bm, name, root, mat, sub=1):
    try:
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    except Exception:
        pass
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.update()
    if hasattr(me, "shade_smooth"):
        me.shade_smooth()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.parent = root
    ob.location = (0.0, 0.0, 0.0)
    if mat is not None:
        me.materials.append(mat)
    if sub:
        md = ob.modifiers.new("Subsurf", "SUBSURF")
        md.levels = sub
        md.render_levels = min(sub + 1, 2)
    return ob


def _ellipsoid(bm, loc, rad, u=16, v=10, rot=None):
    if isinstance(rad, (int, float)):
        rad = (rad, rad, rad)
    mat = Matrix.Translation(Vector(loc))
    if rot is not None:
        mat = mat @ rot
    mat = mat @ Matrix.Diagonal((rad[0], rad[1], rad[2], 1.0))
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=1.0, matrix=mat)


def _ico(bm, loc, rad, subdiv=2):
    bmesh.ops.create_icosphere(
        bm, subdivisions=subdiv, radius=1.0,
        matrix=Matrix.Translation(Vector(loc)) @ Matrix.Diagonal((rad, rad, rad, 1.0)),
    )


def _disc(bm, loc, n, r, inner=0.0, rot=None):
    mat = Matrix.Translation(Vector(loc))
    if rot is not None:
        mat = mat @ rot
    if inner <= 1e-6:
        bmesh.ops.create_circle(bm, cap_ends=True, segments=n, radius=r, matrix=mat)
        return
    outer = []
    inn = []
    for i in range(n):
        a = math.tau * i / n
        c, s = math.cos(a), math.sin(a)
        outer.append(bm.verts.new(mat @ Vector((r * c, r * s, 0.0))))
        inn.append(bm.verts.new(mat @ Vector((inner * c, inner * s, 0.0))))
    for i in range(n):
        j = (i + 1) % n
        try:
            bm.faces.new((outer[i], outer[j], inn[j], inn[i]))
        except ValueError:
            pass


def _look_rot(origin, target=Vector((2.15, -4.05, 0.83))):
    d = (target - Vector(origin)).normalized()
    return d.to_track_quat("Z", "Y").to_matrix().to_4x4()


def _indent_head(head, root, sockets):
    """Push head verts back at eye sockets so eyeballs sit in, not on."""
    if head is None or getattr(head, "data", None) is None:
        return
    bm = bmesh.new()
    bm.from_mesh(head.data)
    imw = head.matrix_world.inverted()
    rimw = root.matrix_world.inverted()
    for v in bm.verts:
        w = head.matrix_world @ v.co
        for c, rad in sockets:
            off = w - c
            if off.length < rad:
                k = 1.0 - off.length / rad
                w = w + Vector((0.0, 0.012, 0.0)) * (k * k)
                w.z -= 0.004 * k
        rl = rimw @ w
        front = -rl.y
        if front > 0.02 and abs(rl.x - 0.01) < 0.07 and 0.58 < rl.z < 0.72:
            rl.y += 0.010 * min(1.0, (front - 0.02) / 0.08)
            w = root.matrix_world @ rl
        v.co = imw @ w
    bm.to_mesh(head.data)
    bm.free()
    head.data.update()


def build(bpy, root):
    """Create face parts parented to root. Return name->object."""
    skin = bpy.data.materials.get("maid_skin") or _mat(
        bpy, "maid_skin", (0.91, 0.78, 0.74), 0.38,
        extra={"Subsurface Weight": 0.18, "Subsurface Radius": (1.0, 0.2, 0.1)},
    )
    white = _mat(bpy, "maid_eye_white", (0.96, 0.96, 0.97), 0.12, extra={"Coat Weight": 0.35, "Coat Roughness": 0.06})
    iris = _mat(bpy, "maid_iris", (0.42, 0.10, 0.12), 0.18, extra={"Coat Weight": 0.45, "Coat Roughness": 0.08})
    pupil_m = _mat(bpy, "maid_pupil", (0.02, 0.01, 0.01), 0.22)
    hi = _mat(bpy, "maid_eye_hi", (1.0, 1.0, 1.0), 0.05, extra={"Coat Weight": 0.8, "Emission Color": (1, 1, 1, 1), "Emission Strength": 2.4})
    lash = _mat(bpy, "maid_lash", (0.04, 0.03, 0.04), 0.28)
    lip = _mat(bpy, "maid_lip", (0.48, 0.12, 0.16), 0.22, extra={"Coat Weight": 0.4, "Coat Roughness": 0.12})
    tooth = _mat(bpy, "maid_teeth", (0.95, 0.93, 0.90), 0.18)
    brow_m = _mat(bpy, "maid_brow", (0.78, 0.82, 0.88), 0.28, extra={"Coat Weight": 0.2})
    cavity = _mat(bpy, "maid_mouth_cavity", (0.08, 0.03, 0.04), 0.6)

    out = {}
    eye_L = Vector((-0.028, -0.128, 0.664))
    eye_R = Vector((0.048, -0.128, 0.664))
    bpy.context.view_layer.update()
    head_ob = bpy.data.objects.get("maid_head")
    _indent_head(
        head_ob, root,
        ((root.matrix_world @ eye_L, 0.022), (root.matrix_world @ eye_R, 0.022)),
    )

    def eye_set(tag, center):
        rot = _look_rot(center)
        # sclera
        bm = bmesh.new()
        _ellipsoid(bm, center, (0.020, 0.016, 0.017), u=20, v=12, rot=rot)
        out["maid_eye_" + tag] = _to_obj(bpy, bm, "maid_eye_" + tag, root, white, 1)
        # iris disc sitting on front of sclera
        iris_c = Vector(center) + (rot @ Vector((0.0, 0.0, 0.014)))
        bm = bmesh.new()
        _disc(bm, iris_c, 20, 0.0135, inner=0.0048, rot=rot)
        out["maid_iris_" + tag] = _to_obj(bpy, bm, "maid_iris_" + tag, root, iris, 0)
        bm = bmesh.new()
        _disc(bm, iris_c + (rot @ Vector((0.0, 0.0, 0.0012))), 16, 0.0050, rot=rot)
        out["maid_pupil_" + tag] = _to_obj(bpy, bm, "maid_pupil_" + tag, root, pupil_m, 0)
        # catchlight
        bm = bmesh.new()
        hi_p = Vector(center) + (rot @ Vector((-0.0045, 0.004, 0.013)))
        _ico(bm, hi_p, 0.0032, 2)
        out["maid_eyehilight_" + tag] = _to_obj(bpy, bm, "maid_eyehilight_" + tag, root, hi, 0)
        # upper lid + lash
        bm = bmesh.new()
        lid_c = Vector(center) + Vector((0.0, 0.002, 0.006))
        bmesh.ops.create_uvsphere(
            bm, u_segments=14, v_segments=8, radius=1.0,
            matrix=Matrix.Translation(lid_c) @ Matrix.Diagonal((0.020, 0.010, 0.009, 1.0)),
        )
        kill = [v for v in bm.verts if v.co.z < lid_c.z - 0.001]
        if kill:
            bmesh.ops.delete(bm, geom=kill, context="VERTS")
        out["maid_lid_" + tag] = _to_obj(bpy, bm, "maid_lid_" + tag, root, lash, 1)
        # lower lid
        bm = bmesh.new()
        low = Vector(center) + Vector((0.0, 0.001, -0.008))
        bmesh.ops.create_uvsphere(
            bm, u_segments=12, v_segments=6, radius=1.0,
            matrix=Matrix.Translation(low) @ Matrix.Diagonal((0.016, 0.007, 0.005, 1.0)),
        )
        kill = [v for v in bm.verts if v.co.z > low.z + 0.001]
        if kill:
            bmesh.ops.delete(bm, geom=kill, context="VERTS")
        out["maid_lidlow_" + tag] = _to_obj(bpy, bm, "maid_lidlow_" + tag, root, skin, 1)

    eye_set("L", eye_L)
    eye_set("R", eye_R)

    # brows
    for tag, cx, flip in (("L", -0.032, 1.0), ("R", 0.052, -1.0)):
        bm = bmesh.new()
        pts = []
        for i in range(7):
            t = i / 6.0
            x = cx + flip * (-0.016 + 0.032 * t)
            y = -0.100 - 0.004 * math.sin(t * math.pi)
            z = 0.678 + 0.007 * math.sin(t * math.pi) - 0.003 * t
            pts.append(Vector((x, y, z)))
        for i, p in enumerate(pts):
            r = 0.0038 if 0.15 < i / 6.0 < 0.75 else 0.0026
            _ico(bm, p, r, 1)
        out["maid_brow_" + tag] = _to_obj(bpy, bm, "maid_brow_" + tag, root, brow_m, 1)

    # nose
    bm = bmesh.new()
    bridge = Vector((0.010, -0.112, 0.648))
    tip = Vector((0.010, -0.124, 0.630))
    _ellipsoid(bm, bridge, (0.007, 0.010, 0.016), u=10, v=8)
    _ellipsoid(bm, tip, (0.009, 0.011, 0.008), u=10, v=8)
    _ico(bm, tip + Vector((-0.006, -0.002, -0.001)), 0.0036, 1)
    _ico(bm, tip + Vector((0.006, -0.002, -0.001)), 0.0036, 1)
    out["maid_nose"] = _to_obj(bpy, bm, "maid_nose", root, skin, 1)

    # mouth: cavity, teeth, lips (slightly open)
    mouth = Vector((0.010, -0.108, 0.598))
    bm = bmesh.new()
    _ellipsoid(bm, mouth + Vector((0.0, 0.006, 0.0)), (0.018, 0.010, 0.010), u=12, v=8)
    out["maid_mouth"] = _to_obj(bpy, bm, "maid_mouth", root, cavity, 0)

    bm = bmesh.new()
    bmesh.ops.create_cube(
        bm, size=1.0,
        matrix=Matrix.Translation(mouth + Vector((0.0, -0.004, 0.003))) @ Matrix.Diagonal((0.022, 0.006, 0.005, 1.0)),
    )
    out["maid_teeth"] = _to_obj(bpy, bm, "maid_teeth", root, tooth, 0)

    bm = bmesh.new()
    _ellipsoid(bm, mouth + Vector((0.0, -0.006, 0.007)), (0.020, 0.007, 0.0055), u=14, v=8)
    out["maid_lip_upper"] = _to_obj(bpy, bm, "maid_lip_upper", root, lip, 1)

    bm = bmesh.new()
    _ellipsoid(bm, mouth + Vector((0.0, -0.005, -0.006)), (0.018, 0.008, 0.005), u=14, v=8)
    out["maid_lip_lower"] = _to_obj(bpy, bm, "maid_lip_lower", root, lip, 1)

    # ears
    for tag, sx in (("L", -1.0), ("R", 1.0)):
        bm = bmesh.new()
        loc = _HEAD + Vector((0.088 * sx, 0.010, -0.010))
        rot = Matrix.Rotation(math.radians(18 * sx), 4, "Z") @ Matrix.Rotation(math.radians(90), 4, "Y")
        bmesh.ops.create_uvsphere(
            bm, u_segments=12, v_segments=8, radius=1.0,
            matrix=Matrix.Translation(loc) @ rot @ Matrix.Diagonal((0.022, 0.014, 0.008, 1.0)),
        )
        out["maid_ear_" + tag] = _to_obj(bpy, bm, "maid_ear_" + tag, root, skin, 1)

    return out
