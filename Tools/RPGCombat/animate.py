"""Author reviewable action clips on the supplied rig, keeping source weights intact."""
import bpy, math, os, json, shutil
from mathutils import Vector, Matrix
ROOT=r'C:\Unity\Hybrid_Casual_RPG'
ART=os.path.join(ROOT,'Art','RPGCharacter')
ASSET=os.path.join(ROOT,'Assets','RPG','CombatSample')
os.makedirs(os.path.join(ASSET,'Textures'),exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ART,'Source.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
sc=bpy.context.scene;sc.render.fps=60
ref={b.name:b.matrix_local.copy() for b in rig.data.bones}
# Repair a single unweighted source vertex rather than allowing it to stay at bind position.
for v in mesh.data.vertices:
 if not len(v.groups):
  nearest=min((b for b in rig.data.bones if mesh.vertex_groups.get(b.name)),key=lambda b:(b.head_local-v.co).length)
  mesh.vertex_groups[nearest.name].add([v.index],1.0,'REPLACE')
def put(n,m):
 rig.pose.bones[n].matrix=m;bpy.context.view_layer.update()
def inherit(n,axis=None,deg=0):
 pb=rig.pose.bones[n];p=pb.parent
 m=p.matrix@ref[p.name].inverted()@ref[n] if p else ref[n].copy()
 if axis:
  pos=m.translation.copy();m=Matrix.Rotation(math.radians(deg),4,axis)@m;m.translation=pos
 put(n,m)
def aim(n,a,b):
 direction=ref[n].to_3x3()@Vector((0,1,0));q=direction.rotation_difference((b-a).normalized())
 m=q.to_matrix().to_4x4()@ref[n];m.translation=a;put(n,m)
def chain(n1,n2,head,target,pole):
 l1=rig.data.bones[n1].length;l2=rig.data.bones[n2].length
 line=target-head;d=max(abs(l1-l2)+.001,min(line.length,(l1+l2)*.99));axis=line.normalized()
 end=head+axis*d;a=(l1*l1-l2*l2+d*d)/(2*d);h=math.sqrt(max(0,l1*l1-a*a))
 bend=(pole-axis*pole.dot(axis)).normalized();joint=head+axis*a+bend*h
 aim(n1,head,joint);aim(n2,joint,end);return end
def hand(wrist,blade):
 # Map the sword's bind direction (-Y) to the authored blade direction.
 z=Vector(blade).normalized();y=Vector((-1,0,0));y=(y-z*y.dot(z)).normalized();x=y.cross(z).normalized()
 target=Matrix((x,y,z)).transposed()
 rz=Vector((0,-1,0));ry=(ref['Right_Hand'].to_3x3()@Vector((0,1,0))).normalized()
 ry=(ry-rz*ry.dot(rz)).normalized();rx=ry.cross(rz).normalized()
 rotation=target@Matrix((rx,ry,rz)).transposed().inverted()
 m=(rotation@ref['Right_Hand'].to_3x3()).to_4x4();m.translation=wrist;put('Right_Hand',m)
IDLE={'w':(-.155,-.115,.53),'b':(.04,-.5,.87),'twist':-8,'dip':-.016,'left':(.14,-.10,.54)}
def state(w,b,twist=0,dip=-.025,left=(.14,-.11,.54)):
 return dict(w=w,b=b,twist=twist,dip=dip,left=left)
KEYS={
 'Attack01':[(0,IDLE),(.23,state((-.28,.015,.68),(-.8,.3,.5),-38)),(.39,state((-.08,-.215,.58),(.6,-.75,-.2),15)),(.57,state((.13,-.12,.49),(.95,-.1,-.3),37)),(1,IDLE)],
 'Attack02':[(0,IDLE),(.21,state((.115,-.10,.55),(.9,-.1,.5),35)),(.39,state((-.12,-.21,.63),(-.65,-.7,.3),-12)),(.60,state((-.29,-.025,.64),(-.95,.1,.1),-40)),(1,IDLE)],
 'Attack03':[(0,IDLE),(.32,state((-.06,-.04,.855),(.02,.2,.98),-18,-.01,(.09,-.09,.69))),(.44,state((-.07,-.13,.78),(0,-.85,.52),5,-.035,(.08,-.14,.63))),(.58,state((-.06,-.20,.43),(.05,-.6,-.8),24,-.055)),(.73,state((-.07,-.18,.42),(.05,-.5,-.85),26,-.055)),(1,IDLE)]}
def sample(keys,t):
 for (a,pa),(b,pb) in zip(keys,keys[1:]):
  if t<=b:
   u=max(0,min(1,(t-a)/(b-a)));u=u*u*(3-2*u)
   return {k:tuple((1-u)*x+u*y for x,y in zip(pa[k],pb[k])) if isinstance(pa[k],tuple) else (1-u)*pa[k]+u*pb[k] for k in pa}
 return keys[-1][1]
def pose(kind,t):
 for pb in rig.pose.bones:pb.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update()
 p=sample(KEYS[kind],t) if kind in KEYS else dict(IDLE)
 run=kind=='Run';dodge=kind=='Dodge';a=t*2*math.pi
 if run:p['dip']=-.035+.01*math.cos(a*2);p['twist']=-8+5*math.sin(a)
 if dodge:p['dip']=-.016-.11*math.sin(math.pi*t)**2;p['w']=(-.14,-.13,.48)
 hips=ref['Hips'].copy();hips.translation.z+=p['dip']+.002*math.sin(a) if kind=='CombatIdle' else p['dip'];put('Hips',hips)
 inherit('Spine','Z',p['twist']*.3);inherit('Chest','Z',p['twist']*.4);inherit('UpperChest','Z',p['twist']*.3)
 inherit('Neck');inherit('Neck_Twist_A');inherit('Head','Z',-p['twist']*.55)
 pelvis=rig.pose.bones['Hips'].matrix@ref['Hips'].inverted()
 for side,sign,offset in [('Left',1,0),('Right',-1,.5)]:
  phase=(t+offset)%1
  ankle=ref[side+'_Foot'].translation.copy();ankle.x+=sign*.023
  ankle.y+=-.045 if side=='Left' else .045
  if run:
   ankle.y+=.10*math.cos(phase*2*math.pi);ankle.z+=.065*max(0,math.sin(phase*2*math.pi))
  hip=pelvis@ref[side+'_UpperLeg'].translation
  end=chain(side+'_UpperLeg',side+'_LowerLeg',hip,ankle,Vector((0,-1,0)))
  fm=ref[side+'_Foot'].copy();fm.translation=end;put(side+'_Foot',fm);inherit(side+'_Toes');inherit(side+'_ToesEnd')
  inherit(side+'_Shoulder')
  sh=rig.pose.bones[side+'_Shoulder'].matrix@ref[side+'_Shoulder'].inverted()@ref[side+'_UpperArm']
  target=Vector(p['w'] if side=='Right' else p['left'])
  if run:target.y+=sign*.035*math.cos(a)
  wrist=chain(side+'_UpperArm',side+'_LowerArm',sh.translation,target,Vector((sign*.5,.25,-.8)))
  if side=='Right':hand(wrist,p['b'])
  else:aim(side+'_Hand',wrist,wrist+Vector((.015,-.04,-.055)))
  # Curl source finger bones around the hilt, using their local X bending axis.
  for pb in rig.pose.bones:
   if pb.name.startswith(side+'_') and any(f in pb.name for f in ['Thumb','Index','Middle','Ring','Pinky']):
    inherit(pb.name)
    if 'End' not in pb.name:
     # Curl around the anatomical palm axis rather than the imported FBX bone roll.
     base=ref[pb.name].to_3x3()@Vector((0,1,0))
     axis=base.cross(Vector((0,0,-1))).normalized()
     transport=rig.pose.bones[side+'_Hand'].matrix.to_3x3()@ref[side+'_Hand'].to_3x3().inverted()
     axis=transport@axis
     angle=(62 if 'Proximal' in pb.name else 65 if 'Intermediate' in pb.name else 40)*(1 if side=='Right' else .55)
     m=pb.matrix.copy();pos=m.translation.copy();m=Matrix.Rotation(math.radians(angle),4,axis)@m;m.translation=pos;put(pb.name,m)
 bpy.context.view_layer.update()
rig.animation_data_create()
durations={'CombatIdle':2.0,'Run':.66,'Attack01':.68,'Attack02':.70,'Attack03':.98,'Dodge':.56}
for name,duration in durations.items():
 action=bpy.data.actions.new(name);rig.animation_data.action=action;prev={};frames=round(duration*60)
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
# Preserve textures in the source scene; export body only so Unity can attach any weapon.
for im in bpy.data.images:
 if im.source=='FILE' and im.has_data:im.pack()
rig.animation_data.action=None
for pb in rig.pose.bones:pb.matrix_basis=Matrix.Identity(4)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(ASSET,'RPGCharacter_Combat.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
rig.animation_data.action=bpy.data.actions['CombatIdle'];sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ART,'RPGCharacter_Combat.blend'))
src=os.path.join(ART,'Source','tripo_convert_4149599b-c9cf-4048-9fc3-6c31c0b939f5.fbm')
for suffix in ['basecolor.JPEG','normal.JPEG','metallic.JPEG','roughness.JPEG']:
 shutil.copy2(os.path.join(src,'RPG_Character_'+suffix),os.path.join(ASSET,'Textures',suffix))
report={'clips':durations,'bones':len(rig.data.bones),'vertices':len(mesh.data.vertices),'unweighted':sum(not len(v.groups) for v in mesh.data.vertices),'note':'Authored prototype poses on the supplied Tripo rig; source skin weights preserved.'}
json.dump(report,open(os.path.join(ART,'combat_report.json'),'w'),indent=2);print('REPORT',json.dumps(report))
