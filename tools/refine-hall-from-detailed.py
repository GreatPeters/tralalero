import bpy,json,hashlib,sys,argparse
import numpy as np
from pathlib import Path
from mathutils import Vector
parser=argparse.ArgumentParser();parser.add_argument('--key',choices=['reststop_hall','reststop_restroom'],default='reststop_hall');parser.add_argument('--triangles',type=int,default=65000);args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
root=Path(__file__).resolve().parent.parent;source=next((root/'outputs/skins-reststop-2026-09-12/production/reststop').glob(args.key+'_*'));out=root/'outputs/reststop-repairs-2026-09-12'/args.key;out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source/'model.blend'));high=bpy.data.objects['Detailed_Source'];low=bpy.data.objects['Asset_Optimized']
def bounds(obj):
    points=[obj.matrix_world@Vector(v) for v in obj.bound_box]
    return Vector([min(p[i] for p in points) for i in range(3)]),Vector([max(p[i] for p in points) for i in range(3)])
a,b=bounds(low);c,d=bounds(high);high.scale*=(b-a).length/(d-c).length;bpy.context.view_layer.update();c,d=bounds(high);high.location+=(a+b-c-d)*.5
high.hide_render=False;high.hide_set(False);low.hide_render=True;bpy.ops.object.select_all(action='DESELECT');high.select_set(True);bpy.context.view_layer.objects.active=high
high.data.calc_loop_triangles();before=len(high.data.loop_triangles);modifier=high.modifiers.new('Roof-preserving detail','DECIMATE');modifier.ratio=min(1,args.triangles/before);modifier.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=modifier.name);high.data.calc_loop_triangles()
images={}
for material in high.data.materials:
    if material and material.use_nodes:
        for node in material.node_tree.nodes:
            if node.type=='TEX_IMAGE' and node.image:images[node.image.name]=node.image.filepath
textures=out/'textures';textures.mkdir(exist_ok=True)
base=bpy.data.images['Image_0'];base.filepath_raw=str(textures/'BaseColor.png');base.file_format='PNG';base.save()
packed=bpy.data.images['Image_1'];values=np.array(packed.pixels[:],dtype=np.float32).reshape(packed.size[1],packed.size[0],4)
for name,channel in [('Roughness',1),('Metallic',2)]:
    image=bpy.data.images.new(name,width=packed.size[0],height=packed.size[1],alpha=True);image.colorspace_settings.name='Non-Color';pixels=np.ones_like(values);pixels[:,:,:3]=values[:,:,channel,None];image.pixels.foreach_set(pixels.ravel());image.filepath_raw=str(textures/(name+'.png'));image.file_format='PNG';image.save()
normal=bpy.data.images.new('Flat tangent normal',width=4,height=4,alpha=True);normal.colorspace_settings.name='Non-Color';normal.generated_color=(.5,.5,1,1);normal.filepath_raw=str(textures/'Normal.png');normal.file_format='PNG';normal.save()
scene=bpy.context.scene
if scene.render.engine=='CYCLES':scene.cycles.device='CPU';scene.cycles.samples=12
scene.render.resolution_x=1024;scene.render.resolution_y=768;scene.render.resolution_percentage=100;scene.render.filepath=str(out/'refined-preview.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'refined.blend'));bpy.ops.export_scene.fbx(filepath=str(out/'refined.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,path_mode='AUTO',use_mesh_modifiers=True)
report={'source':str(source/'model.blend'),'sourceTriangles':before,'triangles':len(high.data.loop_triangles),'images':images,'reason':f'The35k export tears building surfaces. Retain{args.triangles} triangles from detailed source with its own UV textures.'}
(out/'refined-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report),flush=True)
