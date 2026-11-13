import bpy, os, json
from mathutils import Vector
OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\FantasyAdventurer'
SRC=r'C:\Users\tjals\Downloads\fantasy adventurer 3d model\tripo_convert_53991fc8-bcb9-4d4a-8921-5e688fc57ddf.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
info=[]
for o in bpy.context.scene.objects:
    d=dict(name=o.name,type=o.type,dimensions=list(o.dimensions),location=list(o.location),scale=list(o.scale),rotation=list(o.rotation_euler))
    if o.type=='MESH':
        d.update(vertices=len(o.data.vertices),faces=len(o.data.polygons),materials=[m.name if m else None for m in o.data.materials])
    if o.type=='ARMATURE':d['bones']=[b.name for b in o.data.bones]
    info.append(d)
print('SOURCE_INFO',json.dumps(info))
print('IMAGES',[(i.name,i.filepath,list(i.size)) for i in bpy.data.images])
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Source_Inspection.blend'))
corners=[o.matrix_world@Vector(c) for o in bpy.context.scene.objects if o.type=='MESH' for c in o.bound_box]
lo=Vector([min(v[i] for v in corners) for i in range(3)]);hi=Vector([max(v[i] for v in corners) for i in range(3)])
center=(lo+hi)/2;extent=max(hi-lo)
bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=extent*1.15
sc=bpy.context.scene;sc.camera=cam;sc.render.engine='BLENDER_WORKBENCH';sc.display.shading.light='STUDIO';sc.display.shading.color_type='TEXTURE'
sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100
for name,vec in [('Front',(0,-2,.05)),('Side',(2,0,.05)),('Back',(0,2,.05))]:
    cam.location=center+Vector(vec)*extent;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=os.path.join(OUT,'Source_'+name+'.png');bpy.ops.render.render(write_still=True)
