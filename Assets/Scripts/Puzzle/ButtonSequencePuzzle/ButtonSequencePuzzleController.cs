using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Puzzle.ButtonSequencePuzzle
{
    public class ButtonSequencePuzzleController : PuzzleController
    {
        [SerializeField] private Button[] buttons;
        [SerializeField] private SequenceIndicator[] indicators;
        [SerializeField] private int[] correctSequence = { 1, 2, 0 };

        private ButtonSequenceState _state;

        private void Awake()
        {
            _state = new ButtonSequenceState(correctSequence);
            ResetIndicators();

            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i].onClick.AddListener(() => OnButtonPressed(index));
            }
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Complete(PuzzleResult.Cancelled);
        }

        private void OnButtonPressed(int index)
        {
            bool correct = _state.Press(index);
            if (correct)
            {
                indicators[index].TurnOn();
                if (_state.IsSolved)
                    Complete(PuzzleResult.Success);
            }
            else
            {
                ResetIndicators();
            }
        }

        private void ResetIndicators()
        {
            foreach (var indicator in indicators)
                indicator.TurnOff();
        }
    }
}
