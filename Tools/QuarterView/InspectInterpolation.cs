using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InspectInterpolation
{
 public static object Main() {
 var a=GameObject.Find("Player").GetComponentInChildren<Animator>();var c=(AnimatorController)a.runtimeAnimatorController;
 var clips=c.animationClips.Where(x=>x.name.Contains("walk")||x.name.Contains("run")).Distinct().ToArray();
 return new {tree=c.layers[0].stateMachine.states.Where(s=>s.state.motion is BlendTree).Select(s=>new {s.state.name,children=((BlendTree)s.state.motion).children.Select(ch=>new {motion=ch.motion.name,ch.threshold,ch.timeScale}).ToArray()}).ToArray(), clips=clips.Select(clip=>{
 var bindings=AnimationUtility.GetCurveBindings(clip);var dynamic=bindings.Select(b=>new {binding=b,curve=AnimationUtility.GetEditorCurve(clip,b)}).Where(x=>x.curve.keys.Max(k=>k.value)-x.curve.keys.Min(k=>k.value)>.05f).ToArray();
 var b=dynamic.First(x=>x.binding.path.EndsWith("Right_LowerLeg")&&x.binding.propertyName.StartsWith("m_LocalRotation"));var keys=b.curve.keys;
 var set=AnimationUtility.GetAnimationClipSettings(clip);
 return new {clip.name,path=AssetDatabase.GetAssetPath(clip),set.loopTime,set.loopBlend,set.startTime,set.stopTime,dynamicCurves=dynamic.Length,flatTangentFraction=dynamic.Sum(x=>x.curve.keys.Count(k=>k.inTangent==0&&k.outTangent==0))/(float)dynamic.Sum(x=>x.curve.length),example=new {b.binding.path,b.binding.propertyName,keys=keys.Take(8).Select(k=>new {k.time,k.value,k.inTangent,k.outTangent}).ToArray()},phases=Enumerable.Range(0,4).Select(phase=>Enumerable.Range(1,keys.Length-2).Average(i=>{float dt=keys[i+1].time-keys[i].time;float t=keys[i].time+(phase+.5f)*dt/4;float v=Mathf.Abs((b.curve.Evaluate(t+dt/100)-b.curve.Evaluate(t-dt/100))/(dt/50));return (double)v;})).ToArray()};}).ToArray()};
 }
}
