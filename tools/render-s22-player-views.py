"""Render player mesh JSON candidates in Blender for anatomy review.

Usage:
  blender -b --factory-startup --python tools/render-s22-player-views.py -- <out_dir> <label>=<mesh.json>[|albedo.png] [...]

Accepts the source geometry format (`vertices`/`triangles`) and the flattened
tail format (`positions`/`submeshes`). Renders side, rear-gameplay, front-3/4
and top views so silhouette changes can be compared before Unity import.
"""
import json,math,sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
BASE=json.loads((ROOT/'outputs/s22-polish-2026-10-01/player-geometry-before.json').read_text(encoding='utf-8'))
SCALE=2.8/(max(v[1] for v in BASE['vertices'])-min(v[1] for v in BASE['vertices']))
VIEWS={
 'side':(Vector((-9.5,0,1.6)),Vector((0,0,1.2))),
 'rear-game':(Vector((0,7.0,5.2)),Vector((0,-0.4,1.1))),
 'front34':(Vector((-4.8,-5.6,2.6)),Vector((0,0,1.2))),
 'rear34':(Vector((4.6,5.4,2.8)),Vector((0,0,1.2))),
 'face':(Vector((-3.2,-3.9,2.0)),Vector((0,-2.1,1.75))),
}

def load(path):
 d=json.loads(Path(path).read_text(encoding='utf-8'))
 if 'positions' in d:
  p=d['positions'];verts=[(p[i]*SCALE,-p[i+2]*SCALE,p[i+1]*SCALE) for i in range(0,len(p),3)]
  uv=[(d['uv'][i],d['uv'][i+1]) for i in range(0,len(d['uv']),2)];subs=[s['triangles'] for s in d['submeshes']]
 else:
  verts=[(v[0]*SCALE,-v[2]*SCALE,v[1]*SCALE) for v in d['vertices']];uv=[tuple(x) for x in d['uv']];subs=d['triangles']
 return verts,uv,subs

def material(key,path):
 mat=bpy.data.materials.new(key);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF')
 tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path if Path(path).is_absolute() else ROOT/path))
 mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.7;mat.use_backface_culling=True
 return mat

def build(verts,uv,subs,albedo=None):
 faces=[];slots=[]
 for slot,tris in enumerate(subs):
  for i in range(0,len(tris),3):faces.append(tris[i:i+3]);slots.append(slot)
 mesh=bpy.data.meshes.new('candidate');mesh.from_pydata(verts,[],faces);mesh.update();layer=mesh.uv_layers.new(name='UVMap')
 for p,slot in zip(mesh.polygons,slots):
  p.material_index=slot;p.use_smooth=True
  for li in p.loop_indices:layer.data[li].uv=uv[mesh.loops[li].vertex_index]
 for i,t in enumerate(BASE['textures']):mesh.materials.append(material(t['key'],albedo if (albedo and i==0) else t['path']))
 o=bpy.data.objects.new('candidate',mesh);bpy.context.collection.objects.link(o);return o

def scene():
 bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene
 s.render.engine='BLENDER_EEVEE_NEXT';s.render.resolution_x=640;s.render.resolution_y=640;s.render.film_transparent=False
 w=bpy.data.worlds.new('w');s.world=w;w.use_nodes=True;w.node_tree.nodes['Background'].inputs[0].default_value=(.16,.2,.26,1);w.node_tree.nodes['Background'].inputs[1].default_value=1.0
 sun=bpy.data.lights.new('sun','SUN');sun.energy=3.2;so=bpy.data.objects.new('sun',sun);so.rotation_euler=(math.radians(50),0,math.radians(30));s.collection.objects.link(so)
 plane=bpy.data.meshes.new('ground');plane.from_pydata([(-6,-6,0.1),(6,-6,0.1),(6,6,0.1),(-6,6,0.1)],[],[(0,1,2,3)]);g=bpy.data.objects.new('ground',plane);s.collection.objects.link(g)
 gm=bpy.data.materials.new('g');gm.use_nodes=True;gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.42,.3,.2,1);plane.materials.append(gm)
 cam=bpy.data.cameras.new('cam');cam.lens=50;co=bpy.data.objects.new('cam',cam);s.collection.objects.link(co);s.camera=co
 return s,co

def aim(co,eye,target):
 co.location=eye;co.rotation_euler=(target-eye).to_track_quat('-Z','Y').to_euler()

args=sys.argv[sys.argv.index('--')+1:];out=Path(args[0]);out.mkdir(parents=True,exist_ok=True)
for spec in args[1:]:
 label,path=spec.split('=',1);path,_,albedo=path.partition('|');s,co=scene();build(*load(path),albedo=albedo or None)
 for name,(eye,target) in VIEWS.items():
  aim(co,eye,target);s.render.filepath=str(out/f'{label}-{name}.png');bpy.ops.render.render(write_still=True)
 print('rendered',label)
