"""Fresh-import S02 glazing transmission and two-shelf geometry probes."""
import argparse
import json
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

parser = argparse.ArgumentParser()
parser.add_argument('--folder',required=True)
parser.add_argument('--prototype',action='store_true')
parser.add_argument('--glb-only',action='store_true',help='Intermediate PBR assembly without a final FBX yet')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
folder = Path(args.folder).resolve()
assert 'S02' in str(folder)
target = folder/'visibility-validation.json'
assert not target.exists()
reports = {}
required_formats=('glb',) if args.prototype or args.glb_only else ('glb','fbx')
for extension in required_formats:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source = folder/('model.'+extension)
    if extension == 'glb':
        bpy.ops.import_scene.gltf(filepath=str(source))
    else:
        bpy.ops.import_scene.fbx(filepath=str(source))
    vertices, faces, coordinates, surfaces = [], [], [], []
    cached = {}
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        mesh = obj.data
        mesh.calc_loop_triangles()
        offset = len(vertices)
        vertices.extend(obj.matrix_world@v.co for v in mesh.vertices)
        uv = mesh.uv_layers.active
        for tri in mesh.loop_triangles:
            faces.append(tuple(i+offset for i in tri.vertices))
            coordinates.append([Vector((*uv.data[i].uv,0)) for i in tri.loops] if uv else None)
            mat = mesh.materials[tri.material_index]
            if mat not in cached:
                shader = next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
                alpha = shader.inputs['Alpha']
                if alpha.is_linked:
                    node = alpha.links[0].from_node
                    assert node.type=='TEX_IMAGE', 'Inspect an unsupported alpha graph explicitly'
                    im = node.image
                    pix = np.empty(len(im.pixels),dtype=np.float32)
                    im.pixels.foreach_get(pix)
                    cached[mat] = (int(im.size[0]),int(im.size[1]),pix.reshape(-1,4)[:,3])
                else:
                    cached[mat] = float(alpha.default_value)
            surfaces.append(cached[mat])
    lo = Vector(tuple(min(v[i] for v in vertices) for i in range(3)))
    hi = Vector(tuple(max(v[i] for v in vertices) for i in range(3)))
    dim = hi-lo
    tree = BVHTree.FromPolygons(vertices,faces,all_triangles=True)

    def alpha_at(point,index):
        value = surfaces[index]
        if isinstance(value,float):
            return value
        assert coordinates[index] is not None
        mapped = barycentric_transform(point,*(vertices[i] for i in faces[index]),*coordinates[index])
        width,height,pixels = value
        x = max(0,min(width-1,int(mapped.x*width)))
        y = max(0,min(height-1,int(mapped.y*height)))
        return float(pixels[y*width+x])

    def crossings(origin,direction):
        result=[]
        for _ in range(80):
            point,normal,index,distance = tree.ray_cast(origin,direction)
            if point is None:
                break
            result.append((point.copy(),normal,alpha_at(point,index)))
            origin = point+direction*dim.length*.00003
        return result

    front=[]
    for fx in (.25,.5,.75):
        for fz in (.45,.5,.75):
            origin = Vector((lo.x+dim.x*fx,lo.y-dim.length,lo.z+dim.z*fz))
            hits=crossings(origin,Vector((0,1,0)))
            transmission=1.
            first_opaque=None
            clear_count=0
            for point,normal,alpha in hits:
                fraction=(point.y-lo.y)/dim.y
                if alpha>=.9:
                    first_opaque=fraction
                    break
                transmission*=1-alpha
                clear_count+=1
            ok=clear_count>0 and transmission>.65 and first_opaque is not None and first_opaque>.5
            front.append({'x':fx,'z':fz,'clear_surfaces':clear_count,'transmission':transmission,
                          'first_opaque_y':first_opaque,'ok':ok})
    shelves=[]
    for fx in (.25,.5,.75):
        for fy in (.3,.45):
            origin=Vector((lo.x+dim.x*fx,lo.y+dim.y*fy,hi.z+dim.length))
            hits=crossings(origin,Vector((0,0,-1)))
            levels=[(p.z-lo.z)/dim.z for p,n,a in hits if a>=.9 and abs(n.z)>.7]
            lower=any(.28<z<.40 for z in levels)
            upper=any(.56<z<.70 for z in levels)
            shelves.append({'x':fx,'y':fy,'opaque_horizontal_levels':levels,'lower':lower,'upper':upper,'ok':lower and upper})
    reports[extension]={'bounds_min':list(lo),'bounds_max':list(hi),'front':front,'shelves':shelves,
                        'ok':all(r['ok'] for r in front+shelves)}
report={'prototype_only':args.prototype,'required_formats':required_formats,
        'complete_export_check':len(required_formats)==2,'ok':all(r['ok'] for r in reports.values()),'formats':reports,
        'limits':'Geometric/alpha probes at explicit sample lines, not a substitute for rendered visual review.'}
target.write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report),flush=True)
assert report['ok'], 'Glazing or shelf probe failed; retain evidence and inspect'
