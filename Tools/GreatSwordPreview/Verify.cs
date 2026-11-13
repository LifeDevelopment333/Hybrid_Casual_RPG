using System;
using System.Linq;
using UnityEngine;
using Rpg.GreatSwordPreview;
public static class VerifyGreatSwordPreview
{
 public static object Main()
 {
  var p=UnityEngine.Object.FindFirstObjectByType<GreatSwordPreviewPlayer>();p.playing=false;
  int count=0;float maxSword=0;
  for(int i=0;i<p.clips.Length;i++)
  {
   p.SelectClip(i);p.timeline.value=.5f;p.SendMessage("Update");
   foreach(var t in p.model.GetComponentsInChildren<Transform>())
   {var v=t.position;if(float.IsNaN(v.x)||float.IsInfinity(v.x)||v.magnitude>100)throw new Exception("Invalid pose: "+p.clips[i].name);}
   maxSword=Mathf.Max(maxSword,p.swordOffset.GetComponentInChildren<Renderer>().bounds.size.magnitude);count++;
  }
  p.attack.onClick.Invoke();if(p.clips[p.clipIndex].name!="great sword slash")throw new Exception("Attack selector failed");
  p.run.onClick.Invoke();if(p.clips[p.clipIndex].name!="great sword run")throw new Exception("Run selector failed");
  p.rotationX.value=25;p.rotationY.value=-15;p.rotationZ.value=10;
  if(Quaternion.Angle(p.swordOffset.localRotation,Quaternion.Euler(25,-15,10))>.01f)throw new Exception("Rotation controls failed");
  p.reset.onClick.Invoke();if(Quaternion.Angle(p.swordOffset.localRotation,Quaternion.identity)>.01f)throw new Exception("Rotation reset failed");
  p.save.onClick.Invoke();p.idle.onClick.Invoke();p.playing=true;p.SendMessage("Update");
  int events=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length;
  if(events!=1)throw new Exception("EventSystem count: "+events);
  p.GetComponentInParent<Transform>().gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.UI.CanvasScaler>()).ToList().ForEach(c=>c.matchWidthOrHeight=1);
  return new{motionsSampled=count,controlsVerified=true,eventSystems=events,frame=Time.frameCount,maxSwordBounds=maxSword,referencePoseExcluded=!p.clips.Any(c=>c.name.Contains("Maria"))};
 }
}
