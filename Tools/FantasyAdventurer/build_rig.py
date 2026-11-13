import bpy, os, math, json, numpy as np
from mathutils import Vector, Matrix
OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\FantasyAdventurer'
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'Prepared.blend'))
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bones={
 'Root':((0,0,0),(0,0,.14),None),
 'Hips':((0,.035,.93),(0,.025,1.04),'Root'),
 'Spine':((0,.025,1.04),(0,.02,1.17),'Hips'),
 'Chest':((0,.02,1.17),(0,.02,1.31),'Spine'),
 'UpperChest':((0,.02,1.31),(0,.025,1.43),'Chest'),
 'Neck':((0,.025,1.43),(0,.02,1.52),'UpperChest'),
 'Head':((0,.02,1.52),(0,.015,1.76),'Neck')}
for side,s in [('Left',1),('Right',-1)]:
    sh=(s*.285,.025,1.39);el=(s*.55,.03,1.39);wr=(s*.765,.025,1.37);hand=(s*.91,-.005,1.365)
    hip=(s*.13,.045,.93);knee=(s*.18,.025,.53);ankle=(s*.215,.055,.155);toe=(s*.22,-.10,.055)
    bones.update({side+'Shoulder':((s*.075,.025,1.405),sh,'UpperChest'),side+'UpperArm':(sh,el,side+'Shoulder'),side+'LowerArm':(el,wr,side+'UpperArm'),side+'Hand':(wr,hand,side+'LowerArm'),side+'UpperLeg':(hip,knee,'Hips'),side+'LowerLeg':(knee,ankle,side+'UpperLeg'),side+'Foot':(ankle,toe,side+'LowerLeg'),side+'Toes':(toe,(s*.22,-.16,.045),side+'Foot')})
ad=bpy.data.armatures.new('FantasyAdventurer_Humanoid');rig=bpy.data.objects.new('FantasyAdventurer_Rig',ad);bpy.context.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for name,(h,t,parent) in bones.items():
    b=ad.edit_bones.new(name);b.head=h;b.tail=t
    if parent:b.parent=ad.edit_bones[parent]
    b.use_deform=name!='Root'
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True;ad.display_type='OCTAHEDRAL'
mesh.select_set(True)
try:
    bpy.ops.object.parent_set(type='ARMATURE_AUTO');print('HEAT_WEIGHTS_FINISHED',flush=True)
except RuntimeError as e:print('HEAT_WARNING',str(e),flush=True)
mesh.parent=rig
if not any(m.type=='ARMATURE' for m in mesh.modifiers):
    mod=mesh.modifiers.new('Humanoid Skin','ARMATURE');mod.object=rig
for b in ad.bones:
    if b.use_deform and not mesh.vertex_groups.get(b.name):mesh.vertex_groups.new(name=b.name)

# Keep heat diffusion at shoulders and elbows; correct cross-leg and head leakage.
def smooth(a,b,v):
    t=max(0,min(1,(v-a)/(b-a)));return t*t*(3-2*t)
