using UnityEngine;
using UnityEngine.EventSystems;

namespace Rpg.Player
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.12f;
        private RectTransform area;
        private int pointerId = int.MinValue;
        public Vector2 Value { get; private set; }

        public void Configure(RectTransform knob) => handle = knob;
        private void Awake() => area = (RectTransform)transform;
        public void OnPointerDown(PointerEventData data)
        {
            if (pointerId != int.MinValue) return;
            pointerId = data.pointerId;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (pointerId != data.pointerId || !handle) return;
            if (!area) area = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, data.position, data.pressEventCamera, out var point)) return;
            float radius = Mathf.Max(1f, (Mathf.Min(area.rect.width, area.rect.height) - handle.rect.width) * 0.5f);
            Vector2 raw = Vector2.ClampMagnitude(point / radius, 1f);
            handle.anchoredPosition = raw * radius;
            Value = raw.magnitude <= deadZone ? Vector2.zero : raw.normalized * Mathf.InverseLerp(deadZone, 1f, raw.magnitude);
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (pointerId == data.pointerId) ResetInput();
        }
        private void OnDisable() => ResetInput();
        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        private void ResetInput()
        {
            pointerId = int.MinValue;
            Value = Vector2.zero;
            if (handle) handle.anchoredPosition = Vector2.zero;
        }
    }
}
