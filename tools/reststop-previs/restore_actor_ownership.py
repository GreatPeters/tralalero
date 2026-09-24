"""Keep idle actors editable: separate their legs from static material batches."""
import bpy,json,sys,shutil
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as g
OUT=Path.cwd()/'outputs/reststop-blender-300s-2026-09-23';source=OUT/'reststop-master.blend'
backup=OUT/'reststop-master-before-actor-ownership.blend'
if not backup.exists():shutil.copy2(source,backup)
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.context.scene;s.frame_set(1)
if s.get('actor_ownership_restored'):raise RuntimeError('Ownership already restored')
names=['Parking_staff','Snack_chef','Barista','Cashier','Cleaner','Fuel_staff']
roots=[bpy.data.objects[n] for n in names];batches=[bpy.data.objects.get('LANDSCAPE__'+m) for m in ('navy','charcoal','white')]
assert all(batches),[o.name if o else None for o in batches]
old={};old_triangles=0
for obj in batches:
    mat=obj.data.materials[0].name;points=[obj.matrix_world@v.co for v in obj.data.vertices];old[mat]=np.array([tuple(v) for v in points])
    for point in points:
        assert any((point-root.location).xy.length<1.1 and -.03<point.z-root.location.z<1.02 for root in roots),(obj.name,tuple(point))
    old_triangles+=sum(len(p.vertices)-2 for p in obj.data.polygons)
g.M={m.name:m for m in bpy.data.materials};new=[]
for name in names:
    for side in (-1,1):
        leg=bpy.data.objects[name+('_leg_L' if side<0 else '_leg_R')]
        new.append(g.loft(name+'_owned_trouser',[(0,.145,.165,0),(-.25,.15,.16,0),(-.43,.12,.13,0),(-.66,.10,.11,0)],'navy',leg))
        new.append(g.cube(name+'_owned_shoe',(0,-.12,-.76),(.27,.48,.23),'charcoal',.10,leg))
        new.append(g.cube(name+'_owned_sole',(0,-.13,-.86),(.28,.49,.045),'white',.015,leg))
bpy.context.view_layer.update();errors={}
for mat,points in old.items():
    current=np.array([tuple(o.matrix_world@v.co) for o in new if o.data.materials[0].name==mat for v in o.data.vertices])
    assert points.shape==current.shape,(mat,points.shape,current.shape)
    # Match the identical vertex multiset after quantizing only for sorting.
    def order(a):
        q=np.round(a,4);return np.lexsort((q[:,2],q[:,1],q[:,0]))
    error=float(np.max(np.linalg.norm(points[order(points)]-current[order(current)],axis=1)));errors[mat]=error
    assert error<.00003,(mat,error)
new_triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in new)
assert old_triangles==new_triangles,(old_triangles,new_triangles)
for obj in batches:bpy.data.objects.remove(obj,do_unlink=True)
s['actor_ownership_restored']=True;s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True)
report={'actors':names,'triangles_before':old_triangles,'triangles_after':new_triangles,'maximum_vertex_error_by_material_m':errors,'render_geometry_unchanged':True,'purpose':'Actor roots own all their parts; static environment export excludes all actor descendants.'}
(OUT/'actor-ownership-validation.json').write_text(json.dumps(report,indent=2));print('ACTOR_OWNERSHIP_PASS',json.dumps(report),flush=True)
