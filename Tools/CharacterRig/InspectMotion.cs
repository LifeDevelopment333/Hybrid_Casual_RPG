using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Threading.Tasks;

public static class InspectAdventurerMotion
{
    public static async Task<object> Capture()
    {
        string dir=Path.GetFullPath("Art/Characters/TripoAdventurer/AnimationFix");Directory.CreateDirectory(dir);
        var actors=UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).OrderBy(a=>a.name).ToArray();
        var cam=Camera.main;var previous=cam.targetTexture;var rt=new RenderTexture(1000,600,24);cam.targetTexture=rt;
        var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        var previousActive=RenderTexture.active;
        var samples=new System.Collections.Generic.List<object>();
        var speeds=actors.Select(a=>a.speed).ToArray();foreach(var a in actors)a.speed=0;
        try
        {
            for(int i=0;i<48;i++)
            {
                float elapsed=i/48f*4f;
                foreach(var animator in actors)
                {
                    string state=animator.name.Replace("Adventurer_","");
                    float phase=elapsed/animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;
                    animator.Play(state,0,phase);animator.Update(0);
                }
                // Skinning is cached per render frame; let the player loop update it.
                await Task.Delay(45);
                foreach(var animator in actors)
                {
                    var foot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var toe=animator.GetBoneTransform(HumanBodyBones.LeftToes);
                    var knee=animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                    float phase=animator.GetCurrentAnimatorStateInfo(0).normalizedTime%1;
                    var skin=animator.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked);
                    float soleY=baked.vertices.Select(v=>skin.transform.TransformPoint(v)).Where(v=>Mathf.Abs(v.x-foot.position.x)<.09f&&v.y<.3f).Select(v=>v.y).DefaultIfEmpty(float.NaN).Min();
                    UnityEngine.Object.DestroyImmediate(baked);
                    samples.Add(new{actor=animator.name,elapsed,phase,ankleY=foot.position.y,toeY=toe.position.y,soleY,ankleZ=foot.position.z,kneeZ=knee.position.z});
                }
                cam.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(dir,$"Unity_Motion_{i:00}.png"),texture.EncodeToPNG());
            }
        }
        finally{for(int i=0;i<actors.Length;i++)actors[i].speed=speeds[i];cam.targetTexture=previous;RenderTexture.active=previousActive;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        File.WriteAllText(Path.Combine(dir,"Unity_Motion_Samples.json"),JsonConvert.SerializeObject(samples,Formatting.Indented));
        return new{dir,frames=48,seconds=4,samples=samples.Count};
    }
}
