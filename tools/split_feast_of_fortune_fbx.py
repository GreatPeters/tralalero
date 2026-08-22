"""Split the Feast of Fortune Meshy FBX into independent left/right wall models.

Run with Blender, not CPython:
  blender --background --python tools/split_feast_of_fortune_fbx.py -- \
    source.fbx FeastOfFortune_Left.fbx FeastOfFortune_Right.fbx
"""

import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


def delete_other_half(mesh, keep_left, split_x):
    editable = bmesh.new()
    editable.from_mesh(mesh)

    faces_to_delete = []
    for face in editable.faces:
        center_x = sum(vertex.co.x for vertex in face.verts) / len(face.verts)
        if (center_x < split_x) != keep_left:
            faces_to_delete.append(face)

    bmesh.ops.delete(editable, geom=faces_to_delete, context="FACES")
    unused_vertices = [vertex for vertex in editable.verts if not vertex.link_faces]
    if unused_vertices:
        bmesh.ops.delete(editable, geom=unused_vertices, context="VERTS")

    min_x = min(vertex.co.x for vertex in editable.verts)
    max_x = max(vertex.co.x for vertex in editable.verts)
    min_y = min(vertex.co.y for vertex in editable.verts)
    max_y = max(vertex.co.y for vertex in editable.verts)
    min_z = min(vertex.co.z for vertex in editable.verts)
    pivot_offset = Vector(((min_x + max_x) * 0.5, (min_y + max_y) * 0.5, min_z))
    for vertex in editable.verts:
        vertex.co -= pivot_offset

    editable.to_mesh(mesh)
    editable.free()
    mesh.update()


def export_half(source, output_path, name, keep_left, split_x):
    mesh = source.data.copy()
    mesh.name = name
    delete_other_half(mesh, keep_left, split_x)

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )
    bpy.data.objects.remove(obj, do_unlink=True)


def main():
    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) != 3:
        raise RuntimeError("Expected source, left output, and right output FBX paths.")

    source_path, left_output, right_output = map(Path, args)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source_path))

    source_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(source_objects) != 1:
        raise RuntimeError(f"Expected one source mesh, found {len(source_objects)}")

    source = source_objects[0]
    bpy.context.view_layer.objects.active = source
    source.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    min_x = min(vertex.co.x for vertex in source.data.vertices)
    max_x = max(vertex.co.x for vertex in source.data.vertices)
    split_x = (min_x + max_x) * 0.5

    crossing_faces = [
        polygon
        for polygon in source.data.polygons
        if min(source.data.vertices[index].co.x for index in polygon.vertices) < split_x
        and max(source.data.vertices[index].co.x for index in polygon.vertices) > split_x
    ]
    if crossing_faces:
        raise RuntimeError(
            f"Split plane crosses {len(crossing_faces)} faces; refusing to create torn wall halves."
        )

    export_half(source, left_output, "FeastOfFortune_Left", True, split_x)
    export_half(source, right_output, "FeastOfFortune_Right", False, split_x)
    print(f"MESHY_SPLIT=left:{left_output}|right:{right_output}|split_x:{split_x}")


if __name__ == "__main__":
    main()
