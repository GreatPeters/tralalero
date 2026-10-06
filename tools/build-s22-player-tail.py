"""Repair anatomy in the existing mesh coordinate/UV/weight system.

Retain the front pair, close the removed rear-leg cuts, and join the body to a
single curved, shod tail. Outputs are candidates until native animation QA.
"""
import json,math,collections
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'outputs/s22-polish-2026-10-01'
base=json.loads((OUT/'player-geometry-before.json').read_text(encoding='utf-8'))
SCALE=2.8/(max(v[1] for v in base['vertices'])-min(v[1] for v in base['vertices']))
OFFSET=Vector((.52,1.50,0))
def to_view(p):return Vector((p[0]*SCALE,-p[2]*SCALE,p[1]*SCALE))
def to_unity(p):return [p.x/SCALE,p.z/SCALE,-p.y/SCALE]
def ws(raw):return {int(raw[i]):raw[i+1] for i in range(0,8,2) if raw[i+1]>0}
def packed(w):
 items=sorted(w.items(),key=lambda x:-x[1])[:4];total=sum(v for _,v in items);out=[]
 for k,v in items:out.extend([k,v/total])
 return out+[0,0]*(4-len(items))
def process(d,shoe_only=False,body_only=False):
 original=[{'p':to_view(p),'uv':d['uv'][i],'n':Vector((d['normals'][i][0],-d['normals'][i][2],d['normals'][i][1])),'w':ws(d['weights'][i])} for i,p in enumerate(d['vertices'])]
 result={'vertices':[],'uv':[],'normals':[],'weights':[],'triangles':[[] for _ in d['triangles']]};lookup={};cuts=[[],[]];tail_cut=[]
 def vertex(a):
  weight=packed(a['w']);key=tuple(round(x,8) for x in [*a['p'],*a['uv'],*a['n'],*weight])
  if key not in lookup:
   lookup[key]=len(result['vertices']);result['vertices'].append(to_unity(a['p']));result['uv'].append(a['uv']);result['normals'].append([a['n'].x,a['n'].z,-a['n'].y]);result['weights'].append(weight)
  return lookup[key]
 def face(poly,slot):
  if len(poly)<3:return
  for i in range(1,len(poly)-1):result['triangles'][slot].extend([vertex(poly[0]),vertex(poly[i]),vertex(poly[i+1])])
 def interpolate(a,b,t):
  return {'p':a['p'].lerp(b['p'],t),'n':a['n'].lerp(b['n'],t).normalized(),'uv':[a['uv'][k]*(1-t)+b['uv'][k]*t for k in range(2)],'w':{k:a['w'].get(k,0)*(1-t)+b['w'].get(k,0)*t for k in set(a['w'])|set(b['w'])}}
 def heal(v):
  p=v['p'].copy();influence=sum(v['w'].get(k,0) for k in range(6,14))
  if p.y>-.22 and p.z<1.46 and influence>.1:
   t=min(1,(influence-.1)/.6)*max(0,min(1,(1.46-p.z)/.30));p.x*=1-.45*t;p.z+=.12*t
  return dict(v,p=p)
 def clean_weights(weights):
  w=dict(weights);removed=sum(w.pop(k,0) for k in range(6,14));w[0]=w.get(0,0)+removed;return w
 def clip(poly,axis,value,above,collect=False):
  out=[]
  for a,b in zip(poly,poly[1:]+poly[:1]):
   ain=(a['p'][axis]>=value) if above else (a['p'][axis]<=value);bin=(b['p'][axis]>=value) if above else (b['p'][axis]<=value)
   if ain:out.append(a)
   if ain!=bin:
    t=(value-a['p'][axis])/(b['p'][axis]-a['p'][axis]);v=interpolate(a,b,t);out.append(v)
    if collect:cuts[0 if v['p'].x<0 else 1].append(v)
    elif axis==1 and not above:tail_cut.append(v)
  return out
 for slot,tris in enumerate(d['triangles']):
  for i in range(0,len(tris),3):
   tri=[original[j] for j in tris[i:i+3]];center=sum((x['p'] for x in tri),Vector())/3
   rear=sum(sum(x['w'].get(k,0) for k in range(6,14)) for x in tri)/3>.25 and center.y>-.22
   kept=tri
   if rear:kept=[] if shoe_only else clip(kept,2,1.08,True,True)
   if not shoe_only:kept=clip(kept,1,1.60,False)
   clean=[]
   for v in kept:
    clean.append(dict(heal(v),w=clean_weights(v['w'])))
   face(clean,slot)
   left=sum(sum(x['w'].get(k,0) for k in range(6,10)) for x in tri)/3>.25 and center.x<0
   if left and (shoe_only or (not body_only and slot==1)):
    copied=[]
    for v in tri:
     w={}
     for k,val in v['w'].items():
      target=27+(k-6)%4 if 6<=k<14 else k;w[target]=w.get(target,0)+val
     copied.append(dict(v,p=v['p']+OFFSET,w=w))
    face(copied,slot)
 if not shoe_only:
  belly=min(original,key=lambda v:(v['p']-Vector((0,.25,1.04))).length_squared)['uv']
  dark=min(original,key=lambda v:(v['p']-Vector((0,1.25,1.58))).length_squared)['uv']
  for group in []: # Close actual geometric boundary loops after all cuts below.
   unique={tuple(round(x,5) for x in v['p']):heal(v) for v in group};ring=list(unique.values())
   if len(ring)<3:continue
   center=sum((v['p'] for v in ring),Vector())/len(ring);ring.sort(key=lambda v:math.atan2(v['p'].y-center.y,v['p'].x-center.x));center_weights={}
   for v in ring:
    for k,w in clean_weights(v['w']).items():center_weights[k]=center_weights.get(k,0)+w/len(ring)
   c={'p':center,'uv':belly,'n':Vector((0,0,-1)),'w':center_weights}
   for a,b in zip(ring,ring[1:]+ring[:1]):face([c,dict(b,uv=belly,n=c['n'],w=clean_weights(b['w'])),dict(a,uv=belly,n=c['n'],w=clean_weights(a['w']))],0)
  boundary=list({tuple(round(x,7) for x in v['p']):v for v in tail_cut}.values())
  if len(boundary)<8:raise RuntimeError('Tail boundary not found')
  p0=sum((v['p'] for v in boundary),Vector())/len(boundary);boundary.sort(key=lambda v:math.atan2(-(v['p'].z-p0.z),v['p'].x-p0.x))
  p1=Vector((.03,1.82,1.35));p2=Vector((0,2.29,.70));p3=Vector((0,2.31,.50));rings=[];steps=18;sides=len(boundary)
  for i in range(steps+1):
   t=i/steps;q=1-t;center=q*q*q*p0+3*q*q*t*p1+3*q*t*t*p2+t*t*t*p3;tangent=(3*q*q*(p1-p0)+6*q*t*(p2-p1)+3*t*t*(p3-p2)).normalized();side=tangent.cross(Vector((1,0,0))).normalized();rad=.19*(1-t)+.105*t;w=t*t*(3-2*t);ring=[]
   for j,b in enumerate(boundary):
    delta=b['p']-p0;a=math.atan2(-delta.z,delta.x);n=(Vector((1,0,0))*math.cos(a)+side*math.sin(a)).normalized();radius=math.sqrt(delta.x*delta.x+delta.z*delta.z)*(1-t)+.105*t
    weights={k:value*(1-w) for k,value in clean_weights(b['w']).items()};weights[30]=weights.get(30,0)+w
    ring.append({'p':b['p'] if i==0 else center+n*radius,'n':b['n'] if i==0 else n,'w':weights,'uv':dark})
   rings.append(ring)
  for i in range(steps):
   for j in range(sides):
    color=belly if math.sin(2*math.pi*(j+.5)/sides)>.25 else dark;quad=[rings[i][j],rings[i][(j+1)%sides],rings[i+1][(j+1)%sides],rings[i+1][j]];face([dict(v,uv=color) for v in quad],0)
 if not shoe_only:
  positions=[to_view(p) for p in result['vertices']];keys=[tuple(round(x,5) for x in p) for p in positions];edge_counts=collections.Counter();directed={};adj=collections.defaultdict(set)
  tris=result['triangles'][0]
  for i in range(0,len(tris),3):
   ids=tris[i:i+3]
   for a,b in zip(ids,ids[1:]+ids[:1]):
    if keys[a]==keys[b]:continue
    key=tuple(sorted((keys[a],keys[b])));edge_counts[key]+=1;directed[key]=(a,b)
  boundaries=[]
  for key,count in edge_counts.items():
   if count==1:
    a,b=directed[key];boundaries.append((a,b));adj[keys[a]].add(keys[b]);adj[keys[b]].add(keys[a])
  remaining=set(adj)
  while remaining:
   start=remaining.pop();component={start};todo=[start]
   while todo:
    for point in adj[todo.pop()]:
     if point not in component:component.add(point);remaining.discard(point);todo.append(point)
   if len(component)<6 or not all(-.5<p[1]<1.65 and .85<p[2]<1.65 for p in component):continue
   edges=[(a,b) for a,b in boundaries if keys[a] in component];indices=set(i for edge in edges for i in edge)
   center=sum((positions[i] for i in indices),Vector())/len(indices);cw={};cn=Vector()
   for i in indices:
    n=result['normals'][i];cn+=Vector((n[0],-n[2],n[1]))
    for k,w in ws(result['weights'][i]).items():cw[k]=cw.get(k,0)+w/len(indices)
   c={'p':center,'n':cn.normalized(),'uv':belly,'w':cw}
   def existing(i):
    n=result['normals'][i];return {'p':positions[i],'n':Vector((n[0],-n[2],n[1])),'uv':belly,'w':ws(result['weights'][i])}
   for a,b in edges:face([c,existing(b),existing(a)],0)
 result['tailOffset']=to_unity(OFFSET);return result

