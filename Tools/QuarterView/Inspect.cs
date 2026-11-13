using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class InspectQuarterView {
 public static object Main() {
 var scene=SceneManager.GetActiveScene();
 return new { playing=Application.isPlaying, scene=scene.path, roots=scene.GetRootGameObjects().Select(g=>new {g.name,pos=g.transform.position.ToString(),scale=g.transform.localScale.ToString(),components=g.GetComponents<Component>().Select(c=>c?c.GetType().Name:"Missing").ToArray()}).ToArray(), animators=Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Select(a=>new {a.name,controller=AssetDatabase.GetAssetPath(a.runtimeAnimatorController),rootMotion=a.applyRootMotion,clips=a.runtimeAnimatorController?a.runtimeAnimatorController.animationClips.Select(c=>new {c.name,c.length,c.isLooping}).ToArray():null}).ToArray(), renderers=GameObject.Find("Player").GetComponentsInChildren<Renderer>().Select(r=>new {r.name,bounds=r.bounds.ToString()}).ToArray(),cameras=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>new {c.name,pos=c.transform.position.ToString(),rot=c.transform.eulerAngles.ToString(),c.orthographic,c.orthographicSize}).ToArray() };
 }
}
