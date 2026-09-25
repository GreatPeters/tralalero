"""Sample the actual T03 ceramic metallic/roughness map in the bowl region."""
import json
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

root=Path(__file__).resolve().parents[1]
out=root/'outputs/reststop-production-2026-09-24'
dest=out/'reviews/T03-ceramic-pbr-samples-r1.json';assert not dest.exists()
bpy.ops.wm.open_mainfile(filepath=str(out/'manual/T03-r3/source/model.blend'))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
mesh=obj.data
points=[obj.matrix_world@Vector(v) for v in obj.bound_box]
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)))
hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));span=hi-lo
report=[]
for material_index,material in enumerate(mesh.materials):
    shader=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    record={'material':material.name,'channels':{}}
    for channel in ('Metallic','Roughness'):
        socket=shader.inputs[channel]
        if not socket.is_linked:
            record['channels'][channel]={'constant':socket.default_value};continue
        link=socket.links[0];node=link.from_node
        assert node.type=='SEPARATE_COLOR',(node.type,channel)
        index={'Red':0,'Green':1,'Blue':2}[link.from_socket.name]
        image_node=node.inputs['Color'].links[0].from_node;assert image_node.type=='TEX_IMAGE'
        image=image_node.image;pixels=np.empty(len(image.pixels),dtype=np.float32);image.pixels.foreach_get(pixels)
        w,h=image.size;pixels=pixels.reshape(h,w,4)
        samples=[]
        for polygon in mesh.polygons:
            p=obj.matrix_world@polygon.center
            if polygon.material_index!=material_index or not(.15<(p.z-lo.z)/span.z<.56 and (p.y-lo.y)/span.y<.61):continue
            uv=sum((mesh.uv_layers.active.data[i].uv for i in polygon.loop_indices),Vector((0,0)))/len(polygon.loop_indices)
            samples.append(float(pixels[min(h-1,max(0,int(uv.y*h))),min(w-1,max(0,int(uv.x*w))),index]))
        record['channels'][channel]={'samples':len(samples),'minimum':min(samples),'median':float(np.median(samples)),'p95':float(np.quantile(samples,.95)),'maximum':max(samples)}
    report.append(record)
dest.write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report),flush=True)
