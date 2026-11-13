import bpy, json, os
from mathutils import Vector
out = r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\TripoAdventurer'
os.makedirs(out, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r'C:\Users\tjals\Downloads\adventurer character 3d model.fbx')
info=[]
for ob in bpy.context.scene.objects:
    d={'name':ob.name,'type':ob.type,'dimensions':list(ob.dimensions),'location':list(ob.location),'rotation':list(ob.rotation_euler),'scale':list(ob.scale)}
    if ob.type=='MESH':
        d.update(vertices=len(ob.data.vertices), polygons=len(ob.data.polygons), groups=[g.name for g in ob.vertex_groups], materials=[m.name if m else None for m in ob.data.materials])
        d['bounds']=[list(ob.matrix_world@Vector(c)) for c in ob.bound_box]
    if ob.type=='ARMATURE':d['bones']=[b.name for b in ob.data.bones]
    info.append(d)
images=[]
for im in bpy.data.images:
    images.append({'name':im.name,'filepath':im.filepath,'size':list(im.size),'packed':bool(im.packed_file)})
    if im.has_data:
        im.filepath_raw=os.path.join(out,im.name.split('.')[0]+'.png');im.file_format='PNG';im.save()
json.dump({'objects':info,'images':images},open(os.path.join(out,'source_info.json'),'w'),indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Source_Inspection.blend'))
mesh=[o for o in bpy.context.scene.objects if o.type=='MESH']
corners=[o.matrix_world@Vector(c) for o in mesh for c in o.bound_box]
lo=Vector([min(v[i] for v in corners) for i in range(3)]);hi=Vector([max(v[i] for v in corners) for i in range(3)])
center=(lo+hi)/2; extent=max(hi-lo)
bpy.ops.object.camera_add(location=center+Vector((0,-extent*2,extent*.12)))
cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=extent*1.2;bpy.context.scene.camera=cam
sc=bpy.context.scene;sc.render.engine='BLENDER_WORKBENCH';sc.display.shading.light='STUDIO';sc.display.shading.color_type='TEXTURE';sc.display.shading.show_shadows=True
sc.render.resolution_x=900;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.render.filepath=os.path.join(out,'Source_Front.png');bpy.ops.render.render(write_still=True)
cam.location=center+Vector((extent*2,0,extent*.12));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=os.path.join(out,'Source_Side.png');bpy.ops.render.render(write_still=True)
print(json.dumps({'objects':info,'images':images}))
