using NUnit.Framework;
using Metacraft.TextCrawl;

namespace Tests.EditMode.TextCrawl
{
    public class TextCrawlMathTests
    {
        [Test]
        public void ComputeScrollDistance_ReturnsSumOfViewportAndContentHeight()
        {
            float distance = TextCrawlMath.ComputeScrollDistance(1080f, 600f);

            Assert.AreEqual(1680f, distance);
        }

        [Test]
        public void ComputeScrollDistance_ZeroContentHeight_ReturnsViewportHeight()
        {
            float distance = TextCrawlMath.ComputeScrollDistance(1080f, 0f);

            Assert.AreEqual(1080f, distance);
        }

        [Test]
        public void ComputeScrollDistance_ZeroViewportHeight_ReturnsContentHeight()
        {
            float distance = TextCrawlMath.ComputeScrollDistance(0f, 600f);

            Assert.AreEqual(600f, distance);
        }
    }
}