weights=np.zeros((len(mesh.data.vertices),len(mesh.vertex_groups)))
indices={g.name:g.index for g in mesh.vertex_groups}
for v in mesh.data.vertices:
    for g in v.groups:weights[v.index,g.group]=g.weight
    x,y,z=v.co;row=weights[v.index]
    if z<1.015:
        row[:]=0;side='Left' if x>0 else 'Right'
        hip=smooth(.80,1.01,z);knee=smooth(.47,.61,z);ankle=smooth(.14,.245,z)
        vals={side+'Foot':1-ankle,side+'LowerLeg':ankle*(1-knee),side+'UpperLeg':knee*(1-hip),'Hips':hip}
        # The tunic hem moves gently with both thighs instead of tearing at the centre.
        if z>.77:
            across=smooth(-.055,.055,x);vals={'LeftUpperLeg':knee*(1-hip)*across,'RightUpperLeg':knee*(1-hip)*(1-across),'Hips':hip}
        for n,w in vals.items():row[indices[n]]=w
    elif z>1.47:
        row[:]=0;head=smooth(1.47,1.54,z);row[indices['Head']]=head;row[indices['Neck']]=1-head
    else:
        # Never let a hand influence the torso or the opposite limb.
        allowed=['Hips','Spine','Chest','UpperChest','Neck','Head']
        if abs(x)>.20 and z>1.22:
            side='Left' if x>0 else 'Right';allowed += [side+n for n in ['Shoulder','UpperArm','LowerArm','Hand']]
        for n,i in indices.items():
            if n not in allowed:row[i]=0
        if row.sum()<.0001:
            # Deterministic fallback for disconnected scan islands.
            candidates=[n for n in allowed if n not in ['Head','Neck']]
            def dist(n):
                b=ad.bones[n];p=b.head_local;d=b.tail_local-p;t=max(0,min(1,(v.co-p).dot(d)/d.length_squared));return (v.co-p-t*d).length
            for n in sorted(candidates,key=dist)[:3]:row[indices[n]]=1/(dist(n)+.025)**5
    if row.sum():row/=row.sum()
# Smooth transition rings while keeping feet, face, and hands rigid inside their regions.
edges=np.array([e.vertices[:] for e in mesh.data.edges]);deg=np.maximum(np.bincount(edges.ravel(),minlength=len(weights)),1)[:,None]
pos=np.array([v.co[:] for v in mesh.data.vertices]);fixed=(pos[:,2]<.13)|(pos[:,2]>1.55)|(np.abs(pos[:,0])>.80)
original=weights.copy()
for it in range(6):
    acc=np.zeros_like(weights);np.add.at(acc,edges[:,0],weights[edges[:,1]]);np.add.at(acc,edges[:,1],weights[edges[:,0]])
    weights=.65*weights+.35*acc/deg;weights[fixed]=original[fixed]
for g in mesh.vertex_groups:g.remove(list(range(len(weights))))
for i,row in enumerate(weights):
    ids=np.argsort(row)[-4:];total=sum(row[j] for j in ids if row[j]>.001)
    for j in ids:
        if row[j]>.001:mesh.vertex_groups[int(j)].add([i],float(row[j]/total),'REPLACE')

ref={b.name:b.matrix_local.copy() for b in ad.bones}
def put(n,m):
    rig.pose.bones[n].matrix=m;bpy.context.view_layer.update()
def local(n,axis=None,angle=0):
    pb=rig.pose.bones[n];p=pb.parent;m=p.matrix@ref[p.name].inverted()@ref[n] if p else ref[n].copy()
    if axis:
        pos=m.translation.copy();m=Matrix.Rotation(math.radians(angle),4,axis)@m;m.translation=pos
    put(n,m)
def aim(n,h,t):
    direction=ref[n].to_3x3()@Vector((0,1,0));q=direction.rotation_difference((t-h).normalized())
    m=q.to_matrix().to_4x4()@ref[n];m.translation=h;put(n,m)
def chain(n1,n2,head,target,pole):
    l1=ad.bones[n1].length;l2=ad.bones[n2].length;line=target-head;d=min(line.length,(l1+l2)*.995);axis=line.normalized()
    end=head+axis*d;a=(l1*l1-l2*l2+d*d)/(2*d);height=math.sqrt(max(0,l1*l1-a*a))
    bend=(pole-axis*pole.dot(axis)).normalized();joint=head+axis*a+bend*height
    aim(n1,head,joint);aim(n2,joint,end);return end
def footpath(phase):
    if phase<.5:return -.16+.64*phase,0,True
    u=(phase-.5)*2;s=u*u*(3-2*u);return .16-.32*s,.065*math.sin(math.pi*u)**2,False
