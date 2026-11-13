import bpy, os, json, numpy as np
OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\FantasyAdventurer'
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'Source_Inspection.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.context.view_layer.objects.active=o;o.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
a=np.array([v.co[:] for v in o.data.vertices]);lo=a.min(0);hi=a.max(0)
print('BOUNDS',lo,hi,flush=True)
# Put a 1.8m tall, front=-Y character at ground level.
scale=1.8/(hi[2]-lo[2]);center=(lo[0]+hi[0])/2
for v in o.data.vertices:v.co=((v.co.x-center)*scale,v.co.y*scale,(v.co.z-lo[2])*scale)
mod=o.modifiers.new('Game mesh reduction','DECIMATE');mod.ratio=60000/len(o.data.polygons)
bpy.ops.object.modifier_apply(modifier=mod.name)
o.name='FantasyAdventurer_Body'
a=np.array([v.co[:] for v in o.data.vertices])
for z in [.08,.2,.4,.55,.7,.85,.95,1.05,1.2,1.35,1.4,1.5,1.6]:
    s=a[abs(a[:,2]-z)<.01]
    if len(s):print('SLICE',z,s.min(0).tolist(),s.max(0).tolist(),flush=True)
print('MESH',len(o.data.vertices),len(o.data.polygons),flush=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Prepared.blend'))
