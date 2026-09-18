"""Audit every imported Highway prop; remove large residual floor sheets on vehicles."""
import argparse
import json
import math
from pathlib import Path
import sys
import bpy
import bmesh
import numpy as np
from mathutils import Vector

parser=argparse.ArgumentParser()
parser.add_argument('--root',required=True)
parser.add_argument('--repair',action='store_true')
parser.add_argument('--only',type=int)
parser.add_argument('--revision',default='v1')
parser.add_argument('--weld-floor-seams', action='store_true')
parser.add_argument('--floor-search-height', type=float, default=.035)
parser.add_argument('--clip-floor', action='store_true')
parser.add_argument('--remove-low-fragments', action='store_true')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=Path(args.root)
output=root/'outputs/chapters-polish-2026-09-12'/('props-'+args.revision)
output.mkdir(parents=True,exist_ok=True)
vehicles={54:7,67:4.5,68:9,69:7,80:9,81:4.5,82:4.5,83:7}
guardrails={49:3,57:8,64:8}
reports=[]
for folder in sorted((root/'Assets/ShooterSurvival/Models/Highway/Props').iterdir()):
    if not folder.name.isdigit() or (args.only and int(folder.name)!=args.only): continue
    source=folder/'Model.fbx'
    if not source.exists(): continue
    identity=int(folder.name)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source))
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    if not meshes: raise RuntimeError('No mesh: '+str(source))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes: obj.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]
    if len(meshes)>1: bpy.ops.object.join()
    obj=bpy.context.object
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bm=bmesh.new();bm.from_mesh(obj.data);bm.normal_update()
    points=np.array([tuple(vertex.co) for vertex in bm.verts])
    minimum,maximum=points.min(axis=0),points.max(axis=0)
    dimensions=maximum-minimum;height=max(float(dimensions[2]),.001)
    footprint=max(float(dimensions[0]*dimensions[1]),.001)
    if args.weld_floor_seams:
        # FBX may split the white sheet at UV boundaries. Loop UVs are retained
        # while coincident positions reconnect the planar surface for detection.
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=height*1e-5)
        bm.normal_update()
    candidates={face for face in bm.faces if abs(face.normal.z)>.92 and
                max(vertex.co.z for vertex in face.verts)<minimum[2]+height*args.floor_search_height and
                max(vertex.co.z for vertex in face.verts)-min(vertex.co.z for vertex in face.verts)<height*.012}
    groups=[]
    while candidates:
        seed=candidates.pop();pending=[seed];group=[seed]
        while pending:
            face=pending.pop()
            for edge in face.edges:
                for neighbor in edge.link_faces:
                    if neighbor in candidates:
                        candidates.remove(neighbor);pending.append(neighbor);group.append(neighbor)
        area=sum(face.calc_area() for face in group)
        coordinates=np.array([tuple(vertex.co) for face in group for vertex in face.verts])
        extent=coordinates.max(axis=0)-coordinates.min(axis=0)
        if area>footprint*.08 and extent[0]>dimensions[0]*.5 and extent[1]>dimensions[1]*.5:
            groups.append((group,area))
    floor_faces=[face for group,_ in groups for face in group]
    if floor_faces:
        # Include the thin sheet's vertical rim, not just its horizontal faces.
        # The exact detected height band avoids cutting into the rounded tires.
        low=min(vertex.co.z for face in floor_faces for vertex in face.verts)
        high=max(vertex.co.z for face in floor_faces for vertex in face.verts)
        epsilon=height*1e-5
        floor_faces=[face for face in bm.faces if max(vertex.co.z for vertex in face.verts)<=high+epsilon]
    report={'id':identity,'source':str(source.relative_to(root)), 'beforeDimensions':dimensions.tolist(),
            'floorSheetFaces':len(floor_faces),'floorSheetArea':sum(area for _,area in groups),'vehicle':identity in vehicles}
    if floor_faces: report['floorHeightBand']=[low,high]
    if args.repair and floor_faces and identity in (vehicles.keys() | guardrails.keys()):
        removed_count = len(floor_faces)
        if args.clip_floor:
            cut = bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                plane_co=Vector((0,0,high+epsilon)), plane_no=Vector((0,0,1)), dist=epsilon,
                clear_inner=True, clear_outer=False)
            boundary=[edge for edge in cut['geom_cut'] if isinstance(edge,bmesh.types.BMEdge) and edge.is_boundary]
            if boundary: bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
            floor_faces=[]
        if floor_faces:
            bmesh.ops.delete(bm,geom=floor_faces,context='FACES')
            loose=[vertex for vertex in bm.verts if not vertex.link_faces]
            if loose: bmesh.ops.delete(bm,geom=loose,context='VERTS')
        if args.remove_low_fragments:
            unvisited=set(bm.verts)
            fragments=[]
            while unvisited:
                seed=unvisited.pop();component=[seed];pending=[seed]
                while pending:
                    vertex=pending.pop()
                    for edge in vertex.link_edges:
                        neighbor=edge.other_vert(vertex)
                        if neighbor in unvisited:
                            unvisited.remove(neighbor);pending.append(neighbor);component.append(neighbor)
                z=[vertex.co.z for vertex in component]
                if max(z)<high+height*.05 and max(z)-min(z)<height*.02:
                    fragments.extend(component)
            if fragments: bmesh.ops.delete(bm,geom=fragments,context='VERTS')
            report['removedLowFragmentVertices']=len(fragments)
        bm.to_mesh(obj.data);obj.data.update()
        coordinates=np.array([tuple(vertex.co) for vertex in obj.data.vertices])
        # The car's long axis should be longitudinal. Floor remnants otherwise
        # make diagonal/square bounds a misleading basis for prefab placement.
        covariance=np.cov(coordinates[:,:2].T)
        values,vectors=np.linalg.eigh(covariance)
        long_axis=vectors[:,int(np.argmax(values))]
        angle=math.atan2(long_axis[1],long_axis[0])
        target_angle=math.pi/2 if identity in vehicles else 0
        yaw=(target_angle-angle+math.pi/2)%math.pi-math.pi/2
        rotation=np.array([[math.cos(yaw),-math.sin(yaw)],[math.sin(yaw),math.cos(yaw)]])
        coordinates[:,:2]=coordinates[:,:2]@rotation.T
        extent=coordinates.max(axis=0)-coordinates.min(axis=0)
        scale=(vehicles[identity]/float(extent[1])) if identity in vehicles else (guardrails[identity]/float(extent[0]))
        center=(coordinates.max(axis=0)+coordinates.min(axis=0))*.5;center[2]=coordinates[:,2].min()
        coordinates=(coordinates-center)*scale
        for vertex,point in zip(obj.data.vertices,coordinates): vertex.co=point
        obj.data.update()
        obj.name='Highway_'+str(identity)+'_Visual'
        destination=output/str(identity);destination.mkdir(exist_ok=True)
        if (destination/'Model.blend').exists(): raise RuntimeError('Preserve existing prop evidence: '+str(destination))
        bpy.ops.wm.save_as_mainfile(filepath=str(destination/'Model.blend'))
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        bpy.ops.export_scene.fbx(filepath=str(destination/'Model.fbx'),use_selection=True,add_leaf_bones=False,
            bake_anim=False,path_mode='COPY',embed_textures=True)
        bpy.ops.export_scene.gltf(filepath=str(destination/'Model.glb'),export_format='GLB',use_selection=True,export_animations=False)
        report.update({'output':str(destination.relative_to(root)),'removedFloorFaces':removed_count,'clipFloor':args.clip_floor,
                       'yawCorrectionDegrees':math.degrees(yaw),'afterDimensions':(coordinates.max(axis=0)-coordinates.min(axis=0)).tolist(),
                       'minZ':float(coordinates[:,2].min()),'frontDirectionNeedsVisualReview':True})
        (destination/'repair.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    bm.free();reports.append(report)
record=output/('audit-'+str(args.only)+'.json' if args.only else 'audit.json')
record.write_text(json.dumps(reports,indent=2),encoding='utf-8')
print(json.dumps({'audited':len(reports),'floorCandidates':[r['id'] for r in reports if r['floorSheetFaces']],
                  'repaired':[r['id'] for r in reports if 'output' in r],'report':str(record)},indent=2),flush=True)
