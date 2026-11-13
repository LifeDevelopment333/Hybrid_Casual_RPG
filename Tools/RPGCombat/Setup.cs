// Pipeline eval_file statement script. Run with Edit Mode active.
if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
const string output = "Assets/RPG/CombatSample";
const string source = output + "/RPGCharacter_Combat.fbx";
foreach (var folder in new[]{"Materials","Animations","Prefabs","Scenes"})
    if (!AssetDatabase.IsValidFolder(output+"/"+folder)) AssetDatabase.CreateFolder(output,folder);
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var importer = (ModelImporter)AssetImporter.GetAtPath(source);
importer.animationType = ModelImporterAnimationType.Generic;
importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
importer.importAnimation = true; importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
importer.meshCompression = ModelImporterMeshCompression.Off;
var importClips = importer.defaultClipAnimations;
foreach (var c in importClips)
{
    c.name = c.name.Split('|').Last();
    c.loopTime = c.name == "CombatIdle" || c.name == "Run";
    c.loopPose = c.loopTime;
    c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
    c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
}
importer.clipAnimations = importClips; importer.SaveAndReimport();
var clips = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToDictionary(c=>c.name);
if (clips.Count != 6) throw new System.Exception("Expected 6 clips, found "+clips.Count);
foreach (var entry in clips.ToArray())
{
    string path = output+"/Animations/"+entry.Key+".anim";
    var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    if (!copy) {copy = UnityEngine.Object.Instantiate(entry.Value); copy.name=entry.Key; AssetDatabase.CreateAsset(copy,path);}
    else EditorUtility.CopySerialized(entry.Value,copy);
    clips[entry.Key] = copy;
}
var lit = Shader.Find("Universal Render Pipeline/Lit");
if (!lit) throw new System.Exception("URP Lit shader missing.");
Material Mat(string name, Color color, float metallic=0, float smooth=.3f)
{
    var path=output+"/Materials/"+name+".mat";
    var m=AssetDatabase.LoadAssetAtPath<Material>(path);
    if (!m) {m=new Material(lit);AssetDatabase.CreateAsset(m,path);}
    m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
}
var bodyMat=Mat("Character",Color.white,0,.26f);
foreach(var suffix in new[]{"basecolor.JPEG","normal.JPEG","metallic.JPEG","roughness.JPEG"})
{
    var ti=(TextureImporter)AssetImporter.GetAtPath(output+"/Textures/"+suffix);
    ti.sRGBTexture=suffix.StartsWith("basecolor");ti.maxTextureSize=2048;
    if(suffix.StartsWith("normal"))ti.textureType=TextureImporterType.NormalMap;
    ti.SaveAndReimport();
}
bodyMat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(output+"/Textures/basecolor.JPEG"));
bodyMat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(output+"/Textures/normal.JPEG"));
bodyMat.SetFloat("_BumpScale",.65f);bodyMat.EnableKeyword("_NORMALMAP");
EditorUtility.SetDirty(bodyMat);
importer=(ModelImporter)AssetImporter.GetAtPath(source);
foreach(var material in AssetDatabase.LoadAllAssetsAtPath(source).OfType<Material>())
    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),bodyMat);
