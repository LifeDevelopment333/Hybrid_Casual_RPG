using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using Rpg.Player;

public static class VerifyAutoHunt
{
    public static async Task<object> Main()
    {
        if (!Application.isPlaying) throw new Exception("Enter Play mode first.");
        Application.runInBackground = true; EditorApplication.isPaused = false;
        var player = GameObject.Find("Player"); var hunt = player.GetComponent<PlayerAutoHunt>();
        var animator = player.GetComponentInChildren<Animator>();
        var spawner = UnityEngine.Object.FindFirstObjectByType<MonsterSpawn>();
        var corpses = new Dictionary<Monster, float>();
        int maxAlive = 0, attackFrames = 0, walkFrames = 0, deadObserved = 0, cleanedUp = 0;
        bool registryValid = true, manualOverride = false, resumed = false;
        float minNewSpawnRadius = float.MaxValue;
        var seen = new HashSet<Monster>();
        int firstFrame = Time.frameCount;
        for (int i = 0; i < 100; i++)
        {
            await Task.Delay(200);
            maxAlive = Mathf.Max(maxAlive, Monster.Active.Count);
            foreach (var m in Monster.Active)
            {
                registryValid &= m && m.IsAlive;
                if (seen.Add(m) && m.name.StartsWith("Skeleton_"))
                    minNewSpawnRadius = Mathf.Min(minNewSpawnRadius, Vector3.ProjectOnPlane(m.transform.position - player.transform.position, Vector3.up).magnitude);
            }
            foreach (var m in UnityEngine.Object.FindObjectsByType<Monster>(FindObjectsSortMode.None))
                if (!m.IsAlive && !corpses.ContainsKey(m)) { corpses.Add(m, Time.time); deadObserved++; }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Base Layer.AutoAttack")) attackFrames++;
            if (animator.GetFloat("Speed") > 0.1f) walkFrames++;
            if (!manualOverride && hunt.IsAttacking)
            {
                var joystick = UnityEngine.Object.FindFirstObjectByType<VirtualJoystick>();
                var rect = (RectTransform)joystick.transform; var handle = (RectTransform)rect.Find("Handle");
                float radius = (rect.rect.width - handle.rect.width) * 0.5f * joystick.GetComponentInParent<Canvas>().scaleFactor;
                var center = RectTransformUtility.WorldToScreenPoint(null, rect.position);
                var data = new PointerEventData(EventSystem.current) { pointerId = 91, position = center + Vector2.right * radius };
                Vector3 before = player.transform.position;
                int hitsBefore = hunt.SuccessfulHits;
                joystick.OnPointerDown(data); await Task.Delay(450);
                manualOverride = !hunt.IsAttacking && Vector3.Dot(player.transform.position - before, Camera.main.transform.right) > 0.2f && hitsBefore == hunt.SuccessfulHits;
                joystick.OnPointerUp(data);
            }
            if (manualOverride && hunt.SuccessfulHits > 0) resumed = true;
        }
        foreach (var entry in corpses) if (!entry.Key) cleanedUp++;
        bool passed = Time.frameCount > firstFrame && spawner.SpawnedCount > 5 && maxAlive <= spawner.MaxAlive && Monster.TotalKills >= 3 && attackFrames > 0 && walkFrames > 0 && manualOverride && resumed && registryValid && cleanedUp > 0;
        return new { passed, framesAdvanced = Time.frameCount - firstFrame, spawner.SpawnedCount, alive = Monster.Active.Count, maxAlive, limit = spawner.MaxAlive, kills = Monster.TotalKills, damageEvents = Monster.TotalDamageEvents, hunt.AttackCount, hunt.SuccessfulHits, attackFrames, walkFrames, manualOverride, resumed, registryValid, deadObserved, cleanedUp, minNewSpawnRadius, playerPosition = player.transform.position.ToString() };
    }
}
