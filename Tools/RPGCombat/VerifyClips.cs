if(EditorApplication.isPlaying)throw new System.Exception("Edit Mode only");
var model=GameObject.Find("RPG Character - Combat").transform.Find("Model").gameObject;
var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
var baked=new Mesh();
var reports=new System.Collections.Generic.List<object>();
AnimationMode.StartAnimationMode();
try{
 foreach(var name in new[]{"CombatIdle","Run","Attack01","Attack02","Attack03","Dodge"}){
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/RPG/CombatSample/Animations/"+name+".anim");
  Vector3[] first=null,last=null;float low=float.PositiveInfinity,high=float.NegativeInfinity;
  for(int i=0;i<=12;i++){
   AnimationMode.BeginSampling();AnimationMode.SampleAnimationClip(model,clip,clip.length*i/12f);AnimationMode.EndSampling();
   skin.BakeMesh(baked,false);var points=baked.vertices;
   if(points.Any(v=>float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z)))throw new System.Exception("Nonfinite vertex in "+name);
   // BakeMesh already includes the skin's effective scale on this imported rig.
   foreach(var p in points){var w=skin.transform.position+skin.transform.rotation*p;low=Mathf.Min(low,w.y);high=Mathf.Max(high,w.y);}
   if(i==0)first=points;if(i==12)last=points;
  }
  float seam=first.Zip(last,(a,b)=>Vector3.Distance(a,b)).Max();
  if((name=="Run"||name=="CombatIdle")&&seam>.001f)throw new System.Exception("Loop seam in "+name+": "+seam);
  reports.Add(new{name,seconds=clip.length,curveCount=AnimationUtility.GetCurveBindings(clip).Length,loop=clip.isLooping,loopSeam=seam,minY=low,maxY=high,samples=13});
 }
}finally{AnimationMode.StopAnimationMode();UnityEngine.Object.DestroyImmediate(baked);}
var report=Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("Art/RPGCharacter/unity_animation_validation.json",report);
return reports;
