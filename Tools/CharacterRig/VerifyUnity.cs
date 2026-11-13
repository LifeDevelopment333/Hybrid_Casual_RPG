using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class VerifyTripoAdventurer
{
    public static async Task<object> Run()
    {
        int frame=Time.frameCount;
        var animators=UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).OrderBy(a=>a.name).ToArray();
        var times=animators.Select(a=>a.GetCurrentAnimatorStateInfo(0).normalizedTime).ToArray();
        var legs=animators.Select(a=>a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).rotation).ToArray();
        await Task.Delay(650);
        var checks=animators.Select((a,i)=>new{a.name,valid=a.avatar.isValid,human=a.isHuman,clip=a.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name).ToArray(),before=times[i],after=a.GetCurrentAnimatorStateInfo(0).normalizedTime,legRotationDelta=Quaternion.Angle(legs[i],a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).rotation),triangles=(int)a.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.GetIndexCount(0)/3}).ToArray();
        return new{frameBefore=frame,frameAfter=Time.frameCount,framesAdvanced=Time.frameCount-frame,allAnimationsAdvancing=checks.All(c=>c.after>c.before),allAvatarsValid=checks.All(c=>c.valid&&c.human),checks};
    }
}
