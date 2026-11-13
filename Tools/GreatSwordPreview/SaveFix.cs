using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Rpg.GreatSwordPreview;
public static class SaveGreatSwordFix
{
 public static object Main()
 {
  var p=Object.FindFirstObjectByType<GreatSwordPreviewPlayer>();
  var sword=p.swordOffset.GetChild(0);
  var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG/GreatSwordPreview/GreatSword_Adventurer.fbx");
  sword.localScale=original.transform.localScale*(p.model.lossyScale.x/p.swordOffset.parent.lossyScale.x);sword.localRotation=Quaternion.identity;
  GameObject.Find("GreatSword Preview Camera").transform.LookAt(new Vector3(.6f,.92f,0));
  // Update only our equipment prefab, without preview UI references.
  var prefabPath="Assets/RPG/GreatSwordPreview/Prefabs/GreatSwordAdventurer.prefab";
  var contents=PrefabUtility.LoadPrefabContents(prefabPath);
  try{var dest=contents.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="GreatSword");dest.localScale=sword.localScale;dest.localRotation=Quaternion.identity;PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(contents);}
  foreach(var root in p.gameObject.scene.GetRootGameObjects())foreach(var scaler in root.GetComponentsInChildren<UnityEngine.UI.CanvasScaler>())scaler.matchWidthOrHeight=1;
  EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
  EditorApplication.ExecuteMenuItem("Window/General/Game");
  return new {saved=true,swordLocalScale=sword.localScale.ToString()};
 }
}
