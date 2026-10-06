"""Rebuild the player shark toward the user's Tralalero reference image.

Derived from tools/build-s22-player-tail.py (S22 v5). Differences:
- The caudal fin's upper lobe is kept upright. Only the lower lobe is cut away
  and the cut morphs into the tail leg that ends in the third shoe, so the tail
  still acts as the foot (USER_STATED_REQUIREMENTS U34) without losing the
  shark silhouette.
- The front legs are pulled under the chest instead of splaying sideways.
  Shoe meshes go through the same per-weight shift so fitted shoes stay on.

The skeleton, bind poses, UVs, textures, tail-foot proxy bones and the rear
shoe offset are unchanged, so tools/import-s22-player-tail.cs can install the
output (`--input` points it at this directory).

Usage: blender -b --factory-startup --python tools/build-tralalero-reference-player.py
"""
import json,math,collections
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'outputs/s22-polish-2026-10-01'
OUT=ROOT/'outputs/tralalero-reference-2026-10-01'
OUT.mkdir(parents=True,exist_ok=True)
base=json.loads((SRC/'player-geometry-before.json').read_text(encoding='utf-8'))
SCALE=2.8/(max(v[1] for v in base['vertices'])-min(v[1] for v in base['vertices']))
OFFSET=Vector((.52,1.50,0))          # rear-left shoe -> tail shoe (same as S22 v5)
REAR=range(6,14)                      # backleg chains removed in v5
TAIL_FOOT=30                          # SharkTailFootRig proxy for backleg2
# Front-leg inward shift per bone (view units). Lower segments move most.
FRONT_SHIFT={19:.10,20:.22,21:.34,22:.38}
FRONT_SHIFT.update({k+4:v for k,v in FRONT_SHIFT.items()})
# Lower-lobe cut: keep the side above the line from the peduncle underside to
# the fork notch (measured from the source mesh profile).
CUT_A=Vector((0,1.88,.93));CUT_B=Vector((0,2.42,1.21))
CUT_DIR=(CUT_B-CUT_A).normalized();CUT_N=Vector((0,-CUT_DIR.z,CUT_DIR.y))  # points up/forward
ANKLE=Vector((0,2.31,.50));ANKLE_R=.105

def to_view(p):return Vector((p[0]*SCALE,-p[2]*SCALE,p[1]*SCALE))
def to_unity(p):return [p.x/SCALE,p.z/SCALE,-p.y/SCALE]
def ws(raw):return {int(raw[i]):raw[i+1] for i in range(0,8,2) if raw[i+1]>0}
def packed(w):
 items=sorted(w.items(),key=lambda x:-x[1])[:4];total=sum(v for _,v in items);out=[]
 for k,v in items:out.extend([k,v/total])
 return out+[0,0]*(4-len(items))
def narrowed(p,w):
 left=sum(w.get(k,0)*FRONT_SHIFT[k] for k in range(19,23));right=sum(w.get(k,0)*FRONT_SHIFT[k] for k in range(23,27))
 return Vector((p.x+left-right,p.y,p.z))
def cut_side(p):return (p-CUT_A).dot(CUT_N)

def process(d,shoe_only=False,body_only=False):
 original=[]
 for i,p in enumerate(d['vertices']):
  w=ws(d['weights'][i]);original.append({'p':narrowed(to_view(p),w),'uv':d['uv'][i],'n':Vector((d['normals'][i][0],-d['normals'][i][2],d['normals'][i][1])),'w':w})
 result={'vertices':[],'uv':[],'normals':[],'weights':[],'triangles':[[] for _ in d['triangles']]};lookup={};fin_cut=[]
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
  p=v['p'].copy();influence=sum(v['w'].get(k,0) for k in REAR)
  if p.y>-.22 and p.z<1.46 and influence>.1:
   t=min(1,(influence-.1)/.6)*max(0,min(1,(1.46-p.z)/.30));p.x*=1-.45*t;p.z+=.12*t
  return dict(v,p=p)
 def clean_weights(weights):
  w=dict(weights);removed=sum(w.pop(k,0) for k in REAR);w[0]=w.get(0,0)+removed;return w
 def clip(poly,f,collect=None):
  out=[]
  for a,b in zip(poly,poly[1:]+poly[:1]):
   fa,fb=f(a['p']),f(b['p'])
   if fa>=0:out.append(a)
   if (fa>=0)!=(fb>=0):
    v=interpolate(a,b,fa/(fa-fb));out.append(v)
    if collect is not None:collect.append(v)
  return out
 for slot,tris in enumerate(d['triangles']):
  for i in range(0,len(tris),3):
   tri=[original[j] for j in tris[i:i+3]];center=sum((x['p'] for x in tri),Vector())/3
   rear=sum(sum(x['w'].get(k,0) for k in REAR) for x in tri)/3>.25 and center.y>-.22
   kept=tri
   if rear:kept=[] if shoe_only else clip(kept,lambda p:p.z-1.08)
   if not shoe_only and center.y>1.80:kept=clip(kept,cut_side,fin_cut)
   face([dict(heal(v),w=clean_weights(v['w'])) for v in kept],slot)
   left=sum(sum(x['w'].get(k,0) for k in range(6,10)) for x in tri)/3>.25 and center.x<0
   if left and (shoe_only or (not body_only and slot==1)):
    copied=[]
    for v in tri:
     w={}
     for k,val in v['w'].items():
      target=27+(k-6)%4 if 6<=k<14 else k;w[target]=w.get(target,0)+val
     copied.append(dict(v,p=v['p']+OFFSET,w=w))
    face(copied,slot)
 body=[original[j] for j in set(d['triangles'][0])]
 if not shoe_only:build_tail_leg(body,fin_cut,face,clean_weights,winding(original,d['triangles'][0]))
 if not shoe_only:close_rear_holes(result,face)
 result['tailOffset']=to_unity(OFFSET);return result

