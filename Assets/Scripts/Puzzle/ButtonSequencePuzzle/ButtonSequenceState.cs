namespace Puzzle.ButtonSequencePuzzle
{
    public class ButtonSequenceState
    {
        private readonly int[] _correctSequence;

        public int Progress { get; private set; }
        public bool IsSolved => Progress == _correctSequence.Length;

        public ButtonSequenceState(int[] correctSequence)
        {
            _correctSequence = correctSequence;
        }

        public bool Press(int buttonIndex)
        {
            if (buttonIndex == _correctSequence[Progress])
            {
                Progress++;
                return true;
            }

            Progress = 0;
            return false;
        }
    }
}
