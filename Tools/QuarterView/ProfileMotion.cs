using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class ProfileMotion
{
 public static async Task<object> Main(float blendSpeed = 1f) {
  Application.runInBackground=true;EditorApplication.isPaused=false;
  var go=GameObject.Find("Player");var a=go.GetComponentInChildren<Animator>();var motor=go.GetComponent<Rpg.Player.QuarterViewPlayer>();bool enabled=motor.enabled;motor.enabled=false;
  var hip=a.transform.Find("Armature/Hips");var leg=hip.Find("Right_UpperLeg/Right_LowerLeg");
  var dt=new List<float>();var changes=new List<float>();var speed=new List<float>();var seam=new List<float>();
  var previous=leg.localRotation;float previousPhase=0;var prevHip=hip.position;int frames=0;
  Action<UnityEngine.Rendering.ScriptableRenderContext,List<Camera>> sample=(context,cams)=> {dt.Add(Time.unscaledDeltaTime);changes.Add(Quaternion.Angle(previous,leg.localRotation));speed.Add(a.GetFloat("Speed"));float phase=a.GetCurrentAnimatorStateInfo(0).normalizedTime%1;if(phase<previousPhase)seam.Add(Vector3.Distance(prevHip,hip.position));previousPhase=phase;prevHip=hip.position;previous=leg.localRotation;frames++;};
  try {a.SetFloat("Speed",blendSpeed);await Task.Delay(500);RenderPipelineManager.endContextRendering+=sample;await Task.Delay(3000);}
  finally{RenderPipelineManager.endContextRendering-=sample;motor.enabled=enabled;a.SetFloat("Speed",0);}
  var sorted=dt.OrderBy(t=>t).ToArray();return new {frames,averageMs=dt.Average()*1000,p95Ms=sorted[(int)(sorted.Length*.95)]*1000,maxMs=dt.Max()*1000,slowFrames=dt.Count(t=>t>1f/30),repeatedPoseFrames=changes.Count(t=>t<.01f),speedMin=speed.Min(),speedMax=speed.Max(),seamWorldDisplacements=seam,animatorUpdate=a.updateMode.ToString(),clips=a.GetCurrentAnimatorClipInfo(0).Select(c=>new {c.clip.name,c.weight}).ToArray()};
 }
}
