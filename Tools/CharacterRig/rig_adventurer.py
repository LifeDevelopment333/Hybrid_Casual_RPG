import bpy, math, os, json, numpy as np
from mathutils import Vector, Matrix
OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\TripoAdventurer'
ASSET=r'C:\Unity\Hybrid_Casual_RPG\Assets\Characters\TripoAdventurer'
os.makedirs(ASSET,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'Source_Inspection.blend'))
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
mesh.name='Adventurer_Body'
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.context.view_layer.objects.active=mesh
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
# Preserve the generated silhouette while making the inspection copy practical to animate.
dec=mesh.modifiers.new('Prototype reduction','DECIMATE');dec.ratio=40000/len(mesh.data.polygons)
bpy.ops.object.modifier_apply(modifier=dec.name)
for p in mesh.data.polygons:p.use_smooth=True
bones={
'Root':((.02,.04,0),(.02,.04,.12),None),
'Hips':((.02,.04,.48),(.02,.04,.55),'Root'),
'Spine':((.02,.04,.55),(.02,.04,.64),'Hips'),
'Chest':((.02,.04,.64),(.02,.04,.73),'Spine'),
'UpperChest':((.02,.04,.73),(.02,.035,.80),'Chest'),
'Neck':((.02,.035,.80),(.02,.025,.85),'UpperChest'),
'Head':((.02,.025,.85),(.02,.025,.98),'Neck'),
}
for side,s in [('Left',1),('Right',-1)]:
    cx=.02
    sh=(cx+s*.105,.025,.775);el=(cx+s*.16,.02,.635);wr=(cx+s*.185,-.005,.495);ha=(cx+s*.195,-.018,.445)
    hip=(cx+s*.065,.045,.48);kn=(cx+s*.105,.045,.29);an=(cx+s*.125,.045,.082);toe=(cx+s*.13,-.045,.035)
    bones.update({side+'Shoulder':((cx+s*.025,.03,.785),sh,'UpperChest'),side+'UpperArm':(sh,el,side+'Shoulder'),side+'LowerArm':(el,wr,side+'UpperArm'),side+'Hand':(wr,ha,side+'LowerArm'),side+'UpperLeg':(hip,kn,'Hips'),side+'LowerLeg':(kn,an,side+'UpperLeg'),side+'Foot':(an,toe,side+'LowerLeg'),side+'Toes':(toe,(toe[0],-.095,.025),side+'Foot')})
ad=bpy.data.armatures.new('Adventurer_Humanoid');rig=bpy.data.objects.new('Adventurer_Rig',ad);bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);mesh.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
for name,(h,t,parent) in bones.items():
    b=ad.edit_bones.new(name);b.head=h;b.tail=t
    if parent:b.parent=ad.edit_bones[parent]
    if name=='Root':b.use_deform=False
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
mesh.select_set(True);bpy.context.view_layer.objects.active=rig
try:bpy.ops.object.parent_set(type='ARMATURE_AUTO')
except RuntimeError as e:print('AUTO_WEIGHT_ERROR',str(e))
# Ensure all vertices have useful weights, then make held equipment rigid.
deform=[b for b in ad.bones if b.use_deform]
for b in deform:
    if not mesh.vertex_groups.get(b.name):mesh.vertex_groups.new(name=b.name)
def rigid(v,name):
    for g in mesh.vertex_groups:g.remove([v.index])
    mesh.vertex_groups[name].add([v.index],1,'REPLACE')
