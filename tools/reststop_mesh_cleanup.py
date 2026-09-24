"""Small Blender mesh cleanup preserving imported corner normals and UVs."""
import bmesh
from mathutils import Vector


def clean_degenerate_preserving_normals(mesh):
    normals = [tuple(normal.vector) for normal in mesh.corner_normals]
    lo = Vector(tuple(min(vertex.co[i] for vertex in mesh.vertices) for i in range(3)))
    hi = Vector(tuple(max(vertex.co[i] for vertex in mesh.vertices) for i in range(3)))
    threshold = ((hi - lo).length * 1e-7) ** 2 * .04
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()
    assert len(bm.faces) == len(mesh.polygons)
    layer_name = 'retained_source_loop'
    assert mesh.attributes.get(layer_name) is None
    layer = bm.loops.layers.int.new(layer_name)
    for face in bm.faces:
        polygon = mesh.polygons[face.index]
        lookup = {mesh.loops[index].vertex_index: index for index in polygon.loop_indices}
        assert set(lookup) == {vertex.index for vertex in face.verts}
        for loop in face.loops:
            loop[layer] = lookup[loop.vert.index]
    bad_faces = [face for face in bm.faces if face.calc_area() < threshold]
    report = {'removed_faces': len(bad_faces), 'removed_surface_area': sum(face.calc_area() for face in bad_faces)}
    if bad_faces:
        bmesh.ops.delete(bm, geom=bad_faces, context='FACES_ONLY')
    wire = [edge for edge in bm.edges if not edge.link_faces]
    report['removed_wire_edges'] = len(wire)
    if wire:
        bmesh.ops.delete(bm, geom=wire, context='EDGES')
    loose = [vertex for vertex in bm.verts if not vertex.link_edges]
    report['removed_unused_vertices'] = len(loose)
    if loose:
        bmesh.ops.delete(bm, geom=loose, context='VERTS')
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    mapping = mesh.attributes[layer_name]
    assert mapping.domain == 'CORNER'
    retained_normals = [normals[item.value] for item in mapping.data]
    mesh.attributes.remove(mapping)
    mesh.normals_split_custom_set(retained_normals)
    error = max((normal.vector - Vector(expected)).length for normal, expected in zip(mesh.corner_normals, retained_normals))
    assert error < .001, error
    report['max_retained_corner_normal_error'] = error
    return report
