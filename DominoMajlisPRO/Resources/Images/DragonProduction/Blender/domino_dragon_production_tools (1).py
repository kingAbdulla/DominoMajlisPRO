
bl_info = {
    "name": "Domino Dragon Production Tools",
    "author": "OpenAI / Domino Majlis PRO",
    "version": (1, 0, 0),
    "blender": (4, 0, 0),
    "category": "Object",
    "description": "Production tools for Dragon Master asset separation and cleanup."
}

import bpy
from mathutils import Vector


class DPT_Settings(bpy.types.PropertyGroup):
    lower_z_percent: bpy.props.FloatProperty(
        name="Lower Jaw Height %",
        default=42.0,
        min=5.0,
        max=80.0
    )

    front_percent: bpy.props.FloatProperty(
        name="Front Snout %",
        default=62.0,
        min=10.0,
        max=100.0
    )

    expand_steps: bpy.props.IntProperty(
        name="Expand",
        default=2,
        min=0,
        max=8
    )


def get_active_mesh_object():
    obj = bpy.context.active_object
    if obj is None or obj.type != "MESH":
        raise RuntimeError("Select Dragon_Master_Sculpt first.")
    return obj


def bounds_local(mesh):
    coords = [v.co.copy() for v in mesh.vertices]
    min_v = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    max_v = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return min_v, max_v


def select_lower_jaw(obj, lower_z_percent, front_percent, expand_steps):
    bpy.ops.object.mode_set(mode="OBJECT")
    mesh = obj.data

    for p in mesh.polygons:
        p.select = False
    for v in mesh.vertices:
        v.select = False

    min_v, max_v = bounds_local(mesh)
    size = max_v - min_v

    z_limit = min_v.z + (size.z * (lower_z_percent / 100.0))

    x_center = (min_v.x + max_v.x) * 0.5
    y_center = (min_v.y + max_v.y) * 0.5

    use_x = size.x >= size.y

    if use_x:
        front_limit_pos = x_center + (size.x * (front_percent / 100.0 - 0.5))
        front_limit_neg = x_center - (size.x * (front_percent / 100.0 - 0.5))
    else:
        front_limit_pos = y_center + (size.y * (front_percent / 100.0 - 0.5))
        front_limit_neg = y_center - (size.y * (front_percent / 100.0 - 0.5))

    selected_verts = set()

    for v in mesh.vertices:
        co = v.co
        is_low = co.z <= z_limit

        if use_x:
            is_front = co.x >= front_limit_pos or co.x <= front_limit_neg
        else:
            is_front = co.y >= front_limit_pos or co.y <= front_limit_neg

        if is_low and is_front:
            selected_verts.add(v.index)

    selected_faces = set()
    for poly in mesh.polygons:
        count = sum(1 for i in poly.vertices if i in selected_verts)
        if count >= max(1, int(len(poly.vertices) * 0.55)):
            selected_faces.add(poly.index)

    face_by_vert = {}
    for poly in mesh.polygons:
        for vi in poly.vertices:
            face_by_vert.setdefault(vi, set()).add(poly.index)

    for _ in range(expand_steps):
        grow = set(selected_faces)
        used_verts = set()
        for fi in selected_faces:
            used_verts.update(mesh.polygons[fi].vertices)
        for vi in used_verts:
            grow.update(face_by_vert.get(vi, set()))
        selected_faces = grow

    for fi in selected_faces:
        mesh.polygons[fi].select = True

    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_mode(type="FACE")

    return len(selected_faces)


class DPT_OT_auto_select_lower_jaw(bpy.types.Operator):
    bl_idname = "dpt.auto_select_lower_jaw"
    bl_label = "Auto Lower Jaw Select"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        settings = context.scene.dpt_settings
        obj = get_active_mesh_object()
        count = select_lower_jaw(obj, settings.lower_z_percent, settings.front_percent, settings.expand_steps)
        self.report({"INFO"}, f"Selected {count} candidate lower-jaw faces. Review before separating.")
        return {"FINISHED"}


class DPT_OT_separate_selected_jaw(bpy.types.Operator):
    bl_idname = "dpt.separate_selected_jaw"
    bl_label = "Separate Selected as Lower Jaw"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        obj = get_active_mesh_object()

        if obj.mode != "EDIT":
            bpy.ops.object.mode_set(mode="EDIT")

        original_name = obj.name
        bpy.ops.mesh.separate(type="SELECTED")
        bpy.ops.object.mode_set(mode="OBJECT")

        obj.name = "Dragon_Master_Head"
        obj.data.name = "Dragon_Master_Head_Mesh"

        for item in bpy.context.selected_objects:
            if item.name != "Dragon_Master_Head":
                item.name = "Dragon_Lower_Jaw"
                item.data.name = "Dragon_Lower_Jaw_Mesh"
                bpy.context.view_layer.objects.active = item
                break

        self.report({"INFO"}, "Separated selected faces.")
        return {"FINISHED"}


class DPT_PT_panel(bpy.types.Panel):
    bl_label = "Dragon Production Tools"
    bl_idname = "DPT_PT_panel"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Dragon Tools"

    def draw(self, context):
        layout = self.layout
        settings = context.scene.dpt_settings

        layout.label(text="Lower Jaw Detection")
        layout.prop(settings, "lower_z_percent")
        layout.prop(settings, "front_percent")
        layout.prop(settings, "expand_steps")
        layout.operator("dpt.auto_select_lower_jaw", icon="VIEWZOOM")
        layout.operator("dpt.separate_selected_jaw", icon="MOD_EXPLODE")


classes = (
    DPT_Settings,
    DPT_OT_auto_select_lower_jaw,
    DPT_OT_separate_selected_jaw,
    DPT_PT_panel,
)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.dpt_settings = bpy.props.PointerProperty(type=DPT_Settings)


def unregister():
    del bpy.types.Scene.dpt_settings
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)


if __name__ == "__main__":
    register()
