import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def object_bounds_world(obj):
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    min_corner = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    max_corner = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    return min_corner, max_corner


def measure(path):
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(path))
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not objects:
        return {"path": str(path), "error": "no mesh objects"}

    mins = []
    maxs = []
    object_results = []
    for obj in objects:
        mn, mx = object_bounds_world(obj)
        mins.append(mn)
        maxs.append(mx)
        object_results.append(
            {
                "name": obj.name,
                "dimensions": [round(v, 6) for v in obj.dimensions],
                "min": [round(v, 6) for v in mn],
                "max": [round(v, 6) for v in mx],
                "scale": [round(v, 6) for v in obj.scale],
            }
        )

    scene_min = Vector((min(v.x for v in mins), min(v.y for v in mins), min(v.z for v in mins)))
    scene_max = Vector((max(v.x for v in maxs), max(v.y for v in maxs), max(v.z for v in maxs)))
    scene_size = scene_max - scene_min
    return {
        "path": str(path),
        "scene_size": [round(v, 6) for v in scene_size],
        "scene_min": [round(v, 6) for v in scene_min],
        "scene_max": [round(v, 6) for v in scene_max],
        "objects": object_results,
    }


def main():
    paths = [Path(arg) for arg in sys.argv[sys.argv.index("--") + 1 :]]
    print(json.dumps([measure(path) for path in paths], indent=2))


if __name__ == "__main__":
    main()
