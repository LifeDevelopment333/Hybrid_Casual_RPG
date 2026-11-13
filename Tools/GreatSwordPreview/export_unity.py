import bpy, os, json, shutil
from mathutils import Matrix

OUT=os.path.join(os.path.dirname(__file__),'Export')
TEX=os.path.join(OUT,'Textures');os.makedirs(TEX,exist_ok=True)
scene=bpy.data.scenes['RPG Character Preview'];bpy.context.window.scene=scene
rig=next(o for o in scene.objects if o.type=='ARMATURE')
weapon=next(o for o in scene.objects if o.type=='MESH' and 'model_to_rig_rest' in o)
meshes=[o for o in scene.objects if o.type=='MESH' and o!=weapon]
main=max(meshes,key=lambda o:len(o.data.vertices))
baked=bpy.data.images['RPG Character Clean Base Color 4K']
head=main.data.materials[1];head.node_tree.nodes.clear()
nodes=head.node_tree.nodes;links=head.node_tree.links
bsdf=nodes.new('ShaderNodeBsdfPrincipled');bsdf.inputs['Roughness'].default_value=.68
output=nodes.new('ShaderNodeOutputMaterial');links.new(bsdf.outputs[0],output.inputs['Surface'])
tex=nodes.new('ShaderNodeTexImage');tex.image=baked
uv=nodes.new('ShaderNodeUVMap');uv.uv_map=main.data.uv_layers[0].name
links.new(uv.outputs[0],tex.inputs['Vector']);links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
manifest={'materials':[], 'sword_materials':[], 'actions':[]}
saved={}
for ob in meshes:
    ob.data.uv_layers.active_index=0;ob.data.uv_layers[0].active_render=True
    for mat in ob.data.materials:
        if not mat or any(m['name']==mat.name for m in manifest['materials']):continue
        info={'name':mat.name,'images':[]}
        for node in mat.node_tree.nodes:
            if node.type!='TEX_IMAGE' or not node.image:continue
            img=node.image
            if img.as_pointer() not in saved:
                name=('HeadColor4K' if img==baked else 'LipColor' if 'exec-' in img.name else os.path.splitext(img.name)[0])+'.png'
                img.filepath_raw=os.path.join(TEX,name);img.file_format='PNG';img.save()
                if img.packed_file:img.unpack(method='REMOVE')
                saved[img.as_pointer()]=name
            info['images'].append(saved[img.as_pointer()])
        manifest['materials'].append(info)
for mat in weapon.data.materials:
    p=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    manifest['sword_materials'].append({'name':mat.name,'color':list(p.inputs['Base Color'].default_value),'metallic':p.inputs['Metallic'].default_value,'roughness':p.inputs['Roughness'].default_value})
# Export a socket with the same rest-space transform as the modeled sword.
# FBX converts this helper and the separate sword using the same axis convention.
rig.animation_data.action=None
for p in rig.pose.bones:p.matrix_basis=Matrix.Identity(4)
scene.frame_set(1);bpy.context.view_layer.update()
vals=weapon['model_to_rig_rest'];rest=Matrix([vals[i:i+4] for i in range(0,16,4)])
socket=bpy.data.objects.new('SwordSocket',None);scene.collection.objects.link(socket)
socket.parent=rig;socket.parent_type='BONE';socket.parent_bone='WeaponGrip_R'
socket.matrix_world=rig.matrix_world@rest
bpy.context.view_layer.update()
for o in scene.objects:o.select_set(False)
for o in meshes+[rig,socket]:o.select_set(True)
bpy.context.view_layer.objects.active=rig
actions=[a for a in bpy.data.actions if a.name.startswith('GS | ')]
for a in actions:manifest['actions'].append({'name':a.name,'frames':list(a.frame_range)})
rig.animation_data.action=next(a for a in actions if a.name=='GS | great sword idle')
rig.animation_data.action_slot=rig.animation_data.action.slots[0]
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'Adventurer_GreatSword.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH','EMPTY'},axis_forward='-Z',axis_up='Y',global_scale=1.0,
    add_leaf_bones=False,use_armature_deform_only=False,bake_anim=True,bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,bake_anim_step=1.0,
    path_mode='RELATIVE',embed_textures=False,mesh_smooth_type='FACE')
shutil.copy2(r'C:\Users\tjals\Downloads\RPG_Character\GreatSword\GreatSword_Adventurer.fbx',os.path.join(OUT,'GreatSword_Adventurer.fbx'))
with open(os.path.join(OUT,'manifest.json'),'w') as f:json.dump(manifest,f,indent=2)
print('UNITY_EXPORT_COMPLETE',len(actions),manifest['materials'],flush=True)
