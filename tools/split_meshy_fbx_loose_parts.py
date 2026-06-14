from __future__ import annotations

import argparse
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Split a MeshyAI FBX containing several disconnected road modules into separate FBX files."
    )
    parser.add_argument("--src", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    return parser.parse_args()


def world_bounds(obj: bpy.types.Object) -> tuple[Vector, Vector]:
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    mins = Vector((min(p[i] for p in points) for i in range(3)))
    maxs = Vector((max(p[i] for p in points) for i in range(3)))
    return mins, maxs


def bounds_for_objects(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    mins_list, maxs_list = zip(*(world_bounds(obj) for obj in objects))
    mins = Vector((min(v[i] for v in mins_list) for i in range(3)))
    maxs = Vector((max(v[i] for v in maxs_list) for i in range(3)))
    return mins, maxs


def split_loose_parts() -> list[bpy.types.Object]:
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError("No mesh objects were imported from the FBX.")

    bpy.ops.object.select_all(action="DESELECT")
    for obj in mesh_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_objects[0]
    if len(mesh_objects) > 1:
        bpy.ops.object.join()

    active = bpy.context.view_layer.objects.active
    active.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.separate(type="LOOSE")
    bpy.ops.object.mode_set(mode="OBJECT")

    return [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and len(obj.data.polygons) > 0
    ]


def cluster_into_three(objects: list[bpy.types.Object]) -> list[list[bpy.types.Object]]:
    centers = []
    for obj in objects:
        mins, maxs = world_bounds(obj)
        centers.append((mins + maxs) * 0.5)

    ranges = [
        max(center[i] for center in centers) - min(center[i] for center in centers)
        for i in range(3)
    ]
    axis = max(range(3), key=lambda idx: ranges[idx])

    intervals = []
    for obj in objects:
        mins, maxs = world_bounds(obj)
        intervals.append([mins[axis], maxs[axis], [obj]])
    intervals.sort(key=lambda item: item[0])

    overall_min = min(item[0] for item in intervals)
    overall_max = max(item[1] for item in intervals)
    gap_threshold = max((overall_max - overall_min) * 0.025, 0.001)

    merged: list[list[float | list[bpy.types.Object]]] = []
    for start, end, group in intervals:
        if not merged or start - merged[-1][1] > gap_threshold:
            merged.append([start, end, list(group)])
        else:
            merged[-1][1] = max(merged[-1][1], end)
            merged[-1][2].extend(group)

    while len(merged) > 3:
        best_index = min(
            range(len(merged) - 1),
            key=lambda idx: merged[idx + 1][0] - merged[idx][1],
        )
        merged[best_index][1] = max(merged[best_index][1], merged[best_index + 1][1])
        merged[best_index][2].extend(merged[best_index + 1][2])
        del merged[best_index + 1]

    if len(merged) != 3:
        raise RuntimeError(f"Expected 3 spatial groups after loose split, found {len(merged)}.")

    return [item[2] for item in merged]  # type: ignore[return-value]


def join_group(objects: list[bpy.types.Object], name: str) -> bpy.types.Object:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    joined.data.name = f"{name}_Mesh"
    return joined


def classify_and_join(groups: list[list[bpy.types.Object]]) -> list[bpy.types.Object]:
    group_infos = []
    for group in groups:
        mins, maxs = bounds_for_objects(group)
        dims = maxs - mins
        center = (mins + maxs) * 0.5
        sorted_dims = sorted((dims.x, dims.y, dims.z), reverse=True)
        group_infos.append(
            {
                "group": group,
                "center": center,
                "second_largest_dim": sorted_dims[1],
            }
        )

    straight_info = min(group_infos, key=lambda info: info["second_largest_dim"])
    corner_infos = [info for info in group_infos if info is not straight_info]

    ranges = [
        max(info["center"][axis] for info in group_infos)
        - min(info["center"][axis] for info in group_infos)
        for axis in range(3)
    ]
    layout_axis = max(range(3), key=lambda axis: ranges[axis])
    corner_infos.sort(key=lambda info: info["center"][layout_axis])

    joined = [
        join_group(corner_infos[0]["group"], "Road_Left90"),
        join_group(corner_infos[1]["group"], "Road_Right90"),
        join_group(straight_info["group"], "Road_Straight"),
    ]
    return joined


def center_mesh_at_origin(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    mins, maxs = world_bounds(obj)
    center = (mins + maxs) * 0.5
    for vertex in obj.data.vertices:
        vertex.co -= center
    obj.location = (0.0, 0.0, 0.0)


def export_object(obj: bpy.types.Object, out_path: Path) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=str(out_path),
        use_selection=True,
        object_types={"MESH"},
        path_mode="COPY",
        embed_textures=False,
        add_leaf_bones=False,
    )


def copy_texture_siblings(src: Path, out_dir: Path) -> None:
    for texture in src.parent.glob(f"{src.stem}*.png"):
        shutil.copy2(texture, out_dir / texture.name)


def main() -> None:
    args = parse_args()
    src = args.src.resolve()
    out_dir = args.out.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        bpy.ops.preferences.addon_enable(module="io_scene_fbx")
    except Exception:
        pass

    bpy.ops.import_scene.fbx(filepath=str(src))

    loose_objects = split_loose_parts()
    groups = cluster_into_three(loose_objects)
    joined_objects = classify_and_join(groups)

    copy_texture_siblings(src, out_dir)

    for obj in joined_objects:
        center_mesh_at_origin(obj)
        export_object(obj, out_dir / f"{obj.name}.fbx")

    print(f"Imported: {src}")
    print(f"Loose parts: {len(loose_objects)}")
    print(f"Spatial groups: {len(groups)}")
    for obj in joined_objects:
        mins, maxs = world_bounds(obj)
        dims = maxs - mins
        print(
            f"Exported {obj.name}.fbx "
            f"vertices={len(obj.data.vertices)} polygons={len(obj.data.polygons)} "
            f"dims=({dims.x:.4f}, {dims.y:.4f}, {dims.z:.4f})"
        )
    print(f"Output: {out_dir}")


if __name__ == "__main__":
    main()
