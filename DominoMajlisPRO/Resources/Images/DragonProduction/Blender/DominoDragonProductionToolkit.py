
bl_info = {
    "name": "Domino Living Emblem Studio - Rig Fix",
    "author": "Domino Majlis PRO",
    "version": (1, 1, 0),
    "blender": (4, 0, 0),
    "location": "View3D > Sidebar > Living Emblem",
    "description": "Creates a real Blender Armature and binds Dragon_Master_Sculpt directly without manual Ctrl+P.",
    "category": "3D View",
}

import bpy
from mathutils import Vector

MESH_NAME = "Dragon_Master_Sculpt"
RIG_NAME = "Living_Emblem_Rig"


def ensure_object_mode():
    if bpy.ops.object.mode_set.poll():
        bpy.ops.object.mode_set(mode="OBJECT")


def get_mesh():
    obj = bpy.data.objects.get(MESH_NAME)
    if obj is None or obj.type != "MESH":
        raise RuntimeError("Dragon_Master_Sculpt mesh not found.")
    return obj


def world_bounds(obj):
    verts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    return (
        min(v.x for v in verts), max(v.x for v in verts),
        min(v.y for v in verts), max(v.y for v in verts),
        min(v.z for v in verts), max(v.z for v in verts),
    )


def delete_old_rig():
    old = bpy.data.objects.get(RIG_NAME)
    if old:
        bpy.data.objects.remove(old, do_unlink=True)


def create_edit_bone(arm_data, name, head, tail, parent=None):
    b = arm_data.edit_bones.new(name)
    b.head = head
    b.tail = tail
    b.roll = 0
    if parent:
        b.parent = arm_data.edit_bones[parent]
        b.use_connect = False
    return b


def clear_old_vertex_groups(mesh):
    wanted = {"Root", "Neck", "Head", "Jaw", "Horn_L", "Horn_R", "Side_Fin_L", "Side_Fin_R"}
    for vg in list(mesh.vertex_groups):
        if vg.name in wanted:
            mesh.vertex_groups.remove(vg)


def make_group(mesh, name):
    vg = mesh.vertex_groups.get(name)
    if vg is None:
        vg = mesh.vertex_groups.new(name=name)
    return vg


def assign_simple_weights(mesh):
    clear_old_vertex_groups(mesh)

    vg_root = make_group(mesh, "Root")
    vg_neck = make_group(mesh, "Neck")
    vg_head = make_group(mesh, "Head")
    vg_jaw = make_group(mesh, "Jaw")
    vg_horn_l = make_group(mesh, "Horn_L")
    vg_horn_r = make_group(mesh, "Horn_R")
    vg_fin_l = make_group(mesh, "Side_Fin_L")
    vg_fin_r = make_group(mesh, "Side_Fin_R")

    min_x, max_x, min_y, max_y, min_z, max_z = world_bounds(mesh)
    cx = (min_x + max_x) * 0.5
    cy = (min_y + max_y) * 0.5
    sx = max_x - min_x
    sy = max_y - min_y
    sz = max_z - min_z

    # The current dragon snout points roughly toward negative Y.
    jaw_z = min_z + sz * 0.42
    jaw_front_y = cy - sy * 0.05
    neck_y = cy + sy * 0.10
    horn_z = min_z + sz * 0.72
    side_x = sx * 0.24

    for v in mesh.data.vertices:
        w = mesh.matrix_world @ v.co

        lower = w.z < jaw_z
        front = w.y < jaw_front_y
        center = abs(w.x - cx) < sx * 0.28

        neck = (w.y > neck_y and w.z < min_z + sz * 0.55)
        top = w.z > horn_z
        left = w.x < cx
        right = w.x > cx
        side_left = w.x < cx - side_x and w.z < horn_z
        side_right = w.x > cx + side_x and w.z < horn_z

        if lower and front and center:
            vg_jaw.add([v.index], 1.0, "REPLACE")
        elif top and left:
            vg_horn_l.add([v.index], 0.85, "REPLACE")
            vg_head.add([v.index], 0.15, "ADD")
        elif top and right:
            vg_horn_r.add([v.index], 0.85, "REPLACE")
            vg_head.add([v.index], 0.15, "ADD")
        elif side_left:
            vg_fin_l.add([v.index], 0.75, "REPLACE")
            vg_head.add([v.index], 0.25, "ADD")
        elif side_right:
            vg_fin_r.add([v.index], 0.75, "REPLACE")
            vg_head.add([v.index], 0.25, "ADD")
        elif neck:
            vg_neck.add([v.index], 0.9, "REPLACE")
            vg_head.add([v.index], 0.1, "ADD")
        else:
            vg_head.add([v.index], 1.0, "REPLACE")


