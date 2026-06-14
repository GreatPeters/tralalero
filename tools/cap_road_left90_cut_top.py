import shutil
from pathlib import Path

import bpy
import bmesh


ROOT = Path(r"C:\Users\ljh\tralalero Shooter")
FBX_PATH = ROOT / "Assets" / "ShooterSurvival" / "Models" / "MeshyAI" / "TestFolder" / "Road_Left90.fbx"
BACKUP_PATH = FBX_PATH.with_suffix(".fbx.before_cap_backup")
Z_EPSILON = 0.002


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def cap_top_boundary(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    bm = bmesh.from_edit_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    bm.edges.ensure_lookup_table()
    bm.faces.ensure_lookup_table()

    if not bm.verts:
        bpy.ops.object.mode_set(mode="OBJECT")
        return 0

    top_z = max((obj.matrix_world @ vert.co).z for vert in bm.verts)
    top_boundary_edges = [
        edge
        for edge in bm.edges
        if edge.is_boundary
        and all(abs((obj.matrix_world @ vert.co).z - top_z) <= Z_EPSILON for vert in edge.verts)
    ]

    if not top_boundary_edges:
        bpy.ops.object.mode_set(mode="OBJECT")
        return 0

    result = bmesh.ops.holes_fill(
        bm,
        edges=top_boundary_edges,
        sides=0,
    )
    new_faces = [face for face in result.get("faces", []) if face.is_valid]

    if new_faces:
        # Keep the cap triangulated like the imported Meshy geometry.
        bmesh.ops.triangulate(bm, faces=new_faces)

        # Reuse the first material slot so the cap renders instead of staying default gray.
        for face in new_faces:
            if face.is_valid:
                face.material_index = 0

    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode="OBJECT")
    return len(new_faces)


def main():
    if not FBX_PATH.exists():
        raise FileNotFoundError(FBX_PATH)

    if not BACKUP_PATH.exists():
        shutil.copy2(FBX_PATH, BACKUP_PATH)

    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(FBX_PATH))

    total_faces = 0
    for obj in [item for item in bpy.context.scene.objects if item.type == "MESH"]:
        total_faces += cap_top_boundary(obj)

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH"},
        add_leaf_bones=False,
    )

    print(f"Capped {total_faces} top cut face(s): {FBX_PATH}")
    print(f"Backup: {BACKUP_PATH}")


if __name__ == "__main__":
    main()
