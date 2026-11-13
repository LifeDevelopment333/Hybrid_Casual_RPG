import bpy,numpy as np,json
bpy.ops.wm.open_mainfile(filepath=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\TripoAdventurer\Source_Inspection.blend')
o=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
a=np.array([v.co[:] for v in o.data.vertices])
for z in [.08,.2,.3,.4,.48,.55,.65,.72,.8,.86,.94]:
 s=a[abs(a[:,2]-z)<.008];print(z,[(round(float(s[:,i].min()),4),round(float(s[:,i].max()),4)) for i in range(3)])
for x in [-.22,-.18,-.14,0,.14,.18,.22]:
 s=a[(abs(a[:,0]-x)<.015)&(a[:,2]>.2)&(a[:,2]<.6)];print('x',x,[(round(float(s[:,i].min()),4),round(float(s[:,i].max()),4)) for i in range(3)])