fallback=0;overrides={};fixed=[]
for v in mesh.data.vertices:
    x,y,z=v.co;name=None
    if x<-.15 and .205<z<.51:name='RightHand' # Entire sword, guard and closed grip; exclude boots.
    elif x>.213 and .355<z<.65:name='LeftHand' # Shield shell; avoid the nearby outer thigh.
    elif x>.155 and .43<z<.495:name='LeftHand' # Closed fingers and grip inside the shell.
    elif y>.105 and z>.55:name='Chest' # Bedroll and backpack.
    elif z>.865:name='Head'
    if name:rigid(v,name);overrides[name]=overrides.get(name,0)+1;fixed.append(v.index)
    else:
        p=v.co
        # Restrict influences by anatomy so nearby hands cannot pull the thighs.
        arm_region=.42<z<.8 and abs(x-.02)>(.10 if z>.72 else .14 if z>.57 else .165)
        if arm_region:
            side='Left' if x>.02 else 'Right'
            candidates=[side+s for s in ['Shoulder','UpperArm','LowerArm','Hand']]
        elif z<.52:
            side='Left' if x>.02 else 'Right'
            candidates=[side+s for s in ['UpperLeg','LowerLeg','Foot','Toes']]
            if z>.41:candidates+=['Hips']
        else:candidates=['Hips','Spine','Chest','UpperChest','Neck','Head']
        def dist(n):
            b=ad.bones[n];a=b.head_local;d=b.tail_local-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared));return (p-a-t*d).length
        pairs=sorted([(dist(n),n) for n in candidates])[:3]
        weights=[1/(d+.018)**6 for d,n in pairs]
        if z<.52 and not arm_region:
            def smooth(a,b,t):
                q=max(0,min(1,(t-a)/(b-a)));return q*q*(3-2*q)
            knee=smooth(.25,.335,z);ankle=smooth(.105,.165,z);hip=smooth(.425,.485,z)
            pairs=[(0,side+'Foot'),(0,side+'LowerLeg'),(0,side+'UpperLeg'),(0,'Hips')]
            weights=[1-ankle,ankle*(1-knee),knee*(1-hip),hip]
            if z>.335:
                across=smooth(-.02,.06,x)
                pairs=[(0,'LeftUpperLeg'),(0,'RightUpperLeg'),(0,'Hips')]
                weights=[(1-hip)*across,(1-hip)*(1-across),hip]
        total=sum(weights)
        for g in mesh.vertex_groups:g.remove([v.index])
        for w,(_,n) in zip(weights,pairs):
            if w/total>.008:mesh.vertex_groups[n].add([v.index],w/total,'REPLACE')
        fallback+=1
# Smooth graph weights around joints and region boundaries, preserving rigid equipment.
weights=np.zeros((len(mesh.data.vertices),len(mesh.vertex_groups)),dtype=np.float64)
for v in mesh.data.vertices:
    for g in v.groups:weights[v.index,g.group]=g.weight
edges=np.array([e.vertices[:] for e in mesh.data.edges]);degree=np.bincount(edges.ravel(),minlength=len(weights));degree=np.maximum(degree,1)[:,None]
fixed_weights=weights[fixed].copy()
for iteration in range(18):
    summed=np.zeros_like(weights);np.add.at(summed,edges[:,0],weights[edges[:,1]]);np.add.at(summed,edges[:,1],weights[edges[:,0]])
    weights=.45*weights+.55*summed/degree;weights[fixed]=fixed_weights
for g in mesh.vertex_groups:g.remove(list(range(len(weights))))
for i,row in enumerate(weights):
    ids=np.argsort(row)[-4:];total=sum(row[j] for j in ids)
    for j in ids:
        if row[j]>.001:mesh.vertex_groups[int(j)].add([i],float(row[j]/total),'REPLACE')
# Normalize and cap to four influences for Unity.
bpy.context.view_layer.objects.active=mesh;rig.select_set(False)
bpy.ops.object.vertex_group_limit_total(limit=4);bpy.ops.object.vertex_group_normalize_all(lock_active=False)
# Bake an actual T-pose bind mesh, retaining the source neutral pose as a clip.
original={b.name:b.matrix_local.copy() for b in ad.bones}
for side,s in [('Left',1),('Right',-1)]:
    for suffix in ['UpperArm','LowerArm','Hand']:
        pb=rig.pose.bones[side+suffix];direction=pb.bone.tail_local-pb.bone.head_local
        rot=direction.rotation_difference(Vector((s,0,0))).to_matrix().to_4x4()
        parent=pb.parent
        head=parent.tail if suffix!='UpperArm' else pb.bone.head_local
        mat=rot@original[pb.name];mat.translation=head;pb.matrix=mat
        bpy.context.view_layer.update()
