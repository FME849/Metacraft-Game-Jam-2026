using UnityEngine;
using UnityEngine.EventSystems;

namespace Puzzle.ClockPuzzle
{
    public class ClockHandDragger : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private RectTransform clockFace;
        [SerializeField] private RectTransform minuteHand;
        [SerializeField] private RectTransform hourHand;
        [SerializeField] private float snapDegrees = 6f;
        [SerializeField] private float minDragRadius = 20f;
        [SerializeField] private int startHour = 12;
        [SerializeField] private int startMinute = 0;
        [Tooltip("Bù lệch giữa hướng mặc định (rotation = 0) của hình kim và hướng 12 giờ (lên trên). Để 0 nếu hình kim đã hướng lên sẵn ở rotation = 0.")]
        [SerializeField] private float handRestOffset = 0f;

        private float _lastPointerAngle;
        private float _totalMinuteAngle;

        private void Awake()
        {
            _totalMinuteAngle = startHour * 360f + startMinute * 6f;
            ApplyRotations();
        }

        public void OnPointerDown(PointerEventData eventData)
            => _lastPointerAngle = GetPointerAngle(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                clockFace, eventData.position, eventData.pressEventCamera, out Vector2 local);
            if (local.magnitude < minDragRadius)
                return;

            float pointerAngle = LocalPointToAngle(local);
            float delta = Mathf.DeltaAngle(_lastPointerAngle, pointerAngle);
            _lastPointerAngle = pointerAngle;

            _totalMinuteAngle += delta;

            ApplyRotations();
        }

        public (int hour, int minute) GetCurrentTime()
        {
            int minute = Mathf.RoundToInt(Wrap360(_totalMinuteAngle) / 6f) % 60;
            int hour = Mathf.FloorToInt(_totalMinuteAngle / 360f) % 12;
            if (hour < 0) hour += 12;
            return (hour, minute);
        }

        private void ApplyRotations()
        {
            float minuteAngle = Wrap360(_totalMinuteAngle);
            if (snapDegrees > 0f)
                minuteAngle = Mathf.Round(minuteAngle / snapDegrees) * snapDegrees % 360f;
            minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minuteAngle + handRestOffset);

            float hourAngle = Wrap360(_totalMinuteAngle / 12f);
            hourHand.localRotation = Quaternion.Euler(0f, 0f, -hourAngle + handRestOffset);
        }

        private float GetPointerAngle(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                clockFace, eventData.position, eventData.pressEventCamera, out Vector2 local);
            return LocalPointToAngle(local);
        }

        private static float LocalPointToAngle(Vector2 local)
        {
            float angle = Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg;
            return angle < 0f ? angle + 360f : angle;
        }

        private static float Wrap360(float a) => (a % 360f + 360f) % 360f;
    }
}
