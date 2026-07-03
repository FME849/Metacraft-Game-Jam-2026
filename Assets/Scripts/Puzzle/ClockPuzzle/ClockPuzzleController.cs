using UnityEngine;
using UnityEngine.InputSystem;

namespace Puzzle.ClockPuzzle
{
    public class ClockPuzzleController : PuzzleController
    {
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Complete(PuzzleResult.Cancelled);
        }

        public void OnPuzzleSolved() => Complete(PuzzleResult.Success);

        public void OnPuzzleFailed() => Complete(PuzzleResult.Fail);
    }
}
