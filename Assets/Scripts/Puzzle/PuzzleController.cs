using UnityEngine;

namespace Puzzle
{
    public abstract class PuzzleController : MonoBehaviour
    {
        [SerializeField] protected PuzzleResultChannelSO resultChannel;

        protected void Complete(PuzzleResult result) => resultChannel.Raise(result);

        public virtual void OnPuzzleOpened() { }
    }
}
