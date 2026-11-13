import bpy,os
from mathutils import Vector
OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\TripoAdventurer'
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'Adventurer_Rigged.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');sc=bpy.context.scene
bpy.ops.object.camera_add(location=(3,-5,2.4));cam=bpy.context.object;target=Vector((0,0,.95));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.5;sc.camera=cam
sc.render.engine='BLENDER_WORKBENCH';sc.display.shading.light='STUDIO';sc.display.shading.color_type='SINGLE';sc.display.shading.single_color=(.55,.6,.58);sc.display.shading.show_shadows=True
sc.render.resolution_x=900;sc.render.resolution_y=900;sc.render.resolution_percentage=100
for act,f in [('Idle',1),('Walk',1),('RigCheck',21)]:
 rig.animation_data.action=bpy.data.actions[act];sc.frame_set(f);sc.render.filepath=os.path.join(OUT,'Check_'+act+'.png');bpy.ops.render.render(write_still=True)
rig.animation_data.action=None;sc.frame_set(0);sc.render.filepath=os.path.join(OUT,'Check_TPose.png');bpy.ops.render.render(write_still=True)