class DLES_OT_generate_real_rig(bpy.types.Operator):
    bl_idname = "dles.generate_real_rig"
    bl_label = "Generate Real Rig + Bind"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        ensure_object_mode()

        mesh = get_mesh()
        delete_old_rig()

        min_x, max_x, min_y, max_y, min_z, max_z = world_bounds(mesh)
        cx = (min_x + max_x) * 0.5
        cy = (min_y + max_y) * 0.5
        sx = max_x - min_x
        sy = max_y - min_y
        sz = max_z - min_z

        front_y = min_y
        back_y = max_y

        arm_data = bpy.data.armatures.new(RIG_NAME + "_Data")
        rig = bpy.data.objects.new(RIG_NAME, arm_data)
        context.collection.objects.link(rig)

        bpy.ops.object.select_all(action="DESELECT")
        rig.select_set(True)
        context.view_layer.objects.active = rig

        bpy.ops.object.mode_set(mode="EDIT")

        create_edit_bone(
            arm_data,
            "Root",
            Vector((cx, cy, min_z)),
            Vector((cx, cy, min_z + sz * 0.16))
        )

        create_edit_bone(
            arm_data,
            "Neck",
            Vector((cx, cy + sy * 0.10, min_z + sz * 0.32)),
            Vector((cx, back_y, min_z + sz * 0.20)),
            "Root"
        )

        create_edit_bone(
            arm_data,
            "Head",
            Vector((cx, cy, min_z + sz * 0.36)),
            Vector((cx, cy, max_z)),
            "Neck"
        )

        create_edit_bone(
            arm_data,
            "Jaw",
            Vector((cx, front_y + sy * 0.25, min_z + sz * 0.43)),
            Vector((cx, front_y, min_z + sz * 0.22)),
            "Head"
        )

        create_edit_bone(
            arm_data,
            "Horn_L",
            Vector((cx - sx * 0.20, cy, min_z + sz * 0.72)),
            Vector((cx - sx * 0.38, cy, max_z + sz * 0.10)),
            "Head"
        )

        create_edit_bone(
            arm_data,
            "Horn_R",
            Vector((cx + sx * 0.20, cy, min_z + sz * 0.72)),
            Vector((cx + sx * 0.38, cy, max_z + sz * 0.10)),
            "Head"
        )

        create_edit_bone(
            arm_data,
            "Side_Fin_L",
            Vector((cx - sx * 0.30, cy, min_z + sz * 0.50)),
            Vector((cx - sx * 0.48, cy, min_z + sz * 0.38)),
            "Head"
        )

        create_edit_bone(
            arm_data,
            "Side_Fin_R",
            Vector((cx + sx * 0.30, cy, min_z + sz * 0.50)),
            Vector((cx + sx * 0.48, cy, min_z + sz * 0.38)),
            "Head"
        )

        bpy.ops.object.mode_set(mode="OBJECT")

        rig.show_in_front = True
        arm_data.display_type = "STICK"

        assign_simple_weights(mesh)

        # Direct binding without Ctrl+P menu.
        mod = mesh.modifiers.get("Living_Emblem_Armature")
        if mod is None:
            mod = mesh.modifiers.new("Living_Emblem_Armature", "ARMATURE")
        mod.object = rig
        mod.use_vertex_groups = True

        mesh.parent = rig
        mesh.matrix_parent_inverse = rig.matrix_world.inverted()

        bpy.ops.object.select_all(action="DESELECT")
        rig.select_set(True)
        mesh.select_set(True)
        context.view_layer.objects.active = rig

        self.report({"INFO"}, "Real Armature created and mesh bound directly. Use Mouth Test.")
        return {"FINISHED"}


class DLES_OT_mouth_test(bpy.types.Operator):
    bl_idname = "dles.mouth_test_v2"
    bl_label = "Mouth Test"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        ensure_object_mode()

        rig = bpy.data.objects.get(RIG_NAME)
        if rig is None or rig.type != "ARMATURE":
            self.report({"ERROR"}, "Living_Emblem_Rig Armature not found.")
            return {"CANCELLED"}

        bpy.ops.object.select_all(action="DESELECT")
        rig.select_set(True)
        context.view_layer.objects.active = rig
        bpy.ops.object.mode_set(mode="POSE")

        for pb in rig.pose.bones:
            pb.rotation_mode = "XYZ"
            pb.rotation_euler = (0, 0, 0)
            pb.location = (0, 0, 0)
            pb.scale = (1, 1, 1)

        jaw = rig.pose.bones.get("Jaw")
        if not jaw:
            self.report({"ERROR"}, "Jaw bone not found.")
            return {"CANCELLED"}

        jaw.rotation_euler[0] = -0.55
        self.report({"INFO"}, "Mouth test applied.")
        return {"FINISHED"}


class DLES_OT_reset_pose(bpy.types.Operator):
    bl_idname = "dles.reset_pose_v2"
    bl_label = "Reset Pose"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        rig = bpy.data.objects.get(RIG_NAME)
        if rig is None:
            return {"CANCELLED"}

        bpy.ops.object.mode_set(mode="OBJECT")
        bpy.ops.object.select_all(action="DESELECT")
        rig.select_set(True)
        context.view_layer.objects.active = rig
        bpy.ops.object.mode_set(mode="POSE")

        for pb in rig.pose.bones:
            pb.rotation_mode = "XYZ"
            pb.rotation_euler = (0, 0, 0)
            pb.location = (0, 0, 0)
            pb.scale = (1, 1, 1)

        self.report({"INFO"}, "Pose reset.")
        return {"FINISHED"}


class DLES_PT_panel(bpy.types.Panel):
    bl_label = "Domino Living Emblem Studio"
    bl_idname = "DLES_PT_panel_v2"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Living Emblem"

    def draw(self, context):
        layout = self.layout

        box = layout.box()
        box.label(text="Rig v2")
        box.operator("dles.generate_real_rig", icon="ARMATURE_DATA")
        box.operator("dles.mouth_test_v2", icon="PLAY")
        box.operator("dles.reset_pose_v2", icon="LOOP_BACK")


classes = (
    DLES_OT_generate_real_rig,
    DLES_OT_mouth_test,
    DLES_OT_reset_pose,
    DLES_PT_panel,
)


def register():
    for c in classes:
        bpy.utils.register_class(c)


def unregister():
    for c in reversed(classes):
        bpy.utils.unregister_class(c)


if __name__ == "__main__":
    register()
