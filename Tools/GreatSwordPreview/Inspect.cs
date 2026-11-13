using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InspectGreatSword
{
 public static object Main()
 {
  const string path="Assets/RPG/GreatSwordPreview/Adventurer_GreatSword.fbx";
  var imp=(ModelImporter)AssetImporter.GetAtPath(path);
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  return new {clips=imp.defaultClipAnimations.Select(c=>new {c.name,c.firstFrame,c.lastFrame}).ToArray(), transforms=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Socket")||t.name.Contains("Weapon")||t.name=="Hips").Select(t=>new{t.name,pos=t.position.ToString(),scale=t.lossyScale.ToString(),rot=t.rotation.eulerAngles.ToString(),local=t.localRotation.eulerAngles.ToString()}).ToArray(), materials=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m=>m.name).ToArray(),bounds=model.GetComponentsInChildren<Renderer>().Select(r=>r.bounds.ToString()).ToArray(),scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name).ToArray()};
 }
}
