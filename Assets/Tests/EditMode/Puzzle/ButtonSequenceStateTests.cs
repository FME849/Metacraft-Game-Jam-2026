using NUnit.Framework;
using Puzzle.ButtonSequencePuzzle;

namespace Tests.EditMode.Puzzle
{
    public class ButtonSequenceStateTests
    {
        [Test]
        public void Press_CorrectFirstIndex_ReturnsTrueAndAdvancesProgress()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            bool result = state.Press(1);

            Assert.IsTrue(result);
            Assert.AreEqual(1, state.Progress);
            Assert.IsFalse(state.IsSolved);
        }

        [Test]
        public void Press_FullCorrectSequence_IsSolvedBecomesTrue()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            state.Press(1);
            state.Press(2);
            bool lastResult = state.Press(0);

            Assert.IsTrue(lastResult);
            Assert.IsTrue(state.IsSolved);
        }

        [Test]
        public void Press_WrongIndex_ReturnsFalseAndResetsProgress()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            bool result = state.Press(0);

            Assert.IsFalse(result);
            Assert.AreEqual(0, state.Progress);
        }

        [Test]
        public void Press_WrongIndexAfterPartialProgress_ResetsToZero()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            state.Press(1);
            bool result = state.Press(0);

            Assert.IsFalse(result);
            Assert.AreEqual(0, state.Progress);
        }

        [Test]
        public void Press_AfterReset_CanRetryFromStart()
        {
            var state = new ButtonSequenceState(new[] { 1, 2, 0 });

            state.Press(1);
            state.Press(0);
            bool retryResult = state.Press(1);

            Assert.IsTrue(retryResult);
            Assert.AreEqual(1, state.Progress);
        }
    }
}
