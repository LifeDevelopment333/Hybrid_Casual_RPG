// Run as a statement script with Pipeline eval_file, outside Assets.
const string source="Assets/Characters/tripo_convert_c68bf175-48e4-46b7-ad62-d50e3fed1a30.fbx";
const string textures="Assets/Characters/tripo_convert_c68bf175-48e4-46b7-ad62-d50e3fed1a30.fbm";
const string output="Assets/Characters/MixamoAdventurer";
const string scenePath="Assets/Scenes/MixamoAdventurerPreview.unity";
if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode before importing.");
if(!AssetDatabase.IsValidFolder(output))AssetDatabase.CreateFolder("Assets/Characters","MixamoAdventurer");
var importer=(ModelImporter)AssetImporter.GetAtPath(source);
var model=AssetDatabase.LoadAssetAtPath<GameObject>(source);
var transforms=model.GetComponentsInChildren<Transform>(true);
string Normalize(string value)=>value.Replace(" ","").Replace("_","").Replace("mixamorig:","");
var description=importer.humanDescription;
description.human=HumanTrait.BoneName.Select(h=>new{human=h,bone=transforms.FirstOrDefault(t=>Normalize(t.name)==Normalize(h))})
    .Where(x=>x.bone!=null).Select(x=>new HumanBone{humanName=x.human,boneName=x.bone.name,limit=new HumanLimit{useDefaultValues=true}}).ToArray();
description.upperArmTwist=.5f;description.lowerArmTwist=.5f;
description.upperLegTwist=.5f;description.lowerLegTwist=.5f;
description.armStretch=.05f;description.legStretch=.05f;
importer.animationType=ModelImporterAnimationType.Human;
importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
importer.humanDescription=description;
importer.importAnimation=true;
importer.SaveAndReimport();
model=AssetDatabase.LoadAssetAtPath<GameObject>(source);
var avatar=AssetDatabase.LoadAllAssetsAtPath(source).OfType<Avatar>().FirstOrDefault();
if(!avatar||!avatar.isValid||!avatar.isHuman)throw new System.InvalidOperationException("Humanoid Avatar validation failed.");

string TexturePath(string suffix)=>textures+"/fantasy_adventurer_3d_model_"+suffix+".PNG";
foreach(var suffix in new[]{"basecolor","normal","metallic","roughness","rm"}){
    var ti=(TextureImporter)AssetImporter.GetAtPath(TexturePath(suffix));
    ti.sRGBTexture=suffix=="basecolor";
    ti.textureType=suffix=="normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
    ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.SaveAndReimport();
}
// URP expects metallic in red and smoothness (1 - roughness) in alpha.
var metal=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
var rough=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
Texture2D packed=null;
try{
    ImageConversion.LoadImage(metal,System.IO.File.ReadAllBytes(TexturePath("metallic")));
    ImageConversion.LoadImage(rough,System.IO.File.ReadAllBytes(TexturePath("roughness")));
    if(metal.width!=rough.width||metal.height!=rough.height)throw new System.InvalidOperationException("Texture dimensions differ.");
    var mp=metal.GetPixels32();var rp=rough.GetPixels32();
    for(int i=0;i<mp.Length;i++)mp[i]=new Color32(mp[i].r,0,0,(byte)(255-rp[i].r));
    packed=new Texture2D(metal.width,metal.height,TextureFormat.RGBA32,false,true);
    packed.SetPixels32(mp);packed.Apply();
    System.IO.File.WriteAllBytes(output+"/MetallicSmoothness.png",ImageConversion.EncodeToPNG(packed));
}finally{
    UnityEngine.Object.DestroyImmediate(metal);UnityEngine.Object.DestroyImmediate(rough);
    if(packed)UnityEngine.Object.DestroyImmediate(packed);
}
AssetDatabase.ImportAsset(output+"/MetallicSmoothness.png",ImportAssetOptions.ForceSynchronousImport);
var packedImporter=(TextureImporter)AssetImporter.GetAtPath(output+"/MetallicSmoothness.png");
packedImporter.sRGBTexture=false;packedImporter.maxTextureSize=2048;packedImporter.alphaSource=TextureImporterAlphaSource.FromInput;
packedImporter.alphaIsTransparency=false;packedImporter.SaveAndReimport();
var shader=Shader.Find("Universal Render Pipeline/Lit");
if(!shader)throw new System.InvalidOperationException("URP Lit shader missing.");
var material=AssetDatabase.LoadAssetAtPath<Material>(output+"/FantasyAdventurer.mat");
if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,output+"/FantasyAdventurer.mat");}
material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath("basecolor")));
material.SetColor("_BaseColor",Color.white);
material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath("normal")));
material.SetFloat("_BumpScale",.7f);material.EnableKeyword("_NORMALMAP");
material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(output+"/MetallicSmoothness.png"));
material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);
material.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(material);
importer=(ModelImporter)AssetImporter.GetAtPath(source);
foreach(var embedded in AssetDatabase.LoadAllAssetsAtPath(source).OfType<Material>())
    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),embedded.name),material);
