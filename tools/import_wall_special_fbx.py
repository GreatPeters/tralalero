"""Prepare a Meshy Wall_Special model and its packed BaseColor for Unity.

Run with Blender, not CPython:
  blender --background --python tools/import_wall_special_fbx.py -- \
    source.glb Wall_Special.fbx Wall_Special_BaseColor.png \
    [collapse_ratio] [target_vertices]
"""

import hashlib
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


def find_source_mesh():
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    preferred = bpy.data.objects.get("geometry_0")
    if preferred is not None and preferred.type == "MESH":
        unexpected = [
            obj.name for obj in meshes if obj != preferred and obj.name != "Cube"
        ]
        if unexpected:
            raise RuntimeError(
                "The source model contains unexpected additional meshes: "
                + ", ".join(sorted(unexpected))
            )
        return preferred

    if len(meshes) != 1:
        raise RuntimeError(
            f"Expected one source mesh when geometry_0 is absent, found {len(meshes)}."
        )

    return meshes[0]


def import_source(source_path):
    suffix = source_path.suffix.lower()
    if suffix == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(source_path))
    elif suffix in {".glb", ".gltf"}:
        bpy.ops.import_scene.gltf(filepath=str(source_path))
    else:
        raise RuntimeError(f"Unsupported source model format: {suffix}")


def find_principled_material(source):
    fallback = None
    for material in source.data.materials:
        if material is None or not material.use_nodes:
            continue

        shader = next(
            (node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"),
            None,
        )
        if shader is not None and shader.inputs["Base Color"].is_linked:
            return material, shader

        if shader is not None:
            fallback = (material, shader)

    if fallback is not None:
        return fallback

    raise RuntimeError("The source mesh has no Principled material.")


def image_from_input(shader_input):
    """Return an image connected directly or through Blender's Normal Map node."""
    if not shader_input.is_linked:
        return None

    node = shader_input.links[0].from_node
    if node.type == "TEX_IMAGE":
        return node.image

    if node.type == "NORMAL_MAP":
        return image_from_input(node.inputs["Color"])

    return None


def save_packed_image(image, output_path):
    if image is None:
        raise RuntimeError(f"Missing packed texture for {output_path.name}.")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    image.filepath_raw = str(output_path)
    image.file_format = "PNG"
    image.save()


def center_on_floor(source):
    corners = [Vector(corner) for corner in source.bound_box]
    min_x = min(corner.x for corner in corners)
    max_x = max(corner.x for corner in corners)
    min_y = min(corner.y for corner in corners)
    max_y = max(corner.y for corner in corners)
    min_z = min(corner.z for corner in corners)
    pivot = Vector(((min_x + max_x) * 0.5, (min_y + max_y) * 0.5, min_z))

    for vertex in source.data.vertices:
        vertex.co -= pivot
    source.data.update()


def decimate(source, collapse_ratio):
    if collapse_ratio is None:
        return
    if not 0 < collapse_ratio <= 1:
        raise RuntimeError("Collapse ratio must be greater than 0 and at most 1.")

    mesh = bmesh.new()
    mesh.from_mesh(source.data)
    bmesh.ops.remove_doubles(mesh, verts=mesh.verts, dist=0.000001)
    mesh.to_mesh(source.data)
    mesh.free()
    source.data.update()

    bpy.context.view_layer.objects.active = source
    source.select_set(True)
    modifier = source.modifiers.new(name="Wall Special 10K", type="DECIMATE")
    modifier.decimate_type = "COLLAPSE"
    modifier.ratio = collapse_ratio
    bpy.ops.object.modifier_apply(modifier=modifier.name)

    mesh = bmesh.new()
    mesh.from_mesh(source.data)
    loose_vertices = [vertex for vertex in mesh.verts if not vertex.link_faces]
    if loose_vertices:
        bmesh.ops.delete(mesh, geom=loose_vertices, context="VERTS")
    mesh.to_mesh(source.data)
    mesh.free()
    source.data.update()


def match_vertex_target(source, target_vertices):
    if target_vertices is None:
        return
    if target_vertices <= 0:
        raise RuntimeError("Target vertices must be greater than 0.")

    missing_vertices = target_vertices - len(source.data.vertices)
    if missing_vertices < 0:
        raise RuntimeError(
            f"Decimated mesh exceeds target by {-missing_vertices} vertices. "
            "Use a lower collapse ratio."
        )
    if missing_vertices > 32:
        raise RuntimeError(
            f"Decimated mesh is {missing_vertices} vertices below target. "
            "Use a higher collapse ratio."
        )

    mesh = bmesh.new()
    mesh.from_mesh(source.data)
    for _ in range(missing_vertices):
        longest_edge = max(mesh.edges, key=lambda edge: edge.calc_length())
        bmesh.ops.subdivide_edges(
            mesh,
            edges=[longest_edge],
            cuts=1,
            use_grid_fill=False,
        )
    mesh.to_mesh(source.data)
    mesh.free()
    source.data.update()


def export_model(source, output_path):
    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    source.select_set(True)
    bpy.context.view_layer.objects.active = source
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    center_on_floor(source)

    source.name = "Wall_Special"
    source.data.name = "Wall_Special"
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
        path_mode="STRIP",
        embed_textures=False,
    )


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    if "--" not in sys.argv:
        raise RuntimeError(
            "Expected '--' followed by source model, model output, base-color output, "
            "optional collapse ratio, and optional target vertices."
        )

    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) not in {3, 4, 5}:
        raise RuntimeError(
            "Expected source model, model output, base-color output, and optional "
            "collapse ratio and target vertices."
        )

    source_path, model_output, base_output = (
        Path(argument).resolve() for argument in args[:3]
    )
    collapse_ratio = float(args[3]) if len(args) >= 4 else None
    target_vertices = int(args[4]) if len(args) == 5 else None
    if len({source_path, model_output, base_output}) != 3:
        raise RuntimeError(
            "Source model, model output, and base-color output must be distinct paths."
        )

    if not source_path.is_file():
        raise FileNotFoundError(source_path)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    import_source(source_path)

    source = find_source_mesh()
    original_vertices = len(source.data.vertices)
    original_faces = len(source.data.polygons)
    _, shader = find_principled_material(source)
    base_image = image_from_input(shader.inputs["Base Color"])

    save_packed_image(base_image, base_output)
    decimate(source, collapse_ratio)
    match_vertex_target(source, target_vertices)
    export_model(source, model_output)

    print(
        "WALL_SPECIAL_IMPORT="
        f"model:{model_output}|base:{base_output}:{sha256(base_output)}|"
        f"source_vertices:{original_vertices}|source_faces:{original_faces}|"
        f"vertices:{len(source.data.vertices)}|faces:{len(source.data.polygons)}"
    )


if __name__ == "__main__":
    main()
