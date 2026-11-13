import bpy,os,numpy as np,json
OUT=r'C:\Unity\Hybrid_Casual_RPG\Art\Characters\TripoAdventurer'
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'Adventurer_Rigged.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH');sc=bpy.context.scene
def coords(action,frame):
 rig.animation_data.action=bpy.data.actions[action];sc.frame_set(frame);ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());return np.array([v.co[:] for v in ev.data.vertices])
base=coords('Idle',1);edges=np.array([e.vertices[:] for e in mesh.data.edges]);lens=np.linalg.norm(base[edges[:,0]]-base[edges[:,1]],axis=1)
results=[]
for act,f in [('Walk',1),('Walk',11),('RigCheck',21),('RigCheck',61)]:
 p=coords(act,f);deform=np.linalg.norm(p[edges[:,0]]-p[edges[:,1]],axis=1);rat=deform/np.maximum(lens,.0001)
 idx=np.argsort(deform-lens)[-8:][::-1]
 r={'action':act,'frame':f,'maxStretch':float(max(rat)),'worstEdges':[{'ratio':round(float(rat[i]),2),'delta':round(float(deform[i]-lens[i]),4),'positions':base[edges[i]].round(4).tolist(),'groups':[[[mesh.vertex_groups[g.group].name,round(g.weight,3)] for g in mesh.data.vertices[int(j)].groups] for j in edges[i]]} for i in idx]}
 results.append(r)
json.dump(results,open(os.path.join(OUT,'deformation_report.json'),'w'),indent=2);print(json.dumps(results))
