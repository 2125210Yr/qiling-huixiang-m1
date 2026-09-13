import bpy
print("BLENDER", bpy.app.version_string)
print("ENGINE_ENUM", list(bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys())[:8])
