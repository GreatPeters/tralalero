"""Reference-led volumetric rebuild after the movie-crop TRELLIS depth gate failed."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
OUT=Path(r'D:\Tralalero Shooter\Tralalero Shooter D\outputs\chapters45-2026-10-02\assets\shoe-crown\local-refinement-v2')
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name,color,roughness,metallic=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=roughness;p.inputs['Metallic'].default_value=metallic;return m
WHITE=mat('Crown_OffwhiteCeramic',(.84,.82,.76),.37,.13)
GLASS=mat('Crown_BlueGrayGlazing',(.105,.22,.32),.25,.52)
METAL=mat('Crown_ChampagneFrames',(.52,.43,.29),.32,.6)

def mesh(name,verts,faces,material):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);ob.data.materials.append(material)
 for p in me.polygons:p.use_smooth=True
 return ob
def activate(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def tube(name,pts,radius,material,closed=False):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=8;cu.bevel_depth=radius;cu.bevel_resolution=2
 sp=cu.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,co in zip(sp.points,pts):p.co=(*co,1)
 sp.use_cyclic_u=closed;ob=bpy.data.objects.new(name,cu);bpy.context.collection.objects.link(ob);ob.data.materials.append(material);return ob
def catmull(points,n=5):
 out=[]
 for i in range(len(points)):
  a,b,c,d=[Vector(points[j%len(points)]) for j in [i-1,i,i+1,i+2]]
  for j in range(n):
   t=j/n;out.append((b*2+(-a+c)*t+(a*2-b*5+c*4-d)*t*t+(-a+b*3-c*3+d)*t*t*t)*.5)
 return out
outline=catmull([(-.48,0),(-.466,-.083),(-.38,-.118),(-.18,-.126),(.05,-.158),(.26,-.167),(.445,-.125),(.523,-.047),(.526,.045),(.45,.122),(.26,.164),(.05,.154),(-.18,.132),(-.38,.123),(-.465,.08)],5)
N=len(outline)
def ring_surface(name,rings,material,capbottom=False,captop=False):
 verts=[tuple(v) for ring in rings for v in ring];faces=[]
 for j in range(len(rings)-1):
  for i in range(N):faces.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
 if capbottom:faces.append(tuple(reversed(range(N))))
 if captop:faces.append(tuple((len(rings)-1)*N+i for i in range(N)))
 return mesh(name,verts,faces,material)
def sole_ring(scale,z):return [Vector((p.x*scale,p.y*scale,z+.018*max(0,(p.x-.25)/.28)**2)) for p in outline]
ring_surface('ContinuousRoundedSole',[sole_ring(.965,.0),sole_ring(.993,.01),sole_ring(1,.024),sole_ring(1,.052),sole_ring(.996,.067),sole_ring(.99,.096),sole_ring(.972,.108)],WHITE,True,True)
ring_surface('SoleObservationRibbon',[sole_ring(1.001,.048),sole_ring(1.001,.060)],GLASS)
for z in [.044,.064]:tube('SoleChampagneEdge',sole_ring(1.002,z),.0015,METAL,True)

# The heel/collar ring shares each outline point's polar direction. This makes a
# continuous shell, with actual depth and an open roof courtyard in the collar.
collar=[]
for p in outline:
 ang=math.atan2(p.y,(p.x+.28)*.63)
 x=-.295+.155*math.cos(ang);y=.116*math.sin(ang)
 z=.285+.026*max(0,-math.cos(ang))+.007*math.cos(2*ang)
 collar.append(Vector((x,y,z)))
def upper_ring(u,offset=0):
 result=[]
 for p,q in zip(outline,collar):
  base=Vector((p.x*.961,p.y*.965,.102+.012*max(0,(p.x-.25)/.28)**2))
  v=base.lerp(q,u);v.z+=.039*math.sin(math.pi*u)*(max(0,(p.x+.4))/.93)
  v.y+=math.copysign(offset,p.y) if p.y else 0
  result.append(v)
 return result
ring_surface('ShoeArchitecturalShell',[upper_ring(u) for u in [0,.06,.18,.35,.52,.70,.86,1]],WHITE)
inside=[[Vector((p.x,p.y*.87,p.z-.005)) for p in collar],[Vector((-.295+(p.x+.295)*.80,p.y*.79,.13)) for p in collar]]
ring_surface('RecessedCollarCourtyard',inside,GLASS,False,True)
tube('RoundedCollarParapet',collar,.0115,WHITE,True)

# Two separated side glazing strips with offwhite mullions. The front/toe remains ceramic.
for side in [-1,1]:
 ids=[i for i,p in enumerate(outline) if p.y*side>.07 and p.x<.35]
 lower=upper_ring(.16,.0018);upper=upper_ring(.36,.0018)
 for j,i in enumerate(ids[:-1]):
  k=(i+1)%N
  if k not in ids:continue
  ob=mesh('GalleryWindow_%s_%02d'%(side,j),[lower[i],lower[k],upper[k],upper[i]],[(0,1,2,3)],GLASS)
  if j%2==0:tube('WindowMullion',[lower[i],upper[i]],.0020,METAL)
 for u in [.14,.38]:
  pts=[upper_ring(u,.0021)[i] for i in ids]
  tube('GalleryRibbonFrame',pts,.0028,WHITE)

# Tongue is a shaped ceramic skylight over the vamp, its roof laces act as ribs.
xs=[-.18,-.16,-.10,-.03,.04,.105,.16]
zs=[.397,.401,.355,.317,.277,.238,.209]
widths=[.073,.079,.086,.093,.099,.100,.084]
verts=[]
for x,z,w in zip(xs,zs,widths):
 for f in [-1,-.88,0,.88,1]:verts.append((x,w*f,z-.015*abs(f)**2))
faces=[]
for j in range(len(xs)-1):
 for i in range(4):faces.append((j*5+i,j*5+i+1,(j+1)*5+i+1,(j+1)*5+i))
tongue=mesh('RaisedTongueSkylight',verts,faces,WHITE);activate(tongue);so=tongue.modifiers.new('CeramicShellThickness','SOLIDIFY');so.thickness=.013;be=tongue.modifiers.new('SoftCeramicEdges','BEVEL');be.width=.005;be.segments=3
# Independent near-view review rejected the unsupported canopy read. Continuous
# structural gussets now seat each tongue edge into the shell. Their lower edges
# overlap the upper surface deliberately; the collar behind the rear wall stays open.
base_z=[.255,.252,.232,.211,.190,.168,.145]
for side in [-1,1]:
 gv=[]
 for x,z,w,bz in zip(xs,zs,widths,base_z):
  gv.extend([(x,side*w,z-.016),(x,side*(w+.018),bz)])
 gf=[(i*2,i*2+1,(i+1)*2+1,(i+1)*2) for i in range(len(xs)-1)]
 g=mesh('ContinuousInstepGusset_'+str(side),gv,gf,WHITE);activate(g)
 thick=g.modifiers.new('StructuralGussetThickness','SOLIDIFY');thick.thickness=.004
mesh('TongueRearSupportWall',[(xs[0],-widths[0],zs[0]-.016),(xs[0],widths[0],zs[0]-.016),(xs[0],widths[0]+.018,base_z[0]),(xs[0],-widths[0]-.018,base_z[0])],[(0,1,2,3)],WHITE)
for i in range(1,6):
 x,z,w=xs[i],zs[i]+.005,widths[i]
 pts=[]
 for j in range(13):
  t=j/12;pts.append((x+.023*(t-.5),w*(2*t-1),z+.012*math.sin(math.pi*t)))
 tube('LaceRoofRib_%02d'%i,pts,.008,WHITE)
 for side in [-1,1]:
  bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.011,location=(x,side*w,z-.006));o=bpy.context.object;o.name='LaceAnchor';o.scale=(1,.65,.8);o.data.materials.append(METAL)

# Heel service portal and sole mullions communicate inhabitable architectural scale.
for side in [-1,1]:
 for x in [-.37,-.29,-.21,-.13,-.05,.03,.11,.19,.27,.35,.43]:
  near=min(outline,key=lambda p:abs(p.x-x)+abs(p.y-side*.15))
  tube('SoleWindowMullion',[(near.x*1.002,near.y*1.002,.048),(near.x*1.002,near.y*1.002,.060)],.0012,METAL)

# Final combined object keeps only three material draws. Curves become real geometry.
for o in list(bpy.context.scene.objects):
 activate(o)
 if o.type=='CURVE':bpy.ops.object.convert(target='MESH')
 for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=next(o for o in bpy.context.scene.objects if o.type=='MESH');bpy.ops.object.join();crown=bpy.context.object;crown.name='ShoeCrown_LOD0'
activate(crown);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
for p in crown.data.polygons:p.use_smooth=True
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
metrics=[]
for lod,budget in enumerate([15000,6000,2000]):
 o=crown if lod==0 else crown.copy()
 if lod:o.data=crown.data.copy();bpy.context.collection.objects.link(o)
 activate(o);o.name='ShoeCrown_LOD'+str(lod)
 if tris(o)>budget:
  m=o.modifiers.new('MobileLOD','DECIMATE');m.ratio=budget/tris(o);m.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=m.name)
 bpy.ops.export_scene.gltf(filepath=str(OUT/(o.name+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,mesh_smooth_type='FACE',add_leaf_bones=False)
 metrics.append({'name':o.name,'triangles':tris(o),'vertices':len(o.data.vertices),'materials':len(o.data.materials),'dimensions':list(o.dimensions)})
 if lod:o.hide_set(True);o.hide_render=True
activate(crown);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ShoeCrown.blend'))
(OUT/'build-metrics.json').write_text(json.dumps({'method':'local volumetric architectural rebuild after failed Trellis movie-crop depth gate','source_guide':'../trellis-source.glb (rejected flat shape retained)','lods':metrics,'toe_direction':'+X','pivot':'bottom center','width_m':.335,'length_m':1.009,'materials':'offwhite ceramic, blue-gray glazing, champagne mullions','textures':'none; intentional solid architectural materials; UVs provided'},indent=2),encoding='utf-8')
print(json.dumps(metrics),flush=True)
