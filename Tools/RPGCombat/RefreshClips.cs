AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
const string folder="Assets/RPG/CombatSample";
var source=folder+"/RPGCharacter_Combat.fbx";
var clips=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__")).ToArray();
foreach(var clip in clips){var target=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/Animations/"+clip.name+".anim");if(!target)throw new System.Exception(clip.name);EditorUtility.CopySerialized(clip,target);EditorUtility.SetDirty(target);}
AssetDatabase.SaveAssets();
return clips.Select(x=>new{x.name,x.length}).ToArray();
