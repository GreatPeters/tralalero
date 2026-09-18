"""Inspect existing shark topology/UV correspondence without modifying source FBXs."""
import bpy
import hashlib
import json
from pathlib import Path
import struct

ROOT=Path.cwd()
OUT=ROOT/'map-concepts/skins-reststop-2026-09-12'
OUT.mkdir(parents=True,exist_ok=True)
rows=[]
def digest(values):
    h=hashlib.sha256()
    for value in values:h.update(struct.pack('<f',float(value)))
    return h.hexdigest()
for path in sorted((ROOT/'Assets/JH/Model/Player/Sharks').rglob('*.fbx')):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        mesh=obj.data;mesh.calc_loop_triangles()
        meshes.append({'name':obj.name,'vertices':len(mesh.vertices),'triangles':len(mesh.loop_triangles),
            'vertexHash':digest(c for v in mesh.vertices for c in v.co),
            'uvHash':digest(c for uv in mesh.uv_layers.active.data for c in uv.uv) if mesh.uv_layers.active else None,
            'uvLayers':[layer.name for layer in mesh.uv_layers],'dimensions':list(obj.dimensions),
            'groups':[g.name for g in obj.vertex_groups],
            'materials':[m.name if m else None for m in mesh.materials]})
    rows.append({'file':str(path.relative_to(ROOT)),'meshes':meshes,
        'bones':[b.name for obj in bpy.context.scene.objects if obj.type=='ARMATURE' for b in obj.data.bones],
        'images':[img.filepath for img in bpy.data.images if img.filepath]})
    print(path.stem,[(m['vertices'],m['uvHash'][:12] if m['uvHash'] else None) for m in meshes],flush=True)
(OUT/'existing-shark-uv-audit.json').write_text(json.dumps(rows,indent=2,ensure_ascii=False),encoding='utf-8')
