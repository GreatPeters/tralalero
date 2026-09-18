import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'outputs/harbor-opening-refinement-2026-09-16/merchant-rigged/Merchant-Greeting.blend'))
bpy.context.scene.frame_set(61)
mesh=bpy.data.objects['MerchantBody'];evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
edges=[]
for edge in mesh.data.edges:
 a,b=edge.vertices;before=(mesh.data.vertices[a].co-mesh.data.vertices[b].co).length;after=(evaluated.vertices[a].co-evaluated.vertices[b].co).length
 if before>.005 and after/before>5:
  edges.append({'ratio':round(after/before,1),'length':round(after,3),'vertices':[{'id':i,'position':[round(x/2.6,3) for x in mesh.data.vertices[i].co],'weights':[(mesh.vertex_groups[g.group].name,round(g.weight,3)) for g in mesh.data.vertices[i].groups]} for i in [a,b]]})
(root/'map-concepts/harbor-opening-refinement-2026-09-16/merchant-stretch.json').write_text(json.dumps(sorted(edges,key=lambda e:-e['length'])[:12],indent=2))
