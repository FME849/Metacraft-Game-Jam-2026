using UnityEngine;
using UnityEngine.InputSystem;

namespace Puzzle.GearPuzzle
{
    public class GearPuzzleController : PuzzleController
    {
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Complete(PuzzleResult.Cancelled);
        }

        // Call this when gear puzzle logic determines the puzzle is solved
        public void OnPuzzleSolved() => Complete(PuzzleResult.Success);

        // Call this when puzzle reaches a fail condition (e.g. time up)
        public void OnPuzzleFailed() => Complete(PuzzleResult.Fail);
    }
}
