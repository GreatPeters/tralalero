import bmesh
import bpy


FBX_PATH = r"C:\Users\ljh\tralalero Shooter\Assets\ShooterSurvival\Models\MeshyAI\TestFolder\Road_Left90.fbx"
Z_EPSILON = 0.002


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete()
bpy.ops.import_scene.fbx(filepath=FBX_PATH)

results = []
for obj in [item for item in bpy.context.scene.objects if item.type == "MESH" and item.name.startswith("Road_Left90")]:
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    bm.edges.ensure_lookup_table()
    top_z = max((obj.matrix_world @ vert.co).z for vert in bm.verts)
    top_boundary_edges = sum(
        1
        for edge in bm.edges
        if edge.is_boundary
        and all(abs((obj.matrix_world @ vert.co).z - top_z) <= Z_EPSILON for vert in edge.verts)
    )
    results.append((obj.name, top_z, len(obj.data.vertices), len(obj.data.polygons), top_boundary_edges))
    bm.free()

print(results)
