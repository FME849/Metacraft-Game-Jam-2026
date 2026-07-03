using NUnit.Framework;
using UnityEngine;
using Puzzle;

namespace Tests.EditMode.Puzzle
{
    public class PuzzleResultChannelTests
    {
        [Test]
        public void Raise_FiresOnRaisedWithCorrectResult()
        {
            var channel = ScriptableObject.CreateInstance<PuzzleResultChannelSO>();
            PuzzleResult received = default;
            channel.OnRaised += r => received = r;

            channel.Raise(PuzzleResult.Success);

            Assert.AreEqual(PuzzleResult.Success, received);
        }

        [Test]
        public void Raise_NoSubscribers_DoesNotThrow()
        {
            var channel = ScriptableObject.CreateInstance<PuzzleResultChannelSO>();
            Assert.DoesNotThrow(() => channel.Raise(PuzzleResult.Cancelled));
        }

        [Test]
        public void OnRaised_AfterUnsubscribe_DoesNotFire()
        {
            var channel = ScriptableObject.CreateInstance<PuzzleResultChannelSO>();
            int callCount = 0;
            System.Action<PuzzleResult> handler = _ => callCount++;

            channel.OnRaised += handler;
            channel.OnRaised -= handler;
            channel.Raise(PuzzleResult.Success);

            Assert.AreEqual(0, callCount);
        }
    }
}
