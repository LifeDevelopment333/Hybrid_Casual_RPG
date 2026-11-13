using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FixMotionDrift
{
    public static object Main()
    {
        if (Application.isPlaying) throw new Exception("Stop Play mode before editing clips.");
        var animator = GameObject.Find("Player").GetComponentInChildren<Animator>();
        var hips = animator.transform.Find("Armature/Hips");
        if (!hips) throw new Exception("Expected player rig was not found.");
        var clips = animator.runtimeAnimatorController.animationClips.Distinct()
            .Where(c => c.name.Contains("walk") || c.name.Contains("run") || c.name == "great sword idle").ToArray();
        var results = clips.Select(clip =>
        {
            string path = AssetDatabase.GetAssetPath(clip);
            if (!path.EndsWith(".anim")) throw new Exception("Expected editable animation: " + path);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var axes = new[] { "x", "y", "z" };
            var curves = axes.Select(axis => AnimationUtility.GetEditorCurve(clip,
                bindings.Single(b => b.path == "Armature/Hips" && b.propertyName == "m_LocalPosition." + axis))).ToArray();
            Vector3 drift = new Vector3(curves[0].Evaluate(clip.length) - curves[0].Evaluate(0),
                curves[1].Evaluate(clip.length) - curves[1].Evaluate(0), curves[2].Evaluate(clip.length) - curves[2].Evaluate(0));
            Vector3 worldDrift = hips.parent.TransformVector(drift);
            // The imported Armature is rotated -90 degrees about X. Its local Y is
            // world forward, while local Z is world up. Never flatten local Z bobbing.
            Vector3 horizontalDrift = hips.parent.InverseTransformVector(Vector3.ProjectOnPlane(worldDrift, Vector3.up));
            string backup = "Tools/QuarterView/AnimationBackups/" + AssetDatabase.AssetPathToGUID(path) + ".anim.original";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(path, backup);
            Undo.RecordObject(clip, "Remove locomotion root travel");
            for (int axis = 0; axis < 3; axis++)
            {
                float travel = horizontalDrift[axis];
                if (Mathf.Abs(travel) < 0.0000001f) continue;
                var curve = curves[axis]; var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i].value -= travel * (keys[i].time / clip.length);
                    keys[i].inTangent -= travel / clip.length;
                    keys[i].outTangent -= travel / clip.length;
                }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, bindings.Single(b => b.path == "Armature/Hips" && b.propertyName == "m_LocalPosition." + axes[axis]), curve);
            }
            clip.EnsureQuaternionContinuity(); EditorUtility.SetDirty(clip);
            return new { clip.name, path, removedWorldTravel = Vector3.ProjectOnPlane(worldDrift, Vector3.up).magnitude };
        }).ToArray();
        AssetDatabase.SaveAssets();
        return results;
    }
}
