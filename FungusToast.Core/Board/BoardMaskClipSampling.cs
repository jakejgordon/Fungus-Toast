namespace FungusToast.Core.Board
{
    public static class BoardMaskClipSampling
    {
        public static float[] BuildClipBudgetSampleOffsets(float playableSurfaceTileScale, float maximumTileClipFraction, int sampleResolution)
        {
            float clampedScale = playableSurfaceTileScale < 0f ? 0f : playableSurfaceTileScale;
            float clampedMaximumClipFraction = maximumTileClipFraction < 0f
                ? 0f
                : (maximumTileClipFraction > 0.49f ? 0.49f : maximumTileClipFraction);
            float halfTileSpan = clampedScale * 0.5f;
            float requiredInsetFromCenter = halfTileSpan - clampedMaximumClipFraction;
            if (requiredInsetFromCenter <= 0.0001f)
            {
                return System.Array.Empty<float>();
            }

            if (requiredInsetFromCenter > 0.5f)
            {
                requiredInsetFromCenter = 0.5f;
            }

            int clampedResolution = sampleResolution < 2 ? 2 : (sampleResolution > 7 ? 7 : sampleResolution);
            var sampleOffsets = new float[clampedResolution];
            if (clampedResolution == 2)
            {
                sampleOffsets[0] = -requiredInsetFromCenter;
                sampleOffsets[1] = requiredInsetFromCenter;
                return sampleOffsets;
            }

            float start = -requiredInsetFromCenter;
            float span = requiredInsetFromCenter * 2f;
            for (int i = 0; i < clampedResolution; i++)
            {
                sampleOffsets[i] = start + (span * (i / (float)(clampedResolution - 1)));
            }

            return sampleOffsets;
        }
    }
}
