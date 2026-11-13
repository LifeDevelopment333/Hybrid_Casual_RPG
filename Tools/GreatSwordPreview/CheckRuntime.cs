using System.Linq;
using UnityEngine;
using Rpg.GreatSwordPreview;
public static class CheckGreatSwordRuntime
{
 public static object Main()
 {
  var p=Object.FindFirstObjectByType<GreatSwordPreviewPlayer>();
  return new {clip=p.clips[p.clipIndex].name,frame=Time.frameCount,
   sword=p.swordOffset.GetComponentsInChildren<Transform>().Select(t=>new{t.name,position=t.position.ToString("F4"),local=t.localPosition.ToString("F4"),scale=t.lossyScale.ToString("F5"),localScale=t.localScale.ToString("F5"),rot=t.eulerAngles.ToString("F2")}).ToArray(),renderers=p.swordOffset.GetComponentsInChildren<Renderer>().Select(r=>new{r.name,bounds=r.bounds.ToString("F5"),r.enabled,materials=r.sharedMaterials.Select(m=>m?m.name:"NULL").ToArray()}).ToArray()};
 }
}
