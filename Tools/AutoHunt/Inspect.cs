using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
public static class InspectAutoHunt {
 public static object Main(){
 var path="Assets/polyperfect/Low Poly Animated Fantasy Creatures/_Prefabs/Creatures/Skeleton_Basic.prefab";
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var a=prefab.GetComponentInChildren<Animator>();var p=GameObject.Find("Player");var pa=p.GetComponentInChildren<Animator>();var pc=(AnimatorController)pa.runtimeAnimatorController;
 return new {scene=SceneManager.GetActiveScene().path,playing=Application.isPlaying,roots=SceneManager.GetActiveScene().GetRootGameObjects().Select(g=>new {g.name,components=g.GetComponents<Component>().Select(c=>c?c.GetType().Name:"Missing").ToArray()}).ToArray(),player=new {pos=p.transform.position.ToString(),bounds=p.GetComponentsInChildren<Renderer>().Select(r=>r.bounds.ToString()).ToArray(),states=pc.layers[0].stateMachine.states.Select(s=>new {s.state.name,motion=s.state.motion?s.state.motion.name:"null"}).ToArray(),clips=pc.animationClips.Where(c=>c.name.Contains("slash")||c.name.Contains("attack")).Select(c=>new {c.name,c.length,path=AssetDatabase.GetAssetPath(c)}).ToArray()},skeleton=new {components=prefab.GetComponentsInChildren<Component>(true).Select(c=>c?c.GetType().Name:"Missing").Distinct().ToArray(),scale=prefab.transform.localScale.ToString(),bounds=prefab.GetComponentsInChildren<Renderer>().Select(r=>new {r.name,bounds=r.bounds.ToString(),mats=r.sharedMaterials.Select(m=>new{m.name,shader=m.shader.name}).ToArray()}).ToArray(),controller=AssetDatabase.GetAssetPath(a.runtimeAnimatorController),clips=a.runtimeAnimatorController.animationClips.Select(c=>new {c.name,c.length,c.isLooping,path=AssetDatabase.GetAssetPath(c)}).ToArray()},ground=GameObject.Find("Ground").GetComponent<Renderer>().bounds.ToString()};
 }
}
