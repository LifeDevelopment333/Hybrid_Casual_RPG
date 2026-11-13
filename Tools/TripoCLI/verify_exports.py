import bpy
import json
from pathlib import Path
import sys

root = Path(sys.argv[sys.argv.index('--')+1])
results = []
for filename in ['NoviceAdventurer_Rigged.fbx', 'NoviceAdventurer_Walk.fbx']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(root/filename))
    rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    images = [i for i in bpy.data.images if i.type == 'IMAGE']
    result = {'file': filename, 'bones': sum(len(o.data.bones) for o in rigs),
              'meshes': len(meshes), 'triangles': sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
              'actions': [a.name for a in bpy.data.actions],
              'textures': [{'name': i.name, 'size': list(i.size)} for i in images]}
    assert result['bones'] == 67, result
    assert result['meshes'] == 1, result
    assert result['triangles'] == 22324, result
    assert images and all(i.size[0] > 0 for i in images), result
    if 'Walk' in filename:
        assert bpy.data.actions, result
    for obj in meshes:
        assert any(m.type == 'ARMATURE' for m in obj.modifiers), result
    results.append(result)
(root/'export_verification.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print('EXPORT_VERIFICATION', json.dumps(results))
