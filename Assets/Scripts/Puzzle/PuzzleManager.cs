using UnityEngine;

namespace Puzzle
{
    public class PuzzleManager : MonoBehaviour
    {
        [SerializeField] PuzzleResultChannelSO resultChannel;

        private PuzzleController _activePuzzle;

        public void Open(PuzzleController prefab)
        {
            _activePuzzle = Instantiate(prefab);
            Time.timeScale = 0f;
            _activePuzzle.OnPuzzleOpened();
            resultChannel.OnRaised += OnPuzzleCompleted;
        }

        private void OnPuzzleCompleted(PuzzleResult result)
        {
            resultChannel.OnRaised -= OnPuzzleCompleted;
            Destroy(_activePuzzle.gameObject);
            Time.timeScale = 1f;
            _activePuzzle = null;
        }
    }
}