def render_source(d,result,path):
 bpy.ops.wm.read_factory_settings(use_empty=True);vertices=[to_view(v) for v in result['vertices']];faces=[];slots=[]
 for slot,tris in enumerate(result['triangles']):
  for i in range(0,len(tris),3):faces.append(tris[i:i+3]);slots.append(slot)
 mesh=bpy.data.meshes.new('Tail foot repaired candidate');mesh.from_pydata(vertices,[],faces);mesh.update();o=bpy.data.objects.new('Two feet and a shod tail',mesh);bpy.context.collection.objects.link(o);layer=mesh.uv_layers.new(name='UVMap')
 for p,slot in zip(mesh.polygons,slots):
  p.material_index=slot;p.use_smooth=True
  for li in p.loop_indices:layer.data[li].uv=result['uv'][mesh.loops[li].vertex_index]
 for t in d['textures']:
  mat=bpy.data.materials.new(t['key']);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/t['path']));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.65;mesh.materials.append(mat)
 bpy.ops.wm.save_as_mainfile(filepath=str(path))

def flattened(result):
 return {'positions':[v for p in result['vertices'] for v in p],'uv':[v for p in result['uv'] for v in p],'normals':[v for p in result['normals'] for v in p],'weights':[v for p in result['weights'] for v in p],'submeshes':[{'triangles':t} for t in result['triangles']],'tailOffset':result['tailOffset']}
result=process(base);(OUT/'player-tail-candidate-v5.json').write_text(json.dumps(flattened(result)),encoding='utf-8');render_source(base,result,OUT/'player-tail-candidate-v5.blend');print('vertices',len(result['vertices']),'triangles',sum(len(x)//3 for x in result['triangles']))
source_dir=OUT/'player-source-meshes'
if (source_dir/'manifest.json').exists():
 target=OUT/'player-tail-meshes';target.mkdir(exist_ok=True);rows=[]
 for item in json.loads((source_dir/'manifest.json').read_text(encoding='utf-8')):
  data=json.loads((source_dir/(item['name']+'.json')).read_text(encoding='utf-8'));built=process(data,item['shoeOnly'],item['bodyOnly']);(target/(item['name']+'.json')).write_text(json.dumps(flattened(built)),encoding='utf-8');rows.append(dict(item,vertices=len(built['vertices']),triangles=sum(len(t)//3 for t in built['triangles'])))
 (target/'manifest.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
