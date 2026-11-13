using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Rpg.Player;

public static class SetupQuarterView
{
    const string Folder = "Assets/RPG/ART/Player/Locomotion";
    static T Ensure<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component ? component : Undo.AddComponent<T>(go);
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create joystick UI");
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        return rect;
    }
    static AnimationClip Clip(string name)
    {
        string sourcePath = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/RPG/ART/Player/Animations" })
            .Select(AssetDatabase.GUIDToAssetPath).SingleOrDefault(p => Path.GetFileNameWithoutExtension(p) == name);
        var source = string.IsNullOrEmpty(sourcePath) ? null : AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath);
        if (!source) throw new Exception("Missing animation: " + name);
        string path = Folder + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (!clip) { clip = UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(clip, path); }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        // The imported Armature rotates local Y into world forward and local Z into
        // world up. Remove cumulative travel, preserving lateral sway and vertical bob.
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.type != typeof(Transform)) continue;
            bool root = binding.path == "" || binding.path == "Armature" || binding.path == "Armature/Hips";
            if (root && (binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "m_LocalPosition.y"))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                float travel = curve.Evaluate(clip.length) - curve.Evaluate(0f);
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i].value -= travel * (keys[i].time / clip.length);
                    keys[i].inTangent -= travel / clip.length;
                    keys[i].outTangent -= travel / clip.length;
                }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }
        EditorUtility.SetDirty(clip);
        return clip;
    }
    public static object Main()
    {
        if (Application.isPlaying) throw new Exception("Exit Play mode first.");
        var player = GameObject.Find("Player");
        if (!player) throw new Exception("Current scene has no Player.");
        var animator = player.GetComponentInChildren<Animator>();
        var camera = Camera.main;
        if (!animator || !camera) throw new Exception("Player Animator or Main Camera missing.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var idle = Clip("great sword idle");
        var walk = Clip("great sword walk");
        var run = Clip("great sword run");
        string controllerPath = Folder + "/QuarterViewLocomotion.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var state = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f); tree.AddChild(walk, 0.45f); tree.AddChild(run, 1f);
            controller.layers[0].stateMachine.defaultState = state;
        }
        Undo.RecordObject(animator, "Assign locomotion");
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        var capsule = Ensure<CharacterController>(player);
        capsule.height = 1.8f; capsule.radius = 0.3f; capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.stepOffset = 0.3f; capsule.slopeLimit = 45f; capsule.skinWidth = 0.025f; capsule.minMoveDistance = 0f;

        var canvasObject = GameObject.Find("PlayerControls");
        if (!canvasObject)
        {
            canvasObject = new GameObject("PlayerControls", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create player controls");
        }
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f); scaler.matchWidthOrHeight = 0.5f;
        var joystick = canvasObject.GetComponentInChildren<VirtualJoystick>();
        if (!joystick)
        {
            // Built-in circular sprite remains an editor-managed dependency.
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var background = Rect("Joystick", canvasObject.transform, new Vector2(230f, 230f));
            background.anchorMin = background.anchorMax = Vector2.zero;
            background.pivot = new Vector2(0.5f, 0.5f);
            background.anchoredPosition = new Vector2(185f, 230f);
            var image = background.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite; image.color = new Color(0.08f, 0.13f, 0.18f, 0.55f); image.raycastTarget = true;
            var knob = Rect("Handle", background, new Vector2(88f, 88f));
            knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f); knob.anchoredPosition = Vector2.zero;
            var knobImage = knob.gameObject.AddComponent<UnityEngine.UI.Image>();
            knobImage.sprite = sprite; knobImage.color = new Color(0.78f, 0.91f, 1f, 0.92f); knobImage.raycastTarget = false;
            joystick = background.gameObject.AddComponent<VirtualJoystick>(); joystick.Configure(knob);
            EditorUtility.SetDirty(joystick);
        }
        var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length == 0)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create UI input");
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        var movement = Ensure<QuarterViewPlayer>(player);
        movement.Configure(joystick, camera.transform, animator); EditorUtility.SetDirty(movement);
        var follow = Ensure<QuarterViewCamera>(camera.gameObject);
        follow.Configure(player.transform); EditorUtility.SetDirty(follow);
        Undo.RecordObject(camera, "Quarter view camera");
        camera.orthographic = true; camera.orthographicSize = 6f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 300f;
        EditorUtility.SetDirty(camera.transform);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(player.scene); EditorSceneManager.SaveScene(player.scene);
        return new { scene = player.scene.path, player = player.name, controller = controllerPath, clips = controller.animationClips.Select(c => c.name).ToArray(), joystick = joystick.name, eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length };
    }
}