armmod=next(m for m in mesh.modifiers if m.type=='ARMATURE')
bpy.context.view_layer.objects.active=mesh;bpy.ops.object.modifier_apply(modifier=armmod.name)
mesh.select_set(False);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
mod=mesh.modifiers.new('Humanoid Skin','ARMATURE');mod.object=rig
# Match a standard 1.8m adventurer without relying on import scale factors.
rig.scale=(1.8,)*3;mesh.scale=(1.8,)*3
# mesh inherits rig scale through parent: its local scale stays 1.
mesh.scale=(1,1,1)
neutral={}
for pb in rig.pose.bones:
    pb.matrix=original[pb.name];bpy.context.view_layer.update()
for pb in rig.pose.bones:neutral[pb.name]=pb.matrix_basis.copy()
def key_pose(frame,offsets,root_bob=0):
    for pb in rig.pose.bones:
        pb.matrix_basis=neutral[pb.name]
        pb.rotation_mode='QUATERNION'
        if pb.name in offsets:
            axis,deg=offsets[pb.name];pb.matrix_basis=neutral[pb.name]@Matrix.Rotation(math.radians(deg),4,axis)
        if pb.name=='Hips':pb.location.z+=root_bob
        pb.keyframe_insert('location',frame=frame);pb.keyframe_insert('rotation_quaternion',frame=frame);pb.keyframe_insert('scale',frame=frame)
rig.animation_data_create()
for name,end in [('Idle',60),('Walk',40),('RigCheck',120)]:
    act=bpy.data.actions.new(name);rig.animation_data.action=act
    if name=='Idle':
        for f,a in [(1,0),(16,1.0),(31,0),(46,-.7),(61,0)]:key_pose(f,{'Chest':('X',a),'Head':('X',-a*.4)},.001*abs(a))
    elif name=='Walk':
        for f,phase in [(1,0),(6,.25),(11,.5),(16,.75),(21,1),(26,1.25),(31,1.5),(36,1.75),(41,2)]:
            t=phase*math.pi;p=math.cos(t)
            key_pose(f,{'LeftUpperLeg':('X',25*p),'RightUpperLeg':('X',-25*p),'LeftLowerLeg':('X',-max(0,-p)*35),'RightLowerLeg':('X',-max(0,p)*35),'LeftUpperArm':('X',-12*p),'RightUpperArm':('X',12*p)},.006*abs(p))
    else:
        for f,a in [(1,0),(21,1),(41,0),(61,-1),(81,0),(101,1),(121,0)]:
            key_pose(f,{'LeftUpperArm':('X',30*a),'RightUpperArm':('X',-30*a),'LeftLowerArm':('X',-55*max(a,0)),'RightLowerArm':('X',-55*max(-a,0)),'LeftUpperLeg':('X',18*a),'RightUpperLeg':('X',-18*a),'LeftLowerLeg':('X',-25*max(-a,0)),'RightLowerLeg':('X',-25*max(a,0)),'Head':('Y',15*a)})
    act.use_fake_user=True
rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
sc=bpy.context.scene;sc.render.fps=30;sc.frame_start=1;sc.frame_end=120
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Adventurer_Rigged.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(ASSET,'Adventurer_Rigged.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
report={'sourceTriangles':978712,'triangles':len(mesh.data.polygons),'vertices':len(mesh.data.vertices),'bones':list(bones),'fallbackVertices':fallback,'rigidEquipmentVertices':overrides,'unweighted':sum(not len(v.groups) for v in mesh.data.vertices),'clips':['Idle','Walk','RigCheck'],'notes':['No source textures provided','Prototype skin weights; no individual finger rig','Generated unified equipment mesh retains original topology']}
json.dump(report,open(os.path.join(OUT,'rig_report.json'),'w'),indent=2);print(json.dumps(report))
