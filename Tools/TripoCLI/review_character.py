"""Inspect a Tripo download and produce a Blender scene and honest local previews."""
import argparse
import json
import math
import os
import sys

import bpy
from mathutils import Vector


parser = argparse.ArgumentParser()
parser.add_argument('--input', required=True)
parser.add_argument('--out', required=True)
parser.add_argument('--animate', action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
os.makedirs(args.out, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
if args.input.lower().endswith('.fbx'):
    bpy.ops.import_scene.fbx(filepath=args.input)
else:
    bpy.ops.import_scene.gltf(filepath=args.input)
characters = list(bpy.context.scene.objects)
rigs = [o for o in characters if o.type == 'ARMATURE']
helpers = {b.custom_shape for rig in rigs for b in rig.pose.bones if b.custom_shape}
meshes = [o for o in characters if o.type == 'MESH' and o not in helpers and not o.hide_render]
characters = [o for o in characters if o not in helpers and (o.type != 'MESH' or o in meshes)]
scene = bpy.context.scene
if args.animate and bpy.data.actions:
    scene.frame_start = math.floor(min(a.frame_range[0] for a in bpy.data.actions))
    scene.frame_end = math.ceil(max(a.frame_range[1] for a in bpy.data.actions))
scene.frame_set(1)
bpy.context.view_layer.update()
points = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
lo = Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi = Vector(tuple(max(p[i] for p in points) for i in range(3)))
center = (lo + hi) * .5
height = max(hi.z - lo.z, .01)
report = {'input': args.input, 'bounds_min': list(lo), 'bounds_max': list(hi),
          'meshes': [], 'rigs': [], 'actions': []}
for obj in meshes:
    modifiers = [m for m in obj.modifiers if m.type == 'ARMATURE' and m.object]
    deform_names = {b.name for m in modifiers for b in m.object.data.bones if b.use_deform}
    deform_indices = {g.index for g in obj.vertex_groups if g.name in deform_names}
    weighted = sum(any(g.group in deform_indices and g.weight > 1e-6 for g in v.groups)
                   for v in obj.data.vertices)
    report['meshes'].append({'name': obj.name, 'vertices': len(obj.data.vertices),
        'polygons': len(obj.data.polygons), 'triangles': sum(len(p.vertices)-2 for p in obj.data.polygons),
        'armature_modifiers': len(modifiers), 'weighted_vertices': weighted,
        'unweighted_vertices': len(obj.data.vertices)-weighted,
        'materials': [m.name for m in obj.data.materials if m]})
for rig in rigs:
    rig.show_in_front = True
    report['rigs'].append({'name': rig.name, 'bones': len(rig.data.bones),
                          'bone_names': [b.name for b in rig.data.bones]})
for action in bpy.data.actions:
    report['actions'].append({'name': action.name, 'range': list(action.frame_range)})

# Verify that the downloaded skin responds to an actual joint rotation.
if rigs:
    rig = rigs[0]
    bone = next((b for b in rig.pose.bones if 'upperarm' in b.name.lower().replace('_', '')), None)
    if bone is None:
        bone = next((b for b in rig.pose.bones if 'arm' in b.name.lower()), None)
    if bone:
        action = rig.animation_data.action if rig.animation_data else None
        if rig.animation_data:
            rig.animation_data.action = None
        bpy.context.view_layer.update()
        depsgraph = bpy.context.evaluated_depsgraph_get()
        before = {o.name: [v.co.copy() for v in o.evaluated_get(depsgraph).data.vertices] for o in meshes}
        original_matrix = bone.matrix_basis.copy()
        from mathutils import Matrix
        bone.matrix_basis = bone.matrix_basis @ Matrix.Rotation(math.radians(12), 4, 'X')
        bpy.context.view_layer.update()
        moved = 0
        maximum = 0.0
        for o in meshes:
            for a, b in zip(before[o.name], o.evaluated_get(depsgraph).data.vertices):
                delta = (a-b.co).length
                moved += delta > height*1e-6
                maximum = max(maximum, delta)
        report['deformation_check'] = {'bone': bone.name, 'rotation_degrees': 12,
            'moved_vertices': moved, 'maximum_local_displacement': maximum}
        bone.matrix_basis = original_matrix
        if action:
            rig.animation_data.action = action
        scene.frame_set(1)
        bpy.context.view_layer.update()

with open(os.path.join(args.out, 'inspection.json'), 'w', encoding='utf-8') as f:
    json.dump(report, f, ensure_ascii=False, indent=2)
print('INSPECTION', json.dumps(report))

# Export just the asset, excluding the review lights and ground.
if rigs:
    bpy.ops.object.select_all(action='DESELECT')
    for obj in characters:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rigs[0]
    filename = 'NoviceAdventurer_Walk.fbx' if args.animate else 'NoviceAdventurer_Rigged.fbx'
    bpy.ops.export_scene.fbx(filepath=os.path.join(args.out, filename),
        use_selection=True, object_types={'ARMATURE', 'MESH', 'EMPTY'},
        add_leaf_bones=False, bake_anim=args.animate, bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False, path_mode='COPY', embed_textures=True,
        axis_forward='-Z', axis_up='Y')

scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.cycles.use_denoising = True
scene.render.resolution_x = 600
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Review World')
scene.world.use_nodes = True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value = (.18, .20, .24, 1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value = .6

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z', 'Y').to_euler()

for name, direction, strength, size in [
        ('Key', (2,-3,4), 500, 3), ('Fill', (-3,-1,2), 300, 3), ('Rim', (1,3,3), 600, 2)]:
    bpy.ops.object.light_add(type='AREA', location=center+Vector(direction)*height)
    light = bpy.context.object
    light.name = name
    light.data.energy = strength * height * height
    light.data.shape = 'DISK'
    light.data.size = size * height
    aim(light, center)
bpy.ops.mesh.primitive_plane_add(size=height*200, location=(center.x,center.y,lo.z-height*.003))
ground = bpy.context.object
ground.name = 'Review Ground'
material = bpy.data.materials.new('Review Ground Material')
material.use_nodes = True
bsdf = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
bsdf.inputs['Base Color'].default_value = (.095,.115,.15,1)
bsdf.inputs['Roughness'].default_value = .85
ground.data.materials.append(material)
bpy.ops.object.camera_add()
camera = bpy.context.object
camera.name = 'Review Camera'
camera.data.type = 'ORTHO'
camera.data.ortho_scale = height*1.24
scene.camera = camera
angles = [('front', (1,0,.08)), ('right', (0,-1,.08)),
          ('back', (-1,0,.08)), ('threequarter', (1,-.6,.13))]
if args.animate:
    angles = [('motion', (1,-.55,.12))]
for name, direction in angles:
    camera.location = center+Vector(direction).normalized()*height*4
    aim(camera, center)
    if args.animate:
        active_action = next((o.animation_data.action for o in rigs if o.animation_data and o.animation_data.action), None)
        if active_action is None:
            raise RuntimeError('Downloaded model has no active animation')
        start, end = active_action.frame_range
        scene.frame_start, scene.frame_end = round(start), round(end)
        scene.render.resolution_x, scene.render.resolution_y = 450, 600
        for i in range(20):
            scene.frame_set(round(start+(end-start)*i/20))
            scene.render.filepath = os.path.join(args.out, f'motion_{i:02d}.png')
            bpy.ops.render.render(write_still=True)
    else:
        scene.render.filepath = os.path.join(args.out, name+'.png')
        bpy.ops.render.render(write_still=True)
scene.frame_set(1)
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_perspective = 'CAMERA'
        area.spaces.active.shading.type = 'MATERIAL'
bpy.ops.file.pack_all()
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(args.out, 'NoviceAdventurer_Review.blend'))
print('REVIEW_COMPLETE', args.out)
