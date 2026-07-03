using UnityEngine;

namespace Puzzle
{
    public class ChestInteractable : MonoBehaviour
    {
        [SerializeField] PuzzleManager puzzleManager;
        [SerializeField] PuzzleController gearPuzzlePrefab;
        [SerializeField] PuzzleResultChannelSO resultChannel;

        private bool _puzzleOpen;

        public void OnInteract()
        {
            if (_puzzleOpen) return;
            _puzzleOpen = true;
            resultChannel.OnRaised += HandleResult;
            puzzleManager.Open(gearPuzzlePrefab);
        }

        private void HandleResult(PuzzleResult result)
        {
            _puzzleOpen = false;
            resultChannel.OnRaised -= HandleResult;
            if (result == PuzzleResult.Success)
                gameObject.SetActive(false);
        }
    }
}
