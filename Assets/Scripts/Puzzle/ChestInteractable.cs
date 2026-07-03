using UnityEngine;

namespace Puzzle
{
    public class ChestInteractable : MonoBehaviour
    {
        [SerializeField] PuzzleManager puzzleManager;
        [SerializeField] PuzzleController gearPuzzlePrefab;
        [SerializeField] PuzzleResultChannelSO resultChannel;

        public void OnInteract()
        {
            resultChannel.OnRaised += HandleResult;
            puzzleManager.Open(gearPuzzlePrefab);
        }

        private void HandleResult(PuzzleResult result)
        {
            resultChannel.OnRaised -= HandleResult;
            if (result == PuzzleResult.Success)
                gameObject.SetActive(false);
        }
    }
}