stance_errors=[]
def pose(kind,t):
    for pb in rig.pose.bones:pb.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update();a=2*math.pi*t;walk=kind=='Walk';sway=.012*math.sin(a) if walk else .004*math.sin(a)
    local('Root');hips=ref['Hips'].copy();hips.translation+=Vector((sway,0,-.04+.009*(1-math.cos(2*a)) if walk else -.012))
    put('Hips',hips);local('Spine','Z',2*math.sin(a) if walk else .45*math.sin(a))
    local('Chest','Z',-3*math.sin(a) if walk else -.3*math.sin(a));local('UpperChest','X',1.7 if walk else .6*math.sin(a))
    local('Neck');local('Head','Z',.7*math.sin(a) if walk else 1.2*math.sin(a))
    pelvis=rig.pose.bones['Hips'].matrix@ref['Hips'].inverted();body=rig.pose.bones['UpperChest'].matrix@ref['UpperChest'].inverted()
    for side,sign,offset in [('Left',1,0),('Right',-1,.5)]:
        phase=(t+offset)%1;y,lift,stance=footpath(phase) if walk else (0,0,True)
        ankle=ref[side+'Foot'].translation.copy();ankle.y+=y;ankle.z+=lift
        hip=pelvis@ref[side+'UpperLeg'].translation;target=chain(side+'UpperLeg',side+'LowerLeg',hip,ankle,Vector((0,-1,0)))
        if walk and stance:stance_errors.append((target-ankle).length)
        fm=ref[side+'Foot'].copy();fm.translation=target;put(side+'Foot',fm);local(side+'Toes')
        local(side+'Shoulder','Y',sign*5)
        sh=rig.pose.bones[side+'Shoulder'].matrix@ref[side+'Shoulder'].inverted()@ref[side+'UpperArm'];shoulder=sh.translation
        wrist=body@Vector((sign*.305,-.045,.952))
        if walk:wrist.y+=sign*.095*math.cos(a)
        else:wrist.y+=.004*math.sin(a);wrist.z+=.002*math.cos(a)
        end=chain(side+'UpperArm',side+'LowerArm',shoulder,wrist,Vector((sign*.4,-1,0)))
        aim(side+'Hand',end,end+Vector((sign*.023,-.012,-.145)))

rig.animation_data_create()
for name,frames in [('Idle',90),('Walk',32)]:
    action=bpy.data.actions.new(name);rig.animation_data.action=action;prev={}
    for f in range(frames+1):
        pose(name,f/frames)
        for pb in rig.pose.bones:
            pb.rotation_mode='QUATERNION';q=pb.rotation_quaternion.copy()
            if pb.name in prev and q.dot(prev[pb.name])<0:q.negate();pb.rotation_quaternion=q
            prev[pb.name]=q.copy()
            for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(prop,frame=f+1)
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for k in fc.keyframe_points:k.interpolation='LINEAR'
    action.use_fake_user=True
    action['loop']=True;action['duration_seconds']=frames/30
sc=bpy.context.scene;sc.render.fps=30;sc.frame_start=1;sc.frame_end=91
rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
rig['notes']='T-pose humanoid bind. Idle: 1-91; Walk: 1-33. 30fps. In-place walk; speed 0.60 m/s. No finger or facial animation.'
for im in bpy.data.images:
    if im.source=='FILE' and im.has_data:im.pack()
report={'vertices':len(mesh.data.vertices),'triangles':len(mesh.data.polygons),'bones':len(ad.bones),'unweighted':sum(len(v.groups)==0 for v in mesh.data.vertices),'maxInfluences':max(len(v.groups) for v in mesh.data.vertices),'stanceTargetErrorMetres':max(stance_errors),'clips':{'Idle':{'frames':[1,91],'seconds':3},'Walk':{'frames':[1,33],'seconds':32/30,'inPlace':True,'recommendedSpeed':.6}}}
json.dump(report,open(os.path.join(OUT,'rig_report.json'),'w'),indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'FantasyAdventurer_Rigged.blend'))
print('RIG_REPORT',json.dumps(report),flush=True)
