using UnityEngine;

namespace Puzzle
{
    public abstract class PuzzleController : MonoBehaviour
    {
        [SerializeField] protected PuzzleResultChannelSO resultChannel;

        protected void Complete(PuzzleResult result)
        {
            if (resultChannel == null)
            {
                Debug.LogWarning($"{name} completed with {result}, but no Puzzle Result Channel is assigned.", this);
                return;
            }

            resultChannel.Raise(result);
        }

        public virtual void OnPuzzleOpened() { }
    }
}