importer.SaveAndReimport();
var stone=Mat("ArenaStone",new Color(.045f,.06f,.08f),.15f,.3f);
var edge=Mat("ArenaEdge",new Color(.009f,.014f,.022f),.3f,.5f);
var metal=Mat("Blade",new Color(.73f,.83f,.88f),.85f,.8f);
var gold=Mat("Hilt",new Color(.36f,.23f,.08f),.7f,.5f);
var leather=Mat("Grip",new Color(.06f,.08f,.1f),0,.35f);
var wood=Mat("TargetWood",new Color(.30f,.18f,.10f),0,.2f);
var glow=Mat("CyanInlay",new Color(.04f,.35f,.45f),.35f,.7f);
glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.06f,.55f,.8f)*1.5f);
glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(glow);
var slashShader=Shader.Find("RPGCombatSample/Slash");if(!slashShader)throw new System.Exception("Slash shader missing.");
var slashPath=output+"/Materials/Slash.mat";
var slash=AssetDatabase.LoadAssetAtPath<Material>(slashPath);
if(!slash){slash=new Material(slashShader);AssetDatabase.CreateAsset(slash,slashPath);}
slash.SetColor("_Tint",new Color(.1f,1.3f,2.2f));slash.SetFloat("_Opacity",1);
var controllerPath=output+"/Animations/RPGCombat.controller";
var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if(!controller)controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
var machine=controller.layers[0].stateMachine;
foreach(var child in machine.states)machine.RemoveState(child.state);
int stateIndex=0;
foreach(var name in new[]{"CombatIdle","Run","Attack01","Attack02","Attack03","Dodge"})
{
    var state=machine.AddState(name,new Vector3(260+(stateIndex%2)*250,70+(stateIndex/2)*95));
    state.motion=clips[name];state.writeDefaultValues=true;
    if(name=="CombatIdle")machine.defaultState=state;
    stateIndex++;
}
EditorUtility.SetDirty(controller);
// Work in an additive review scene, preserving the user's open scene.
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var stage=new GameObject("RPG Combat Review");
GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material material)
{
    var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;
    go.GetComponent<Renderer>().sharedMaterial=material;var collider=go.GetComponent<Collider>();if(collider)UnityEngine.Object.DestroyImmediate(collider);return go;
}
Shape("Stage rim",PrimitiveType.Cylinder,stage.transform,new Vector3(0,-.18f,0),new Vector3(9,.15f,9),edge);
Shape("Training platform",PrimitiveType.Cylinder,stage.transform,new Vector3(0,-.06f,0),new Vector3(8.6f,.06f,8.6f),stone);
Shape("Backdrop",PrimitiveType.Plane,stage.transform,new Vector3(0,-.35f,0),Vector3.one*20,edge);
void Ring(string name,float radius,float y,Material material,float width)
{
    var go=new GameObject(name);go.transform.SetParent(stage.transform,false);
    var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.loop=true;line.useWorldSpace=false;line.widthMultiplier=width;line.positionCount=160;
    for(int i=0;i<160;i++){float a=i*Mathf.PI*2/160;line.SetPosition(i,new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius));}
}
Ring("Outer inlay",4.05f,.008f,glow,.018f);Ring("Inner inlay",3.75f,.008f,gold,.013f);
for(int i=0;i<16;i++)
{
    float a=i*Mathf.PI*2/16;
    var tile=Shape("Radial marker",PrimitiveType.Cube,stage.transform,new Vector3(Mathf.Sin(a)*3.89f,.012f,Mathf.Cos(a)*3.89f),new Vector3(.045f,.012f,.16f),gold);
    tile.transform.localRotation=Quaternion.Euler(0,i*22.5f,0);
}
var hero=new GameObject("RPG Character - Combat");hero.transform.SetParent(stage.transform,false);
var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),scene);
model.name="Model";model.transform.SetParent(hero.transform,false);model.transform.localScale=Vector3.one*(1.75f/.89123535f);
var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();
animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>()){renderer.sharedMaterial=bodyMat;renderer.updateWhenOffscreen=true;renderer.quality=SkinQuality.Bone4;}
var hand=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Right_Hand");
var sword=new GameObject("Review Sword");sword.transform.SetParent(hand,false);
// FBX armature uses centimetre bone transforms, while the visible mesh is in metres.
float handUnits=model.transform.lossyScale.x/hand.lossyScale.x;
sword.transform.localScale=Vector3.one*handUnits;sword.transform.localPosition=Vector3.up*(.033f*handUnits);
var bladeDirection=model.transform.TransformDirection(new Vector3(-.04f,.87f,.5f).normalized);
sword.transform.localRotation=Quaternion.LookRotation(hand.InverseTransformDirection(bladeDirection),hand.InverseTransformDirection(Vector3.right));
Shape("Grip",PrimitiveType.Cylinder,sword.transform,new Vector3(0,0,-.025f),new Vector3(.021f,.046f,.021f),leather).transform.localRotation=Quaternion.Euler(90,0,0);
Shape("Guard",PrimitiveType.Cube,sword.transform,new Vector3(0,0,.025f),new Vector3(.13f,.018f,.018f),gold);
Shape("Pommel",PrimitiveType.Sphere,sword.transform,new Vector3(0,0,-.075f),Vector3.one*.032f,gold);
// Tapered diamond section blade, with a sharp triangular tip.
var swordMesh=new Mesh{name="Review Sword Blade"};
swordMesh.vertices=new[]{new Vector3(-.026f,0,.035f),new Vector3(0,.008f,.035f),new Vector3(.026f,0,.035f),new Vector3(0,-.008f,.035f),new Vector3(-.019f,0,.39f),new Vector3(0,.006f,.39f),new Vector3(.019f,0,.39f),new Vector3(0,-.006f,.39f),new Vector3(0,0,.47f)};
swordMesh.triangles=new[]{0,4,1,1,4,5,1,5,2,2,5,6,2,6,3,3,6,7,3,7,0,0,7,4,4,8,5,5,8,6,6,8,7,7,8,4};
swordMesh.RecalculateNormals();swordMesh.RecalculateBounds();
var meshPath=output+"/Prefabs/ReviewSword.asset";var savedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
if(savedMesh){EditorUtility.CopySerialized(swordMesh,savedMesh);UnityEngine.Object.DestroyImmediate(swordMesh);swordMesh=savedMesh;}else AssetDatabase.CreateAsset(swordMesh,meshPath);
var blade=new GameObject("Blade");blade.transform.SetParent(sword.transform,false);blade.AddComponent<MeshFilter>().sharedMesh=swordMesh;blade.AddComponent<MeshRenderer>().sharedMaterial=metal;
var tip=new GameObject("SwordTip");tip.transform.SetParent(sword.transform,false);tip.transform.localPosition=new Vector3(0,0,.45f);
var trail=tip.AddComponent<TrailRenderer>();trail.sharedMaterial=slash;trail.time=.10f;trail.minVertexDistance=.015f;trail.widthMultiplier=.12f;trail.emitting=false;
trail.widthCurve=AnimationCurve.Linear(0,1,1,0);trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
var target=new GameObject("Training Target");target.transform.SetParent(stage.transform,false);target.transform.localPosition=new Vector3(0,0,2.05f);
Shape("Base",PrimitiveType.Cylinder,target.transform,new Vector3(0,.045f,0),new Vector3(.65f,.045f,.65f),gold);
Shape("Post",PrimitiveType.Cylinder,target.transform,new Vector3(0,.78f,0),new Vector3(.14f,.73f,.14f),wood);
var torso=Shape("Target core",PrimitiveType.Cube,target.transform,new Vector3(0,1.12f,0),new Vector3(.58f,.7f,.25f),wood);
Shape("Crossbar",PrimitiveType.Cube,target.transform,new Vector3(0,1.28f,0),new Vector3(1.05f,.12f,.14f),wood);
Shape("Head",PrimitiveType.Sphere,target.transform,new Vector3(0,1.68f,0),Vector3.one*.31f,wood);
var core=Shape("Hit point",PrimitiveType.Cylinder,target.transform,new Vector3(0,1.15f,-.14f),new Vector3(.27f,.016f,.27f),glow);core.transform.localRotation=Quaternion.Euler(90,0,0);
var cameraObject=new GameObject("Combat Review Camera");cameraObject.transform.SetParent(stage.transform,false);
cameraObject.transform.position=new Vector3(3.7f,2.65f,5.6f);cameraObject.transform.LookAt(new Vector3(0,.78f,.6f));
var camera=cameraObject.AddComponent<Camera>();camera.fieldOfView=36;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.depth=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.016f,.025f,.042f);camera.allowHDR=true;
camera.cullingMask=1<<30;
var cameraData=cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();cameraData.renderPostProcessing=true;cameraData.volumeLayerMask=1<<30;
cameraData.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
void LightAt(string name,Vector3 rotation,Color color,float intensity)
{
    var go=new GameObject(name);go.transform.SetParent(stage.transform,false);go.transform.rotation=Quaternion.Euler(rotation);
    var light=go.AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;light.cullingMask=1<<30;
    light.shadows=LightShadows.Soft;
}
LightAt("Warm key",new Vector3(42,148,0),new Color(1,.88f,.72f),2.7f);
LightAt("Cool rim",new Vector3(24,-35,0),new Color(.25f,.65f,1),1.5f);
var volumeObject=new GameObject("Combat Bloom");volumeObject.transform.SetParent(stage.transform,false);
var volume=volumeObject.AddComponent<UnityEngine.Rendering.Volume>();volume.isGlobal=true;volume.priority=5;
var profilePath=output+"/Materials/CombatVolume.asset";
var profile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
if(!profile){profile=ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
UnityEngine.Rendering.Universal.Bloom bloom;
if(!profile.TryGet(out bloom)){bloom=profile.Add<UnityEngine.Rendering.Universal.Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
bloom.intensity.Override(.5f);bloom.threshold.Override(1.1f);bloom.scatter.Override(.55f);
volume.sharedProfile=profile;
// A compact screen-space camera Canvas is included in camera renders and screenshots.
var hud=new GameObject("Review HUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));hud.transform.SetParent(stage.transform,false);
var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
var scaler=hud.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
UnityEngine.UI.Text Label(string name,string text,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,int fontSize,Color color)
{
    var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Text));go.transform.SetParent(hud.transform,false);
    var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=pos;rect.sizeDelta=size;
    var label=go.GetComponent<UnityEngine.UI.Text>();label.text=text;label.font=font;label.fontSize=fontSize;label.color=color;label.raycastTarget=false;return label;
}
Label("Eyebrow","RPG  /  MOTION STUDY  01",new Vector2(0,1),new Vector2(0,1),new Vector2(48,-32),new Vector2(700,30),16,new Color(.34f,.8f,.88f));
Label("Title","BLADE & MOMENTUM",new Vector2(0,1),new Vector2(0,1),new Vector2(46,-65),new Vector2(900,58),36,new Color(.89f,.94f,.96f));
Label("Subtitle","3-HIT COMBO   /   CYAN SLASH   /   COMBAT LOCOMOTION",new Vector2(0,1),new Vector2(0,1),new Vector2(48,-126),new Vector2(900,30),15,new Color(.52f,.64f,.71f));
var status=Label("State","AUTO DEMO",new Vector2(0,0),new Vector2(0,0),new Vector2(48,82),new Vector2(950,35),21,new Color(.35f,.9f,1));
Label("Controls","WASD  Move     J / LMB  Attack (buffer combo)     SPACE  Dodge     R  Auto demo",new Vector2(0,0),new Vector2(0,0),new Vector2(48,38),new Vector2(1460,30),17,new Color(.67f,.75f,.8f));
foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
var prefab=PrefabUtility.SaveAsPrefabAsset(hero,output+"/Prefabs/RPGCharacter_Combat.prefab");
var demo=hero.AddComponent<RpgCombatSample.CombatSample>();demo.animator=animator;demo.swordTip=tip.transform;demo.swordTrail=trail;demo.slashMaterial=slash;demo.hitTarget=target.transform;demo.targetRenderer=core.GetComponent<Renderer>();demo.reviewCamera=camera;demo.statusText=status;
PrefabUtility.RecordPrefabInstancePropertyModifications(model);
AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,output+"/Scenes/RPGCombatPreview.unity");
Selection.activeGameObject=hero;
if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,.9f,.4f),Quaternion.Euler(20,210,0),4.5f);
return new {scene=scene.path,clips=clips.Select(x=>new{x.Key,x.Value.length}).ToArray(),heroScale=model.transform.localScale.x,urpHDR=UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset.supportsHDR};
