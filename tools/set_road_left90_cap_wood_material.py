import shutil
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(r"C:\Users\ljh\tralalero Shooter")
FBX_PATH = ROOT / "Assets" / "ShooterSurvival" / "Models" / "MeshyAI" / "TestFolder" / "Road_Left90.fbx"
BACKUP_PATH = FBX_PATH.with_suffix(".fbx.before_wood_cap_material_backup")
Z_EPSILON = 0.002
WOOD_MATERIAL_NAME = "Cut_Wood_Brown"
WOOD_COLOR = (0.27, 0.105, 0.045, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def get_or_create_wood_material():
    material = bpy.data.materials.get(WOOD_MATERIAL_NAME)
    if material is None:
        material = bpy.data.materials.new(WOOD_MATERIAL_NAME)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = WOOD_COLOR
        bsdf.inputs["Roughness"].default_value = 0.65
        bsdf.inputs["Metallic"].default_value = 0.0
    material.diffuse_color = WOOD_COLOR
    return material


def assign_top_cap_material(obj, material):
    if material.name not in [slot.name for slot in obj.data.materials]:
        obj.data.materials.append(material)
    material_index = [slot.name for slot in obj.data.materials].index(material.name)

    world_vertices = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    top_z = max(vertex.z for vertex in world_vertices)
    changed = 0

    for polygon in obj.data.polygons:
        if all(abs(world_vertices[index].z - top_z) <= Z_EPSILON for index in polygon.vertices):
            polygon.material_index = material_index
            changed += 1

    return changed, top_z, material_index


def main():
    if not FBX_PATH.exists():
        raise FileNotFoundError(FBX_PATH)

    if not BACKUP_PATH.exists():
        shutil.copy2(FBX_PATH, BACKUP_PATH)

    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(FBX_PATH))

    wood = get_or_create_wood_material()
    total_changed = 0
    details = []
    for obj in [item for item in bpy.context.scene.objects if item.type == "MESH"]:
        changed, top_z, material_index = assign_top_cap_material(obj, wood)
        total_changed += changed
        details.append((obj.name, changed, top_z, material_index))

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH"},
        add_leaf_bones=False,
    )

    print(f"Assigned {total_changed} top cap polygons to {WOOD_MATERIAL_NAME}: {FBX_PATH}")
    print(f"Details: {details}")
    print(f"Backup: {BACKUP_PATH}")


if __name__ == "__main__":
    main()
