import bpy, os, shutil, numpy as np, json
from mathutils import Vector
OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\FantasyAdventurer'
ASSET=r'C:\Unity\Hybrid_Casual_RPG\Assets\Characters\FantasyAdventurer'
TEX=os.path.join(ASSET,'Textures');os.makedirs(TEX,exist_ok=True)
SRC=r'C:\Users\tjals\Downloads\fantasy adventurer 3d model\tripo_convert_53991fc8-bcb9-4d4a-8921-5e688fc57ddf.fbm'
for name in ['basecolor.JPEG','normal.PNG','metallic.JPEG','roughness.JPEG']:
    shutil.copy2(os.path.join(SRC,'fantasy_adventurer_3d_model_'+name),os.path.join(TEX,name))
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'FantasyAdventurer_Rigged.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH');sc=bpy.context.scene
metal=bpy.data.images.load(os.path.join(TEX,'metallic.JPEG'),check_existing=False);rough=bpy.data.images.load(os.path.join(TEX,'roughness.JPEG'),check_existing=False)
metal.colorspace_settings.name='Non-Color';rough.colorspace_settings.name='Non-Color'
n=metal.size[0]*metal.size[1]*4;m=np.empty(n,dtype=np.float32);r=np.empty(n,dtype=np.float32);metal.pixels.foreach_get(m);rough.pixels.foreach_get(r)
packed=np.zeros((n//4,4),dtype=np.float32);packed[:,0]=m.reshape(-1,4)[:,0];packed[:,3]=1-r.reshape(-1,4)[:,0]
im=bpy.data.images.new('MetallicSmoothness',width=metal.size[0],height=metal.size[1],alpha=True);im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(packed.ravel());im.filepath_raw=os.path.join(TEX,'MetallicSmoothness.png');im.file_format='PNG';im.save()
# Confirm the exported motion loops exactly and every skinned vertex stays finite.
check={};deps=bpy.context.evaluated_depsgraph_get()
for name,end in [('Idle',91),('Walk',33)]:
    act=bpy.data.actions[name];rig.animation_data.action=act;coords=[];bounds=[]
    for f in range(1,end+1):
        sc.frame_set(f);ev=mesh.evaluated_get(deps);a=np.array([v.co[:] for v in ev.data.vertices]);assert np.isfinite(a).all()
        bounds.append([a.min(0).tolist(),a.max(0).tolist()])
        if f in [1,end]:coords.append(a)
    check[name]={'maxLoopSeamMetres':float(np.linalg.norm(coords[1]-coords[0],axis=1).max()),'minGroundZ':min(b[0][2] for b in bounds),'maxGroundZ':max(b[0][2] for b in bounds),'framesChecked':end}
rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active;space.shading.type='MATERIAL';space.region_3d.view_location=(0,0,.9);space.region_3d.view_distance=3.2
            space.region_3d.view_rotation=Vector((2.6,-5,1.1)).to_track_quat('Z','Y')
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
rig['animation_validation']=json.dumps(check)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'FantasyAdventurer_Rigged.blend'))
json.dump(check,open(os.path.join(OUT,'animation_validation.json'),'w'),indent=2)
print('VALIDATION',json.dumps(check),flush=True)
