using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Rpg.Player;

public static class VerifyQuarterView
{
    public static async Task<object> Main()
    {
        var player = GameObject.Find("Player");
        var joystick = UnityEngine.Object.FindFirstObjectByType<VirtualJoystick>();
        var animator = player.GetComponentInChildren<Animator>();
        var controller = player.GetComponent<CharacterController>();
        var rect = (RectTransform)joystick.transform;
        var handle = (RectTransform)rect.Find("Handle");
        var start = player.transform.position;
        int frame = Time.frameCount;
        Vector3 hipStart = animator.transform.Find("Armature/Hips").localPosition;
        var center = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        float scale = joystick.GetComponentInParent<Canvas>().scaleFactor;
        float radius = (rect.rect.width - handle.rect.width) * 0.5f * scale;
        var pointer = new PointerEventData(EventSystem.current) { pointerId = 42, position = center + Vector2.up * radius * 0.52f };
        joystick.OnPointerDown(pointer);
        await Task.Delay(700);
        float walkSpeed = animator.GetFloat("Speed");
        float walkDistance = Vector3.Distance(start, player.transform.position);
        pointer.position = center + Vector2.up * radius;
        joystick.OnDrag(pointer);
        await Task.Delay(700);
        float runSpeed = animator.GetFloat("Speed");
        var moved = player.transform.position - start;
        var clips = animator.GetCurrentAnimatorClipInfo(0).Select(c => new { c.clip.name, c.weight }).ToArray();
        float hipChange = Vector3.Distance(hipStart, animator.transform.Find("Armature/Hips").localPosition);
        joystick.OnPointerUp(pointer);
        await Task.Delay(1000);
        var stopped = player.transform.position;
        await Task.Delay(250);
        float drift = Vector3.Distance(stopped, player.transform.position);
        float stoppedSpeed = animator.GetFloat("Speed");
        var expected = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        float directionMatch = Vector3.Dot(Vector3.ProjectOnPlane(moved, Vector3.up).normalized, expected);
        // Hip offsets are in the imported rig's local units (world scale is about 196).
        // In-place clips retain small gait bobbing rather than metres of forward travel.
        bool passed = Time.frameCount > frame && walkDistance > 0.1f && walkSpeed > 0.1f && walkSpeed < 0.75f && runSpeed > 0.85f && moved.magnitude > 1f && directionMatch > 0.98f && drift < 0.02f && stoppedSpeed < 0.02f && controller.isGrounded && hipChange > 0.00001f;
        if (!passed) Debug.LogError("Quarter view verification failed. Inspect returned measurements.");
        return new { passed, framesAdvanced = Time.frameCount - frame, walkDistance, walkSpeed, runSpeed, moved = moved.ToString(), directionMatch, drift, stoppedSpeed, grounded = controller.isGrounded, hipChange, runClips = clips, joystickReset = joystick.Value == Vector2.zero, eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length };
    }
}
