using System;
using UnityEngine;

namespace Puzzle
{
    [CreateAssetMenu(menuName = "Puzzle/Result Channel")]
    public class PuzzleResultChannelSO : ScriptableObject
    {
        public event Action<PuzzleResult> OnRaised;

        public void Raise(PuzzleResult result) => OnRaised?.Invoke(result);
    }
}
