"""Small rigid role props, merged into one skinned mesh per mascot."""
import math
import bpy
from mathutils import Vector


def add_equipment(name, rig, specs, height):
    hand = 'Hand.L' if name == 'AsphaltWorker' else 'Hand.R'
    grip = Vector(specs[hand][0]) * height
    colors = {'Orange':(.92,.28,.035,1), 'Cream':(.93,.87,.70,1),
              'Navy':(.045,.075,.12,1), 'Steel':(.42,.48,.50,1),
              'Rubber':(.028,.035,.045,1), 'Cardboard':(.63,.39,.18,1),
              'Coral':(.88,.18,.13,1), 'Teal':(.06,.42,.43,1)}
    materials, parts = {}, []

    def finish(obj, color):
        if color not in materials:
            material = bpy.data.materials.new(name + '_' + color)
            material.use_nodes = True
            shader = material.node_tree.nodes.get('Principled BSDF')
            shader.inputs['Base Color'].default_value = colors[color]
            shader.inputs['Roughness'].default_value = .82
            materials[color] = material
        obj.name = 'Carry_' + color
        obj.data.materials.append(materials[color])
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        obj.parent = rig
        obj.vertex_groups.new(name=hand).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
        obj.modifiers.new('Rigid hand attachment', 'ARMATURE').object = rig
        parts.append(obj)
        return obj

    def box(color, offset, size):
        bpy.ops.mesh.primitive_cube_add(size=1, location=grip + Vector(offset))
        obj = bpy.context.object; obj.scale = size
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        bevel = obj.modifiers.new('Soft edges', 'BEVEL')
        bevel.width = min(size) * .12; bevel.segments = 2
        bpy.ops.object.modifier_apply(modifier=bevel.name)
        return finish(obj, color)

    def cylinder(color, offset, radius, depth, front=False):
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=radius, depth=depth, location=grip + Vector(offset))
        obj = bpy.context.object
        if front: obj.rotation_euler.x = math.pi / 2
        for face in obj.data.polygons: face.use_smooth = len(face.vertices) == 4
        return finish(obj, color)

    def cone(color, offset, radius_bottom, radius_top, depth):
        bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=radius_bottom, radius2=radius_top,
                                      depth=depth, location=grip + Vector(offset))
        obj = bpy.context.object
        for face in obj.data.polygons: face.use_smooth = len(face.vertices) == 4
        return finish(obj, color)

    if name == 'ConeMechanic':
        box('Orange', (0,-.055,-.19), (.23,.23,.035))
        cone('Orange', (0,-.055,-.07), .10,.024,.22)
        cone('Cream', (0,-.055,-.055), .071,.054,.05)
    elif name == 'TrafficPatrol':
        cylinder('Navy', (0,0,.10), .033,.32)
        cylinder('Cream', (0,0,.19), .034,.06)
        cylinder('Orange', (0,0,.26), .038,.05)
    elif name == 'TollgateChief':
        box('Cream', (0,-.06,.055), (.19,.04,.12))
        box('Orange', (0,-.083,.055), (.11,.004,.022))
    elif name == 'TireBruiser':
        center = grip + Vector((0,-.07,-.07))
        for offset, major, minor in [(0,.21,.067),(-.055,.19,.012),(.055,.19,.012)]:
            bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,
                major_segments=32,minor_segments=10,location=center+Vector((offset,0,0)))
            obj=bpy.context.object;obj.rotation_euler.y=math.pi/2
            for face in obj.data.polygons: face.use_smooth=True
            finish(obj,'Rubber')
    elif name == 'AsphaltWorker':
        cylinder('Cardboard', (0,0,-.06), .018,.46)
        box('Steel', (0,-.02,-.32), (.16,.028,.16))
        box('Navy', (0,0,.17), (.12,.045,.035))
    elif name == 'DeliveryRider':
        box('Cardboard', (0,-.10,-.025), (.22,.19,.20))
        box('Cream', (0,-.197,-.025), (.045,.005,.20))
    elif name == 'SnackChef':
        cylinder('Navy', (0,0,.06), .02,.16)
        box('Steel', (0,0,.18), (.08,.025,.16))
        box('Steel', (0,0,.30), (.16,.025,.11))
    elif name == 'CoffeeVendor':
        cone('Cream', (0,-.055,.025), .048,.064,.16)
        cylinder('Navy', (0,-.055,.11), .068,.022)
        cylinder('Coral', (0,-.055,.025), .058,.04)
    elif name == 'ParkingMarshal':
        cylinder('Navy', (0,0,.06), .018,.20)
        cylinder('Coral', (0,0,.23), .12,.025,True)
        box('Cream', (0,-.015,.23), (.16,.005,.032))
    if not parts:
        return []
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts: part.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join()
    joined=bpy.context.object;joined.name=name+'_Equipment'
    return [joined]
