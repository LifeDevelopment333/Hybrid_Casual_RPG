using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rpg.Player;

public static class SetupAutoHunt
{
    const string Root = "Assets/RPG/ART/Monsters/Skeleton";
    const string Source = "Assets/polyperfect/Low Poly Animated Fantasy Creatures/_Prefabs/Creatures/Skeleton_Basic.prefab";
    static T Ensure<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c ? c : Undo.AddComponent<T>(go); }
    static void ConfigureMonster(GameObject go, AnimatorController controller)
    {
        var animator = go.GetComponentInChildren<Animator>();
        Undo.RecordObject(animator, "Skeleton animation");
        animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var capsule = Ensure<CharacterController>(go);
        capsule.height = 1.75f; capsule.radius = 0.3f; capsule.center = new Vector3(0f, 0.875f, 0f);
        capsule.stepOffset = 0.25f; capsule.skinWidth = 0.025f; capsule.minMoveDistance = 0f;
        var monster = Ensure<Monster>(go); monster.Configure(animator); EditorUtility.SetDirty(monster);
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
    }
    public static object Main()
    {
        if (Application.isPlaying) throw new Exception("Exit Play mode first.");
        var player = GameObject.Find("Player"); var ground = GameObject.Find("Ground").GetComponent<Collider>();
        var animator = player.GetComponentInChildren<Animator>();
        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (!controller || !controller.layers[0].stateMachine.states.Any(s => s.state.name == "Locomotion"))
            throw new Exception("Expected player's existing Locomotion controller.");
        Directory.CreateDirectory(Root + "/Animations"); Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh();
        string attackPath = "Assets/RPG/ART/Player/Animations/Attack/AutoHuntSlash.anim";
        var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(attackPath);
        if (!attackClip)
        {
            var original = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/RPG/ART/Player/Animations/Slash/great sword slash.anim");
            attackClip = UnityEngine.Object.Instantiate(original); attackClip.name = "AutoHuntSlash";
            AssetDatabase.CreateAsset(attackClip, attackPath);
            var settings = AnimationUtility.GetAnimationClipSettings(attackClip); settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(attackClip, settings);
            // Match in-place movement: this rig's local Y points horizontally in world space.
            var bindings = AnimationUtility.GetCurveBindings(attackClip);
            foreach (var binding in bindings.Where(b => b.path == "Armature/Hips" && (b.propertyName == "m_LocalPosition.x" || b.propertyName == "m_LocalPosition.y")))
            {
                var curve = AnimationUtility.GetEditorCurve(attackClip, binding);
                float travel = curve.Evaluate(attackClip.length) - curve.Evaluate(0f);
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                { keys[i].value -= travel * keys[i].time / attackClip.length; keys[i].inTangent -= travel / attackClip.length; keys[i].outTangent -= travel / attackClip.length; }
                curve.keys = keys; AnimationUtility.SetEditorCurve(attackClip, binding, curve);
            }
            EditorUtility.SetDirty(attackClip);
        }
        var machine = controller.layers[0].stateMachine;
        var attack = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "AutoAttack");
        if (!attack) attack = machine.AddState("AutoAttack", new Vector3(450f, 120f));
        attack.motion = attackClip; attack.speed = 1.3f; attack.writeDefaultValues = true;
        var hunting = Ensure<PlayerAutoHunt>(player); hunting.Configure(animator, attackClip.length / attack.speed); EditorUtility.SetDirty(hunting);
        EditorUtility.SetDirty(controller);

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        var skeletonClips = source.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips;
        AnimationClip Find(string suffix) => skeletonClips.First(c => c.name == "Skeleton_Basic_" + suffix);
        string skeletonControllerPath = Root + "/SkeletonAutoHunt.controller";
        var skeletonController = AssetDatabase.LoadAssetAtPath<AnimatorController>(skeletonControllerPath);
        if (!skeletonController)
        {
            skeletonController = AnimatorController.CreateAnimatorControllerAtPath(skeletonControllerPath);
            skeletonController.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var locomotion = skeletonController.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            tree.AddChild(Find("Idle"), 0f); tree.AddChild(Find("Walk"), 1f);
            var sm = skeletonController.layers[0].stateMachine; sm.defaultState = locomotion;
            var death = sm.AddState("Death", new Vector3(450, 140)); death.motion = Find("Death");
            var skeletonAttack = sm.AddState("Attack", new Vector3(450, 260)); skeletonAttack.motion = Find("Attack");
        }
        string prefabPath = Root + "/Prefabs/SkeletonMonster.prefab";
        var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (!monsterPrefab)
        {
            var wrapper = new GameObject("SkeletonMonster");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source); model.name = "Model";
            model.transform.SetParent(wrapper.transform, false); model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            ConfigureMonster(wrapper, skeletonController);
            monsterPrefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
            UnityEngine.Object.DestroyImmediate(wrapper);
        }
        // Connect the skeleton already placed in the scene as well as new spawns.
        var existing = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None)
            .Where(a => a != animator && a.gameObject.name == "Skeleton_Basic").ToArray();
        foreach (var skeleton in existing) ConfigureMonster(skeleton.gameObject, skeletonController);
        var spawnerObject = GameObject.Find("MonsterSpawner");
        if (!spawnerObject) { spawnerObject = new GameObject("MonsterSpawner"); Undo.RegisterCreatedObjectUndo(spawnerObject, "Create skeleton spawner"); }
        var spawner = Ensure<MonsterSpawn>(spawnerObject);
        spawner.Configure(monsterPrefab.GetComponent<Monster>(), player.transform, ground); EditorUtility.SetDirty(spawner);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(player.scene); EditorSceneManager.SaveScene(player.scene);
        return new { scene = player.scene.path, monsterPrefab = prefabPath, skeletonController = skeletonControllerPath, playerController = AssetDatabase.GetAssetPath(controller), attackDuration = attackClip.length / attack.speed, existingSkeletonsConnected = existing.Length };
    }
}
