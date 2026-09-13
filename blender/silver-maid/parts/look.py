"""World, 3-point lighting, cameras, EEVEE stills. No character mesh."""

import os
from mathutils import Vector

_LOOK = Vector((0.0, 0.05, 1.05))
_RES = (832, 1248)
_WORLD_RGB = (0.01, 0.01, 0.012)
_WORLD_STR = 0.2
_KEY_COLOR = (1.0, 0.86, 0.72)
_FILL_COLOR = (0.58, 0.74, 1.0)
_RIM_COLOR = (0.86, 0.92, 1.0)


def _aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def _engine(bpy):
    items = bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items
    ids = {it.identifier for it in items}
    if "BLENDER_EEVEE_NEXT" in ids:
        return "BLENDER_EEVEE_NEXT"
    return "BLENDER_EEVEE"


def _link(bpy, obj):
    col = bpy.context.scene.collection
    if obj.name not in col.objects:
        col.objects.link(obj)
    return obj


def _area(bpy, name, loc, target, color, energy, size, size_y):
    light = bpy.data.lights.get(name) or bpy.data.lights.new(name, "AREA")
    light.type = "AREA"
    light.shape = "RECTANGLE"
    light.size = size
    light.size_y = size_y
    light.energy = energy
    light.color = color
    if hasattr(light, "normalize"):
        light.normalize = True
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, light)
    else:
        obj.data = light
    obj.location = loc
    obj.hide_render = False
    _link(bpy, obj)
    _aim(obj, target)
    obj.name = name
    return obj


def _camera(bpy, name, loc, target, lens=50.0):
    cam = bpy.data.cameras.get(name) or bpy.data.cameras.new(name)
    cam.lens = lens
    cam.sensor_width = 36.0
    cam.sensor_fit = "AUTO"
    cam.clip_start = 0.05
    cam.clip_end = 80.0
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, cam)
    else:
        obj.data = cam
    obj.location = loc
    obj.hide_render = False
    _link(bpy, obj)
    _aim(obj, target)
    obj.name = name
    return obj


def _world(bpy):
    scene = bpy.context.scene
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    bg = next((n for n in nt.nodes if n.type == "BACKGROUND"), None)
    out = next((n for n in nt.nodes if n.type == "OUTPUT_WORLD"), None)
    if bg is None:
        bg = nt.nodes.new("ShaderNodeBackground")
    if out is None:
        out = nt.nodes.new("ShaderNodeOutputWorld")
    bg.inputs["Color"].default_value = (*_WORLD_RGB, 1.0)
    bg.inputs["Strength"].default_value = _WORLD_STR
    linked = any(
        lk.from_socket == bg.outputs["Background"] and lk.to_socket == out.inputs["Surface"]
        for lk in nt.links
    )
    if not linked:
        nt.links.new(bg.outputs["Background"], out.inputs["Surface"])
    world.color = _WORLD_RGB


def _render_settings(bpy):
    scene = bpy.context.scene
    rd = scene.render
    rd.engine = _engine(bpy)
    rd.resolution_x, rd.resolution_y = _RES
    rd.resolution_percentage = 100
    rd.film_transparent = False
    rd.image_settings.file_format = "PNG"
    rd.image_settings.color_mode = "RGB"
    rd.use_file_extension = True
    rd.use_overwrite = True
    ee = scene.eevee
    ee.taa_render_samples = 32
    if hasattr(ee, "use_shadows"):
        ee.use_shadows = True
    if hasattr(ee, "use_raytracing"):
        ee.use_raytracing = True


def setup(bpy, root, built):
    """World black, 3-point light, camera matching 3/4 still."""
    _ = (root, built)
    _world(bpy)
    _render_settings(bpy)
    # Key sits upper-left of the 3/4 camera; energy is high so black cloth reads.
    _area(
        bpy, "maid_key", Vector((0.36, -3.26, 2.50)), Vector((0.04, 0.02, 1.20)),
        _KEY_COLOR, 820.0, 1.40, 1.00,
    )
    _area(
        bpy, "maid_fill", Vector((2.78, -1.72, 0.84)), Vector((0.0, 0.04, 0.98)),
        _FILL_COLOR, 92.0, 1.90, 1.65,
    )
    # Rim from behind-right, grazing the silver hair edge.
    _area(
        bpy, "maid_rim", Vector((1.74, 1.60, 1.84)), Vector((0.12, 0.02, 1.42)),
        _RIM_COLOR, 460.0, 0.36, 1.55,
    )
    threeq = _camera(bpy, "maid_cam_threeq", Vector((2.15, -4.05, 1.55)), _LOOK)
    _camera(bpy, "maid_cam_front", Vector((0.05, -4.6, 1.45)), _LOOK)
    _camera(bpy, "maid_cam_side", Vector((4.4, 0.05, 1.45)), _LOOK)
    bpy.context.scene.camera = threeq


def render_views(bpy, out_dir):
    """Write front.png threeq.png side.png 832x1248 PNG, black background, EEVEE_NEXT."""
    _render_settings(bpy)
    os.makedirs(out_dir, exist_ok=True)
    scene = bpy.context.scene
    for cam_name, stem in (
        ("maid_cam_front", "front"),
        ("maid_cam_threeq", "threeq"),
        ("maid_cam_side", "side"),
    ):
        scene.camera = bpy.data.objects[cam_name]
        scene.render.filepath = os.path.join(out_dir, stem)
        bpy.ops.render.render(write_still=True)