importer.SaveAndReimport();
model=AssetDatabase.LoadAssetAtPath<GameObject>(source);

// Keep the user's unsaved scene open; author the review in its own additive scene.
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var root=new GameObject("Fantasy Adventurer");Undo.RegisterCreatedObjectUndo(root,"Create adventurer preview");
var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);
instance.name="Model";instance.transform.SetParent(root.transform,false);
instance.transform.localRotation=Quaternion.Euler(0,180,0);
var renderers=instance.GetComponentsInChildren<Renderer>(true);
Bounds BoundsOfModel(){var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
var bounds=BoundsOfModel();
instance.transform.localScale=Vector3.one*(1.75f/bounds.size.y);
bounds=BoundsOfModel();instance.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z);
foreach(var r in renderers){r.sharedMaterials=r.sharedMaterials.Select(_=>material).ToArray();if(r is SkinnedMeshRenderer s)s.updateWhenOffscreen=true;}
var animator=instance.GetComponent<Animator>();animator.applyRootMotion=false;
PrefabUtility.SaveAsPrefabAsset(root,output+"/FantasyAdventurer.prefab");

Material MakeMaterial(string name,Color color){
    var path=output+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}
    m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(m);return m;
}
var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Preview Floor";
floor.transform.localScale=Vector3.one*2;floor.transform.position=new Vector3(0,-.035f,0);
floor.GetComponent<Renderer>().sharedMaterial=MakeMaterial("PreviewFloor",new Color(.12f,.16f,.20f));
var camera=new GameObject("Adventurer Preview Camera").AddComponent<Camera>();camera.tag="MainCamera";
camera.depth=10;camera.transform.position=new Vector3(2.5f,1.65f,-4.5f);camera.transform.LookAt(new Vector3(0,.88f,0));
camera.orthographic=true;camera.orthographicSize=1.2f;camera.clearFlags=CameraClearFlags.SolidColor;
camera.backgroundColor=new Color(.045f,.065f,.09f);camera.nearClipPlane=.05f;camera.farClipPlane=50;
var key=new GameObject("Preview Key Light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.5f;
key.transform.rotation=Quaternion.Euler(40,-30,0);key.shadows=LightShadows.Soft;
var fill=new GameObject("Preview Fill Light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;
fill.color=new Color(.75f,.85f,1);fill.transform.rotation=Quaternion.Euler(25,145,0);
RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.44f,.5f);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath);
AssetDatabase.SaveAssets();Selection.activeGameObject=root;
if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,.88f,0),camera.transform.rotation,2.2f,false,true);
EditorApplication.RepaintHierarchyWindow();SceneView.RepaintAll();
return new {scene=scenePath,prefab=output+"/FantasyAdventurer.prefab",avatarValid=avatar.isValid,avatarHuman=avatar.isHuman,mappedBones=description.human.Length,height=BoundsOfModel().size.y,
    clips=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).Select(c=>new{c.name,c.length}).ToArray()};
