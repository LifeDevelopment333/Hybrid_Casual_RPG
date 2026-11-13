import bpy,numpy as np
bpy.ops.wm.open_mainfile(filepath=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\TripoAdventurer\Source_Inspection.blend')
o=next(o for o in bpy.data.objects if o.type=='MESH');bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
a=np.array([v.co[:] for v in o.data.vertices])
for z in [.36,.4,.45,.5,.55,.6,.64]:
 for y in [-.1,-.06,0,.06,.1]:
  s=a[(a[:,0]>.14)&(abs(a[:,2]-z)<.008)&(abs(a[:,1]-y)<.01)]
  if len(s):print(z,y,'x',round(float(min(s[:,0])),4),round(float(max(s[:,0])),4),'count',len(s))
