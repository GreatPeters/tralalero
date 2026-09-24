"""Fresh-import checks for the corrected open frame and its thin guide rail."""
import argparse,json,sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser();parser.add_argument('--folder',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);folder=Path(args.folder).resolve();reports=[]
for suffix in ('fbx','glb'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if suffix=='fbx':bpy.ops.import_scene.fbx(filepath=str(folder/'model.fbx'))
    else:bpy.ops.import_scene.gltf(filepath=str(folder/'model.glb'))
    vertices=[];triangles=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        obj.data.calc_loop_triangles();offset=len(vertices)
        vertices.extend(obj.matrix_world@v.co for v in obj.data.vertices)
        triangles.extend(tuple(i+offset for i in t.vertices) for t in obj.data.loop_triangles)
    lo=Vector(tuple(min(v[i] for v in vertices) for i in range(3)));hi=Vector(tuple(max(v[i] for v in vertices) for i in range(3)));d=hi-lo
    tree=BVHTree.FromPolygons(vertices,triangles,all_triangles=True)
    open_samples=[]
    for xf in (.2,.5,.8):
        for zf in (.2,.45,.75):
            hit=tree.ray_cast(Vector((lo.x+d.x*xf,hi.y+d.length,lo.z+d.z*zf)),Vector((0,-1,0)))[0]
            open_samples.append(hit is None)
    center_rail=[]
    for i in range(129):
        y=lo.y+d.y*i/128
        hit=tree.ray_cast(Vector(((lo.x+hi.x)/2,y,lo.z+d.z*.065)),Vector((0,0,-1)))[0]
        if hit is not None and hit.z<lo.z+d.z*.012:center_rail.append((y,hit.z))
    assert center_rail,'No lower guide at the center of the doorway'
    ymid=(center_rail[0][0]+center_rail[-1][0])/2;rail_samples=[]
    for xf in (.1,.3,.5,.7,.9):
        hit=tree.ray_cast(Vector((lo.x+d.x*xf,ymid,lo.z+d.z*.065)),Vector((0,0,-1)))[0]
        rail_samples.append(hit is not None and hit.z<lo.z+d.z*.012)
    reports.append({'format':suffix,'triangles':len(triangles),'dimensions':list(d),'opening_clear_9_rays':open_samples,'rail_continuous_5_rays':rail_samples,'guide_depth_fraction':(center_rail[-1][0]-center_rail[0][0])/d.y})
ok=all(all(r['opening_clear_9_rays']) and all(r['rail_continuous_5_rays']) and r['guide_depth_fraction']<.85 for r in reports)
(folder/'opening-validation.json').write_text(json.dumps({'ok':ok,'reports':reports},indent=2),encoding='utf8')
print(json.dumps({'ok':ok,'reports':reports}),flush=True);assert ok,'Doorway or guide rail did not survive export'
