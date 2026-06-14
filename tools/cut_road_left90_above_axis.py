import shutil
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector


ROOT = Path(r"C:\Users\ljh\tralalero Shooter")
FBX_PATH = ROOT / "Assets" / "ShooterSurvival" / "Models" / "MeshyAI" / "TestFolder" / "Road_Left90.fbx"
BACKUP_PATH = FBX_PATH.with_suffix(".fbx.before_cut_backup")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def mesh_world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    mins = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maxs = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return mins, maxs


def cut_mesh_above_world_z(obj, cut_z=0.0):
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

    loose_edges = [edge for edge in bm.edges if edge.is_valid and len(edge.link_faces) == 0]
    if loose_edges:
        bmesh.ops.delete(bm, geom=loose_edges, context="EDGES")
    loose_verts = [vert for vert in bm.verts if vert.is_valid and len(vert.link_edges) == 0]
    if loose_verts:
        bmesh.ops.delete(bm, geom=loose_verts, context="VERTS")

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
    if not meshes:
        raise RuntimeError("No mesh objects found in FBX")

    print("Imported meshes:")
    for obj in meshes:
        mins, maxs = mesh_world_bounds(obj)
        print(f"- {obj.name}: z=[{mins.z:.4f}, {maxs.z:.4f}], verts={len(obj.data.vertices)}")

    for obj in meshes:
        mins, maxs = mesh_world_bounds(obj)
        if maxs.z > 0.0001:
            cut_mesh_above_world_z(obj, cut_z=0.0)

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH"},
        add_leaf_bones=False,
    )

    print(f"Saved cut FBX: {FBX_PATH}")
    print(f"Backup: {BACKUP_PATH}")


if __name__ == "__main__":
    main()
