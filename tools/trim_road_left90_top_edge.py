import shutil
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector


ROOT = Path(r"C:\Users\ljh\tralalero Shooter")
FBX_PATH = ROOT / "Assets" / "ShooterSurvival" / "Models" / "MeshyAI" / "TestFolder" / "Road_Left90.fbx"
BACKUP_PATH = FBX_PATH.with_suffix(".fbx.before_top_trim_backup")
CUT_Z = -0.025


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def cut_mesh_above_world_z(obj, cut_z):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    bm = bmesh.from_edit_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    bm.edges.ensure_lookup_table()
    bm.faces.ensure_lookup_table()

    inv = obj.matrix_world.inverted()
    plane_co = inv @ Vector((0.0, 0.0, cut_z))
    plane_no = inv.to_3x3() @ Vector((0.0, 0.0, 1.0))
    plane_no.normalize()

    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    bmesh.ops.bisect_plane(
        bm,
        geom=geom,
        dist=0.0001,
        plane_co=plane_co,
        plane_no=plane_no,
        clear_outer=True,
        clear_inner=False,
        use_snap_center=False,
    )

    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode="OBJECT")


def main():
    if not FBX_PATH.exists():
        raise FileNotFoundError(FBX_PATH)

    if not BACKUP_PATH.exists():
        shutil.copy2(FBX_PATH, BACKUP_PATH)

    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(FBX_PATH))

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    for obj in meshes:
        cut_mesh_above_world_z(obj, CUT_Z)

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH"},
        add_leaf_bones=False,
    )

    print(f"Trimmed top above world Z={CUT_Z}: {FBX_PATH}")
    print(f"Backup: {BACKUP_PATH}")


if __name__ == "__main__":
    main()
