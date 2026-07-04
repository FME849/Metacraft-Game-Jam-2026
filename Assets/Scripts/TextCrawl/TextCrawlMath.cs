namespace Metacraft.TextCrawl
{
    public static class TextCrawlMath
    {
        public static float ComputeScrollDistance(float viewportHeight, float contentHeight)
        {
            return viewportHeight + contentHeight;
        }
    }
}
