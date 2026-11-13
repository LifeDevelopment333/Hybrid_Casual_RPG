"""Rebuild preview motion from joint targets, with an explicit knee pole and flat feet."""
import bpy, os, math, json, shutil
from mathutils import Vector, Matrix

OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\TripoAdventurer'
ASSET=r'C:\Unity\Hybrid_Casual_RPG\Assets\Characters\TripoAdventurer'
backup=os.path.join(OUT,'BeforeAnimationFix');os.makedirs(backup,exist_ok=True)
for p in [os.path.join(OUT,'Adventurer_Rigged.blend'),os.path.join(ASSET,'Adventurer_Rigged.fbx')]:
    dst=os.path.join(backup,os.path.basename(p))
    if not os.path.exists(dst):shutil.copy2(p,dst)
bpy.ops.wm.open_mainfile(filepath=os.path.join(backup,'Adventurer_Rigged.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH');sc=bpy.context.scene
rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);bpy.context.view_layer.update()
ref={p.name:p.matrix.copy() for p in rig.pose.bones}
rig.animation_data_clear()
for act in list(bpy.data.actions):bpy.data.actions.remove(act)

def put(n,m):
    rig.pose.bones[n].matrix=m
    bpy.context.view_layer.update()
def local(n,axis=None,angle=0):
    p=rig.pose.bones[n];parent=p.parent
    m=(parent.matrix@ref[parent.name].inverted()@ref[n]) if parent else ref[n].copy()
    if axis and angle:
        r=Matrix.Rotation(math.radians(angle),4,axis);pos=m.translation.copy();m=r@m;m.translation=pos
    put(n,m)
def aim(n,head,tail):
    direction=ref[n].to_3x3()@Vector((0,1,0))
    q=direction.rotation_difference((tail-head).normalized())
    m=q.to_matrix().to_4x4()@ref[n];m.translation=head;put(n,m)
def chain(prefix,hip,target,pole):
    n1=prefix+'UpperLeg';n2=prefix+'LowerLeg'
    l1=rig.data.bones[n1].length;l2=rig.data.bones[n2].length
    line=target-hip;distance=min(line.length,(l1+l2)*.998);axis=line.normalized()
    target=hip+axis*distance
    a=(l1*l1-l2*l2+distance*distance)/(2*distance)
    h=math.sqrt(max(0,l1*l1-a*a));bend=(pole-axis*pole.dot(axis)).normalized()
    knee=hip+axis*a+bend*h
    aim(n1,hip,knee);aim(n2,knee,target)
    return target,knee
def arm(side,shoulder,target,pole,body_delta):
    n1=side+'UpperArm';n2=side+'LowerArm'
    l1=rig.data.bones[n1].length;l2=rig.data.bones[n2].length
    v=target-shoulder;d=min(v.length,(l1+l2)*.992);axis=v.normalized();target=shoulder+axis*d
    a=(l1*l1-l2*l2+d*d)/(2*d);h=math.sqrt(max(0,l1*l1-a*a))
    bend=(pole-axis*pole.dot(axis)).normalized();elbow=shoulder+axis*a+bend*h
    aim(n1,shoulder,elbow);aim(n2,elbow,target)
    hand=body_delta@ref[side+'Hand'];hand.translation=target;put(side+'Hand',hand)

def footpath(phase):
    # Half a cycle in stance: linear travel cancels forward movement in a real walk.
    if phase<.5:return -.065+.26*phase,0.0,True
    u=(phase-.5)*2;s=u*u*(3-2*u)
    return .065-.13*s,.032*math.sin(math.pi*u)**2,False

metrics=[]
def pose(kind,t):
    for p in rig.pose.bones:p.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()
    a=2*math.pi*t
    sway=.003*math.sin(a) if kind=='Walk' else 0
    lower=.020 if kind=='Walk' else .004*(1-math.cos(a)) if kind=='RigCheck' else 0
    bob=.003*(1-math.cos(2*a)) if kind=='Walk' else 0
    local('Root')
    hips=ref['Hips'].copy();hips.translation+=Vector((sway,0,-lower+bob));put('Hips',hips)
    local('Spine','Z',1.5*math.sin(a) if kind=='Walk' else 0)
    local('Chest','Z',-2.2*math.sin(a) if kind=='Walk' else 0)
    local('UpperChest','X',.45*math.sin(a) if kind=='Idle' else 0)
    local('Neck');local('Head')
    delta=rig.pose.bones['UpperChest'].matrix@ref['UpperChest'].inverted()
    pelvis_delta=rig.pose.bones['Hips'].matrix@ref['Hips'].inverted()
    guard=(.5-.5*math.cos(a)) if kind=='RigCheck' else 0
    for side,offset,sign in [('Left',0,1),('Right',.5,-1)]:
        phase=(t+offset)%1
        y,lift,stance=footpath(phase) if kind=='Walk' else (0,0,True)
        ankle=ref[side+'Foot'].translation.copy();ankle.y+=y;ankle.z+=lift
        hip=pelvis_delta@ref[side+'UpperLeg'].translation
        ankle,knee=chain(side,hip,ankle,Vector((0,-1,0)))
        fm=ref[side+'Foot'].copy();fm.translation=ankle;put(side+'Foot',fm)
        local(side+'Toes')
        local(side+'Shoulder')
        upper=rig.pose.bones[side+'Shoulder'].matrix@ref[side+'Shoulder'].inverted()@ref[side+'UpperArm']
        shoulder=upper.translation
        wrist=delta@ref[side+'Hand'].translation
        if kind=='Walk':wrist.y+=sign*.018*math.cos(a)
        if kind=='RigCheck':wrist+=Vector((0,-.025*guard,.035*guard))
        # An elbow pole in front of the body prevents backwards folding.
        arm(side,shoulder,wrist,Vector((sign*.15,-1,0)),delta)
        if kind=='Walk':metrics.append({'framePhase':t,'side':side,'stance':stance,'ankle':list(ankle),'knee':list(knee),'targetHeight':ref[side+'Foot'].translation.z+lift})
    bpy.context.view_layer.update()

rig.animation_data_create()
for name,duration in [('Idle',60),('Walk',40),('RigCheck',120)]:
    action=bpy.data.actions.new(name);rig.animation_data.action=action
    prev={}
    for f in range(duration+1):
        pose(name,f/duration)
        for pb in rig.pose.bones:
            pb.rotation_mode='QUATERNION'
            q=pb.rotation_quaternion.copy()
            if pb.name in prev and q.dot(prev[pb.name])<0:q.negate();pb.rotation_quaternion=q
            prev[pb.name]=q.copy()
            for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(prop,frame=f+1)
    # Linear dense samples avoid Bezier overshoot below the ground.
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for k in fc.keyframe_points:k.interpolation='LINEAR'
    action.use_fake_user=True
rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);sc.render.fps=30;sc.frame_end=121
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Adventurer_Rigged.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(ASSET,'Adventurer_Rigged.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
stance_error=max(abs(m['ankle'][2]-m['targetHeight']) for m in metrics if m['stance'])
report={'method':'Two-bone analytic IK, forward knee pole, stance/swing foot trajectories, world-stable foot and grip orientations','stanceAnkleErrorMetres':stance_error*1.8,'walkCycleSeconds':40/30,'stepLengthMetres':.13*1.8,'swingClearanceMetres':.032*1.8,'samples':metrics}
json.dump(report,open(os.path.join(OUT,'animation_fix_report.json'),'w'),indent=2)
print(json.dumps({k:v for k,v in report.items() if k!='samples'}))
