import bpy, os, json
from mathutils import Vector
OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\FantasyAdventurer'
ASSET=r'C:\Unity\Hybrid_Casual_RPG\Assets\Characters\FantasyAdventurer'
os.makedirs(ASSET,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'FantasyAdventurer_Rigged.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH');sc=bpy.context.scene
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(ASSET,'FantasyAdventurer_Rigged.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,apply_scale_options='FBX_SCALE_UNITS',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
print('EXPORTED',flush=True)
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.12
sc.render.engine='BLENDER_WORKBENCH';sc.display.shading.light='STUDIO';sc.display.shading.color_type='TEXTURE';sc.display.shading.show_shadows=True;sc.display.shading.show_cavity=True
sc.display.shading.background_type='WORLD'
if not sc.world:sc.world=bpy.data.worlds.new('Preview World')
sc.world.color=(.045,.055,.075)
sc.render.resolution_x=550;sc.render.resolution_y=650;sc.render.resolution_percentage=100
os.makedirs(os.path.join(OUT,'PreviewFrames'),exist_ok=True)
for kind in ['Idle','Walk']:
    rig.animation_data.action=bpy.data.actions[kind]
    end=90 if kind=='Idle' else 32
    for i in range(16):
        sc.frame_set(1+round(end*i/16));cam.location=Vector((2.6,-5,2));target=Vector((0,0,.9));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=os.path.join(OUT,'PreviewFrames',kind+'_'+str(i).zfill(2)+'.png');bpy.ops.render.render(write_still=True)
    for name,loc in [('Front',(0,-5,1)),('Side',(5,0,1))]:
        sc.frame_set(1);cam.location=loc;cam.rotation_euler=(Vector((0,0,.9))-cam.location).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=os.path.join(OUT,kind+'_'+name+'.png');bpy.ops.render.render(write_still=True)
print('PREVIEW_FINISHED',flush=True)
