from pathlib import Path

import bpy
from mathutils import Vector


ROAD_ROOT = Path(r"C:\Users\ljh\tralalero Shooter\Assets\ShooterSurvival\Models\MeshyAI\Stage01_Noryangjin")

ROADS = [
    {
        "name": "straight",
        "path": ROAD_ROOT / "046_STAGE01_NRY_ROAD_038_Noryangjin_modular_straight_timber_road_module" / "046_STAGE01_NRY_ROAD_038_Noryangjin_modular_straight_timber_road_module.fbx",
        "prefab_scale": (610.0, 430.0, 520.0),
    },
    {
        "name": "left90",
        "path": ROAD_ROOT / "047_STAGE01_NRY_ROAD_039_Noryangjin_modular_left_90_timber_road_module" / "047_STAGE01_NRY_ROAD_039_Noryangjin_modular_left_90_timber_road_module.fbx",
        "prefab_scale": (525.0, 540.0, 562.0),
    },
    {
        "name": "right90",
        "path": ROAD_ROOT / "048_STAGE01_NRY_ROAD_040_Noryangjin_modular_right_90_timber_road_module" / "048_STAGE01_NRY_ROAD_040_Noryangjin_modular_right_90_timber_road_module.fbx",
        "prefab_scale": (610.0, 540.0, 560.0),
    },
]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def bounds_world(objects):
    corners = []
    for obj in objects:
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    mn = Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners)))
    mx = Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners)))
    return mn, mx, mx - mn


def transform_mesh_vertices(objects, center, scale):
    for obj in objects:
        inv = obj.matrix_world.inverted()
        for vertex in obj.data.vertices:
            world = obj.matrix_world @ vertex.co
            corrected = Vector(
                (
                    center.x + (world.x - center.x) * scale.x,
                    center.y + (world.y - center.y) * scale.y,
                    center.z + (world.z - center.z) * scale.z,
                )
            )
            vertex.co = inv @ corrected
        obj.data.update()


def export_corrected(road, target_width):
    clear_scene()
    path = road["path"]
    bpy.ops.import_scene.fbx(filepath=str(path))
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not objects:
        raise RuntimeError(f"No mesh objects in {path}")

    mn, mx, size = bounds_world(objects)
    center = (mn + mx) * 0.5
    prefab_scale = Vector(road["prefab_scale"])
    scale = Vector((target_width / size.x, prefab_scale.y, prefab_scale.z))
    transform_mesh_vertices(objects, center, scale)

    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_NONE",
        bake_space_transform=False,
        object_types={"MESH"},
        add_leaf_bones=False,
        path_mode="AUTO",
    )

    _, _, new_size = bounds_world(objects)
    print(f"{road['name']}: {tuple(round(v, 4) for v in size)} -> {tuple(round(v, 4) for v in new_size)}")


def main():
    current_world_widths = []
    for road in ROADS:
        clear_scene()
        bpy.ops.import_scene.fbx(filepath=str(road["path"]))
        objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
        _, _, size = bounds_world(objects)
        current_world_widths.append(size.x * road["prefab_scale"][0])

    target_width = sum(current_world_widths[1:]) / 2.0
    print(f"target_width={target_width:.4f}")
    for road in ROADS:
        export_corrected(road, target_width)


if __name__ == "__main__":
    main()
