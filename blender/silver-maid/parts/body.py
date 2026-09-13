"""Sitting 8-head fashion body. Icospheres/UV spheres, bmesh, subsurf."""


def build(bpy, root):
    """Create body parts, parent to root, return dict name->object."""
    import math
    import bmesh
    from mathutils import Vector

    col = bpy.context.scene.collection
    yaw = math.radians(18.0)

    def skin_mat():
        mat = bpy.data.materials.get("maid_skin")
        if mat is None:
            mat = bpy.data.materials.new("maid_skin")
        mat.use_nodes = True
        pr = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        pr.inputs["Base Color"].default_value = (0.91, 0.78, 0.74, 1.0)
        pr.inputs["Roughness"].default_value = 0.38
        pr.inputs["Metallic"].default_value = 0.0
        pr.inputs["Subsurface Weight"].default_value = 0.18
        pr.inputs["Subsurface Radius"].default_value = (1.0, 0.2, 0.1)
        return mat

    def link(name, me):
        ob = bpy.data.objects.new(name, me)
        col.objects.link(ob)
        ob.name = name
        return ob

    def ico_uv(kind, subdiv, seg, rings):
        bm = bmesh.new()
        if kind == "ico":
            bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0)
        else:
            bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
        return bm

    def to_mesh(bm, name):
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        me.update()
        return me

    def sphere(name, loc, rad, kind="uv", subdiv=2, seg=20, rings=12, sculpt=None):
        bm = ico_uv(kind, subdiv, seg, rings)
        rx, ry, rz = rad
        for v in bm.verts:
            v.co.x *= rx
            v.co.y *= ry
            v.co.z *= rz
        if sculpt:
            sculpt(bm)
        ob = link(name, to_mesh(bm, name))
        ob.location = Vector(loc)
        return ob

    def capsule(name, a, b, r0, r1, subdiv=2):
        a, b = Vector(a), Vector(b)
        d = b - a
        ln = max(d.length, 0.02)
        bm = ico_uv("ico", subdiv, 8, 8)
        hl = ln * 0.5
        for v in bm.verts:
            t = (v.co.z + 1.0) * 0.5
            r = r0 * (1.0 - t) + r1 * t
            v.co.x *= r
            v.co.y *= r
            v.co.z *= hl
        ob = link(name, to_mesh(bm, name))
        ob.location = (a + b) * 0.5
        ob.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
        return ob

    def join(base, extras):
        bm = bmesh.new()
        bm.from_mesh(base.data)
        bm.transform(base.matrix_world)
        for ob in extras:
            b2 = bmesh.new()
            b2.from_mesh(ob.data)
            b2.transform(ob.matrix_world)
            mapping = {v: bm.verts.new(v.co.copy()) for v in b2.verts}
            for f in b2.faces:
                try:
                    bm.faces.new([mapping[v] for v in f.verts])
                except ValueError:
                    pass
            b2.free()
            me = ob.data
            bpy.data.objects.remove(ob, do_unlink=True)
            if me.users == 0:
                bpy.data.meshes.remove(me)
        bm.normal_update()
        bm.transform(base.matrix_world.inverted())
        bm.to_mesh(base.data)
        bm.free()
        base.data.update()
        return base

    def frame(axis):
        z = Vector(axis)
        z = z.normalized() if z.length_squared > 1e-10 else Vector((0.0, 0.0, 1.0))
        x = Vector((0.0, 0.0, 1.0)).cross(z)
        if x.length < 0.08:
            x = Vector((1.0, 0.0, 0.0)).cross(z)
        x.normalize()
        return x, z.cross(x).normalized(), z

    def aimed(ob, axis):
        d = Vector(axis)
        if d.length_squared > 1e-10:
            ob.rotation_euler = d.normalized().to_track_quat("Z", "Y").to_euler()
        return ob

    def hand(name, loc, axis, side, curl=0.15):
        loc = Vector(loc)
        x, y, z = frame(axis)
        palm = aimed(sphere(name, loc + z * 0.016, (0.032, 0.016, 0.044), "ico"), z)
        bits = []
        lengths = (0.040, 0.046, 0.044, 0.036)
        for i, ox in enumerate((-0.018, -0.006, 0.006, 0.018)):
            fl = lengths[i]
            c0 = curl * (0.75 + 0.08 * i)
            dir0 = (z * math.cos(c0) - y * math.sin(c0))
            dir0 = dir0.normalized() if dir0.length_squared > 1e-10 else z
            c1 = c0 * 1.15
            dir1 = (z * math.cos(c1) - y * math.sin(c1))
            dir1 = dir1.normalized() if dir1.length_squared > 1e-10 else dir0
            c2 = c0 * 1.35
            dir2 = (z * math.cos(c2) - y * math.sin(c2))
            dir2 = dir2.normalized() if dir2.length_squared > 1e-10 else dir1
            p0 = loc + z * 0.036 + x * ox
            p1 = p0 + dir0 * (fl * 0.38)
            p2 = p1 + dir1 * (fl * 0.32)
            p3 = p2 + dir2 * (fl * 0.28)
            bits.append(capsule(name + "f" + str(i) + "a", p0, p1, 0.0062, 0.0052, 1))
            bits.append(capsule(name + "f" + str(i) + "b", p1, p2, 0.0052, 0.0042, 1))
            bits.append(capsule(name + "f" + str(i) + "c", p2, p3, 0.0042, 0.0030, 1))
        td = (z * 0.25 + x * (0.85 * side) - y * 0.35).normalized()
        th0 = loc + x * (0.022 * side) + z * 0.008 + y * 0.006
        th1 = th0 + td * 0.022
        th2 = th1 + (td * 0.7 + z * 0.4).normalized() * 0.018
        bits.append(capsule(name + "th0", th0, th1, 0.0070, 0.0058, 1))
        bits.append(capsule(name + "th1", th1, th2, 0.0058, 0.0042, 1))
        return join(palm, bits)

    def foot(name, ankle, toe):
        ankle, toe = Vector(ankle), Vector(toe)
        d = toe - ankle
        d = d.normalized() if d.length_squared > 1e-10 else Vector((0.0, -1.0, -0.25))
        side = Vector((d.y, -d.x, 0.0))
        if side.length_squared < 1e-8:
            side = Vector((1.0, 0.0, 0.0))
        else:
            side.normalize()
        sole = aimed(sphere(name, ankle + d * 0.065, (0.028, 0.020, 0.088), "ico"), d)
        heel = sphere(name + "h", ankle - d * 0.018 + Vector((0.0, 0.0, -0.008)), (0.022, 0.020, 0.024), "ico", 1)
        ball = aimed(sphere(name + "ball", ankle + d * 0.118, (0.024, 0.016, 0.022), "ico", 1), d)
        bits = [heel, ball]
        for i, ox in enumerate((-0.016, -0.008, 0.0, 0.008, 0.016)):
            tl = 0.016 - abs(i - 2) * 0.002
            p0 = ankle + d * 0.132 + side * ox
            p1 = p0 + d * tl + Vector((0.0, 0.0, -0.004))
            bits.append(capsule(name + "toe" + str(i), p0, p1, 0.0048, 0.0032, 1))
        return join(sole, bits)

    def finish(ob, mat):
        md = ob.modifiers.new("Subsurf", "SUBSURF")
        md.levels = 2
        md.render_levels = 3
        ob.data.shade_smooth()
        ob.data.materials.append(mat)
        ob.parent = root
        return ob

    def chin_sculpt(bm):
        zs = [v.co.z for v in bm.verts]
        zmin, zmax = min(zs), max(zs)
        span = max(zmax - zmin, 1e-6)
        for v in bm.verts:
            t = (v.co.z - zmin) / span
            pinch = 0.34 + 0.66 * min(1.0, t / 0.55)
            v.co.x *= pinch
            v.co.y *= 0.82 + 0.18 * t
            if t < 0.35:
                k = (0.35 - t) / 0.35
                v.co.y -= 0.018 * k
                v.co.z -= 0.012 * k

    def waist_sculpt(bm):
        m = max((abs(v.co.z) for v in bm.verts), default=1.0) or 1.0
        for v in bm.verts:
            t = abs(v.co.z) / m
            s = 0.70 + 0.30 * t
            v.co.x *= s
            v.co.y *= 0.82 + 0.18 * t

    def hip_sculpt(bm):
        for v in bm.verts:
            if v.co.z < 0.0:
                v.co.z *= 0.62
            v.co.x *= 1.06
            v.co.y *= 1.04

    skin = skin_mat()
    out = {}

    def put(ob):
        finish(ob, skin)
        out[ob.name] = ob
        return ob

    # Local to MaidRoot (stool top). World hip ~ (0, 0.02, 0.74). _L = -X, _R = +X.
    put(sphere("maid_hips", (0.0, 0.02, 0.02), (0.155, 0.125, 0.088), "ico", sculpt=hip_sculpt))
    put(sphere("maid_torso", (0.0, 0.01, 0.205), (0.108, 0.092, 0.145), "uv", sculpt=waist_sculpt))
    chest = sphere("maid_chest", (0.0, -0.02, 0.375), (0.145, 0.100, 0.115), "uv")
    bust_l = sphere("_bust_L", (-0.055, -0.082, 0.365), (0.078, 0.088, 0.080), "ico")
    bust_r = sphere("_bust_R", (0.055, -0.082, 0.365), (0.078, 0.088, 0.080), "ico")
    put(join(chest, [bust_l, bust_r]))
    put(capsule("maid_neck", (0.0, -0.008, 0.455), (0.0, -0.02, 0.575), 0.042, 0.036))
    cranium = sphere("maid_head", (0.01, -0.03, 0.655), (0.086, 0.100, 0.112), "uv", seg=24, rings=14, sculpt=chin_sculpt)
    chin = sphere("_chin", (0.01, -0.085, 0.575), (0.028, 0.024, 0.030), "ico")
    put(join(cranium, [chin]))

    put(capsule("maid_arm_upper_L", (-0.155, -0.02, 0.40), (-0.38, -0.16, 0.32), 0.044, 0.034))
    put(capsule("maid_arm_forearm_L", (-0.38, -0.16, 0.32), (-0.52, -0.32, 0.28), 0.033, 0.024))
    put(hand("maid_hand_L", (-0.55, -0.36, 0.27), (-0.35, -0.40, -0.10), -1.0, curl=0.12))

    put(capsule("maid_arm_upper_R", (0.155, -0.02, 0.40), (0.28, -0.14, 0.30), 0.044, 0.034))
    put(capsule("maid_arm_forearm_R", (0.28, -0.14, 0.30), (0.18, -0.22, 0.22), 0.033, 0.024))
    put(hand("maid_hand_R", (0.16, -0.24, 0.20), (-0.15, -0.40, -0.25), 1.0, curl=0.42))

    put(capsule("maid_thigh_L", (-0.07, 0.02, 0.00), (-0.08, -0.12, -0.38), 0.078, 0.052))
    put(capsule("maid_calf_L", (-0.08, -0.12, -0.38), (-0.06, -0.10, -0.66), 0.048, 0.032))
    put(foot("maid_foot_L", (-0.06, -0.10, -0.66), (-0.05, -0.12, -0.78)))

    put(capsule("maid_thigh_R", (0.07, 0.02, 0.00), (0.09, -0.18, -0.36), 0.078, 0.052))
    put(capsule("maid_calf_R", (0.09, -0.18, -0.36), (0.08, -0.14, -0.62), 0.048, 0.032))
    put(foot("maid_foot_R", (0.08, -0.14, -0.62), (0.09, -0.26, -0.72)))

    root.rotation_euler = (0.0, 0.0, yaw)
    return out
