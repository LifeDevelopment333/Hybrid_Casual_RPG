using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class DiagnoseMotion
{
 public static object Main()
 {
  var player=GameObject.Find("Player");var animator=player.GetComponentInChildren<Animator>();
  var clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/RPG/ART/Player"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".anim")&&(p.Contains("walk")||p.Contains("run")||p.Contains("Locomotion/great sword idle"))).ToArray();
  var clone=UnityEngine.Object.Instantiate(animator.gameObject);clone.hideFlags=HideFlags.HideAndDontSave;UnityEngine.Object.DestroyImmediate(clone.GetComponent<Animator>());
  var bones=clone.GetComponentsInChildren<Transform>();
  var reports=clips.Select(path=>{
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);var bindings=AnimationUtility.GetCurveBindings(clip);
   clip.SampleAnimation(clone,0);var start=bones.Select(b=>b.localRotation).ToArray();var positions=bones.Select(b=>b.localPosition).ToArray();
   clip.SampleAnimation(clone,clip.length);var seams=bones.Select((b,i)=>new {b.name,angle=Quaternion.Angle(start[i],b.localRotation),distance=Vector3.Distance(positions[i],b.localPosition)}).OrderByDescending(b=>b.angle).Take(5).ToArray();
   float maxStep=0;string worst="";float worstTime=0;var previous=start;int samples=Mathf.CeilToInt(clip.length*60);
   for(int s=1;s<=samples;s++){float t=Mathf.Min(s/60f,clip.length);clip.SampleAnimation(clone,t);for(int i=0;i<bones.Length;i++){float angle=Quaternion.Angle(previous[i],bones[i].localRotation);if(angle>maxStep){maxStep=angle;worst=bones[i].name;worstTime=t;}previous[i]=bones[i].localRotation;}}
   return new {path,clip.length,clip.frameRate,clip.isLooping,curves=bindings.Length,steppedKeys=bindings.Sum(b=>AnimationUtility.GetEditorCurve(clip,b).keys.Count(k=>float.IsInfinity(k.inTangent)||float.IsInfinity(k.outTangent))),maxStep,worst,worstTime,seams,rootCurves=bindings.Where(b=>b.propertyName.StartsWith("m_LocalPosition")&&(b.path==""||b.path=="Armature"||b.path=="Armature/Hips")).Select(b=>new {b.path,b.propertyName,first=AnimationUtility.GetEditorCurve(clip,b).Evaluate(0),last=AnimationUtility.GetEditorCurve(clip,b).Evaluate(clip.length)}).ToArray()};
  }).ToArray();
  UnityEngine.Object.DestroyImmediate(clone);
  return new {animator= new {animator.updateMode,animator.cullingMode,animator.speed,animator.applyRootMotion,controller=AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)},QualitySettings.vSyncCount,Application.targetFrameRate,Time.fixedDeltaTime,reports};
 }
}
