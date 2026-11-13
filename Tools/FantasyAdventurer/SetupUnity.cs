
const string dir="Assets/Characters/FantasyAdventurer";
const string modelPath=dir+"/FantasyAdventurer_Rigged.fbx";
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
if(importer==null)throw new Exception("FBX importer was not found");
importer.animationType=ModelImporterAnimationType.Human;
importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
importer.importAnimation=true;
importer.animationCompression=ModelImporterAnimationCompression.Off;
importer.skinWeights=ModelImporterSkinWeights.Standard;
importer.importBlendShapes=false;
importer.SaveAndReimport();
var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
var hd=importer.humanDescription;
var transforms=model.GetComponentsInChildren<Transform>(true);
// Reconstruct the Avatar reference skeleton from skin bind matrices. This also
// keeps the reference correct if FBX unit conversion changes on re-export.
var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
var bindWorld=new System.Collections.Generic.Dictionary<Transform,Matrix4x4>();
for(int i=0;i<skin.bones.Length;i++)bindWorld[skin.bones[i]]=skin.transform.localToWorldMatrix*skin.sharedMesh.bindposes[i].inverse;
hd.skeleton=transforms.Select(t=>{
    var world=bindWorld.ContainsKey(t)?bindWorld[t]:t.localToWorldMatrix;
    var parent=t.parent?(bindWorld.ContainsKey(t.parent)?bindWorld[t.parent]:t.parent.localToWorldMatrix):Matrix4x4.identity;
    var local=parent.inverse*world;
    return new SkeletonBone{name=t.name,position=local.GetColumn(3),rotation=local.rotation,scale=local.lossyScale};
}).ToArray();
hd.human=HumanTrait.BoneName.Where(n=>transforms.Any(t=>t.name==n.Replace(" ","")))
    .Select(n=>new HumanBone{humanName=n,boneName=n.Replace(" ",""),limit=new HumanLimit{useDefaultValues=true}}).ToArray();
hd.upperArmTwist=.5f;hd.lowerArmTwist=.5f;hd.upperLegTwist=.5f;hd.lowerLegTwist=.5f;
hd.armStretch=.02f;hd.legStretch=.02f;hd.feetSpacing=0;
importer.humanDescription=hd;
var clips=importer.defaultClipAnimations;
foreach(var c in clips){
    c.name=c.name.Split('|').Last();c.loopTime=true;c.loopPose=true;
    c.lockRootPositionXZ=true;c.lockRootHeightY=true;c.lockRootRotation=true;
}
importer.clipAnimations=clips;importer.SaveAndReimport();
var avatar=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
if(avatar==null||!avatar.isValid||!avatar.isHuman)throw new Exception("Humanoid Avatar validation failed");
var animations=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
foreach(var clip in animations){
    string path=dir+"/"+clip.name+".anim";
    var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    if(existing)EditorUtility.CopySerialized(clip,existing);
    else {existing=UnityEngine.Object.Instantiate(clip);existing.name=clip.name;AssetDatabase.CreateAsset(existing,path);}
}
// Explicit URP texture setup, including packed metallic(R) and smoothness(A).
foreach(string file in new[]{"normal.PNG","metallic.JPEG","roughness.JPEG","MetallicSmoothness.png"}){
    var texImporter=(TextureImporter)AssetImporter.GetAtPath(dir+"/Textures/"+file);
    texImporter.sRGBTexture=false;
    if(file=="normal.PNG")texImporter.textureType=TextureImporterType.NormalMap;
    texImporter.SaveAndReimport();
}
var shader=Shader.Find("Universal Render Pipeline/Lit");
if(!shader)throw new Exception("URP Lit shader unavailable");
var mat=AssetDatabase.LoadAssetAtPath<Material>(dir+"/FantasyAdventurer.mat");
if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,dir+"/FantasyAdventurer.mat");}
mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/Textures/basecolor.JPEG"));
mat.SetColor("_BaseColor",Color.white);
mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/Textures/normal.PNG"));mat.EnableKeyword("_NORMALMAP");
mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/Textures/MetallicSmoothness.png"));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",1);mat.SetFloat("_BumpScale",.65f);EditorUtility.SetDirty(mat);
var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(dir+"/FantasyAdventurer.controller");
if(!controller)controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(dir+"/FantasyAdventurer.controller");
controller.parameters=new[]{new AnimatorControllerParameter{name="Speed",type=AnimatorControllerParameterType.Float,defaultFloat=0}};
var sm=controller.layers[0].stateMachine;
foreach(var state in sm.states)sm.RemoveState(state.state);
var idle=sm.AddState("Idle");idle.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(dir+"/Idle.anim");sm.defaultState=idle;
var walk=sm.AddState("Walk");walk.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(dir+"/Walk.anim");
var iw=idle.AddTransition(walk);iw.hasExitTime=false;iw.hasFixedDuration=true;iw.duration=.16f;iw.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater,.1f,"Speed");
var wi=walk.AddTransition(idle);wi.hasExitTime=false;wi.hasFixedDuration=true;wi.duration=.16f;wi.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less,.1f,"Speed");
EditorUtility.SetDirty(controller);
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try{
    var go=(GameObject)PrefabUtility.InstantiatePrefab(model,preview);go.name="FantasyAdventurer";
    var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
    foreach(var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>()){
        renderer.sharedMaterial=mat;renderer.localBounds=new Bounds(new Vector3(0,.9f,0),new Vector3(2.2f,2.2f,1.2f));
    }
    PrefabUtility.SaveAsPrefabAsset(go,dir+"/FantasyAdventurer.prefab");
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
AssetDatabase.SaveAssets();
var report=new {avatarValid=avatar.isValid,avatarHuman=avatar.isHuman,mappedBones=hd.human.Length,clips=animations.Select(c=>new{c.name,c.length,c.isHumanMotion,c.isLooping}).ToArray(),prefab=dir+"/FantasyAdventurer.prefab"};
return report;
