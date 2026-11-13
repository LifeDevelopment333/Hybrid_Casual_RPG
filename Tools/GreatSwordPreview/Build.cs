using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rpg.GreatSwordPreview;

public static class BuildGreatSwordPreview
{
 const string Root="Assets/RPG/GreatSwordPreview";
 [Serializable] public class Manifest {public MaterialInfo[] materials;public SwordMaterial[] sword_materials;}
 [Serializable] public class MaterialInfo {public string name;public string[] images;}
 [Serializable] public class SwordMaterial {public string name;public float[] color;public float metallic,roughness;}
 static Shader lit;
 static Material Mat(string name,Color color,float metal=0,float smooth=.3f)
 {
  string path=Root+"/Materials/"+name+".mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(lit);AssetDatabase.CreateAsset(m,path);}
  m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
 }
 static void Layer(GameObject go){go.layer=30;foreach(Transform t in go.transform)Layer(t.gameObject);}
 static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material material)
 {
  var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());Layer(go);return go;
 }
 static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
 {
  var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
  var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=size;go.layer=30;return r;
 }
 static TMP_Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,float font=20,Color? color=null)
 {
  var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=font;t.color=color??new Color(.84f,.88f,.87f);t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
 }
 static UnityEngine.UI.Button Button(string name,Transform parent,string label,float x,float y,float w=100)
 {
  var r=Rect(name,parent,new Vector2(x,y),new Vector2(w,38));var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.17f,.23f,.25f);
  var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;
  var text=Text("Label",r,label,new Vector2(4,-2),new Vector2(w-8,34),16);text.alignment=TextAlignmentOptions.Center;return b;
 }
 static UnityEngine.UI.Slider Slider(string name,Transform parent,float y,float min,float max,float value)
 {
  var r=Rect(name,parent,new Vector2(28,y),new Vector2(302,20));
  var bg=r.gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=new Color(.18f,.24f,.26f);
  var handle=Rect("Handle",r,Vector2.zero,new Vector2(12,28));var hi=handle.gameObject.AddComponent<UnityEngine.UI.Image>();hi.color=new Color(.72f,.68f,.43f);
  var s=r.gameObject.AddComponent<UnityEngine.UI.Slider>();s.targetGraphic=hi;s.handleRect=handle;s.direction=UnityEngine.UI.Slider.Direction.LeftToRight;s.minValue=min;s.maxValue=max;s.value=value;return s;
 }
 public static object Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before building.");
  foreach(string dir in new[]{"Materials","Animations","Prefabs","Scenes","Settings"})Directory.CreateDirectory(Root+"/"+dir);
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  lit=Shader.Find("Universal Render Pipeline/Lit");
  var manifest=Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(Root+"/manifest.json"));
  foreach(string path in Directory.GetFiles(Root+"/Textures","*.png"))
  {
   var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=true;ti.anisoLevel=4;
   ti.sRGBTexture=path.Contains("Color");if(path.Contains("normal"))ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();
  }
  string source=Root+"/Adventurer_GreatSword.fbx";
  var importer=(ModelImporter)AssetImporter.GetAtPath(source);
  importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;
  var defaults=importer.defaultClipAnimations;
  foreach(var c in defaults){c.name=c.name.Split('|').Last().Trim();c.loopTime=!c.name.Contains("reference");c.loopPose=false;c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;}
  importer.clipAnimations=defaults;
  foreach(var info in manifest.materials)
  {
   var m=Mat(info.name.Replace("/","_"),Color.white,0,.24f);
   m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+info.images[0]));
   if(info.name.Contains("Body")){m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/RPG_Character_normal.png"));m.SetFloat("_BumpScale",.45f);m.EnableKeyword("_NORMALMAP");}
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),info.name),m);
  }
  importer.SaveAndReimport();
  string swordSource=Root+"/GreatSword_Adventurer.fbx";
  var si=(ModelImporter)AssetImporter.GetAtPath(swordSource);si.importAnimation=false;
  foreach(var info in manifest.sword_materials)
  {
   var col=new Color(info.color[0],info.color[1],info.color[2],1).gamma;
   var m=Mat(info.name.Replace('|','_'),col,info.metallic,1-info.roughness);
   si.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),info.name),m);
  }
  si.SaveAndReimport();
  var clips=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")&&!c.name.Contains("reference pose")).OrderBy(c=>c.name).Select(c=>{
   string path=Root+"/Animations/"+c.name+".anim";var copy=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
   if(!copy){copy=UnityEngine.Object.Instantiate(c);AssetDatabase.CreateAsset(copy,path);}else EditorUtility.CopySerialized(c,copy);
   copy.name=c.name;return copy;
  }).ToArray();
  if(clips.Length!=51)throw new Exception("Expected 51 motions: "+clips.Length);
  string scenePath=Root+"/Scenes/GreatSwordPreview.unity";
  if(SceneManager.GetSceneByPath(scenePath).isLoaded)throw new Exception("Preview scene is already open; edit it in place.");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  var stage=new GameObject("GreatSword Preview Stage");
  var stone=Mat("Stage stone",new Color(.045f,.064f,.07f),.15f,.3f);
  var trim=Mat("Stage trim",new Color(.24f,.22f,.12f),.65f,.45f);
  Shape("Pedestal",PrimitiveType.Cylinder,stage.transform,new Vector3(0,-.10f,0),new Vector3(2.65f,.1f,2.65f),stone);
  Shape("Bronze rim",PrimitiveType.Cylinder,stage.transform,new Vector3(0,-.205f,0),new Vector3(2.7f,.025f,2.7f),trim);
  var actor=new GameObject("GreatSword Adventurer");actor.transform.SetParent(stage.transform,false);
  var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),scene);model.name="Model";model.transform.SetParent(actor.transform,false);model.transform.localScale=Vector3.one*(1.75f/.89123535f);
  var transforms=model.GetComponentsInChildren<Transform>(true);var socket=transforms.Single(t=>t.name=="SwordSocket");
  var offset=new GameObject("SwordRotationOffset");offset.transform.SetParent(socket,false);
  var sword=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(swordSource),scene);sword.name="GreatSword";sword.transform.SetParent(offset.transform,false);sword.transform.localScale*=model.transform.lossyScale.x/socket.lossyScale.x;sword.transform.localRotation=Quaternion.identity;
  var pose=AssetDatabase.LoadAssetAtPath<SwordPoseSettings>(Root+"/Settings/SwordPose.asset");if(!pose){pose=ScriptableObject.CreateInstance<SwordPoseSettings>();AssetDatabase.CreateAsset(pose,Root+"/Settings/SwordPose.asset");}
  offset.transform.localRotation=Quaternion.Euler(pose.localEuler);
  foreach(var sk in model.GetComponentsInChildren<SkinnedMeshRenderer>())sk.updateWhenOffscreen=true;
  Layer(actor);PrefabUtility.SaveAsPrefabAsset(actor,Root+"/Prefabs/GreatSwordAdventurer.prefab");
  var camGo=new GameObject("GreatSword Preview Camera");camGo.transform.SetParent(stage.transform,false);camGo.transform.position=new Vector3(1.5f,1.65f,4.1f);camGo.transform.LookAt(new Vector3(.60f,.92f,0));
  var camera=camGo.AddComponent<Camera>();camera.fieldOfView=36;camera.nearClipPlane=.05f;camera.farClipPlane=40;camera.depth=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.024f,.039f,.047f);camera.cullingMask=1<<30;camera.allowHDR=true;
  var cd=camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();cd.renderPostProcessing=false;cd.volumeLayerMask=1<<30;
  void Light(string name,Vector3 angles,Color color,float intensity){var go=new GameObject(name);go.transform.SetParent(stage.transform,false);go.transform.rotation=Quaternion.Euler(angles);var l=go.AddComponent<Light>();l.type=LightType.Directional;l.color=color;l.intensity=intensity;l.cullingMask=1<<30;}
  Light("Warm key",new Vector3(35,-35,0),new Color(1,.88f,.72f),2.1f);Light("Cool rim",new Vector3(40,145,0),new Color(.52f,.75f,1),1.5f);Light("Front fill",new Vector3(10,180,0),new Color(.84f,.91f,1),.6f);
  var canvasGo=new GameObject("GreatSword Controls",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));canvasGo.transform.SetParent(stage.transform,false);
  var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=100;
  var scaler=canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1400,900);scaler.matchWidthOrHeight=1;
  var panel=Rect("ControlPanel",canvasGo.transform,new Vector2(28,-28),new Vector2(358,844));panel.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.025f,.045f,.052f,.97f);
  Text("Eyebrow",panel,"RPG  /  EQUIPMENT REVIEW",new Vector2(28,-24),new Vector2(302,25),15,new Color(.72f,.68f,.43f));
  Text("Title",panel,"GREAT SWORD",new Vector2(28,-58),new Vector2(310,45),32);
  Text("Subtitle",panel,"51 motions · original character rig",new Vector2(28,-107),new Vector2(310,28),16);
  var player=actor.AddComponent<GreatSwordPreviewPlayer>();player.animator=model.GetComponent<Animator>();player.model=model.transform;player.hips=transforms.Single(t=>t.name=="Hips");player.swordOffset=offset.transform;player.poseSettings=pose;player.clips=clips;
  Text("AnimationLabel",panel,"ANIMATION",new Vector2(28,-162),new Vector2(302,25),14,new Color(.72f,.68f,.43f));
  player.clipText=Text("Clip",panel,"great sword idle",new Vector2(28,-195),new Vector2(302,58),21);
  player.previous=Button("Previous",panel,"<",28,-263,66);player.playPause=Button("PlayPause",panel,"PAUSE",104,-263,150);player.playText=player.playPause.GetComponentInChildren<TMP_Text>();player.next=Button("Next",panel,">",264,-263,66);
  player.idle=Button("Idle",panel,"IDLE",28,-314,94);player.attack=Button("Attack",panel,"ATTACK",132,-314,94);player.run=Button("Run",panel,"RUN",236,-314,94);
  player.timeline=Slider("Timeline",panel,-373,0,1,0);player.timeText=Text("Time",panel,"",new Vector2(28,-401),new Vector2(302,24),16);
  Text("SpeedLabel",panel,"PLAYBACK SPEED",new Vector2(28,-438),new Vector2(302,22),13);player.speed=Slider("Speed",panel,-467,.1f,2,1);
  Text("RotationLabel",panel,"SWORD LOCAL ROTATION",new Vector2(28,-518),new Vector2(302,25),14,new Color(.72f,.68f,.43f));
  player.rotationText=Text("RotationValues",panel,"",new Vector2(28,-549),new Vector2(302,25),17);
  player.rotationX=Slider("RotationX",panel,-591,-180,180,0);player.rotationY=Slider("RotationY",panel,-628,-180,180,0);player.rotationZ=Slider("RotationZ",panel,-665,-180,180,0);
  player.reset=Button("ResetRotation",panel,"RESET",28,-706,94);player.save=Button("SaveRotation",panel,"SAVE ROTATION",132,-706,198);
  player.noticeText=Text("Notice",panel,"Drag the X / Y / Z sliders to adjust the blade.",new Vector2(28,-758),new Vector2(302,54),14,new Color(.63f,.72f,.72f));
  var turnPanel=Rect("TurnControl",canvasGo.transform,new Vector2(450,-810),new Vector2(358,70));Text("TurnLabel",turnPanel,"ROTATE CHARACTER",new Vector2(28,0),new Vector2(302,25),14);player.turn=Slider("Turn",turnPanel,-35,-180,180,0);
  Text("Hint",canvasGo.transform,"MIXAMO MOTION  /  SEPARATE WEAPON",new Vector2(450,-38),new Vector2(700,28),16,new Color(.62f,.71f,.70f));
  var es=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  if(es.Length==0){var e=new GameObject("Preview EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));e.transform.SetParent(stage.transform,false);}
  Layer(stage);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,scenePath);
  Selection.activeGameObject=actor;SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0,.9f,0),new Vector3(2.8f,2.8f,2.8f)),false);
  return new {scene=scenePath,motions=clips.Length,socket=socket.name,swordScale=sword.transform.lossyScale.ToString(),prefab=Root+"/Prefabs/GreatSwordAdventurer.prefab"};
 }
}