def winding(original,tris):
 """+1 when the source's cross(b-a,c-a) agrees with its vertex normals."""
 total=0
 for i in range(0,len(tris),3):
  a,b,c=(original[j] for j in tris[i:i+3]);total+=(b['p']-a['p']).cross(c['p']-a['p']).dot(a['n']+b['n']+c['n'])
 return 1 if total>0 else -1

def build_tail_leg(body,fin_cut,face,clean_weights,convention):
 boundary=list({tuple(round(x,6) for x in v['p']):v for v in fin_cut}.values())
 if len(boundary)<8:raise RuntimeError('Lower-lobe cut not found')
 p0=sum((v['p'] for v in boundary),Vector())/len(boundary);X=Vector((1,0,0))
 boundary.sort(key=lambda v:math.atan2((v['p']-p0).dot(CUT_DIR),(v['p']-p0).x))
 # Outer skin of the left front shin (frontleg1) gives the leg's gray.
 gray=min((v for v in body if v['w'].get(21,0)>.8),key=lambda v:v['p'].x)['uv']
 belly=min(body,key=lambda v:(v['p']-Vector((0,.25,1.04))).length_squared)['uv']
 p1=p0+Vector((0,.02,-.22));p2=ANKLE+Vector((0,-.02,.20));p3=ANKLE;steps=16;sides=len(boundary);rings=[]
 for i in range(steps+1):
  t=i/steps;q=1-t;center=q*q*q*p0+3*q*q*t*p1+3*q*t*t*p2+t*t*t*p3
  tangent=(3*q*q*(p1-p0)+6*q*t*(p2-p1)+3*t*t*(p3-p2)).normalized()
  side=(CUT_DIR-tangent*CUT_DIR.dot(tangent)).normalized()
  e=1-(1-t)**2.2;w=t*t*(3-2*t);ring=[]
  for b in boundary:
   delta=b['p']-p0;a=math.atan2(delta.dot(CUT_DIR),delta.x);n=(X*math.cos(a)+side*math.sin(a)).normalized()
   radius=delta.length*(1-e)+ANKLE_R*e
   weights={k:value*(1-w) for k,value in clean_weights(b['w']).items()};weights[TAIL_FOOT]=weights.get(TAIL_FOOT,0)+w
   ring.append({'p':b['p'] if i==0 else center+n*radius,'n':b['n'] if i==0 else n,'w':weights})
  rings.append(ring)
 # Orient quads so their winding faces away from the tube axis.
 mid=steps//2;a,b,c=rings[mid][0]['p'],rings[mid][1]['p'],rings[mid+1][1]['p']
 outward=(b-a).cross(c-a).dot(a-(sum((v['p'] for v in rings[mid]),Vector())/sides))*convention>0
 for i in range(steps):
  for j in range(sides):
   # The front-facing strip at the top keeps the white underside colour.
   uv=belly if (rings[0][j]['p']-p0).dot(CUT_DIR)<-.12 and i<steps//3 else gray
   quad=[rings[i][j],rings[i][(j+1)%sides],rings[i+1][(j+1)%sides],rings[i+1][j]]
   face([dict(v,uv=uv) for v in (quad if outward else reversed(quad))],0)

def close_rear_holes(result,face):
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
 remaining=set(adj);belly_uv=None
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
  if belly_uv is None:belly_uv=min(range(len(positions)),key=lambda i:(positions[i]-Vector((0,.25,1.04))).length_squared)
  uv=result['uv'][belly_uv];c={'p':center,'n':cn.normalized(),'uv':uv,'w':cw}
  def existing(i):
   n=result['normals'][i];return {'p':positions[i],'n':Vector((n[0],-n[2],n[1])),'uv':uv,'w':ws(result['weights'][i])}
  for a,b in edges:face([c,existing(b),existing(a)],0)

def flattened(result):
 return {'positions':[v for p in result['vertices'] for v in p],'uv':[v for p in result['uv'] for v in p],'normals':[v for p in result['normals'] for v in p],'weights':[v for p in result['weights'] for v in p],'submeshes':[{'triangles':t} for t in result['triangles']],'tailOffset':result['tailOffset']}

def open_edges(result):
 keys=[tuple(round(x,5) for x in p) for p in result['vertices']];c=collections.Counter()
 tris=result['triangles'][0]
 for i in range(0,len(tris),3):
  ids=tris[i:i+3]
  for a,b in zip(ids,ids[1:]+ids[:1]):
   if keys[a]!=keys[b]:c[tuple(sorted((keys[a],keys[b])))]+=1
 return sum(1 for v in c.values() if v==1)

import sys
only=set(sys.argv[sys.argv.index('--')+1:]) if '--' in sys.argv else set()
meshes=OUT/'player-meshes';meshes.mkdir(exist_ok=True);rows=[]
for item in json.loads((SRC/'player-source-meshes/manifest.json').read_text(encoding='utf-8')):
 if only and item['name'] not in only:continue
 data=json.loads((SRC/'player-source-meshes'/(item['name']+'.json')).read_text(encoding='utf-8'))
 built=process(data,item['shoeOnly'],item['bodyOnly'])
 (meshes/(item['name']+'.json')).write_text(json.dumps(flattened(built)),encoding='utf-8')
 rows.append(dict(item,vertices=len(built['vertices']),triangles=sum(len(t)//3 for t in built['triangles']),openBodyEdges=None if item['shoeOnly'] else open_edges(built)))
 print(item['name'],rows[-1]['vertices'],rows[-1]['triangles'],rows[-1]['openBodyEdges'])
if not only:(meshes/'manifest.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
