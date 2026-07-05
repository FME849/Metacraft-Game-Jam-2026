using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Puzzle.ClockPuzzle
{
    public class ClockPuzzleController : PuzzleController
    {
        [SerializeField] private ClockHandDragger handDragger;
        [SerializeField] private Image lightIndicator;
        [SerializeField] private Color lightOffColor = Color.gray;
        [SerializeField] private Color lightOnColor = Color.red;
        [SerializeField] private int targetHour = 1;
        [SerializeField] private int targetMinute = 20;

        private void Awake()
        {
            if (lightIndicator != null)
            {
                lightIndicator.color = lightOffColor;
            }
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Complete(PuzzleResult.Cancelled);
        }

        public void OnConfirmPressed()
        {
            var (hour, minute) = handDragger.GetCurrentTime();
            if (hour == targetHour && minute == targetMinute)
                OnPuzzleSolved();
            else
                Debug.Log($"Wrong time! Current: {hour}:{minute}, Target: {targetHour}:{targetMinute}");
        }

        public void OnPuzzleSolved()
        {
            Debug.Log($"Puzzle solved! Current: {targetHour}:{targetMinute}");
            if (lightIndicator != null)
            {
                lightIndicator.color = lightOnColor;
            }

            Complete(PuzzleResult.Success);
        }

        public void OnPuzzleFailed() => Complete(PuzzleResult.Fail);
    }
}
