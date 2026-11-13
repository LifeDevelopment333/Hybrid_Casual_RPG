import bpy,json,os
from mathutils import Vector
root=r'C:\Unity\Hybrid_Casual_RPG\Art\RPGCharacter'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(root,'Source','tripo_convert_4149599b-c9cf-4048-9fc3-6c31c0b939f5.fbx'))
info=[]
for o in bpy.context.scene.objects:
 d={'name':o.name,'type':o.type,'dimensions':list(o.dimensions),'rotation':list(o.rotation_euler),'scale':list(o.scale)}
 if o.type=='MESH':d.update(vertices=len(o.data.vertices),groups=[g.name for g in o.vertex_groups],modifiers=[m.type for m in o.modifiers],bounds=[list(o.matrix_world@Vector(c)) for c in o.bound_box])
 if o.type=='ARMATURE':d['bones']=[{'name':b.name,'head':list(b.head_local),'tail':list(b.tail_local)} for b in o.data.bones]
 info.append(d)
print('INSPECTION',json.dumps(info));print('ACTIONS',[(a.name,a.frame_range[:]) for a in bpy.data.actions])
json.dump(info,open(os.path.join(root,'inspection.json'),'w'),indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Source.blend'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
v=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector([min(p[i] for p in v) for i in range(3)]);hi=Vector([max(p[i] for p in v) for i in range(3)]);center=(lo+hi)/2;ext=max(hi-lo)
bpy.ops.object.camera_add(location=center+Vector((0,-2*ext,.2*ext)))
cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=ext*1.2
sc=bpy.context.scene;sc.camera=cam;sc.render.engine='BLENDER_WORKBENCH';sc.display.shading.light='STUDIO';sc.display.shading.color_type='TEXTURE';sc.render.resolution_x=850;sc.render.resolution_y=850;sc.render.resolution_percentage=100;sc.render.filepath=os.path.join(root,'Source.png');bpy.ops.render.render(write_still=True)
